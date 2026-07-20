# Rulebook System

This folder is the living, player-readable rulebook. It describes accepted behavior, not implementation guesses. Each chapter references stable rule IDs and calculation examples; technical implementation details live in `docs/technical-design/`.

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
