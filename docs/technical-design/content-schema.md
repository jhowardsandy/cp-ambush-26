# Content Schema

## Scenario foundation

`ScenarioDefinition` is the reusable, data-serializable definition of one tactical encounter. It contains a stable scenario ID, a setting-neutral `GridMapDefinition`, an initial `GameState`, and a content version. `GridMapDefinition` supplies a stable map ID and rectangular dimensions. It is intentionally not a Unity scene, a rendered tilemap, or a setting-specific level format.

`ScenarioFactory` creates simulation requests from scenario data. The resolver validates map dimensions, initial unit positions, request positions, and movement-path bounds when a scenario is present. Scenario data is preserved in replay serialization.

## Deferred schema

`TerrainCellDefinition` is the first generic scenario extension. It supplies a cell position, positive movement-tick cost, and passability. The movement resolver derives timing directly from these fields.

`ScenarioSerializer` reads and writes portable JSON without requiring Unity. The example at `docs/examples/scenarios/movement-sandbox-01.json` demonstrates a reusable map, terrain, units, and content version. Unity scenes may render a scenario, but they are not the authoritative scenario data format.

Walls, doors, elevation, cover, deployment zones, objectives, faction metadata, scripted triggers, and content import/validation tooling are deferred. They must extend the generic scenario model rather than force historical or fantasy assumptions into `Game.Core`.
# Scenario content schema

`GridMapDefinition` is setting-neutral map content with dimensions, optional terrain cells, and optional named map areas. `MapAreaDefinition` contains a stable ID and a non-empty unique set of in-bounds tiles. Areas have no behavior on their own; they provide validated content references for future deployment zones, buildings/search zones, extraction areas, capture points, and reinforcement spawns.

Scenario validation rejects empty/duplicate area IDs, empty areas, duplicate tiles within an area, and out-of-bounds area tiles. Area content round-trips through scenario JSON with terrain, units, objectives, and content version.
