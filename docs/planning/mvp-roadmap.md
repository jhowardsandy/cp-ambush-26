# Tactical Strategy Game Roadmap

Status: living roadmap, reconciled with implemented foundation on 2026-07-20.

## Product outcome

The MVP is a desktop tactical vertical slice in which a player plans actions for four units, an opposing faction executes a deterministic plan, and the player watches one fully explainable round resolve on a small grid map. It must be enjoyable enough for human playtesting and trustworthy enough that every material outcome can be reproduced, inspected, and tested without the Unity presentation.

The MVP proves the engine, not a large campaign or a finished art direction.

## Delivery principles

- Rules first, presentation second: a visual feature cannot introduce untestable outcomes.
- Each new rule has a stable rule ID, prose specification, inputs/outputs, calculation examples, automated tests, replay fixtures, and a change note.
- A milestone is not complete until its rules can be exercised headlessly and its player-facing behavior is explainable.
- Major unresolved game decisions are recorded before implementation; no rule is inferred solely from animation or UI behavior.

## Roadmap

| Phase | Outcome | Rule-engineering exit criteria |
| --- | --- | --- |
| 0. Foundation | Deterministic round timeline | Complete: core, event log, checksum, replay serialization, 10 Edit Mode tests. |
| 1. Spatial movement | Units traverse a grid predictably | Accepted movement-conflict policy; path, speed, reservation, swap, crossing, and invalidation tests; golden replays. |
| 2. Perception | Factions have bounded information | LOS/visibility algorithms, faction knowledge model, reveal/loss explanations, boundary maps, symmetry/property tests. |
| 3. Combat kernel | Aim and attacks have explainable results | Weapon profiles, range, cover, hit/damage calculation sheets, seeded rolls, exhaustive boundary tests, calculation breakdown events. |
| 4. Reactions | Timed interrupts work consistently | Trigger, priority, delay, interruption/resume policy, conflict matrix, deterministic multi-trigger tests. |
| 5. Scenario loop | A complete tactical encounter exists | Scenario foundation implemented: reusable map/initial-state/content-version data, bounds validation, and replay serialization. Add deployment, objectives, deterministic conventional enemy planner, and replay fixtures. |
| 6. Planning and playback | Humans can play and inspect it | First movement/effects sandbox scene and multi-round encounter-state core implemented: placeholder grid/tokens, deterministic resolve/reset controls, event overlay, checksum, and valid-round state carry-forward. Add unit selection, order editing, timeline playback, validation feedback, and inspector. |
| 7. MVP hardening | Playtest-ready vertical slice | Regression suite, seed corpus, balance harness, telemetry schema, accessibility pass, known-rules manual, playtest protocol. |
| 8. MVP release candidate | One defensible, replayable prototype build | Clean install/build, scenario acceptance suite, human-playtest findings triaged, documented release notes and rollback build. |

## Current implementation position

| Phase | Current position | What remains before the phase outcome is complete |
| --- | --- | --- |
| 0. Foundation | **Complete** | Keep compatibility and replay guarantees intact as the core grows. |
| 1. Spatial movement | **Strong foundation** | Movement modifiers, richer reservations, larger-map performance, and additional golden/property coverage. |
| 2. Perception | **Foundation implemented** | Per-unit data-defined vision, faction memory, reveal/loss events, fog-of-war presentation, facing/elevation, and concealment. |
| 3. Combat kernel | **First direct-fire slice implemented** | Data-defined unit/weapon catalogs, seeded accuracy, cover, armor, ammunition, alternate delivery types, and balance coverage. |
| 4. Reactions | **Not started** | Accepted trigger/priority/interruption policy and deterministic resolver implementation. |
| 5. Scenario loop | **Foundation implemented** | Four-versus-four original scenario, deployment, objectives, PvE enemy planner, acceptance fixtures, and scenario outcomes. |
| 6. Planning and playback | **Proof surface implemented** | Unit selection, player-authored order editing, target selection, validation feedback, timeline controls, and inspector. |
| 7. MVP hardening | **Not started** | Regression corpus, balance harness, telemetry, accessibility, manual, and structured playtests. |
| 8. MVP release candidate | **Not started** | Build/distribution validation, release notes, and playtest-led defect closure. |

## Next stretch: from proof surface to playable vertical slice

1. **Player order authoring:** select a friendly unit, choose a legal move/action, preview its timing/validation, and submit it as the player command bundle.
2. **Scenario loop:** expand the two-unit demonstration into an original four-versus-four map with deployment and a simple objective.
3. **PvE intelligence:** produce deterministic, explainable enemy command bundles from the same state/visibility contract.
4. **Combat depth:** add data-defined unit and weapon profiles, then seeded accuracy, cover, armor, ammunition, and additional delivery types one accepted rule at a time.
5. **Perception and reactions:** add memory/fog-of-war presentation, then reaction rules after their conflict policy is accepted.
6. **Hardening and playtests:** automate scenario batches and begin structured human testing once the authored-order loop is usable.

## Phase detail

### Phase 1: Spatial movement

Before code, accept answers to these questions: What is a path? When does a unit enter/leave a tile? Are tiles reserved? Can allies swap? Can enemies cross? What happens when a destination becomes occupied? Does moving rotate facing? How do stance and suppression later modify speed?

Deliverables: movement rulebook chapter, data model, event types, movement visualizer, table-driven unit tests, property tests, and at least ten named golden replays covering conflicts and boundary ticks.

### Phase 2: Perception

Separate objective world state from each faction's knowledge. Start with facing, sight range, opaque blockers, visibility gained/lost events, and remembered contacts. Defer sound, lighting, smoke, and camouflage unless a tested design requires them.

### Phase 3: Combat kernel

Introduce weapon/action definitions as versioned data. Every attack result must carry a human-readable breakdown: base value, modifiers, clamp, seeded roll, and applied result. No hidden random call is permitted.

### Phase 4: Reactions

Reactions are a resolver feature, not a presentation callback. Define trigger evaluation moments, detection prerequisites, reaction delay, tie-break ordering, resource cost, and whether interrupted actions resume. Add a conflict matrix before implementation.

### Phase 5–6: Playable vertical slice

Create one original scenario with placeholder assets, four player units, four enemy units, one objective, basic conventional deterministic AI, planning validation, playback, pause/speed controls, and an inspector that answers “what happened and why?”

PvE is the first complete game mode: deterministic enemy intelligence generates the opposing faction's command bundle from the same observable state and rule engine available to the player. Its decision inputs, seed, content version, selected orders, and explanation must be replayable.

PvP is a later expansion built on the same round boundary: each human submits orders for a faction, server or peer coordination validates that both submissions are present, then the shared deterministic resolver produces the sole authoritative result. Hidden-information policy, disconnect/reconnect, anti-cheat, synchronization, matchmaking, and networking are explicitly deferred until the single-device PvE loop is proven.

### Phase 7–8: Hardening and MVP release

Run scripted simulation batches over the scenario corpus; preserve every regression seed. Conduct structured human playtests separately from mathematical/rule verification. Address rule defects before tuning presentation polish.

## Beyond MVP

1. Campaign layer: persistent units, injuries, original equipment, progression/upgrades, and consequence tracking. Each upgrade must be a data-defined stat transformation with before/after calculations and migration tests.
2. Content tooling: scenario editor, data validator, replay inspector, and balance dashboard.
3. Expanded tactical systems: doors, elevation, sound, suppression, morale, objectives, and environmental effects.
4. Setting packages: historical-inspired content and an **original fantasy** package with original creatures, terminology, abilities, lore, and visual identity.
5. Optional PvP multiplayer (initially asynchronous or hot-seat, then networked only after simulation/content-version validation is mature). It reuses the command-bundle and shared-resolver contract rather than a separate ruleset.
6. Mobile adaptation after the desktop tactical loop is proven.

## Reusable-engine boundary

The reusable kernel owns time, grid/world geometry, unit state, command validation, timeline scheduling, events, deterministic randomness, replay, checksums, and generic effect application. A setting package owns definitions and content: actions, weapons, abilities, species/archetypes, equipment, scenarios, UI language, and art.

An original fantasy game can therefore reuse the engine for maps, units, attacks, facing, perception, tactical timing, and strategy while supplying its own original warriors, archers, spellcasters, creatures, abilities, and world. It must not reuse protected names, settings, rule text, monsters, spells, or branding from tabletop products.
