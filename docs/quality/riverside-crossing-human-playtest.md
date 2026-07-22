# Riverside Crossing Human Playtest

Status: active graybox protocol  
Scenario: `Riverside Crossing 4v4` (`Assets/Scenes/GrayboxPve4v4.unity`)  
Purpose: validate that a new player can understand and intentionally use accepted rules. This complements, but never replaces, deterministic automated tests.

## Before each session

1. In Unity, use **CP Ambush → Create or Open Graybox PvE 4v4** and enter Play mode.
2. Confirm four Blue and four Red units: one capsule Rifleman, one cylinder Marksman, and two sphere Combat Medics per side.
3. Confirm Blue labels show role/vitality, Medics show `kit:2`, the **Round plan** is empty, and no console errors are present.
4. Press **Reset** before every case. Record Unity version, source commit, playtester, date, and result below.

## Pass/fail rules

- **Pass:** the stated result occurs and a new player can explain why from the map, plan panel, or event log.
- **Usability finding:** the simulation is correct, but the player cannot reliably understand, predict, or operate it. Record a reproduction and presentation improvement.
- **Rules defect:** observed state, event detail, checksum/replay, or resource change conflicts with an accepted rule. Preserve the order sequence, event log, checksum, and screenshot/video if available; add a deterministic regression before changing the rule or implementation.

## Core walkthrough

### RC-HUM-001: Board orientation and roster readability

Select each Blue unit. Identify its role, vitality, and—when it is a Medic—med-kit count. Identify a visible and a `HIDDEN` Red label.

Expected: Blue selection is obvious; Riflemen, Marksmen, and Medics remain distinguishable; the Marksman shows its own role/longer reach; red observation state is understandable as development feedback, not final fog of war.

### RC-HUM-002: Suggested route, plan preview, and AP/tick budget

Select Blue 1. Click an open map tile several cells away. Inspect the cyan numbered route and Blue 1 Round-plan row. Add **Attack red**, then use **Undo** once.

Expected: the route is cardinal and avoids blockers/current occupants; the selected-unit strip and plan show remaining/spent AP and final tick out of 10 before submission. An over-budget route extension or action is refused with its AP calculation. Undo removes the latest action or final movement tile without changing another unit’s plan.

### RC-HUM-003: Manual route and terrain understanding

Turn **Manual route: ON**. Select a Blue unit and click one legal adjacent tile at a time, including brush if reachable. Try a non-adjacent tile or blocker, then inspect route timing.

Expected: only a legal next adjacent tile extends the manual route. Brush costs additional movement time; buildings/walls cannot be routed through. Invalid clicks do not silently create a different route.

### RC-HUM-004: Multi-unit intent and seeded clash warning

Plan two Blue routes that arrive on the same open tile on the same tick. Submit the round.

Expected: the shared intended tile is magenta and both affected rows say `seeded clash` before submission. The event log identifies the resolver outcome and the delayed unit receives a transient `DELAYED` label; one unit can be delayed rather than both occupying the tile. This warning does not claim the plan is invalid.

### RC-HUM-005: Speculative attack and terrain sight

Queue a move followed by **Attack red** against the selected Red target. Also try an attack with a building/wall between the units, or where the target is currently `HIDDEN`.

Expected: the plan says the attack is checked at resolution. A legal completion draws a projectile, shows a transient damage label, and applies visible vitality loss, including target-tile cover mitigation in the event detail. A blocked, hidden, or out-of-range completion shows `FAILED` clearly and does not fabricate damage.

### RC-HUM-006: Targeted medic action and inventory

This case may take one or more prior combat rounds to damage a Blue unit. Select a Blue Combat Medic, use **Next heal target** to choose an injured active ally within one tile and clear line of sight, then press **Medic heal target** and submit.

Expected: the plan identifies the intended Blue target. A legal completion restores vitality up to the target maximum and reduces the acting Medic’s kit count by one. Out-of-range, blocked, incapacitated, or empty-kit attempts show a clear result and do not consume a kit.

### RC-HUM-007: Rifleman overwatch zone

Select a Rifleman, queue **N**, **E**, **S**, or **W** under Overwatch, and inspect the highlighted cone and `PLANNED` marker. Submit a round in which Red may enter that cone.

Expected: the cone extends only in the selected 90-degree direction and rifle range. During playback the marker becomes `ARMED`; the first eligible entry can trigger one reaction shot. A Medic receives a skill-gated explanation instead of an overwatch order.

### RC-HUM-008: Round resolution, outcome, and replay confidence

Submit at least three purposeful rounds involving two or more Blue units. Review event log and checksum after each. Then press **Reset** and use **Auto-play demo** once.

Expected: units with no drafted order wait; Red receives ordinary deterministic orders; playback progresses through ordered tick events; incapacitated units are distinct; each completed round shows a checksum. Auto-play resets first, runs no more than 12 rounds, and stops early on objective completion.

### RC-HUM-009: Hold Central Crossing objective

Move at least one active Blue unit into the named central-crossing tiles while no active Red unit occupies them. Submit consecutive rounds while preserving that uncontested state.

Expected: the objective line progresses from `0/3` to `1/3`, then `2/3`, then Blue wins at `3/3`. A Red unit entering any central-crossing tile, or Blue leaving it empty, resets the count to `0/3`. Elimination of Red remains an alternative Blue victory condition.

### RC-HUM-010: Inspectable doctrine order and manual override

Select a Blue unit, choose **Hold**, **Keep range**, or **Support**, optionally choose **Next follow unit**, then press **Auto-plan selected**. Inspect the generated Round-plan action and rationale. Use **Undo**, **Clear**, or a manual order afterward.

Expected: selection alone changes no state. Auto-plan adds only the selected unit’s ordinary legal action and explains the choice. A manual order explicitly replaces the auto-generated action; it does not combine into an opaque second authority path. A Marksman crowded inside range 2–5 shows a reposition intent; Support moves toward its chosen ally when no legal heal is available.

### RC-HUM-011: Mission briefing and authoritative debrief

Read the mission briefing before drafting any order. Complete either the hold objective or elimination objective, then inspect the status line.

Expected: a new player can state both winning paths before play. Completion names the winning faction and resolver-provided objective result; it does not claim that a presentation animation or AI intent decided the outcome.

## Session log template

| Field | Record |
| --- | --- |
| Date / playtester | |
| Unity version / source commit | |
| Device / display mode | |
| Cases attempted / passed | |
| Usability findings | case, reproduction, expected clarity improvement |
| Rules defects | case, exact order sequence, event detail, checksum, evidence link |
| Overall confidence (1–5) | |
| Follow-up owner / disposition | |

## Recorded sessions

| Date | Playtester | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-21 | Justin Howard | RC-HUM-001 through RC-HUM-008 passed | Auto-play did not complete in 12 rounds; recorded as current PvE-tuning observation, not a rules defect. |

## Exit criteria for this graybox phase

- Each case has one recorded result from a player new to the scene.
- No unresolved rules defects remain.
- Usability findings are triaged into presentation, rule clarification, or a consciously deferred item.
- Any issue affecting a calculation, movement, effect, inventory, observation, reaction, or objective has an automated regression before it is marked resolved.
