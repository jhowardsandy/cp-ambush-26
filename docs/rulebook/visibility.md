# Visibility

## VIS-CON-001: Concealment-limited observation

Status: accepted.

A unit observes a target when Manhattan distance is at most `observer vision range − target-tile concealment − target posture concealment`, never below zero. Standing adds 0 posture concealment, crouched adds 1, and prone adds 2. Direct attacks require this observation check in addition to weapon range and line of sight. PvE only selects observable targets; without one, it advances to scout rather than targeting hidden units.

Evidence: `Concealment_reduces_observation_range_and_blocks_direct_attack`.

## VIS-LOS-001: Terrain line of sight

Status: accepted.

Two positions have objective line of sight when the deterministic grid ray between them stays clear of intermediate terrain marked as blocking line of sight. The origin and target positions are excluded from blocker checks. This is map truth, not what either faction currently knows.

Examples: a building wall or dense tree may be marked as a line-of-sight blocker. A future smoke cloud may supply the same generic blocker behavior dynamically. Whether bushes conceal rather than fully block is a later rule.

Evidence: `Line_of_sight_is_blocked_by_an_intermediate_terrain_cell` and `Line_of_sight_is_symmetric_and_excludes_origin_and_target_as_blockers`.

## VIS-RNG-001: Present-time faction visibility

Status: accepted.

A faction always has current visibility of its own units. An opposing unit is currently visible when at least one active unit of that faction is within the supplied Manhattan vision range and has objective terrain line of sight to the opposing unit. A range boundary is inclusive: a target at exactly the range is visible when the line is clear.

This produces a present-time visibility snapshot only. It does not remember a previously seen unit, create a player-facing fog-of-war display, use unit facing, or grant vision to incapacitated observers.

Examples:

| Observer to target distance | Clear line | Observer active | Result |
| ---: | --- | --- | --- |
| 3, range 3 | yes | yes | visible |
| 4, range 3 | yes | yes | not visible |
| 4, range 4 | blocked | yes | not visible |
| 1, range 1 | yes | no | not visible |

Evidence: `Faction_visibility_reveals_an_enemy_within_range_and_clear_line_of_sight`, `Faction_visibility_does_not_reveal_an_enemy_behind_a_blocker_or_outside_range`, and `Faction_visibility_always_includes_friendly_units_but_an_incapacitated_observer_reveals_no_enemy`.
