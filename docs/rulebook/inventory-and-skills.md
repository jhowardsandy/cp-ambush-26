# Inventory and Skills

## INV-SKL-001: Content-defined entitlement

Status: accepted

Player statement: A unit can use a gated weapon or item only if its archetype grants the required skill and its authoritative inventory contains the required item.

Inputs: acting unit's `UnitDefinitionId`, that definition's skill IDs, the unit's inventory balances, and the selected attack/effect profile requirement.

Resolution: The resolver validates the requirement before a round begins. A gated action from a unit without a definition, required skill, or required item makes the submitted plan invalid; the round does not resolve.

Calculation/example: A Combat Medic with `field-medicine` and two `med-kit` items may use `field-med-kit`; a Rifleman lacking `field-medicine` may not. A profile may require a carried item without consuming it, such as a service rifle.

Events emitted: Validated actions retain their normal action/effect events. Invalid plans return explicit diagnostics including `action-requires-unit-definition`, `missing-required-skill`, or `missing-required-inventory-item`.

Tests/fixtures: `Skill_and_inventory_gated_medical_effect_consumes_a_med_kit_deterministically`; `Gated_actions_reject_missing_skills_and_oversubscribed_inventory`.

## INV-SKL-002: Consumable inventory accounting

Status: accepted

Player statement: A successful action with a positive item cost spends that quantity from the acting unit's inventory and records the remaining amount.

Inputs: profile item ID and quantity cost, acting unit inventory, and all planned costs for that unit.

Resolution: A plan cannot reserve more of an item than the unit starts the round with. At successful completion, the resolver decrements the acting unit's inventory. A failed direct attack does not consume its item cost because it did not successfully complete.

Calculation/example: A Combat Medic with two med kits resolves `field-med-kit` with cost one: `2 - 1 = 1` remaining. Two one-kit actions planned from one kit are rejected before resolution.

Events emitted: `EffectApplied` includes `item`, `spent`, and `remaining` when the effect consumes inventory. The final deterministic checksum includes non-empty inventory balances.

Tests/fixtures: `Skill_and_inventory_gated_medical_effect_consumes_a_med_kit_deterministically`; `Gated_actions_reject_missing_skills_and_oversubscribed_inventory`.

## INV-AMM-001: Ammunition is an attack-attempt resource

Status: accepted.

Player statement: A weapon is a carried entitlement; ammunition is a separate named inventory item. Each legal direct-fire attempt spends its profile's ammunition quantity, whether it hits or misses. A triggered overwatch shot follows the same rule. Arming overwatch itself spends nothing; it spends ammunition only if a reaction is actually triggered.

Inputs: `AttackProfile.AmmunitionItemId`, `AmmunitionQuantityCost`, authoritative unit inventory, and all attack/overwatch orders planned by that unit.

Resolution: Ammo quantities must be non-negative and a positive cost requires an item ID. Planning reserves the required quantity for every direct attack and armed overwatch order, rejecting an over-subscribed plan before the round. A direct attack that is illegal at its completion tick spends no ammunition because it did not become an attack attempt. A legal hit or miss spends exactly the stated amount. A triggered overwatch reaction spends exactly the stated amount; an untriggered watch leaves ammunition untouched.

Calculation/example: A Rifleman begins with `rifle-ammo ×8`. A legal miss with service-rifle cost one produces `8 - 1 = 7`; the event records `item=rifle-ammo; spent=1; remaining=7`. Two one-round attack attempts planned from one round of ammunition are rejected.

Events emitted: `AttackResolved` and `ReactionAttackResolved` append `item`, `spent`, and `remaining` when ammunition is consumed. The final deterministic checksum includes the resulting inventory balance.

Tests/fixtures: `Legal_attack_attempt_spends_its_named_ammunition_even_when_it_misses`; `Planned_attack_attempts_cannot_reserve_more_ammunition_than_a_unit_carries`; `Triggered_overwatch_consumes_one_ammunition_even_when_the_reaction_misses`.

## Initial transparent catalog

The first provisional military archetypes are intentionally small and data-driven:

| Archetype | Vitality | Skills | Starting inventory |
| --- | ---: | --- | --- |
| Rifleman | 10 | rifle training, overwatch | service rifle ×1, rifle ammo ×8, field dressing ×1 |
| Combat Medic | 9 | rifle training, field medicine | service rifle ×1, rifle ammo ×8, med kit ×2 |
| Marksman | 8 | marksman training, overwatch | marksman rifle ×1, marksman ammo ×6, field dressing ×1 |

Rifleman and Combat Medic use the provisional service-rifle profile (range 1–3, 75% accuracy, 5 damage, 2 AP, rifle ammo ×1 per legal shot). Marksman uses the marksman-rifle profile (range 2–5, 85% accuracy, 4 damage, 3 AP, marksman ammo ×1 per legal shot); it cannot fire at an adjacent target. Combat Medic's field med kit restores 4 vitality, costs one med kit, targets an active friendly unit at range 0–1, and requires line of sight at its completion tick. These are vertical-slice content values, not final balance.
