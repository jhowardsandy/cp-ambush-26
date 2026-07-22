#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

/// <summary>Setting-neutral unit archetype content; listed capabilities become rules only when explicitly bound by later systems.</summary>
public sealed record NumericAttributeDefinition(string Id, int Value);
public sealed record InventoryItemDefinition(string ItemId, int Quantity);

public sealed record UnitDefinition(
    string Id,
    int MaxHitPoints,
    int VisionRange,
    int BaseMovementTicks = 1,
    IReadOnlyList<string>? RoleTags = null,
    IReadOnlyList<string>? AttackProfileIds = null,
    IReadOnlyList<string>? EffectIds = null,
    IReadOnlyList<NumericAttributeDefinition>? Attributes = null,
    IReadOnlyList<string>? SkillIds = null,
    IReadOnlyList<InventoryItemDefinition>? StartingInventory = null,
    int ArmorValue = 0)
{
    public UnitState CreateInitialState(Guid unitId, string factionId, GridPosition position, Facing facing) =>
        new(unitId, factionId, position, facing, UnitActivityState.Active, MaxHitPoints, MaxHitPoints, Id,
            Inventory: (StartingInventory ?? Array.Empty<InventoryItemDefinition>()).Select(item => new InventoryItemState(item.ItemId, item.Quantity)).ToArray(), VisionRange: VisionRange, ArmorValue: ArmorValue);
}

/// <summary>Content-defined faction/culture package. It declares roster legality without special-casing countries or races.</summary>
public sealed record FactionDefinition(string Id, IReadOnlyList<string>? UnitDefinitionIds = null, IReadOnlyList<string>? Tags = null);

}
