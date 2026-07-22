# Vertical-Slice Enrichment Program

Status: active after the initial rule-first engine queue completed on 2026-07-22.

The first pass proved the reusable tactical kernel. This program enriches the already-working vertical slices in playable increments. Each slice must preserve deterministic resolution, add focused automated coverage where it changes rules, update the rulebook/traceability record, and receive a human playtest check before the next slice is called complete.

## E1 — Riverside Crossing playtest proof

Improve the existing 4v4 graybox as a repeatable human test surface: make objective state/outcome and planned intent easier to inspect, add a short scenario briefing/debrief, and turn recorded findings into deterministic regressions. Success is not a prettier map; it is a new player accurately explaining what they can win, what they ordered, and why the round resolved as it did.

## E2 — Roster and loadout proof

Enrich the initial Rifleman, Combat Medic, and Marksman catalog with inspectable role/loadout cards and one additional data-defined tactical choice where it produces a clear scenario decision. Every weapon, kit, armor value, skill, and consumable remains content data and is shown without becoming a Unity-only rule.

## E3 — Mixed-roster encounter depth

Turn Riverside Crossing into a stronger tactical decision space: varied deployment pressure, support/marksman/rifleman interactions, objective alternatives, and scenario-controlled reinforcement/rescue hooks. Add acceptance cases for purposeful player plans, not merely auto-play survival.

## E4 — Planning and playback clarity

Reduce graybox friction: stronger order previews, target/area selection feedback, sequence timeline inspection, pause/speed controls, outcome summary, and replay-oriented event filtering. This includes a player-facing path to exercise area delivery, but animation remains presentation-only.

## E5 — Tactical readability art pass

Replace only the primitives that obscure meaning: first original terrain/cover/brush language, role silhouettes, selection and state indicators, attack/area feedback, and modest resolver-driven animation/sound placeholders. Every visual must map to authoritative state or events.

## E6 — Hardening and release proof

Run a scenario seed corpus and balance checks; complete structured playtests; close rule defects with regressions; validate a clean Unity build; and publish concise prototype release notes. Campaign systems, advanced AI, progression, polished art/audio, and PvP remain later vertical slices.

## Execution order

Work E1 through E6 in order, while allowing a discovered rule defect to interrupt the current slice for a narrowly documented correction. Do not turn any slice into a broad engine rewrite.
