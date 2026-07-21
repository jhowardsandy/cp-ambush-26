# Objective System

`ObjectiveDefinition` is serializable scenario/encounter content. Supported types are `IncapacitateAllOpposingUnits` and `HoldAreaForRounds`. The latter names a winning faction, map-area ID, and positive required uncontested completed rounds. `ObjectiveRules.Evaluate` is a pure deterministic calculation over objective content, map, final round `GameState`, and prior `ObjectiveProgress` at a valid encounter round boundary. The resulting progress is stored in `EncounterState`, never Unity.

`EncounterState` stores an optional `EncounterOutcome`. Once an objective completes, `EncounterResolver` refuses another round, preventing presentation, AI, or network coordination from continuing a completed encounter. Scenarios validate objective IDs, supported types, duplicate IDs, and their declared winning faction.

Capture points, commander elimination, score thresholds, extraction, draws, and simultaneous competing victories are intentionally absent until their own objective data and policy are accepted.
