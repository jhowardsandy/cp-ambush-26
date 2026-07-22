#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

/// <summary>Pure area-delivery contract. Target-tile legality is checked once; the supplied seeded roll is then applied consistently to each affected opposing unit.</summary>
public static class AreaAttackRules
{
    public static AreaAttackResolution Resolve(UnitState attacker, GridPosition targetPosition, IReadOnlyList<UnitState> units, AttackProfile profile, GridMapDefinition map, int? accuracyRoll = null)
    {
        if (profile.Delivery != AttackDeliveryType.Area)
            return new AreaAttackResolution(0, Array.Empty<AreaAttackImpact>(), "Attack profile does not use area delivery.");
        if (!map.Contains(targetPosition))
            return new AreaAttackResolution(0, Array.Empty<AreaAttackImpact>(), "Area target must be inside the scenario map.");

        var distance = GridDistance.Manhattan(attacker.Position, targetPosition);
        if (distance < profile.MinimumRange || distance > profile.MaximumRange)
            return new AreaAttackResolution(distance, Array.Empty<AreaAttackImpact>(), $"Target distance {distance} is outside attack range {profile.MinimumRange}-{profile.MaximumRange}.");
        if (profile.RequiresLineOfSight && !VisibilityRules.HasLineOfSight(map, attacker.Position, targetPosition))
            return new AreaAttackResolution(distance, Array.Empty<AreaAttackImpact>(), "Target line of sight is blocked.");
        if (accuracyRoll is not null && (accuracyRoll < 1 || accuracyRoll > 100))
            throw new ArgumentOutOfRangeException(nameof(accuracyRoll), "Accuracy rolls must be between 1 and 100 inclusive.");

        var impacts = units.Where(unit => unit.ActivityState == UnitActivityState.Active && !StringComparer.Ordinal.Equals(unit.FactionId, attacker.FactionId))
            .Where(unit => GridDistance.Manhattan(unit.Position, targetPosition) <= profile.AreaRadius)
            .Where(unit => VisibilityRules.HasLineOfSight(map, targetPosition, unit.Position))
            .OrderBy(unit => unit.FactionId, StringComparer.Ordinal).ThenBy(unit => unit.Id)
            .Select(unit => new AreaAttackImpact(unit.Id, GridDistance.Manhattan(unit.Position, targetPosition), AttackRules.ResolveAreaImpact(unit, profile, map, GridDistance.Manhattan(unit.Position, targetPosition), accuracyRoll)))
            .ToArray();
        return new AreaAttackResolution(distance, impacts);
    }
}

public sealed record AreaAttackImpact(Guid TargetUnitId, int DistanceFromCenter, AttackResolution Resolution);
public sealed record AreaAttackResolution(int Distance, IReadOnlyList<AreaAttackImpact> Impacts, string? FailureDetail = null);

}
