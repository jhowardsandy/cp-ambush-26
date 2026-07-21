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

## OBJ-HLD-001: Hold a named area for completed rounds

Status: accepted first capture/hold slice.

An encounter may define `HoldAreaForRounds` for a named winning faction, a named map area, and a positive required number of completed rounds. At each valid round boundary, the holding count increases by one only when at least one active unit of the winning faction occupies the area and no active opposing unit occupies any tile in that area. Empty or contested areas reset the count to zero. When the count reaches the required value, the encounter completes for the named faction.

The count belongs to the authoritative encounter planning state, not to Unity or an AI. Invalid rounds do not advance it. The player-facing status must show `held rounds / required rounds` and the completed outcome records the area and final count.

Examples for a three-round Blue hold:

| End-of-round area state | Count after round |
| --- | ---: |
| Blue active; no Red active | 1/3 (then 2/3, then win at 3/3) |
| No active units | 0/3 |
| Blue and Red both active | 0/3 |
| Red only | 0/3 |

Riverside Crossing uses this objective for Blue to hold the named `central-crossing` area for three uncontested completed rounds, alongside its eliminate-Red alternative.

Evidence: `Hold_area_objective_completes_after_required_uncontested_rounds` and `Hold_area_objective_resets_progress_when_contested`.

## Proposed objective family

The following are documented directions, not implemented rules:

- **Eliminate commander:** requires role/identity content, target eligibility, and simultaneous-outcome policy.
- **Neutral/capture ownership:** requires multiple eligible factions, initial owner, handover policy, neutralization, and simultaneous control policy.
- **Accrue control points:** requires score ownership, thresholds, point timing, ties, and overtime/end-of-round policy.
- **Extract/escort/survive:** requires map exits or entities, eligibility, timing, and simultaneous-resolution policy.
- **Search, rescue, and extract:** may place an important person or civilians in a town/building. It requires scan/search actions, unknown or revealed entity state, building/interior policy, rescue eligibility, escort or pickup behavior, extraction location, and failure conditions.
- **Scheduled reinforcements:** may add units every N completed rounds at a named spawn point. It requires a deterministic schedule, unit/content list, stable generated IDs, map placement/blocked-spawn policy, spawn ownership, and replayed schedule inputs.
- **Capture a spawn point:** requires a named map area, occupancy/control rules, contested and handover policy, capture timing, and a declaration that capture disables, delays, or redirects the reinforcement schedule.

Each future objective will be a data-defined objective type evaluated by the same encounter boundary, with explicit events, calculations, tests, and replay coverage.
