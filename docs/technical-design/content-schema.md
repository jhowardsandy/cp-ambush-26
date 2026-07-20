# Content Schema

## Scenario foundation

`ScenarioDefinition` is the reusable, data-serializable definition of one tactical encounter. It contains a stable scenario ID, a setting-neutral `GridMapDefinition`, an initial `GameState`, and a content version. `GridMapDefinition` supplies a stable map ID and rectangular dimensions. It is intentionally not a Unity scene, a rendered tilemap, or a setting-specific level format.

`ScenarioFactory` creates simulation requests from scenario data. The resolver validates map dimensions, initial unit positions, request positions, and movement-path bounds when a scenario is present. Scenario data is preserved in replay serialization.

## Deferred schema

Terrain cells, walls, doors, elevation, cover, deployment zones, objectives, faction metadata, scripted triggers, and content import/validation tooling are deferred. They must extend the generic scenario model rather than force historical or fantasy assumptions into `Game.Core`.
