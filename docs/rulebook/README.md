# Rulebook System

This folder is the living, player-readable rulebook. It describes accepted behavior, not implementation guesses. Each chapter references stable rule IDs and calculation examples; technical implementation details live in `docs/technical-design/`.

## Accepted chapters

1. `movement-and-occupancy.md` — paths, terrain timing, and simultaneous occupancy.
2. `visibility.md` — objective line of sight and present-time faction visibility.
3. `effects-and-vitality.md` — deterministic healing/damage vitality effects.
4. `direct-attacks.md` — guaranteed-hit direct attacks with range and terrain sight.
5. `encounter-rounds.md` — valid round carry-forward and invalid-plan behavior.
6. `objectives.md` — eliminate-all-opponents victory and future objective directions.
7. `action-points.md` — per-round action-point budget validation.
8. `posture.md` — standing, crouched, and prone posture transitions.
9. `terrain-protection.md` — generic cover and concealment terrain data.
10. `overwatch.md` — one-shot movement-triggered watch cones.

## Planned chapters

1. Round structure and timeline
2. Units, states, facing, and distance
3. Movement and occupancy
4. Visibility and faction knowledge
5. Aiming, attacks, cover, and damage
6. Reactions and interrupts
7. Objectives and scenario outcomes
8. Unit progression and upgrades (post-MVP)
9. Replay, explanations, and known-information rules

## Rule record template

```markdown
### MOV-RES-001: Simultaneous arrival conflict

Status: accepted | proposed | deprecated
Player statement: …
Inputs: …
Resolution: …
Calculation/example: …
Events emitted: …
Invalid cases: …
Tests/fixtures: …
Decision reference: …
Change history: …
```

No rule reaches `accepted` without explicit examples and automated coverage.

Implemented rule changes are summarized in `docs/quality/rule-changelog.md` and mapped to tests/replays in `docs/quality/rule-traceability.md`.
