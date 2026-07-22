#nullable enable

namespace TacticalStrategyGame.Core
{

/// <summary>Small transparent content catalog for the first mixed-roster slice; balance values are provisional content, not engine constants.</summary>
public static class StarterMilitaryContent
{
    public static readonly AttackProfile ServiceRifle = new("service-rifle", 1, 3, 5, RequiredSkillId: "rifle-training", RequiredInventoryItemId: "service-rifle", AccuracyPercent: 75, AmmunitionItemId: "rifle-ammo", AmmunitionQuantityCost: 1);
    public static readonly AttackProfile MarksmanRifle = new("marksman-rifle", 2, 5, 4, ActionPointCost: 3, RequiredSkillId: "marksman-training", RequiredInventoryItemId: "marksman-rifle", AccuracyPercent: 85, AmmunitionItemId: "marksman-ammo", AmmunitionQuantityCost: 1, RequiresProneForOverwatch: true);
    public static readonly EffectDefinition FieldMedKit = new("field-med-kit", 4, RequiredSkillId: "field-medicine", RequiredInventoryItemId: "med-kit", InventoryQuantityCost: 1, TargetPolicy: EffectTargetPolicy.Friendly, MaximumRange: 1, RequiresLineOfSight: true);

    public static readonly UnitDefinition Rifleman = new(
        "rifleman",
        MaxHitPoints: 10,
        VisionRange: 5,
        RoleTags: new[] { "line", "rifle" },
        AttackProfileIds: new[] { ServiceRifle.Id },
        SkillIds: new[] { "rifle-training", "overwatch" },
        StartingInventory: new[] { new InventoryItemDefinition("service-rifle", 1), new InventoryItemDefinition("rifle-ammo", 8), new InventoryItemDefinition("field-dressing", 1) },
        ArmorValue: 1);

    public static readonly UnitDefinition CombatMedic = new(
        "combat-medic",
        MaxHitPoints: 9,
        VisionRange: 5,
        RoleTags: new[] { "support", "medic" },
        AttackProfileIds: new[] { ServiceRifle.Id },
        EffectIds: new[] { FieldMedKit.Id },
        SkillIds: new[] { "rifle-training", "field-medicine" },
        StartingInventory: new[] { new InventoryItemDefinition("service-rifle", 1), new InventoryItemDefinition("rifle-ammo", 8), new InventoryItemDefinition("med-kit", 2) });

    public static readonly UnitDefinition Marksman = new(
        "marksman",
        MaxHitPoints: 8,
        VisionRange: 7,
        RoleTags: new[] { "ranged", "marksman" },
        AttackProfileIds: new[] { MarksmanRifle.Id },
        SkillIds: new[] { "marksman-training", "overwatch" },
        StartingInventory: new[] { new InventoryItemDefinition("marksman-rifle", 1), new InventoryItemDefinition("marksman-ammo", 6), new InventoryItemDefinition("field-dressing", 1) });
}

}
