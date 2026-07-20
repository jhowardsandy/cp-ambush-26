#nullable enable

using System;
using System.Collections.Generic;

namespace TacticalStrategyGame.Core
{

/// <summary>Setting-neutral unit archetype content; listed capabilities become rules only when explicitly bound by later systems.</summary>
public sealed record UnitDefinition(
    string Id,
    int MaxHitPoints,
    int VisionRange,
    int BaseMovementTicks = 1,
    IReadOnlyList<string>? RoleTags = null,
    IReadOnlyList<string>? AttackProfileIds = null,
    IReadOnlyList<string>? EffectIds = null)
{
    public UnitState CreateInitialState(Guid unitId, string factionId, GridPosition position, Facing facing) =>
        new(unitId, factionId, position, facing, UnitActivityState.Active, MaxHitPoints, MaxHitPoints, Id);
}

}
