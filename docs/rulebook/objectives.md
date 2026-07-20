# Objectives and Encounter Outcomes

## OBJ-ELM-001: Incapacitate all opposing units

Status: accepted.

An encounter may define an `IncapacitateAllOpposingUnits` objective with a winning faction. After every valid resolved round, the objective is complete when that faction has at least one active unit and every unit from every other faction is incapacitated. The encounter records the winning faction and objective explanation; no further rounds may resolve after completion.

Examples:

| Blue active units | Opposing active units | Outcome |
| ---: | ---: | --- |
| 1+ | 0 | Blue wins |
| 1+ | 1+ | Encounter continues |
| 0 | 0 | No victory under this objective; draw/other outcome policy is future work |

The core evaluates objectives at the encounter round boundary, after timeline resolution. It does not make Unity, AI, or networking responsible for declaring a winner.

Evidence: `Eliminate_all_opponents_objective_completes_when_last_enemy_is_incapacitated`, `Eliminate_all_opponents_objective_remains_incomplete_while_an_enemy_is_active`, and `Completed_encounter_cannot_resolve_another_round`.

## Proposed objective family

The following are documented directions, not implemented rules:

- **Eliminate commander:** requires role/identity content, target eligibility, and simultaneous-outcome policy.
- **Capture/hold area:** requires named map areas, eligible occupiers, contested-state policy, tick/round duration, and reset/decay rules.
- **Accrue control points:** requires score ownership, thresholds, point timing, ties, and overtime/end-of-round policy.
- **Extract/escort/survive:** requires map exits or entities, eligibility, timing, and simultaneous-resolution policy.
- **Search, rescue, and extract:** may place an important person or civilians in a town/building. It requires scan/search actions, unknown or revealed entity state, building/interior policy, rescue eligibility, escort or pickup behavior, extraction location, and failure conditions.
- **Scheduled reinforcements:** may add units every N completed rounds at a named spawn point. It requires a deterministic schedule, unit/content list, stable generated IDs, map placement/blocked-spawn policy, spawn ownership, and replayed schedule inputs.
- **Capture a spawn point:** requires a named map area, occupancy/control rules, contested and handover policy, capture timing, and a declaration that capture disables, delays, or redirects the reinforcement schedule.

Each future objective will be a data-defined objective type evaluated by the same encounter boundary, with explicit events, calculations, tests, and replay coverage.
