# PvE Planning

## PVE-PLAN-001: Explainable terrain-aware enemy orders

Status: accepted for the first conventional planner.

Player statement: Computer-controlled units follow the same round-order and combat rules as player units. They do not know the location of a hidden enemy.

Resolution: At the start of planning, each active unit is considered in stable unit-ID order.

1. A compatible medic carrying the required supplies first heals the legal friendly unit missing the most vitality. Ties use nearest distance, then stable unit ID.
2. Otherwise, the unit considers only active opposing units it can currently observe. It chooses the nearest by Manhattan distance, faction ID, then unit ID.
3. It attacks that target when the selected profile is legal and affordable.
4. If it cannot attack, it chooses an unreserved legal adjacent tile that reduces its distance to the target. If several such tiles are equally near, it chooses the tile with the greatest cover value, then uses North, East, South, West as its final deterministic tie-break.
5. If no opposing unit is observable, it may take one legal faction-direction scouting step. It does not name or chase a hidden target.

Planned destinations are reserved while a faction is being planned, so the planner does not knowingly issue two same-side moves to the same tile.

Calculation/example: A unit with two equally distance-reducing movement options chooses cover 2 over cover 0. A medic with a legal adjacent ally at 4/10 vitality and a visible enemy chooses treatment first; a Rifleman without `field-medicine` cannot do so.

Player-facing explanation: The event/decision log identifies whether the unit chose `heal`, `attack`, `move`, `scout`, or `wait`, and names the reason. The final encounter result still comes solely from the shared resolver, so an intended attack can fail if the board changes before its completion tick.

Not yet included: memory/fog-of-war, posture tactics, overwatch policy, multi-action AI plans, weapon selection, stochastic choice, and difficulty tuning.

Tests/fixtures: `Pve_planner_is_deterministic_and_uses_the_same_attack_command_contract`, `Pve_planner_moves_toward_nearest_target_with_an_explanation`, `Pve_planner_prefers_advancing_cover_when_distance_is_equal`, `Pve_planner_medic_treats_the_most_injured_legal_ally`, and `Graybox_four_v_four_pve_encounter_is_repeatable_across_multiple_rounds`.
