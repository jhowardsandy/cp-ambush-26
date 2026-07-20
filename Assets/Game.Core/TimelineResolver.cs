#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public sealed class TimelineResolver
{
    public SimulationResult Resolve(SimulationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        request.Configuration.Validate();

        var diagnostics = Validate(request);
        if (diagnostics.Count > 0)
            return new SimulationResult(request.InitialState, Array.Empty<DomainEvent>(), diagnostics, StateChecksum.Calculate(request.InitialState));

        var scheduled = request.CommandBundles
            .SelectMany(bundle => bundle.Actions.Select(action => new ScheduledAction(bundle.FactionId, action)))
            .ToArray();
        var events = new List<DomainEvent>();
        var failedActions = new HashSet<Guid>();
        var state = request.InitialState;
        var sequence = 0;

        AddEvent(0, DomainEventType.RoundStarted, string.Empty);

        for (var tick = 0; tick <= request.Configuration.TicksPerRound; tick++)
        {
            foreach (var item in scheduled.Where(item => item.Action.StartTick == tick).OrderBy(item => item, ScheduledActionComparer.Instance))
            {
                var unit = state.FindUnit(item.Action.UnitId)!;
                if (unit.ActivityState != UnitActivityState.Active)
                {
                    failedActions.Add(item.Action.ActionId);
                    AddEvent(tick, DomainEventType.ActionFailed, item.FactionId, unit.Id, item.Action.ActionId, "Unit is not active.");
                    continue;
                }

                AddEvent(tick, DomainEventType.ActionStarted, item.FactionId, unit.Id, item.Action.ActionId);
            }

            ResolveMovementSteps(tick);

            foreach (var item in scheduled.Where(item => item.Action.StartTick + item.Action.DurationTicks == tick).OrderBy(item => item, ScheduledActionComparer.Instance))
            {
                if (failedActions.Contains(item.Action.ActionId))
                    continue;

                var unit = state.FindUnit(item.Action.UnitId)!;
                state = ApplyCompletion(state, unit, item.Action);
                AddEvent(tick, DomainEventType.ActionCompleted, item.FactionId, unit.Id, item.Action.ActionId);
            }
        }

        AddEvent(request.Configuration.TicksPerRound, DomainEventType.RoundCompleted, string.Empty);
        return new SimulationResult(state, events, diagnostics, StateChecksum.Calculate(state));

        void ResolveMovementSteps(int tick)
        {
            var intents = scheduled
                .Where(item => item.Action.Type == TacticalActionType.Move && !failedActions.Contains(item.Action.ActionId))
                .Select(item => new MovementIntent(item, tick - item.Action.StartTick - 1))
                .Where(intent => intent.StepIndex >= 0 && intent.StepIndex < MovementRules.PathFor(intent.Scheduled.Action).Count)
                .OrderBy(intent => intent.Scheduled, ScheduledActionComparer.Instance)
                .ToArray();

            if (intents.Length == 0)
                return;

            var occupiedAtTickStart = state.Units.ToDictionary(unit => unit.Position, unit => unit.Id);
            var contestedDestinations = intents.GroupBy(intent => intent.Destination).Where(group => group.Count() > 1)
                .Select(group => group.Key).ToHashSet();

            foreach (var intent in intents)
            {
                if (contestedDestinations.Contains(intent.Destination))
                {
                    Fail(tick, intent, "Destination contested by simultaneous movement.");
                    continue;
                }

                if (occupiedAtTickStart.ContainsKey(intent.Destination))
                {
                    Fail(tick, intent, "Destination was occupied at the start of this tick.");
                    continue;
                }

                var unit = state.FindUnit(intent.Scheduled.Action.UnitId)!;
                state = state.WithUnit(unit with { Position = intent.Destination });
                AddEvent(tick, DomainEventType.UnitExitedTile, intent.Scheduled.FactionId, unit.Id, intent.Scheduled.Action.ActionId, null, unit.Position, intent.Destination);
                AddEvent(tick, DomainEventType.UnitEnteredTile, intent.Scheduled.FactionId, unit.Id, intent.Scheduled.Action.ActionId, null, unit.Position, intent.Destination);
            }
        }

        void Fail(int tick, MovementIntent intent, string detail)
        {
            failedActions.Add(intent.Scheduled.Action.ActionId);
            AddEvent(tick, DomainEventType.ActionFailed, intent.Scheduled.FactionId, intent.Scheduled.Action.UnitId, intent.Scheduled.Action.ActionId, detail);
        }

        void AddEvent(int tick, DomainEventType type, string factionId, Guid? unitId = null, Guid? actionId = null, string? detail = null, GridPosition? fromPosition = null, GridPosition? toPosition = null) =>
            events.Add(new DomainEvent(sequence++, tick, type, factionId, unitId, actionId, detail, fromPosition, toPosition));
    }

    private static GameState ApplyCompletion(GameState state, UnitState unit, TacticalAction action) => action.Type switch
    {
        TacticalActionType.Rotate when action.Facing is not null => state.WithUnit(unit with { Facing = action.Facing.Value }),
        _ => state
    };

    private static IReadOnlyList<ValidationDiagnostic> Validate(SimulationRequest request)
    {
        var diagnostics = new List<ValidationDiagnostic>();
        var units = request.InitialState.Units.ToDictionary(unit => unit.Id);
        if (request.InitialState.Units.GroupBy(unit => unit.Position).Any(group => group.Count() > 1))
            diagnostics.Add(new("overlapping-initial-positions", "Initial state cannot place more than one unit in a tile."));

        foreach (var item in request.CommandBundles.SelectMany(bundle => bundle.Actions.Select(action => new ScheduledAction(bundle.FactionId, action))))
        {
            var action = item.Action;
            if (!units.TryGetValue(action.UnitId, out var unit))
                diagnostics.Add(new("unknown-unit", "Action references an unknown unit.", action.ActionId));
            else if (!StringComparer.Ordinal.Equals(unit.FactionId, item.FactionId))
                diagnostics.Add(new("wrong-faction", "A command bundle may only command its own faction's unit.", action.ActionId));
            if (action.StartTick < 0)
                diagnostics.Add(new("negative-start", "Action start tick cannot be negative.", action.ActionId));
            if (action.DurationTicks <= 0)
                diagnostics.Add(new("non-positive-duration", "Action duration must be positive.", action.ActionId));
            if (action.StartTick + action.DurationTicks > request.Configuration.TicksPerRound)
                diagnostics.Add(new("round-overrun", "Action completion must be within the round.", action.ActionId));
            if (action.Type == TacticalActionType.Move)
                ValidateMovePath(action, unit, diagnostics);
            if (action.Type == TacticalActionType.Rotate && action.Facing is null)
                diagnostics.Add(new("missing-facing", "Rotate requires a facing.", action.ActionId));
        }

        foreach (var group in request.CommandBundles.SelectMany(bundle => bundle.Actions).GroupBy(action => action.UnitId))
        {
            var ordered = group.OrderBy(action => action.StartTick).ThenBy(action => action.DurationTicks).ThenBy(action => action.ActionId.ToString("N"), StringComparer.Ordinal).ToArray();
            for (var index = 1; index < ordered.Length; index++)
            {
                if (ordered[index - 1].StartTick + ordered[index - 1].DurationTicks > ordered[index].StartTick)
                    diagnostics.Add(new("overlapping-actions", "A unit cannot have overlapping actions.", ordered[index].ActionId));
            }

            if (group.Count(action => action.Type == TacticalActionType.Move) > 1)
                diagnostics.Add(new("multiple-move-actions", "Milestone 2 permits at most one Move action per unit per round.", group.First(action => action.Type == TacticalActionType.Move).ActionId));
        }

        return diagnostics;
    }

    private static void ValidateMovePath(TacticalAction action, UnitState? unit, ICollection<ValidationDiagnostic> diagnostics)
    {
        var path = MovementRules.PathFor(action);
        if (path.Count == 0)
        {
            diagnostics.Add(new("missing-path", "Move requires a non-empty explicit path or destination.", action.ActionId));
            return;
        }

        if (path.Count != action.DurationTicks)
            diagnostics.Add(new("movement-duration-mismatch", "Move duration must equal the number of path tiles at one tick per tile.", action.ActionId));

        if (unit == null)
            return;

        var previous = unit.Position;
        var visited = new HashSet<GridPosition>();
        foreach (var position in path)
        {
            if (!MovementRules.IsCardinalStep(previous, position))
                diagnostics.Add(new("invalid-movement-step", "Each movement path step must be exactly one cardinal tile.", action.ActionId));
            if (!visited.Add(position))
                diagnostics.Add(new("repeated-path-position", "A movement path cannot revisit a tile during one action.", action.ActionId));
            previous = position;
        }
    }

    private sealed record ScheduledAction(string FactionId, TacticalAction Action);

    private sealed record MovementIntent(ScheduledAction Scheduled, int StepIndex)
    {
        public GridPosition Destination => MovementRules.PathFor(Scheduled.Action)[StepIndex];
    }

    private sealed class ScheduledActionComparer : IComparer<ScheduledAction>
    {
        public static readonly ScheduledActionComparer Instance = new();
        public int Compare(ScheduledAction? left, ScheduledAction? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;
            var faction = StringComparer.Ordinal.Compare(left.FactionId, right.FactionId);
            if (faction != 0) return faction;
            var unit = StringComparer.Ordinal.Compare(left.Action.UnitId.ToString("N"), right.Action.UnitId.ToString("N"));
            return unit != 0 ? unit : StringComparer.Ordinal.Compare(left.Action.ActionId.ToString("N"), right.Action.ActionId.ToString("N"));
        }
    }
}

}
