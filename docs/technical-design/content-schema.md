# Content Schema

## Scenario foundation

`ScenarioDefinition` is the reusable, data-serializable definition of one tactical encounter. It contains a stable scenario ID, a setting-neutral `GridMapDefinition`, an initial `GameState`, content version, objectives, and optional unit archetype definitions. `GridMapDefinition` supplies a stable map ID and rectangular dimensions. It is intentionally not a Unity scene, a rendered tilemap, or a setting-specific level format.

`ScenarioFactory` creates simulation requests from scenario data. The resolver validates map dimensions, initial unit positions, request positions, and movement-path bounds when a scenario is present. Scenario data is preserved in replay serialization.

## Deferred schema

`TerrainCellDefinition` is the first generic scenario extension. It supplies a cell position, positive movement-tick cost, passability, AP traversal cost, and non-negative cover/concealment values. The movement resolver derives timing directly from these fields; cover and concealment currently round-trip as validated terrain data only, awaiting their accepted attack/visibility calculations.

`ScenarioSerializer` reads and writes portable JSON without requiring Unity. The example at `docs/examples/scenarios/movement-sandbox-01.json` demonstrates a reusable map, terrain, units, and content version. Unity scenes may render a scenario, but they are not the authoritative scenario data format.

Walls, doors, elevation, cover, faction metadata, scripted triggers, and content import tooling remain deferred. Objectives and named map areas are now foundational scenario content; richer objective behavior still requires dedicated rules.
## Map-area foundation

`GridMapDefinition` is setting-neutral map content with dimensions, optional terrain cells, and optional named map areas. `MapAreaDefinition` contains a stable ID and a non-empty unique set of in-bounds tiles. Areas have no behavior on their own; they provide validated content references for future deployment zones, buildings/search zones, extraction areas, capture points, and reinforcement spawns.

Scenario validation rejects empty/duplicate area IDs, empty areas, duplicate tiles within an area, and out-of-bounds area tiles. Area content round-trips through scenario JSON with terrain, units, objectives, and content version.

## Unit archetype foundation

`UnitDefinition` is portable archetype content: stable ID, maximum hit points, vision range, base movement timing, role tags, listed attack/effect IDs, and extensible named numeric attributes. It can create a starting `UnitState` at full vitality with a `UnitDefinitionId` reference. `FactionDefinition` is a content-defined roster package: stable faction ID, allowed unit-definition IDs, and tags. Scenario validation checks definition field domains, identifier-list quality, duplicate IDs, unknown unit/faction references, roster legality, and maximum-hit-point agreement between a referenced definition and a starting unit.

The first catalog does not yet automatically bind vision range, movement timing, listed attacks, effects, attributes, equipment, or bonuses to resolver behavior. Those values are intentionally explicit content prepared for their future accepted rule integrations. A country, culture, race, or other setting package is represented by faction/archetype content rather than special-case core code.
