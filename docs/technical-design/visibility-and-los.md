# Visibility and Line of Sight

## Implemented geometry foundation

`VisibilityRules.HasLineOfSight` performs deterministic grid ray traversal between two in-bounds positions. Intermediate terrain cells marked `BlocksLineOfSight` block the ray. The origin and target tiles themselves do not block the ray; only cells between them do. The algorithm is symmetric for a given map and pair of positions.

This is objective geometry only. It does not yet implement faction knowledge, view range, facing arcs, elevation, concealment, lighting, smoke, hearing, remembered contacts, or player-facing fog of war.

## Deferred policy

Diagonal corner blocking, elevation/high-ground interactions, partial cover, windows, doors, and dynamic blockers require dedicated accepted rules before implementation.
