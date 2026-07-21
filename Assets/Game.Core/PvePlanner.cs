#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{
public sealed record PveDecision(Guid UnitId, string Decision, string Explanation);

public sealed record PvePlan(CommandBundle Commands, IReadOnlyList<PveDecision> Decisions);

/// <summary>First deterministic conventional enemy planner. It produces ordinary command bundles; it never resolves outcomes itself.</summary>
public static class PvePlanner
{
    private static readonly GridPosition[] MovementPreference =
    {
        new(0, 1), new(1, 0), new(0, -1), new(-1, 0)
    };

    public static PvePlan Plan(string factionId, GameState state, GridMapDefinition map, AttackProfile profile)
    {
        var actions = new List<TacticalAction>();
        var decisions = new List<PveDecision>();
        var reservedDestinations = new HashSet<GridPosition>(state.Units.Select(unit => unit.Position));

        foreach (var unit in state.Units.Where(unit => unit.ActivityState == UnitActivityState.Active && StringComparer.Ordinal.Equals(unit.FactionId, factionId))
                     .OrderBy(unit => unit.Id))
        {
            var target = state.Units.Where(candidate => candidate.ActivityState == UnitActivityState.Active && !StringComparer.Ordinal.Equals(candidate.FactionId, factionId))
                .Where(candidate => VisibilityRules.Observe(map, unit, candidate).IsObservable)
                .OrderBy(candidate => GridDistance.Manhattan(unit.Position, candidate.Position))
                .ThenBy(candidate => candidate.FactionId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Id)
                .FirstOrDefault();
            if (target is null)
            {
                var scoutingDelta = StringComparer.Ordinal.Equals(factionId, "red") ? new GridPosition(0, -1) : new GridPosition(0, 1);
                var scoutingDestination = new GridPosition(unit.Position.X + scoutingDelta.X, unit.Position.Y + scoutingDelta.Y);
                if (map.Contains(scoutingDestination) && map.CellAt(scoutingDestination).IsPassable && !reservedDestinations.Contains(scoutingDestination))
                {
                    reservedDestinations.Add(scoutingDestination);
                    actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.Move, 0, map.CellAt(scoutingDestination).MovementTicks, Path: new[] { scoutingDestination }));
                    decisions.Add(new PveDecision(unit.Id, "scout", "No observable opposing unit exists; advance without hidden-target knowledge."));
                }
                else decisions.Add(new PveDecision(unit.Id, "wait", "No observable opposing unit exists."));
                continue;
            }

            if (profile.ActionPointCost <= unit.ActionPointBudget && AttackRules.Resolve(unit, target, profile, map).FailureDetail is null)
            {
                actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.Attack, 0, 1, TargetUnitId: target.Id, AttackProfileId: profile.Id));
                decisions.Add(new PveDecision(unit.Id, "attack", $"Nearest legal target is {target.Id} at distance {GridDistance.Manhattan(unit.Position, target.Position)}."));
                continue;
            }

            var candidates = MovementPreference.Select(delta => new GridPosition(unit.Position.X + delta.X, unit.Position.Y + delta.Y))
                .Where(candidate => map.Contains(candidate) && map.CellAt(candidate).IsPassable && !reservedDestinations.Contains(candidate))
                .Where(candidate => map.CellAt(candidate).ActionPointCost <= unit.ActionPointBudget);
            var destination = candidates.FirstOrDefault(candidate => GridDistance.Manhattan(candidate, target.Position) < GridDistance.Manhattan(unit.Position, target.Position))
                ?? candidates.FirstOrDefault();
            if (destination is not null)
            {
                reservedDestinations.Add(destination);
                actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.Move, 0, map.CellAt(destination).MovementTicks, Path: new[] { destination }));
                var improvesDistance = GridDistance.Manhattan(destination, target.Position) < GridDistance.Manhattan(unit.Position, target.Position);
                decisions.Add(new PveDecision(unit.Id, "move", improvesDistance
                    ? $"Nearest target is {target.Id}; move to ({destination.X},{destination.Y}) reduces distance."
                    : $"Nearest target is {target.Id}; move to ({destination.X},{destination.Y}) to clear the formation."));
                continue;
            }

            decisions.Add(new PveDecision(unit.Id, "wait", $"Nearest target is {target.Id}, but no unreserved legal adjacent tile reduces distance."));
        }

        return new PvePlan(new CommandBundle(factionId, actions), decisions);
    }
}
}
