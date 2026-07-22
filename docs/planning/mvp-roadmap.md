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
| 4. Reactions | Timed interrupts work consistently | Trigger, priority, delay, interruption/resume policy, watched-zone/facing policy, reaction-consumption policy, conflict matrix, deterministic multi-trigger tests. |
| 5. Scenario loop | A complete tactical encounter exists | Scenario/encounter/objective foundation implemented: reusable map/initial-state/content-version/objective data, bounds validation, replay serialization, and eliminate-all-opponents outcome. Add deployment, richer objectives, deterministic conventional enemy planner, and replay fixtures. |
| 6. Planning and playback | Humans can play and inspect it | First movement/effects sandbox scene and multi-round encounter-state core implemented: placeholder grid/tokens, deterministic resolve/reset controls, event overlay, checksum, and valid-round state carry-forward. Add unit selection, order editing, timeline playback, validation feedback, and inspector. |
| 7. MVP hardening | Playtest-ready vertical slice | Regression suite, seed corpus, balance harness, telemetry schema, accessibility pass, known-rules manual, playtest protocol. |
| 8. MVP release candidate | One defensible, replayable prototype build | Clean install/build, scenario acceptance suite, human-playtest findings triaged, documented release notes and rollback build. |

## Current implementation position

| Phase | Current position | What remains before the phase outcome is complete |
| --- | --- | --- |
| 0. Foundation | **Complete** | Keep compatibility and replay guarantees intact as the core grows. |
| 1. Spatial movement | **Strong foundation** | Movement modifiers, richer reservations, larger-map performance, and additional golden/property coverage. |
| 2. Perception | **Foundation implemented** | Per-unit data-defined vision, faction memory, reveal/loss events, fog-of-war presentation, facing/elevation, and concealment. |
| 3. Combat kernel | **Direct fire + seeded accuracy + ammunition + armor implemented** | Data-defined unit/weapon catalogs, alternate delivery types, and balance coverage. |
| 4. Reactions | **First slice implemented** | One-shot overwatch works; expand trigger/priority/interruption, posture/scoped, and resource policy only through accepted rules. |
| 5. Scenario loop | **Playable graybox implemented** | Riverside Crossing 4v4 has elimination plus hold-area objectives, terrain, PvE, auto-play, acceptance coverage, and a human protocol. Add deployment, extraction/rescue, and scenario scripting. |
| 6. Planning and playback | **Playable graybox implemented** | Multi-unit route/action sequences, target selection, previews, feedback, event log, and inspectable per-unit doctrine auto-orders work. Add posture controls, richer plan editing, pause/speed, and replay inspection. |
| 7. MVP hardening | **Started** | 84 deterministic tests, rulebook, traceability, acceptance fixture, and human protocol exist. Add seed corpus, balance harness, accessibility, recorded playtests, and release checks. |
| 8. MVP release candidate | **Not started** | Build/distribution validation, release notes, and playtest-led defect closure. |

## Next stretch: from proof surface to playable vertical slice

1. **Combat depth—next:** keep accuracy modifiers such as posture, range bands, concealment, and skills deferred until their own calculation contracts are accepted; then add alternate delivery types only with their own timeline/area contracts.
2. **Perception and posture:** add one explicit posture interaction, then faction memory/reveal/loss events and finished fog-of-war presentation; defer elevation until its grid policy is accepted.
3. **Scenario/objective expansion:** add extraction or rescue plus a small scenario-scripting/reinforcement foundation before increasing campaign scope.
4. **PvE maturation:** extend objective/role/doctrine planning to multi-action plans, overwatch policy, target priorities, and tuning only with explicit decision contracts.
5. **Player experience:** pause/speed controls, richer plan editing and replay inspection, accessibility, basic original terrain/unit art, sound, and animation passes.
6. **Hardening and release proof:** scenario batches, seed corpus, balance harness, packaged build, release notes, and recorded playtests.

The portable `iron-timeline-squad-skirmish-01` fixture now provides the initial 16×12 four-versus-four roster, terrain, map areas, and elimination objective for this next scenario-loop work. It is validated content, not yet a fully playable Unity scenario.

## Defined post-armor engine slices

These are the seven currently bounded, rule-first slices. They are not a promise to finish the engine before further playtesting; each can be reordered when a scenario exposes a higher-value rule question.

1. **Posture interaction:** make one stance effect explicit (start with crouch changing concealment or cover) with AP/timing and calculation tests.
2. **Faction memory:** **implemented** — retain observed contacts, emit reveal/loss events, and expose a presentation-ready knowledge snapshot without giving PvE hidden knowledge.
3. **Reaction expansion:** formalize trigger priority, interruption/resume, and scoped/posture prerequisites before adding more overwatch variants.
4. **Rescue/extraction objectives:** add discoverable rescue targets and extraction completion using named map areas and authoritative progress events.
5. **Scenario scripting/reinforcements:** data-defined scheduled spawns, capture-to-disable spawn points, and deterministic scenario triggers.
6. **PvE tactical maturation:** multi-action plans, overwatch policy, target priority, role coordination, and explainable policy tests.
7. **Alternate delivery foundation:** accept a generic projectile/area-effect contract for grenades, artillery, fireballs, arrows, and similar setting content before adding visual physics.

Cross-cutting work proceeds beside those slices: richer planning/replay inspection, scenario acceptance batches and seed corpus, balance harness, accessibility, art/audio/animation, campaign progression, and eventual PvP coordination. Those are substantial, but they are not all core-rule slices.

## Phase detail

### Phase 1: Spatial movement

Before code, accept answers to these questions: What is a path? When does a unit enter/leave a tile? Are tiles reserved? Can allies swap? Can enemies cross? What happens when a destination becomes occupied? Does moving rotate facing? How do stance and suppression later modify speed?

Deliverables: movement rulebook chapter, data model, event types, movement visualizer, table-driven unit tests, property tests, and at least ten named golden replays covering conflicts and boundary ticks.

### Phase 2: Perception

Separate objective world state from each faction's knowledge. Start with facing, sight range, opaque blockers, visibility gained/lost events, and remembered contacts. Defer sound, lighting, smoke, and camouflage unless a tested design requires them.

### Phase 3: Combat kernel

Introduce weapon/action definitions as versioned data. Every attack result must carry a human-readable breakdown: base value, modifiers, clamp, seeded roll, and applied result. No hidden random call is permitted.

### Phase 4: Reactions

Reactions are a resolver feature, not a presentation callback. Define trigger evaluation moments, detection prerequisites, watched-zone/facing geometry, reaction delay, tie-break ordering, one-shot consumption policy, resource cost, and whether interrupted actions resume. Add a conflict matrix before implementation.

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
