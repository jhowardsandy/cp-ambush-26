#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

/// <summary>Objective faction visibility snapshot; it deliberately contains no remembered contacts.</summary>
public sealed record FactionVisibilitySnapshot(string FactionId, IReadOnlyList<Guid> VisibleUnitIds)
{
    public bool CanSee(Guid unitId) => VisibleUnitIds.Contains(unitId);
}

public sealed record KnownContact(Guid UnitId, GridPosition LastKnownPosition, int LastObservedRound);
public sealed record FactionKnowledgeState(string FactionId, IReadOnlyList<Guid> VisibleEnemyUnitIds, IReadOnlyList<KnownContact> Contacts);

public static class PerceptionRules
{
    /// <summary>
    /// Evaluates present-time visibility. Friendly units are always visible to their own faction.
    /// An opposing unit requires one active friendly observer within the supplied Manhattan range
    /// and a clear objective terrain line of sight.
    /// </summary>
    public static FactionVisibilitySnapshot Evaluate(GridMapDefinition map, GameState state, string factionId, int visionRange)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (String.IsNullOrWhiteSpace(factionId)) throw new ArgumentException("Faction ID is required.", nameof(factionId));
        if (visionRange < 0) throw new ArgumentOutOfRangeException(nameof(visionRange), "Vision range cannot be negative.");

        var friendlyUnits = state.Units.Where(unit => StringComparer.Ordinal.Equals(unit.FactionId, factionId)).ToArray();
        var activeObservers = friendlyUnits.Where(unit => unit.ActivityState == UnitActivityState.Active).ToArray();
        var visible = new HashSet<Guid>(friendlyUnits.Select(unit => unit.Id));

        foreach (var target in state.Units.Where(unit => !StringComparer.Ordinal.Equals(unit.FactionId, factionId)))
        {
            if (activeObservers.Any(observer =>
                GridDistance.Manhattan(observer.Position, target.Position) <= visionRange &&
                VisibilityRules.HasLineOfSight(map, observer.Position, target.Position)))
            {
                visible.Add(target.Id);
            }
        }

        return new FactionVisibilitySnapshot(factionId, visible.OrderBy(id => id.ToString("N"), StringComparer.Ordinal).ToArray());
    }

    /// <summary>Uses each active observer's authored vision range and the shared concealment/LOS contract.</summary>
    public static FactionVisibilitySnapshot Evaluate(GridMapDefinition map, GameState state, string factionId)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (String.IsNullOrWhiteSpace(factionId)) throw new ArgumentException("Faction ID is required.", nameof(factionId));

        var friendlies = state.Units.Where(unit => StringComparer.Ordinal.Equals(unit.FactionId, factionId)).ToArray();
        var observers = friendlies.Where(unit => unit.ActivityState == UnitActivityState.Active).ToArray();
        var visible = new HashSet<Guid>(friendlies.Select(unit => unit.Id));
        foreach (var target in state.Units.Where(unit => !StringComparer.Ordinal.Equals(unit.FactionId, factionId)))
            if (observers.Any(observer => VisibilityRules.Observe(map, observer, target).IsObservable && VisibilityRules.HasLineOfSight(map, observer.Position, target.Position)))
                visible.Add(target.Id);
        return new FactionVisibilitySnapshot(factionId, visible.OrderBy(id => id.ToString("N"), StringComparer.Ordinal).ToArray());
    }
}

}
