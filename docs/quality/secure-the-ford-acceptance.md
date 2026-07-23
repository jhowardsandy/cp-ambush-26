# Secure the Ford Acceptance Fixture

Status: implemented deterministic engine acceptance fixture.

`SecureTheFordShowcase.Run` is the first guaranteed-completion tactical fixture. It is intentionally separate from Riverside Crossing free auto-play: free auto-play probes PvE behavior and may stop at a safety cap; this fixture is an exact repeatable proof of an accepted scenario outcome.

## Scenario data

- Map: 10×6 with named `central-ford`, `blue-signal`, and `red-reinforcement-entry` areas.
- Blue: a Rifleman already securing the Ford and a Combat Medic holding the signal site.
- Red: a vulnerable skirmisher in legal grenade range plus a reserve unit outside the blast.
- Objective: Blue holds `central-ford` uncontested for three completed rounds.
- Pressure: a Red Rifleman is scheduled for round two at the reinforcement entry, but Blue signal control disables that schedule.

## Deterministic sequence

1. Round one: Blue Rifleman legally targets the Red skirmisher with `fragmentation-grenade`; the area attack produces a normal authoritative detonation/attack event.
2. Round two: the engine evaluates the scheduled Red reinforcement and emits `ReinforcementDisabled` because Blue controls `blue-signal`.
3. Round three: the Ford reaches its third uncontested completed hold round and the objective evaluator declares Blue the winner.

## Acceptance evidence

`Secure_the_ford_showcase_reaches_blue_hold_victory_with_grenade_and_disabled_reinforcement` verifies all three milestones and the terminal outcome. It uses ordinary encounter rounds, command bundles, inventory, area delivery, reinforcement logic, objective progress, deterministic seeds, and event records—no test-only outcome shortcut.

## Presentation handoff

The current Unity Riverside Crossing scene remains the open player/PvE sandbox. The next presentation slice should expose Secure the Ford as a separate “Showcase replay” entry that loads this fixture, renders its event sequence, and displays its final outcome/checksum. Do not replace free auto-play with this fixture; both modes answer different test questions.
