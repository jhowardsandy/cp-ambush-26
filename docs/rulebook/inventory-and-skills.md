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

## Initial transparent catalog

The first two provisional military archetypes are intentionally small and data-driven:

| Archetype | Vitality | Skills | Starting inventory |
| --- | ---: | --- | --- |
| Rifleman | 10 | rifle training, overwatch | service rifle ×1, field dressing ×1 |
| Combat Medic | 9 | rifle training, field medicine | service rifle ×1, med kit ×2 |

Both use the provisional service-rifle profile (range 1–3, guaranteed 5 damage in the current combat slice). Combat Medic's field med kit restores 4 vitality and costs one med kit. These are vertical-slice content values, not final balance.
