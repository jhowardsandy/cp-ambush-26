# Effects and Vitality

## EFF-VIT-001: Deterministic vitality effects

Status: accepted.

An `ApplyEffect` order names a stable effect definition and a target unit. At the order's completion tick, the effect's signed vitality value is added to the target's hit points and clamped from zero through that unit's maximum hit points. A positive value restores vitality; a negative value removes it. If a target reaches zero hit points, it becomes incapacitated. Healing does not restore an incapacitated unit to active status; revival is a future explicit rule.

The resolver emits `EffectApplied` before `ActionCompleted`, including the effect ID and the before, requested, applied, and after values. The applied value may differ from the requested value only because of the zero/maximum clamp.

Calculation examples:

| Target before | Maximum | Effect value | Applied | Target after |
| ---: | ---: | ---: | ---: | ---: |
| 8 | 10 | +5 | +2 | 10 |
| 3 | 10 | -5 | -3 | 0 (incapacitated) |

Invalid cases: a missing/unknown target, a missing/unknown effect ID, duplicate effect IDs, zero-value effects, or invalid unit vitality values reject the entire request.

Evidence: `Healing_effect_clamps_to_maximum_and_emits_its_calculation`, `Damaging_effect_clamps_at_zero_and_incapacitates_the_target`, and `Effect_action_with_an_unknown_definition_is_rejected`.
