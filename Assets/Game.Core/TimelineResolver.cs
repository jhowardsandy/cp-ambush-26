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

        void AddEvent(int tick, DomainEventType type, string factionId, Guid? unitId = null, Guid? actionId = null, string? detail = null) =>
            events.Add(new DomainEvent(sequence++, tick, type, factionId, unitId, actionId, detail));
    }

    private static GameState ApplyCompletion(GameState state, UnitState unit, TacticalAction action) => action.Type switch
    {
        TacticalActionType.Move when action.Destination is not null => state.WithUnit(unit with { Position = action.Destination }),
        TacticalActionType.Rotate when action.Facing is not null => state.WithUnit(unit with { Facing = action.Facing.Value }),
        _ => state
    };

    private static IReadOnlyList<ValidationDiagnostic> Validate(SimulationRequest request)
    {
        var diagnostics = new List<ValidationDiagnostic>();
        var units = request.InitialState.Units.ToDictionary(unit => unit.Id);
        var factions = request.CommandBundles.Select(bundle => bundle.FactionId).ToHashSet(StringComparer.Ordinal);

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
            if (action.Type == TacticalActionType.Move && action.Destination is null)
                diagnostics.Add(new("missing-destination", "Move requires a destination.", action.ActionId));
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
        }

        return diagnostics;
    }

    private sealed record ScheduledAction(string FactionId, TacticalAction Action);

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
