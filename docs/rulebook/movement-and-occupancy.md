# Movement and Occupancy

## MOV-PTH-001: Cardinal timed paths

Status: accepted.

A movement order supplies a sequence of orthogonally adjacent tiles. The unit enters one listed tile each tick after its movement action begins. A three-tile path takes three ticks. Diagonal, zero-distance, and repeated path steps are invalid. Movement preserves facing.

Example: a unit at `(0,0)` starts path `[(1,0), (1,1)]` at tick 10. It enters `(1,0)` at tick 11, `(1,1)` at tick 12, and completes at tick 12.

Evidence: `Move_enters_each_cardinal_path_tile_on_its_matching_tick`, `Diagonal_path_step_is_rejected`, and `Move_duration_must_match_number_of_path_tiles`.

## MOV-TRN-001: Terrain movement cost and passability

Status: accepted.

A scenario tile may define a positive movement-tick cost and passability. Entering a normal tile costs one tick. Entering a two-tick tile occurs after two ticks; an impassable tile cannot appear in a movement path. These data values are setting-neutral and may later represent mud, rubble, magical hazards, or other original terrain concepts without changing the timeline engine.

Evidence: `Terrain_movement_ticks_delay_entry_and_determine_action_duration` and `Impassable_terrain_rejects_movement_path`.

## MOV-RES-001: Start-of-tick occupancy

Status: accepted.

A unit may enter a tile only if that tile was unoccupied at the start of the tick. A unit cannot follow another into a tile during the same tick, even if the other unit leaves it.

Evidence: `Move_into_occupied_tile_fails_even_when_occupant_leaves_on_same_tick`.

## MOV-RES-002: Simultaneous destination conflict

Status: accepted.

If two or more units attempt to enter the same tile on the same tick, all those movement actions fail. No faction, unit, or action-ID priority resolves the conflict.

Evidence: `Simultaneous_contested_destination_fails_all_movers_without_priority`.

## MOV-RES-003: No same-tick swaps or crossing

Status: accepted.

Because each destination is checked against start-of-tick occupancy, two units cannot exchange tiles or cross the same edge in opposite directions during one tick. Both actions fail.

Evidence: `Swap_attempt_fails_for_both_units`.
