# Objective System

`ObjectiveDefinition` is serializable scenario/encounter content. The initial supported type is `IncapacitateAllOpposingUnits`, which names its winning faction. `ObjectiveRules.Evaluate` is a pure deterministic calculation over the objective list and final `GameState` at a valid encounter round boundary.

`EncounterState` stores an optional `EncounterOutcome`. Once an objective completes, `EncounterResolver` refuses another round, preventing presentation, AI, or network coordination from continuing a completed encounter. Scenarios validate objective IDs, supported types, duplicate IDs, and their declared winning faction.

Capture points, commander elimination, score thresholds, extraction, draws, and simultaneous competing victories are intentionally absent until their own objective data and policy are accepted.
