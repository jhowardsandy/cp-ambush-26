# Direct Attacks

## ATK-DIR-001: Direct-attack legality and damage

Status: accepted.

An `Attack` order names an opposing target unit and an attack profile. At completion, the resolver evaluates the attacker's and target's then-current positions. The attack is legal only when the target is active, the Manhattan distance is inside the profile's inclusive minimum/maximum range, and required terrain line of sight is clear. A successful hit applies `max(1, profile damage − target tile cover)` as a negative vitality effect. Vitality clamps at zero and a zero-vitality target becomes incapacitated.

## ATK-ACC-001: Seeded weapon accuracy

Status: accepted.

Every `AttackProfile` carries `AccuracyPercent`, an inclusive integer from 0 through 100. After an attack has passed the legality checks above, the timeline resolver draws one integer from 1 through 100 using the request's seeded round random stream. A roll less than or equal to accuracy is a hit; a higher roll is a legal miss. A miss completes the order, emits `AttackResolved`, causes no vitality change, and still consumes a one-shot overwatch reaction. An illegal attack draws no roll and remains an `ActionFailed` outcome.

The resolver uses one stream in deterministic timeline order: seeded movement contention at its relevant tick, then overwatch reactions after movement entries, then scheduled action completions. Therefore an identical request—including seed, content, order set, and timeline—replays identically. The emitted attack/reaction detail records `accuracy`, `roll`, `result`, cover, effective damage, and applied vitality so a player can inspect the calculation.

The starter military catalog is deliberately provisional: service rifle 75%, marksman rifle 85%. Profiles that omit the optional field retain 100% accuracy, which preserves guaranteed-hit fixtures and is useful for deterministic instructional scenarios. Armor, evasion, facing, projectile travel time, area effects, and concealment modifiers remain later rules. Ammunition is a separate accepted inventory rule in `inventory-and-skills.md`.

`AttackResolved` is emitted before `ActionCompleted`, with attacker/target IDs, start/end positions, range distance, accuracy, roll, result, damage, before/applied/after vitality, and target activity state. If the target is no longer legal at completion (for example, blocked line of sight), the resolver emits `ActionFailed` and does not complete the attack.

Calculation examples:

| Distance | Profile range / accuracy / roll | LOS | Target before | Damage | Target after |
| ---: | --- | --- | ---: | ---: | ---: |
| 3 | 1–3 / 100 / 64 | clear | 4 | 5 | 0 (incapacitated) |
| 3 | 1–4 / n/a / n/a | blocked | 10 | 5 | no change; action fails |
| 2 when ordered; 4 at completion after target moves | 1–3 / n/a / n/a | clear | 10 | 5 | no change; action fails |
| 4 when ordered; 2 at completion after target moves | 1–3 / 100 / 12 | clear | 10 | 5 | 5 |
| 2 | 1–3 / 100 / 73 | clear; target cover 4 | 10 | 5 → 1 | 9 |
| 2 | 1–3 / 75 / 76 | clear | 10 | 5 | 10 (legal miss) |

The third example is an intentional planning outcome, not an invalid submitted plan: both factions committed orders, movement resolved first, and the target had left the legal range by the attack completion tick. This shared-timeline uncertainty is central to the game: a player's intent is resolved against the authoritative battlefield state at that moment, rather than being silently retargeted by the presentation.

Invalid cases: missing/unknown profile, missing/unknown target, friendly target, invalid profile range/damage/accuracy/ammunition cost, insufficient ammunition, or attack without a scenario map reject the submitted plan.

Evidence: `Direct_attack_within_range_and_line_of_sight_damages_and_incapacities_target`, `Legal_attack_with_zero_accuracy_misses_without_damaging_its_target`, `Seeded_accuracy_rolls_make_identical_requests_replay_identically`, `Attack_profile_accuracy_must_be_a_percentage`, `Target_cover_mitigates_direct_attack_damage_but_never_below_one`, `Direct_attack_fails_at_resolution_when_line_of_sight_is_blocked`, `Direct_attack_fails_when_target_moves_out_of_range_before_attack_completes`, `Direct_attack_succeeds_when_target_moves_into_range_before_attack_completes`, and `Direct_attack_rejects_missing_profile_and_friendly_target`.
