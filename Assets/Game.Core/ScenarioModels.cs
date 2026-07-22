#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public sealed record TerrainCellDefinition(
    GridPosition Position,
    int MovementTicks = 1,
    bool IsPassable = true,
    bool BlocksLineOfSight = false,
    int ActionPointCost = 1,
    int CoverValue = 0,
    int ConcealmentValue = 0);

/// <summary>Named set of map tiles for objectives, deployment, buildings, extraction, or spawn rules.</summary>
public sealed record MapAreaDefinition(string Id, IReadOnlyList<GridPosition> Tiles);

/// <summary>Setting-neutral rectangular tactical map definition.</summary>
public sealed record GridMapDefinition(
    string Id,
    int Width,
    int Height,
    IReadOnlyList<TerrainCellDefinition>? Terrain = null,
    IReadOnlyList<MapAreaDefinition>? Areas = null)
{
    public bool Contains(GridPosition position) =>
        position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

    public TerrainCellDefinition CellAt(GridPosition position) =>
        Terrain?.FirstOrDefault(cell => cell.Position == position) ?? new TerrainCellDefinition(position);

    public MapAreaDefinition? AreaById(string id) =>
        Areas?.FirstOrDefault(area => StringComparer.Ordinal.Equals(area.Id, id));
}

/// <summary>Reusable, data-serializable starting point for one tactical encounter.</summary>
public sealed record ScenarioDefinition(
    string Id,
    GridMapDefinition Map,
    GameState InitialState,
    string ContentVersion = "1",
    IReadOnlyList<ObjectiveDefinition>? Objectives = null,
    IReadOnlyList<UnitDefinition>? UnitDefinitions = null,
    IReadOnlyList<FactionDefinition>? FactionDefinitions = null);

public static class ScenarioFactory
{
    public static SimulationRequest CreateRequest(
        ScenarioDefinition scenario,
        IReadOnlyList<CommandBundle> commandBundles,
        RoundConfiguration configuration,
        uint randomSeed,
        string simulationVersion = "1",
        IReadOnlyList<EffectDefinition>? effects = null,
        IReadOnlyList<AttackProfile>? attackProfiles = null) =>
        new(scenario.InitialState, commandBundles, configuration, randomSeed, simulationVersion, scenario.ContentVersion, scenario, effects, attackProfiles);
}

public static class ScenarioValidator
{
    public static IReadOnlyList<ValidationDiagnostic> Validate(ScenarioDefinition scenario)
    {
        var diagnostics = new List<ValidationDiagnostic>();
        if (String.IsNullOrWhiteSpace(scenario.Id))
            diagnostics.Add(new("missing-scenario-id", "Scenario ID is required."));
        if (String.IsNullOrWhiteSpace(scenario.Map.Id))
            diagnostics.Add(new("missing-map-id", "Map ID is required."));
        if (scenario.Map.Width <= 0 || scenario.Map.Height <= 0)
            diagnostics.Add(new("invalid-map-size", "Map width and height must both be positive."));

        foreach (var unit in scenario.InitialState.Units)
        {
            if (!scenario.Map.Contains(unit.Position))
                diagnostics.Add(new("unit-out-of-bounds", "Scenario unit position must be inside its map."));
            if (unit.ActionPointBudget < 0)
                diagnostics.Add(new("negative-unit-action-points", "Scenario unit action-point budget cannot be negative."));
            if (unit.ArmorValue < 0)
                diagnostics.Add(new("negative-unit-armor", "Scenario unit armor cannot be negative."));
            ValidateInventory(unit.Inventory, "invalid-unit-inventory", "Unit inventory item IDs must be non-empty and unique with non-negative quantities.", diagnostics);
        }

        var unitDefinitions = scenario.UnitDefinitions ?? Array.Empty<UnitDefinition>();
        foreach (var definition in unitDefinitions)
        {
            if (String.IsNullOrWhiteSpace(definition.Id))
                diagnostics.Add(new("missing-unit-definition-id", "Unit definitions require a stable non-empty ID."));
            if (definition.MaxHitPoints <= 0)
                diagnostics.Add(new("non-positive-unit-definition-hit-points", "Unit definition maximum hit points must be positive."));
            if (definition.VisionRange < 0)
                diagnostics.Add(new("negative-unit-definition-vision", "Unit definition vision range cannot be negative."));
            if (definition.BaseMovementTicks <= 0)
                diagnostics.Add(new("non-positive-unit-definition-movement", "Unit definition base movement ticks must be positive."));
            if (definition.ArmorValue < 0)
                diagnostics.Add(new("negative-unit-definition-armor", "Unit definition armor cannot be negative."));
            ValidateIdentifierList(definition.RoleTags, "invalid-unit-role-tag", "Unit role tags must be non-empty and unique.", diagnostics);
            ValidateIdentifierList(definition.AttackProfileIds, "invalid-unit-attack-profile-id", "Unit attack profile IDs must be non-empty and unique.", diagnostics);
            ValidateIdentifierList(definition.EffectIds, "invalid-unit-effect-id", "Unit effect IDs must be non-empty and unique.", diagnostics);
            ValidateIdentifierList(definition.SkillIds, "invalid-unit-skill-id", "Unit skill IDs must be non-empty and unique.", diagnostics);
            ValidateInventory(definition.StartingInventory, "invalid-unit-starting-inventory", "Unit starting inventory item IDs must be non-empty and unique with positive quantities.", diagnostics, requirePositiveQuantity: true);
            if (definition.Attributes != null && (definition.Attributes.Any(attribute => String.IsNullOrWhiteSpace(attribute.Id)) || definition.Attributes.GroupBy(attribute => attribute.Id, StringComparer.Ordinal).Any(group => group.Count() > 1)))
                diagnostics.Add(new("invalid-unit-attribute-id", "Unit attributes require non-empty unique IDs."));
        }
        if (unitDefinitions.GroupBy(definition => definition.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new("duplicate-unit-definition-id", "Unit definition IDs must be unique."));
        foreach (var unit in scenario.InitialState.Units.Where(unit => !String.IsNullOrWhiteSpace(unit.UnitDefinitionId)))
        {
            var definition = unitDefinitions.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, unit.UnitDefinitionId));
            if (definition == null)
                diagnostics.Add(new("unknown-unit-definition", "Scenario unit references an unknown unit definition."));
            else if (unit.MaxHitPoints != definition.MaxHitPoints)
                diagnostics.Add(new("unit-definition-hit-points-mismatch", "Scenario unit maximum hit points must match its unit definition."));
        }

        var factionDefinitions = scenario.FactionDefinitions ?? Array.Empty<FactionDefinition>();
        foreach (var faction in factionDefinitions)
        {
            if (String.IsNullOrWhiteSpace(faction.Id))
                diagnostics.Add(new("missing-faction-definition-id", "Faction definitions require a stable non-empty ID."));
            ValidateIdentifierList(faction.UnitDefinitionIds, "invalid-faction-unit-definition-id", "Faction unit definition IDs must be non-empty and unique.", diagnostics);
            ValidateIdentifierList(faction.Tags, "invalid-faction-tag", "Faction tags must be non-empty and unique.", diagnostics);
        }
        if (factionDefinitions.GroupBy(faction => faction.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new("duplicate-faction-definition-id", "Faction definition IDs must be unique."));
        if (factionDefinitions.Count > 0)
        {
            foreach (var unit in scenario.InitialState.Units)
            {
                var faction = factionDefinitions.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, unit.FactionId));
                if (faction == null)
                    diagnostics.Add(new("unknown-faction-definition", "Scenario unit references an unknown faction definition."));
                else if (!String.IsNullOrWhiteSpace(unit.UnitDefinitionId) && !(faction.UnitDefinitionIds ?? Array.Empty<string>()).Any(id => StringComparer.Ordinal.Equals(id, unit.UnitDefinitionId)))
                    diagnostics.Add(new("unit-not-allowed-for-faction", "Scenario unit definition is not allowed by its faction definition."));
            }
        }

        var terrain = scenario.Map.Terrain ?? Array.Empty<TerrainCellDefinition>();
        foreach (var cell in terrain)
        {
            if (!scenario.Map.Contains(cell.Position))
                diagnostics.Add(new("terrain-out-of-bounds", "Terrain cell position must be inside its map."));
            if (cell.MovementTicks <= 0)
                diagnostics.Add(new("invalid-terrain-cost", "Terrain movement ticks must be positive."));
            if (cell.ActionPointCost < 0)
                diagnostics.Add(new("negative-terrain-action-points", "Terrain action-point cost cannot be negative."));
            if (cell.CoverValue < 0)
                diagnostics.Add(new("negative-terrain-cover", "Terrain cover value cannot be negative."));
            if (cell.ConcealmentValue < 0)
                diagnostics.Add(new("negative-terrain-concealment", "Terrain concealment value cannot be negative."));
        }
        if (terrain.GroupBy(cell => cell.Position).Any(group => group.Count() > 1))
            diagnostics.Add(new("duplicate-terrain-cell", "A map cannot define terrain more than once for the same tile."));

        var areas = scenario.Map.Areas ?? Array.Empty<MapAreaDefinition>();
        foreach (var area in areas)
        {
            if (String.IsNullOrWhiteSpace(area.Id))
                diagnostics.Add(new("missing-map-area-id", "Map areas require a stable non-empty ID."));
            if (area.Tiles == null || area.Tiles.Count == 0)
                diagnostics.Add(new("empty-map-area", "Map areas must contain at least one tile."));
            else
            {
                if (area.Tiles.Any(position => !scenario.Map.Contains(position)))
                    diagnostics.Add(new("map-area-out-of-bounds", "Map area tiles must be inside the scenario map."));
                if (area.Tiles.GroupBy(position => position).Any(group => group.Count() > 1))
                    diagnostics.Add(new("duplicate-map-area-tile", "A map area cannot list the same tile more than once."));
            }
        }
        if (areas.GroupBy(area => area.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new("duplicate-map-area-id", "Map area IDs must be unique."));

        var objectives = scenario.Objectives ?? Array.Empty<ObjectiveDefinition>();
        foreach (var objective in objectives)
        {
            if (String.IsNullOrWhiteSpace(objective.Id))
                diagnostics.Add(new("missing-objective-id", "Objective definitions require a stable non-empty ID."));
            if (!Enum.IsDefined(typeof(ObjectiveType), objective.Type))
                diagnostics.Add(new("unknown-objective-type", "Objective type is not supported by this simulation version."));
            if (String.IsNullOrWhiteSpace(objective.WinningFactionId) || !scenario.InitialState.Units.Any(unit => StringComparer.Ordinal.Equals(unit.FactionId, objective.WinningFactionId)))
                diagnostics.Add(new("unknown-objective-faction", "Objective winning faction must exist in the scenario initial state."));
            if (objective.Type == ObjectiveType.HoldAreaForRounds)
            {
                if (String.IsNullOrWhiteSpace(objective.AreaId) || !areas.Any(area => StringComparer.Ordinal.Equals(area.Id, objective.AreaId)))
                    diagnostics.Add(new("unknown-objective-area", "Hold-area objective must name an existing map area."));
                if (objective.RequiredControlRounds <= 0)
                    diagnostics.Add(new("non-positive-control-rounds", "Hold-area objective requires one or more uncontested completed rounds."));
            }
        }
        if (objectives.GroupBy(objective => objective.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new("duplicate-objective-id", "Objective definition IDs must be unique."));

        return diagnostics;
    }

    private static void ValidateIdentifierList(IReadOnlyList<string>? values, string code, string message, ICollection<ValidationDiagnostic> diagnostics)
    {
        if (values == null)
            return;
        if (values.Any(String.IsNullOrWhiteSpace) || values.GroupBy(value => value, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new(code, message));
    }

    private static void ValidateInventory(IReadOnlyList<InventoryItemState>? items, string code, string message, ICollection<ValidationDiagnostic> diagnostics) =>
        ValidateInventory(items?.Select(item => new InventoryItemDefinition(item.ItemId, item.Quantity)).ToArray(), code, message, diagnostics);

    private static void ValidateInventory(IReadOnlyList<InventoryItemDefinition>? items, string code, string message, ICollection<ValidationDiagnostic> diagnostics, bool requirePositiveQuantity = false)
    {
        if (items == null) return;
        if (items.Any(item => String.IsNullOrWhiteSpace(item.ItemId) || (requirePositiveQuantity ? item.Quantity <= 0 : item.Quantity < 0)) ||
            items.GroupBy(item => item.ItemId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new(code, message));
    }
}

}
