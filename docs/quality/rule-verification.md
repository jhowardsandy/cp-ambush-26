# Rule Verification and Traceability

## Two complementary forms of quality

Human playtesting answers whether the encounter is readable, tense, satisfying, and easy to plan. Automated rule verification answers whether the game applies its declared rules correctly for every tested input. Neither substitutes for the other.

## Required evidence for each rule

| Artifact | Purpose |
| --- | --- |
| Rule ID and rulebook statement | Stable, player-readable intent. |
| Technical contract | Types, validation, events, ordering, and version impact. |
| Calculation sheet | Inputs, intermediate values, rounding/clamping, output, examples. |
| Unit tests | Known examples, boundaries, invalid input, and event order. |
| Property/metamorphic tests | Invariants across broad generated input sets. |
| Golden replay fixture | Full initial state, commands, seed, expected event log/checksum. |
| Scenario acceptance test | Cross-system behavior in a realistic map. |
| Changelog entry | Why an accepted rule changed and replay compatibility impact. |

## Test layers

1. **Pure calculation tests**: distances, range bands, movement duration, modifiers, clamps, rounding, upgrades, and resource costs. Every formula has zero/boundary/typical/extreme cases.
2. **Domain rule tests**: validation, action legality, action ordering, state transitions, and emitted events.
3. **Property tests**: invariants such as determinism, positions remaining in bounds, non-negative resources, no action completing outside the round, and equivalent input ordering producing equivalent output.
4. **Golden replay tests**: versioned scenarios whose exact event sequence and checksum must not change accidentally.
5. **Scenario acceptance tests**: a complete planned round verifies movement, perception, combat, reactions, and objectives together.
6. **Simulation/balance tests**: thousands of seeded headless runs measure win rates, stalls, casualty distributions, rule failures, and outlier seeds.
7. **Human playtests**: structured tasks, observation, confusion points, perceived fairness, and post-round explanation comprehension.

## Calculation policy

Every calculation must declare units, input domain, ordering, rounding, clamping, and seed use. Use integer ticks and fixed-point/integer arithmetic for rule-critical quantities where possible. Floating-point values must never become an unexplained source of divergent tactical results.

For a future attack, the event explanation should carry the full calculation rather than only “hit” or “miss”:

```text
ATK-HIT-004
Base accuracy: 72%
Range modifier: -10%
Cover modifier: -18%
Final clamped chance: 44%
Seeded roll: 37
Outcome: hit
```

## Unit upgrades and progression

Progression is post-MVP, but its verification standard is set now. An upgrade is a versioned data record that names prerequisites, costs, legal targets, additive/multiplicative modifiers, caps, stacking order, and migration behavior. Tests must cover legal application, rejected application, every stack boundary, respec/reversal if supported, serialization, and the exact effect on downstream calculations.

## Traceability matrix

Maintain `docs/quality/rule-traceability.md` as rules become accepted. Each row links a rule ID to its tests, fixtures, scenario coverage, and current status. CI should eventually reject a change to an accepted calculation or event schema that lacks matching test and documentation updates.
