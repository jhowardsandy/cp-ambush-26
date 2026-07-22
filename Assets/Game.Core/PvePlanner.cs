#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{
public sealed record PveDecision(Guid UnitId, string Decision, string Explanation);

public sealed record PvePlan(CommandBundle Commands, IReadOnlyList<PveDecision> Decisions);

public enum TacticalDoctrine { Aggressive, HoldObjective, KeepRange, SupportFollow }
public sealed record PveDoctrineAssignment(Guid UnitId, TacticalDoctrine Doctrine, Guid? FollowUnitId = null);

/// <summary>Scenario-supplied intent for conventional AI; it is known content, never hidden-opponent information.</summary>
public sealed record PvePlanningPolicy(
    IReadOnlyList<GridPosition>? HoldObjectiveTiles = null,
    bool HoldWhenOccupied = false,
    IReadOnlyList<PveDoctrineAssignment>? DoctrineAssignments = null);

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
        IReadOnlyList<GridPosition>? scoutObjectives = null,
        PvePlanningPolicy? policy = null)
        => Plan(factionId, state, map, new[] { profile }, healingEffect, unitDefinitions, scoutObjectives, policy);

    /// <summary>Plans with unit-content-selected attack profiles; a unit only considers profiles listed by its definition.</summary>
    public static PvePlan Plan(
        string factionId,
        GameState state,
        GridMapDefinition map,
        IReadOnlyList<AttackProfile> profiles,
        EffectDefinition? healingEffect = null,
        IReadOnlyList<UnitDefinition>? unitDefinitions = null,
        IReadOnlyList<GridPosition>? scoutObjectives = null,
        PvePlanningPolicy? policy = null)
    {
        if (profiles == null || profiles.Count == 0) throw new ArgumentException("At least one attack profile is required.", nameof(profiles));
        var actions = new List<TacticalAction>();
        var decisions = new List<PveDecision>();
        var reservedDestinations = new HashSet<GridPosition>(state.Units.Select(unit => unit.Position));

        foreach (var unit in state.Units.Where(unit => unit.ActivityState == UnitActivityState.Active && StringComparer.Ordinal.Equals(unit.FactionId, factionId))
                     .OrderBy(unit => unit.Id))
        {
            var definition = (unitDefinitions ?? Array.Empty<UnitDefinition>()).FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, unit.UnitDefinitionId));
            var profile = ProfileFor(unit, definition, profiles);
            var doctrine = policy?.DoctrineAssignments?.FirstOrDefault(assignment => assignment.UnitId == unit.Id);
            var healTarget = FindHealTarget(unit, state, map, healingEffect, definition);
            if (healTarget is not null && healingEffect is not null)
            {
                actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.ApplyEffect, 0, 1, TargetUnitId: healTarget.Id, EffectId: healingEffect.Id));
                decisions.Add(new PveDecision(unit.Id, "heal", $"Treat injured ally {healTarget.Id} before attacking; {healingEffect.Id} restores up to {healingEffect.VitalityDelta} vitality."));
                continue;
            }

            if (doctrine?.Doctrine == TacticalDoctrine.SupportFollow)
            {
                var followedUnit = FindFollowTarget(unit, state, doctrine);
                if (followedUnit is not null && GridDistance.Manhattan(unit.Position, followedUnit.Position) > 1)
                {
                    var followDestination = LegalAdjacentCandidates(unit, map, reservedDestinations)
                        .Where(candidate => GridDistance.Manhattan(candidate, followedUnit.Position) < GridDistance.Manhattan(unit.Position, followedUnit.Position))
                        .OrderByDescending(candidate => map.CellAt(candidate).CoverValue)
                        .ThenBy(candidate => GridDistance.Manhattan(candidate, followedUnit.Position))
                        .ThenBy(candidate => Array.IndexOf(MovementPreference, new GridPosition(candidate.X - unit.Position.X, candidate.Y - unit.Position.Y)))
                        .FirstOrDefault();
                    if (followDestination is not null)
                    {
                        reservedDestinations.Add(followDestination);
                        actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.Move, 0, map.CellAt(followDestination).MovementTicks, Path: new[] { followDestination }));
                        decisions.Add(new PveDecision(unit.Id, "support", $"Follow doctrine closes toward friendly unit {followedUnit.Id} at ({followDestination.X},{followDestination.Y})."));
                        continue;
                    }
                }
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

            if (profile.ActionPointCost <= unit.ActionPointBudget && HasRequiredAmmunition(unit, profile) && AttackRules.Resolve(unit, target, profile, map).FailureDetail is null)
            {
                actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.Attack, 0, 1, TargetUnitId: target.Id, AttackProfileId: profile.Id));
                decisions.Add(new PveDecision(unit.Id, "attack", $"Nearest legal target is {target.Id} at distance {GridDistance.Manhattan(unit.Position, target.Position)}."));
                continue;
            }

            var candidates = LegalAdjacentCandidates(unit, map, reservedDestinations);
            var distanceToTarget = GridDistance.Manhattan(unit.Position, target.Position);
            var isRanged = (definition?.RoleTags ?? Array.Empty<string>()).Any(tag => StringComparer.Ordinal.Equals(tag, "ranged")) || doctrine?.Doctrine == TacticalDoctrine.KeepRange;
            if (isRanged && distanceToTarget < profile.MinimumRange)
            {
                var retreat = candidates.Where(candidate => GridDistance.Manhattan(candidate, target.Position) > distanceToTarget)
                    .OrderByDescending(candidate => map.CellAt(candidate).CoverValue)
                    .ThenByDescending(candidate => GridDistance.Manhattan(candidate, target.Position))
                    .ThenBy(candidate => Array.IndexOf(MovementPreference, new GridPosition(candidate.X - unit.Position.X, candidate.Y - unit.Position.Y)))
                    .FirstOrDefault();
                if (retreat is not null)
                {
                    reservedDestinations.Add(retreat);
                    actions.Add(new TacticalAction(unit.Id, unit.Id, TacticalActionType.Move, 0, map.CellAt(retreat).MovementTicks, Path: new[] { retreat }));
                    decisions.Add(new PveDecision(unit.Id, "reposition", $"Target {target.Id} is inside {profile.Id} minimum range {profile.MinimumRange}; reposition to ({retreat.X},{retreat.Y}) with cover {map.CellAt(retreat).CoverValue}."));
                    continue;
                }
                decisions.Add(new PveDecision(unit.Id, "hold", $"Target {target.Id} is inside {profile.Id} minimum range {profile.MinimumRange}, but no legal retreat tile exists."));
                continue;
            }

            if ((policy?.HoldWhenOccupied == true || doctrine?.Doctrine == TacticalDoctrine.HoldObjective) && (policy?.HoldObjectiveTiles ?? Array.Empty<GridPosition>()).Contains(unit.Position))
            {
                decisions.Add(new PveDecision(unit.Id, "hold", "Occupying the authored hold objective; retain position until a legal action is available."));
                continue;
            }

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

    private static GridPosition[] LegalAdjacentCandidates(UnitState unit, GridMapDefinition map, ISet<GridPosition> reservedDestinations) =>
        MovementPreference.Select(delta => new GridPosition(unit.Position.X + delta.X, unit.Position.Y + delta.Y))
            .Where(candidate => map.Contains(candidate) && map.CellAt(candidate).IsPassable && !reservedDestinations.Contains(candidate))
            .Where(candidate => map.CellAt(candidate).ActionPointCost <= unit.ActionPointBudget)
            .ToArray();

    private static AttackProfile ProfileFor(UnitState unit, UnitDefinition? definition, IReadOnlyList<AttackProfile> profiles)
    {
        var permittedIds = definition?.AttackProfileIds ?? Array.Empty<string>();
        return profiles.FirstOrDefault(profile => permittedIds.Any(id => StringComparer.Ordinal.Equals(id, profile.Id)))
            ?? profiles[0];
    }

    private static bool HasRequiredAmmunition(UnitState unit, AttackProfile profile) =>
        String.IsNullOrWhiteSpace(profile.AmmunitionItemId) || InventoryRules.QuantityOf(unit, profile.AmmunitionItemId) >= profile.AmmunitionQuantityCost;

    private static UnitState? FindFollowTarget(UnitState unit, GameState state, PveDoctrineAssignment doctrine)
    {
        var allies = state.Units.Where(candidate => candidate.Id != unit.Id && candidate.ActivityState == UnitActivityState.Active && StringComparer.Ordinal.Equals(candidate.FactionId, unit.FactionId));
        if (doctrine.FollowUnitId.HasValue)
            return allies.FirstOrDefault(candidate => candidate.Id == doctrine.FollowUnitId.Value);
        return allies.OrderBy(candidate => GridDistance.Manhattan(unit.Position, candidate.Position)).ThenBy(candidate => candidate.Id).FirstOrDefault();
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
