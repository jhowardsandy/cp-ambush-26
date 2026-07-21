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

## MOV-RES-001: Vacated-tile following

Status: accepted.

A unit may enter a tile vacated by another unit during the same tick, provided this is not a direct two-unit swap. This permits a delayed unit to follow the winner of a contested tile once that winner continues along its route.

Evidence: `Move_into_a_tile_vacated_on_the_same_tick_succeeds`.

## MOV-RES-002: Simultaneous destination conflict

Status: accepted.

If two or more units attempt to enter the same tile on the same tick, the resolver uses the replay seed to select one deterministic-random winner. Every loser remains in place and its remaining unit timeline is delayed by one tick. The event log records `MovementDelayed`; later movement and queued actions shift with the unit. If that shift exceeds the round, the unit keeps any tiles already entered and the unfinished action fails at round end.

Evidence: `Simultaneous_contested_destination_selects_one_seeded_winner_and_delays_the_loser`.

## MOV-RES-003: No same-tick swaps or crossing

Status: accepted.

Because each destination is checked against start-of-tick occupancy, two units cannot exchange tiles or cross the same edge in opposite directions during one tick. Both actions fail.

Evidence: `Swap_attempt_fails_for_both_units`.
