# Visibility and Line of Sight

## Implemented geometry foundation

`VisibilityRules.HasLineOfSight` performs deterministic grid ray traversal between two in-bounds positions. Intermediate terrain cells marked `BlocksLineOfSight` block the ray. The origin and target tiles themselves do not block the ray; only cells between them do. The algorithm is symmetric for a given map and pair of positions.

## Implemented faction visibility foundation

`PerceptionRules.Evaluate` creates a deterministic present-time `FactionVisibilitySnapshot`. A faction always sees its own units. An opposing unit requires an active friendly observer, inclusive Manhattan range supplied by the caller, and `HasLineOfSight`. The snapshot uses stable unit-ID ordering.

`EncounterResolver` turns the end-of-round snapshot into `FactionKnowledgeState`: visible enemy IDs plus `KnownContact` entries with last-known position and observation round. It appends faction-scoped reveal/loss events after valid round resolution. This record is planning information only; no planner may treat remembered contacts as currently observed targets.

The core does not yet decide per-unit range values; that will belong to a future data-driven unit/ability definition. The current supplied range makes the calculation reusable for military optics, fantasy sight, temporary effects, or scenario rules.

The snapshot contains no faction memory or player-facing fog-of-war state. It does not implement facing arcs, elevation, concealment, lighting, smoke, hearing, or remembered contacts.

## Deferred policy

Diagonal corner blocking, elevation/high-ground interactions, partial cover, windows, doors, and dynamic blockers require dedicated accepted rules before implementation.
