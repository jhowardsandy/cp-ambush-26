# Direct Attacks

## ATK-DIR-001: Guaranteed-hit direct attack

Status: accepted.

An `Attack` order names an opposing target unit and an attack profile. At completion, the resolver evaluates the attacker's and target's then-current positions. The attack succeeds only when the target is active, the Manhattan distance is inside the profile's inclusive minimum/maximum range, and required terrain line of sight is clear. A successful attack applies the profile's fixed positive damage as a negative vitality effect. Vitality clamps at zero and a zero-vitality target becomes incapacitated.

The first combat slice is intentionally guaranteed hit once legal. It does not use seeded accuracy, cover, armor, evasion, ammunition, facing, projectile travel time, area effects, or reaction fire. Those are future rules.

`AttackResolved` is emitted before `ActionCompleted`, with attacker/target IDs, start/end positions, range distance, damage, before/applied/after vitality, and target activity state. If the target is no longer legal at completion (for example, blocked line of sight), the resolver emits `ActionFailed` and does not complete the attack.

Calculation examples:

| Distance | Profile range | LOS | Target before | Damage | Target after |
| ---: | --- | --- | ---: | ---: | ---: |
| 3 | 1–3 | clear | 4 | 5 | 0 (incapacitated) |
| 3 | 1–4 | blocked | 10 | 5 | no change; action fails |
| 2 when ordered; 4 at completion after target moves | 1–3 | clear | 10 | 5 | no change; action fails |

The third example is an intentional planning outcome, not an invalid submitted plan: both factions committed orders, movement resolved first, and the target had left the legal range by the attack completion tick. This shared-timeline uncertainty is central to the game: a player's intent is resolved against the authoritative battlefield state at that moment, rather than being silently retargeted by the presentation.

Invalid cases: missing/unknown profile, missing/unknown target, friendly target, invalid profile range/damage, or attack without a scenario map reject the submitted plan.

Evidence: `Direct_attack_within_range_and_line_of_sight_damages_and_incapacities_target`, `Direct_attack_fails_at_resolution_when_line_of_sight_is_blocked`, `Direct_attack_fails_when_target_moves_out_of_range_before_attack_completes`, and `Direct_attack_rejects_missing_profile_and_friendly_target`.
