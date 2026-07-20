# Visibility

## VIS-LOS-001: Terrain line of sight

Status: accepted.

Two positions have objective line of sight when the deterministic grid ray between them stays clear of intermediate terrain marked as blocking line of sight. The origin and target positions are excluded from blocker checks. This is map truth, not what either faction currently knows.

Examples: a building wall or dense tree may be marked as a line-of-sight blocker. A future smoke cloud may supply the same generic blocker behavior dynamically. Whether bushes conceal rather than fully block is a later rule.

Evidence: `Line_of_sight_is_blocked_by_an_intermediate_terrain_cell` and `Line_of_sight_is_symmetric_and_excludes_origin_and_target_as_blockers`.
