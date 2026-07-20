# Encounter Rounds

## ENC-LOOP-001: Round-to-round state carry-forward

Status: accepted.

An encounter owns the authoritative battlefield state at the start of each planning round. The factions submit orders for that state; when the submitted plan is valid, its resolved final state becomes the next round's battlefield state and the encounter's completed-round count increases by one. Commands are issued again at every new round; initial setup never fixes a unit's later decisions.

If a submitted command plan is invalid, the encounter does not advance and its current state remains unchanged. A valid plan may still contain ordinary in-resolution failures, such as a contested movement destination; it is still a resolved round and advances to its deterministic final state.

Examples:

| Starting state | Submitted order | Valid? | Next planning state |
| --- | --- | --- | --- |
| Blue at `(0,0)`, 8/10 vitality | Move to `(1,0)` | yes | Blue at `(1,0)`, 8/10; completed rounds +1 |
| Blue at `(1,0)`, 8/10 vitality | Apply `aid +3` to blue | yes | Blue at `(1,0)`, 10/10; completed rounds +1 |
| Blue at `(0,0)` | Non-cardinal move to `(2,0)` | no | Unchanged; completed rounds unchanged |

PvE enemy intelligence and future PvP participants submit command bundles through this same contract.

Evidence: `Encounter_carries_a_valid_round_result_forward_for_next_round_orders` and `Encounter_does_not_advance_when_submitted_orders_are_invalid`.
