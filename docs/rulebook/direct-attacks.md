# Attack Delivery

## ATK-DIR-001: Direct-attack legality and damage

Status: accepted.

An `Attack` order names an opposing target unit and an attack profile. At completion, the resolver evaluates the attacker's and target's then-current positions. The attack is legal only when the target is active, the Manhattan distance is inside the profile's inclusive minimum/maximum range, and required terrain line of sight is clear. A successful hit applies `max(1, profile damage − target tile cover)` as a negative vitality effect. Vitality clamps at zero and a zero-vitality target becomes incapacitated.

## ATK-AREA-001: Tile-targeted area delivery

Status: accepted.

An `AttackProfile` declares either `Direct` delivery (the compatible default) or `Area` delivery. An area attack names a target map tile, never a target unit. At completion, the resolver checks the attacker's current Manhattan distance and, when required, line of sight to that target tile. It then affects every active *opposing* unit within the profile's inclusive Manhattan `AreaRadius`, ordered by faction ID then unit ID. Friendly units are deliberately unaffected in this first contract.

The timeline draws one seeded 1–100 accuracy roll for the legal area attempt and applies that same hit/miss outcome to every affected unit. A profile may declare non-negative per-tile falloff: `effective damage = max(1, base damage − blast distance × falloff − target cover − target armor)`. Blocking terrain between the blast center and a candidate unit prevents blast propagation, so buildings/walls protect units behind them. A miss damages none. The attack may legally land on an empty tile and still consumes its configured ammunition once. Each affected unit receives its own `AttackResolved` event containing the target tile, radius, blast distance, falloff, target result, and calculation breakdown.

Area delivery cannot enter overwatch in this slice. Projectile travel, non-Manhattan shapes, knockback, terrain destruction, smoke/fire persistence, friendly fire, and presentation physics are deferred. Those extensions must keep the same profile/action/authoritative-event contract rather than introduce weapon-specific resolver paths.

## ATK-ACC-001: Seeded weapon accuracy

Status: accepted.

Every `AttackProfile` carries `AccuracyPercent`, an inclusive integer from 0 through 100. After an attack has passed the legality checks above, the timeline resolver draws one integer from 1 through 100 using the request's seeded round random stream. A roll less than or equal to accuracy is a hit; a higher roll is a legal miss. A miss completes the order, emits `AttackResolved`, causes no vitality change, and still consumes a one-shot overwatch reaction. An illegal attack draws no roll and remains an `ActionFailed` outcome.

The resolver uses one stream in deterministic timeline order: seeded movement contention at its relevant tick, then overwatch reactions after movement entries, then scheduled action completions. Therefore an identical request—including seed, content, order set, and timeline—replays identically. The emitted attack/reaction detail records `accuracy`, `roll`, `result`, cover, effective damage, and applied vitality so a player can inspect the calculation.

The starter military catalog is deliberately provisional: service rifle 75%, marksman rifle 85%. Profiles that omit the optional field retain 100% accuracy, which preserves guaranteed-hit fixtures and is useful for deterministic instructional scenarios. Evasion, facing, projectile travel time, and concealment modifiers remain later rules. Ammunition is a separate accepted inventory rule in `inventory-and-skills.md`.

## ATK-ARM-001: Target armor mitigation

Status: accepted.

Each authoritative `UnitState` carries a non-negative `ArmorValue`, normally initialized from its `UnitDefinition`. On a hit, target-tile cover applies first and armor applies second: `effective damage = max(1, profile damage − target cover − target armor)`. Armor cannot turn a hit into zero damage, cannot change a miss into a hit, and does not alter range, line of sight, or ammunition spending. A legal miss records armor for inspection but applies zero damage.

The starter Rifleman has armor 1; Combat Medic and Marksman currently have armor 0. This is provisional content, while the generic stat supports future armored soldiers, vehicles, shields, magical wards, or setting-equivalent defenses without a type-specific combat branch.

`AttackResolved` is emitted before `ActionCompleted`, with attacker/target IDs, start/end positions, range distance, accuracy, roll, result, base damage, cover, armor, effective damage, before/applied/after vitality, and target activity state. If the target is no longer legal at completion (for example, blocked line of sight), the resolver emits `ActionFailed` and does not complete the attack.

Calculation examples:

| Distance | Profile range / accuracy / roll | LOS | Target before | Damage | Target after |
| ---: | --- | --- | ---: | ---: | ---: |
| 3 | 1–3 / 100 / 64 | clear | 4 | 5 | 0 (incapacitated) |
| 3 | 1–4 / n/a / n/a | blocked | 10 | 5 | no change; action fails |
| 2 when ordered; 4 at completion after target moves | 1–3 / n/a / n/a | clear | 10 | 5 | no change; action fails |
| 4 when ordered; 2 at completion after target moves | 1–3 / 100 / 12 | clear | 10 | 5 | 5 |
| 2 | 1–3 / 100 / 73 | clear; target cover 4 | 10 | 5 → 1 | 9 |
| 2 | 1–3 / 75 / 76 | clear | 10 | 5 | 10 (legal miss) |
| 2 | 1–3 / 100 / 40 | clear; target cover 2, armor 2 | 10 | 5 → 1 | 9 |

The third example is an intentional planning outcome, not an invalid submitted plan: both factions committed orders, movement resolved first, and the target had left the legal range by the attack completion tick. This shared-timeline uncertainty is central to the game: a player's intent is resolved against the authoritative battlefield state at that moment, rather than being silently retargeted by the presentation.

Invalid cases: missing/unknown profile; missing/unknown/friendly direct target; missing, unit-based, or out-of-bounds area target; invalid profile range/damage/accuracy/radius/ammunition cost; insufficient ammunition; or attack without a scenario map reject the submitted plan.

Evidence: `Direct_attack_within_range_and_line_of_sight_damages_and_incapacities_target`, `Area_attack_targets_a_tile_and_applies_one_seeded_result_to_each_enemy_in_radius`, `Area_attack_rejects_a_unit_target_or_missing_target_tile`, `Area_attack_falls_off_from_its_center_and_buildings_block_blast_propagation`, `Legal_attack_with_zero_accuracy_misses_without_damaging_its_target`, `Seeded_accuracy_rolls_make_identical_requests_replay_identically`, `Attack_profile_accuracy_must_be_a_percentage`, `Target_cover_mitigates_direct_attack_damage_but_never_below_one`, `Target_armor_stacks_with_cover_and_damage_never_below_one`, `Direct_attack_fails_at_resolution_when_line_of_sight_is_blocked`, `Direct_attack_fails_when_target_moves_out_of_range_before_attack_completes`, `Direct_attack_succeeds_when_target_moves_into_range_before_attack_completes`, and `Direct_attack_rejects_missing_profile_and_friendly_target`.
