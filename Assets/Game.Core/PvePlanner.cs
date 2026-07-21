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

    /// <param name="healingEffect">Optional support effect available to a compatible, equipped unit.</param>
    /// <param name="unitDefinitions">Optional roster catalog used to verify declared skills and effects before planning.</param>
    /// <param name="scoutObjectives">Known authored map positions to investigate before an opposing unit is observable.</param>
    public static PvePlan Plan(
        string factionId,
        GameState state,
        GridMapDefinition map,
        AttackProfile profile,
        EffectDefinition? healingEffect = null,
        IReadOnlyList<UnitDefinition>? unitDefinitions = null,
        IReadOnlyList<GridPosition>? scoutObjectives = null)
    {
        var actions = new List<TacticalAction>();
        var decisions = new List<PveDecision>();
        var reservedDestinations = new HashSet<GridPosition>(state.Units.Select(unit => unit.Position));

        foreach (var unit in state.Units.Where(unit => unit.ActivityState == UnitActivityState.Active && StringComparer.Ordinal.Equals(unit.FactionId, factionId))
                     .OrderBy(unit => unit.Id))
        {
            var definition = (unitDefinitions ?? Array.Empty<UnitDefinition>()).FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, unit.UnitDefinitionId));
            var healTarget = FindHealTarget(unit, state, map, healingEffect, definition);
            if (healTarget is not null && healingEffect is not null)
            {
                actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.ApplyEffect, 0, 1, TargetUnitId: healTarget.Id, EffectId: healingEffect.Id));
                decisions.Add(new PveDecision(unit.Id, "heal", $"Treat injured ally {healTarget.Id} before attacking; {healingEffect.Id} restores up to {healingEffect.VitalityDelta} vitality."));
                continue;
            }

            var target = state.Units.Where(candidate => candidate.ActivityState == UnitActivityState.Active && !StringComparer.Ordinal.Equals(candidate.FactionId, factionId))
                .Where(candidate => VisibilityRules.Observe(map, unit, candidate).IsObservable)
                .OrderBy(candidate => GridDistance.Manhattan(unit.Position, candidate.Position))
                .ThenBy(candidate => candidate.FactionId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Id)
                .FirstOrDefault();
            if (target is null)
            {
                var scoutingDestination = FindScoutingDestination(unit, map, reservedDestinations, scoutObjectives);
                if (scoutingDestination is not null)
                {
                    reservedDestinations.Add(scoutingDestination);
                    actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.Move, 0, map.CellAt(scoutingDestination).MovementTicks, Path: new[] { scoutingDestination }));
                    decisions.Add(new PveDecision(unit.Id, "scout", $"No observable opposing unit exists; advance toward a known scouting objective at ({scoutingDestination.X},{scoutingDestination.Y}) without hidden-target knowledge."));
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
                .Where(candidate => map.CellAt(candidate).ActionPointCost <= unit.ActionPointBudget)
                .ToArray();
            var advancing = candidates.Where(candidate => GridDistance.Manhattan(candidate, target.Position) < GridDistance.Manhattan(unit.Position, target.Position)).ToArray();
            var destination = advancing.OrderByDescending(candidate => map.CellAt(candidate).CoverValue)
                .ThenBy(candidate => GridDistance.Manhattan(candidate, target.Position)).ThenBy(candidate => Array.IndexOf(MovementPreference, new GridPosition(candidate.X - unit.Position.X, candidate.Y - unit.Position.Y))).FirstOrDefault()
                ?? candidates.OrderByDescending(candidate => map.CellAt(candidate).CoverValue).ThenBy(candidate => Array.IndexOf(MovementPreference, new GridPosition(candidate.X - unit.Position.X, candidate.Y - unit.Position.Y))).FirstOrDefault();
            if (destination is not null)
            {
                reservedDestinations.Add(destination);
                actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.Move, 0, map.CellAt(destination).MovementTicks, Path: new[] { destination }));
                var improvesDistance = GridDistance.Manhattan(destination, target.Position) < GridDistance.Manhattan(unit.Position, target.Position);
                var cover = map.CellAt(destination).CoverValue;
                decisions.Add(new PveDecision(unit.Id, "move", improvesDistance
                    ? $"Nearest target is {target.Id}; move to ({destination.X},{destination.Y}) reduces distance and uses cover {cover}."
                    : $"Nearest target is {target.Id}; move to ({destination.X},{destination.Y}) to clear the formation."));
                continue;
            }

            decisions.Add(new PveDecision(unit.Id, "wait", $"Nearest target is {target.Id}, but no unreserved legal adjacent tile reduces distance."));
        }

        return new PvePlan(new CommandBundle(factionId, actions), decisions);
    }

    private static GridPosition? FindScoutingDestination(UnitState unit, GridMapDefinition map, ISet<GridPosition> reservedDestinations, IReadOnlyList<GridPosition>? scoutObjectives)
    {
        var objectives = (scoutObjectives ?? Array.Empty<GridPosition>()).Where(map.Contains).ToArray();
        if (objectives.Length == 0) objectives = new[] { new GridPosition(map.Width / 2, map.Height / 2) };
        var objective = objectives.OrderBy(position => GridDistance.Manhattan(unit.Position, position)).ThenBy(position => position.X).ThenBy(position => position.Y).First();
        var candidates = MovementPreference.Select(delta => new GridPosition(unit.Position.X + delta.X, unit.Position.Y + delta.Y))
            .Where(candidate => map.Contains(candidate) && map.CellAt(candidate).IsPassable && !reservedDestinations.Contains(candidate))
            .Where(candidate => map.CellAt(candidate).ActionPointCost <= unit.ActionPointBudget)
            .ToArray();
        return candidates.Where(candidate => GridDistance.Manhattan(candidate, objective) < GridDistance.Manhattan(unit.Position, objective))
            .OrderByDescending(candidate => map.CellAt(candidate).CoverValue)
            .ThenBy(candidate => GridDistance.Manhattan(candidate, objective))
            .ThenBy(candidate => Array.IndexOf(MovementPreference, new GridPosition(candidate.X - unit.Position.X, candidate.Y - unit.Position.Y)))
            .FirstOrDefault();
    }

    private static UnitState? FindHealTarget(UnitState unit, GameState state, GridMapDefinition map, EffectDefinition? effect, UnitDefinition? definition)
    {
        if (effect is null || definition is null || effect.VitalityDelta <= 0 || effect.ActionPointCost > unit.ActionPointBudget)
            return null;
        if (!(definition.EffectIds ?? Array.Empty<string>()).Any(id => StringComparer.Ordinal.Equals(id, effect.Id)))
            return null;
        if (effect.RequiredSkillId is not null && !(definition.SkillIds ?? Array.Empty<string>()).Any(id => StringComparer.Ordinal.Equals(id, effect.RequiredSkillId)))
            return null;
        if (effect.RequiredInventoryItemId is not null && InventoryRules.QuantityOf(unit, effect.RequiredInventoryItemId) < effect.InventoryQuantityCost)
            return null;

        return state.Units.Where(candidate => candidate.ActivityState == UnitActivityState.Active
                && StringComparer.Ordinal.Equals(candidate.FactionId, unit.FactionId)
                && candidate.HitPoints < candidate.MaxHitPoints)
            .Where(candidate => EffectRules.Resolve(unit, candidate, effect, map).FailureDetail is null)
            .OrderByDescending(candidate => candidate.MaxHitPoints - candidate.HitPoints)
            .ThenBy(candidate => GridDistance.Manhattan(unit.Position, candidate.Position))
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefault();
    }
}
}
