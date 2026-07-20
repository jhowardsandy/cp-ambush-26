# Tactical Game Project Blueprint

## Working Purpose

This document is the durable source of truth for a new tactical strategy game project inspired by the design philosophy of the Commodore 64 game *Computer Ambush*.

The intent is not to reproduce copyrighted names, art, text, maps, or code. The project should instead create a modern tactical engine and original game experience that preserves the qualities that made that style of game compelling:

- Individual characters matter.
- Orders are planned rather than executed as twitch actions.
- Friendly and enemy actions unfold over the same timeline.
- Time, visibility, position, facing, cover, interruption, injury, morale, and uncertainty matter.
- The player can understand why events happened.
- The simulation is deterministic, testable, replayable, and reusable.

The long-term goal is to build one reusable tactical simulation engine that can support multiple game settings.

Initial settings under consideration:

1. A historical or WWII-inspired squad tactical game.
2. An original fantasy tactical role-playing game inspired by tabletop role-playing experiences.
3. Potential future settings such as science fiction, modern tactical operations, or survival scenarios.

The fantasy game must use original terminology, lore, mechanics, characters, monsters, visual identity, and setting. It may be inspired by tabletop role-playing principles, but it should not depend on protected Dungeons & Dragons names, settings, spells, monsters, logos, text, or brand identity.

---

# 1. Product Vision

The game should make the player feel as though they are commanding a small group of real individuals during a dangerous tactical encounter.

The player does not merely choose "move" and "attack." The player plans what each character will attempt to do across several seconds while enemies act at the same time.

The tension comes from incomplete knowledge and imperfect plans.

A character may begin crossing a room believing it is empty, only for an enemy to appear in a doorway halfway through the movement. A prepared ally may react. A ranged attack may be delayed. A spell may be interrupted. A wounded character may fail to complete an order. A door may block line of sight until it opens. A loud action may reveal a unit that had not been seen.

The resulting sequence must feel understandable rather than arbitrary.

After resolution, the player should be able to determine:

- What each unit attempted.
- When each action began.
- How long each action took.
- Which action completed first.
- What changed the original plan.
- Which units saw or heard an event.
- Why an attack succeeded or failed.
- Whether cover, movement, facing, suppression, injury, or morale affected the outcome.
- What information was available to each side at the time.

The game should reward:

- Planning.
- Positioning.
- Coordination.
- Timing.
- Reconnaissance.
- Contingency planning.
- Adaptation.
- Judicious risk.

The game should not primarily reward:

- Fast reflexes.
- Rapid tapping.
- Memorizing arbitrary bonuses.
- Exploiting opaque systems.
- Guessing how hidden formulas work.

---

# 2. Core Experience

## 2.1 Planning and Resolution

Each tactical round represents a short span of simulated time, such as five or ten seconds.

The round is divided into discrete simulation ticks.

A preliminary prototype may use:

- 100 ticks per round.
- One round representing ten seconds.
- One tick representing one tenth of a second.

These numbers are placeholders and must remain configurable.

Players issue commands to individual units during a planning phase. Enemy units also produce plans. The simulation then resolves all plans across the same shared timeline.

Examples of timed actions:

- Wait.
- Rotate.
- Stand.
- Crouch.
- Go prone.
- Move.
- Sprint.
- Open or close a door.
- Aim.
- Fire.
- Reload.
- Throw an item.
- Use an item.
- Take cover.
- Guard a direction.
- Prepare a reaction.
- Cast a spell.
- Interact with an objective.

An illustrative timeline:

```text
Move three tiles        ticks 0-35
Rotate toward doorway   ticks 35-40
Aim                     ticks 40-58
Fire                    ticks 58-62
Return to cover         ticks 62-90
Wait                    ticks 90-100
```

All units resolve together.

The timeline engine must account for:

- Simultaneous movement.
- Movement conflicts.
- Occupancy conflicts.
- Line-of-sight changes.
- Newly visible enemies.
- Reactions and interrupts.
- Attack timing.
- Projectile or effect timing where applicable.
- Doors opening or closing.
- Environmental changes.
- Injury, incapacitation, panic, suppression, or status effects.
- Actions that become invalid before they complete.

## 2.2 Playback

The player should watch the resolved round unfold as a clear tactical replay.

Presentation should interpolate and animate deterministic simulation events. Rendering must never determine game results.

The player should be able to:

- Pause playback.
- Speed playback up or slow it down.
- Scrub the timeline where feasible.
- Select a unit and inspect its planned actions.
- Inspect interruptions or failed actions.
- Review combat calculations in an understandable form.
- Distinguish known information from information revealed only after resolution.

## 2.3 Limited Information

The game should model uncertainty.

Potential information states:

- Never observed.
- Previously observed.
- Currently visible.
- Heard but not seen.
- Suspected.
- Confirmed.
- Lost contact.

The simulation should track objective truth independently from each faction's knowledge.

A player should not automatically know an enemy's exact position, wounds, ammunition, intent, or identity unless game rules justify that knowledge.

---

# 3. Core Design Principles

1. Individual characters matter.
2. Actions consume measurable time.
3. Friendly and enemy actions resolve simultaneously.
4. Visibility, facing, cover, sound, and interruption matter.
5. The player plans actions and then watches the timeline resolve.
6. Rules must be deterministic and testable.
7. Game content must be data-driven.
8. The simulation must not depend on Unity rendering code.
9. The same engine must support multiple settings.
10. The smallest playable version comes before broad content.
11. Rules should be explainable to the player.
12. Randomness should be seeded, reproducible, and limited to places where uncertainty improves play.
13. Presentation may dramatize events but may not alter simulation outcomes.
14. Agents must not silently invent major rules.
15. Existing implementation is not more authoritative than accepted design decisions.

---

# 4. Recommended Technology Stack

## 4.1 Game Engine

Recommended engine:

- Unity 6 or the current stable Unity LTS version available when the project is initialized.
- C#.

Reasons:

- Strong Android and iOS support.
- Mature editor and tooling.
- Large ecosystem.
- Good support for 2D, 2.5D, and 3D presentation.
- Familiarity for developers with Java or enterprise C# experience.
- Suitable for custom editor tooling.
- Supports desktop prototypes before mobile release.

Unity Personal is generally suitable for an early solo or independent project, subject to Unity's current licensing and revenue thresholds at the time of use. Licensing must be rechecked before publication or commercial launch.

## 4.2 Core Technologies

- Language: C#.
- Unit tests: NUnit or Unity Test Framework where appropriate.
- Source control: Git and GitHub.
- Data formats: JSON for portable content definitions; ScriptableObjects may be used as Unity authoring assets.
- Local campaign persistence: JSON, binary snapshots, or SQLite depending on complexity.
- CI: GitHub Actions and/or Unity Build Automation.
- Crash reporting: Unity diagnostics, Sentry, or another mobile-compatible service.
- Backend later: PlayFab, Firebase, Supabase, or a small custom service.

## 4.3 Art and Content Tools

Recommended tools:

- ChatGPT image generation for concept art, portraits, icons, loading art, marketing images, reference sheets, and visual exploration.
- Blender for 3D modeling, rigging, rendering, camera-consistent sprites, props, and environments.
- Mixamo or equivalent for starter humanoid animations, subject to licensing and suitability.
- Aseprite for sprite cleanup and pixel-art workflows.
- Affinity Photo, Photoshop, GIMP, or Krita for image cleanup.
- Audacity or Reaper for audio editing.
- AI coding agents for scaffolding, implementation, tests, documentation, import tools, and editor tools.

AI-generated assets must be reviewed for:

- Visual consistency.
- Commercial-use terms.
- Trademark or copyrighted character resemblance.
- Anatomy and equipment errors.
- Repeated or malformed details.
- Mobile readability.
- Transparency and edge cleanup.

---

# 5. Required Architecture

## 5.1 Separation of Simulation and Presentation

The core simulation must be a plain deterministic C# layer that can execute without loading a Unity scene.

Suggested structure:

```text
/src or /Assets
  /Game.Core
    /World
    /Units
    /Commands
    /Actions
    /Timeline
    /Movement
    /Visibility
    /Combat
    /Effects
    /Objectives
    /AI
    /Scenarios
    /Serialization
    /Replay

  /Game.Presentation.Unity
    /Scenes
    /Rendering
    /Animation
    /Input
    /UI
    /Audio
    /Camera
    /Mobile

  /Game.Editor
    /ScenarioEditor
    /MapTools
    /DataValidation
    /ReplayInspector

  /Game.Tests
    /Unit
    /Integration
    /Determinism
    /Replay
```

The exact Unity folder structure may differ, but the dependency direction is mandatory:

```text
Presentation -> Core
Editor       -> Core
Tests        -> Core
Core         -X-> Presentation
```

## 5.2 Forbidden Core Dependencies

The simulation layer must not depend on:

- GameObject.
- MonoBehaviour.
- Transform.
- Animator.
- Unity physics.
- Unity scene objects.
- Frame time.
- Rendering state.
- Device input.
- Wall-clock time.
- Network response timing.

Unity renders the simulation. Unity does not define the simulation.

## 5.3 Determinism

All tactical outcomes must be reproducible from:

- Initial game state.
- Player command bundle.
- Enemy command bundle.
- Random seed.
- Simulation version.
- Content version.

The simulation must use a controlled seeded random-number provider.

Do not call uncontrolled random APIs from domain logic.

Every resolved round should be capable of producing:

- A final state.
- An ordered event log.
- A state checksum.
- Replay metadata.
- Validation diagnostics.

## 5.4 Event-Driven Resolution

The simulation should emit explicit domain events.

Examples:

- RoundStarted.
- ActionQueued.
- ActionStarted.
- UnitRotated.
- UnitEnteredTile.
- UnitExitedTile.
- DoorOpened.
- VisibilityGained.
- VisibilityLost.
- ReactionTriggered.
- AttackStarted.
- AttackResolved.
- DamageApplied.
- UnitSuppressed.
- UnitIncapacitated.
- ObjectiveProgressed.
- ActionInterrupted.
- ActionFailed.
- RoundCompleted.

These events support:

- Playback.
- Debugging.
- Player explanations.
- Tests.
- Replays.
- Multiplayer validation.
- Analytics.

---

# 6. Preliminary Domain Model

These are starting concepts, not final implementations.

## 6.1 Tactical Action

```csharp
public sealed record TacticalAction(
    Guid ActionId,
    Guid UnitId,
    ActionType Type,
    int StartTick,
    int DurationTicks,
    GridPosition? Destination,
    Guid? TargetId,
    IReadOnlyDictionary<string, string>? Parameters
);
```

## 6.2 Unit State

A unit may include:

- Unit ID.
- Faction ID.
- Position.
- Facing.
- Stance.
- Movement capability.
- Action capability.
- Vision capability.
- Hearing capability.
- Health or wound state.
- Morale state.
- Suppression state.
- Equipment.
- Ammunition or charges.
- Status effects.
- Skill values.
- Tags or traits.

## 6.3 World State

The world may include:

- Grid dimensions.
- Terrain cells.
- Elevation.
- Walls.
- Doors.
- Windows.
- Cover edges.
- Light values.
- Sound propagation values.
- Hazards.
- Objectives.
- Units.
- Items.
- Environmental effects.
- Faction knowledge states.

## 6.4 Command Bundle

A command bundle should represent one faction's submitted plan for a round.

It should support:

- Multiple units.
- Ordered actions per unit.
- Validation before resolution.
- Explicit unused time.
- Conditional or reaction actions later.
- Versioning.
- Serialization.

## 6.5 Replay Record

A replay record should contain:

- Initial state snapshot or reference.
- Command bundles.
- Seed.
- Simulation version.
- Content version.
- Ordered event log.
- Final checksum.
- Optional intermediate checkpoints.

---

# 7. Game Systems Roadmap

## 7.1 Milestone 1: Deterministic Timeline

Goals:

- Plain C# simulation project.
- Configurable ticks per round.
- Unit model.
- Command model.
- Wait, rotate, move, aim, and attack action types.
- Shared timeline.
- Action start and completion events.
- Seeded random provider.
- Event log.
- Deterministic replay test.

Do not implement full combat, pathfinding, advanced visibility, or final UI yet.

## 7.2 Milestone 2: Grid Movement

Goals:

- Grid coordinates.
- Terrain occupancy.
- Movement paths.
- Movement speed.
- Facing changes.
- Simultaneous movement conflicts.
- Tile reservation policy.
- Swap and crossing rules.
- Action interruption.
- Invalid destination handling.

Important design questions:

- Can two friendly units cross through the same tile during different ticks?
- Can opposing units pass one another?
- Can units swap positions?
- What happens if a destination becomes occupied?
- Does movement automatically rotate the unit?
- Does stance affect speed?

Major rules must be documented before implementation.

## 7.3 Milestone 3: Visibility

Goals:

- Facing.
- Vision arc.
- Range.
- Obstacles.
- Doors and windows.
- Elevation.
- Current visibility state.
- Newly observed events.
- Lost contact.
- Faction knowledge.
- Fog of war.

Later possibilities:

- Lighting.
- Smoke.
- Concealment.
- Camouflage.
- Hearing.
- Noise markers.

## 7.4 Milestone 4: Combat

Goals:

- Aim.
- Attack timing.
- Weapon profiles.
- Range effects.
- Cover.
- Accuracy.
- Hit resolution.
- Damage.
- Incapacitation.
- Ammunition.
- Reaction fire.

Combat should surface explainable results.

Example explanation:

```text
Attack missed.
Base accuracy: 72%
Target movement: -14%
Partial cover: -18%
Shooter suppression: -9%
Final chance: 31%
Seeded roll: 47
```

Whether exact percentages are shown is a design decision, but the player must have a meaningful explanation.

## 7.5 Milestone 5: Reactions and Interrupts

Potential reaction types:

- Fire when an enemy enters a watched area.
- Attack when an enemy becomes visible.
- Defend when approached.
- Dodge when targeted.
- Counterspell or interrupt casting.
- Opportunity attack.
- Close a door.
- Dive for cover.

Reaction rules must define:

- Trigger conditions.
- Detection requirements.
- Reaction delay.
- Priority.
- Conflicts.
- Resource costs.
- Whether the original plan resumes.

## 7.6 Milestone 6: Scenario Objectives

Examples:

- Eliminate hostile units.
- Reach an exit.
- Rescue a captive.
- Prevent an escape.
- Hold an area.
- Survive a number of rounds.
- Retrieve an item.
- Disable an objective.
- Escort a unit.
- Remain undetected.

Objectives must be data-driven.

## 7.7 Milestone 7: Enemy AI

Runtime enemy AI should use conventional deterministic game algorithms rather than LLM calls.

Potential tools:

- A* pathfinding.
- Utility scoring.
- Threat maps.
- Influence maps.
- Goal-oriented action planning.
- Behavior trees where useful.
- Tactical role assignments.

Enemy planning should operate from the enemy faction's knowledge, not objective truth.

LLMs may help create scenarios, content, dialogue, tutorials, or behavior tests during development. They should not determine tactical outcomes during ordinary gameplay.

## 7.8 Milestone 8: Campaign Layer

Only after the tactical prototype is compelling.

Possible campaign systems:

- Persistent characters.
- Injuries.
- Experience.
- Equipment.
- Recruitment.
- Mission choice.
- Resource management.
- Relationships.
- Faction reputation.
- Story consequences.

---

# 8. Initial Prototype Scope

The first playable prototype should intentionally use placeholder presentation.

Required:

- One square-grid map.
- Four player-controlled units.
- Four enemy units.
- Move.
- Face or rotate.
- Wait.
- Aim.
- Ranged attack.
- Line of sight.
- Basic cover.
- Simultaneous timeline resolution.
- Basic reactions when enemies become visible.
- Replay of the resolved round.
- One scenario objective.
- Colored shapes, capsules, or simple tokens instead of final art.

Do not add initially:

- Character progression.
- Deep inventory.
- Crafting.
- Multiplayer.
- Procedural generation.
- Final art.
- Large campaigns.
- Dialogue systems.
- Monetization.
- Live-service features.
- Runtime generative AI.
- Complex weather.
- Vehicles.
- Large maps.

The initial proof is whether the following kind of event feels compelling and understandable:

> The ranger begins crossing the doorway at tick 30. An enemy steps into view at tick 24. The ranger's ally has a prepared reaction and fires at tick 27. The enemy becomes suppressed, changing its movement speed, which prevents it from reaching cover before the ranger completes the crossing.

---

# 9. Multi-Setting Engine Strategy

The tactical engine should not hard-code WWII or fantasy assumptions into shared systems.

Shared engine concepts:

- Units.
- Actions.
- Time.
- Movement.
- Facing.
- Perception.
- Cover.
- Reactions.
- Effects.
- Objectives.
- Equipment definitions.
- Damage events.
- Status effects.
- Scenario data.

Setting modules may define:

## Historical or WWII-Inspired Module

- Firearms.
- Ammunition.
- Reloading.
- Suppression.
- Grenades.
- Wounds.
- Soldier experience.
- Morale.
- Weapon handling.
- Historical environments.

## Original Fantasy Module

- Melee reach.
- Armor.
- Shields.
- Spell casting time.
- Concentration.
- Stealth.
- Light and darkness.
- Magical effects.
- Conditions.
- Reactions.
- Opportunity attacks.
- Character advancement.
- Original classes, archetypes, creatures, spells, and lore.

A content definition should be able to supply different action profiles without changing the core timeline engine.

---

# 10. Data-Driven Content

Scenarios, weapons, abilities, units, terrain, and effects should be defined as data wherever practical.

Illustrative scenario JSON:

```json
{
  "id": "village_ambush_01",
  "displayName": "The East Road",
  "map": "village_night",
  "turnDurationTicks": 100,
  "playerDeploymentZones": ["west_alley"],
  "enemyDeploymentZones": ["market_square"],
  "objectives": [
    {
      "type": "prevent_escape",
      "unitTag": "enemy_courier",
      "exitZone": "east_road"
    }
  ]
}
```

Illustrative weapon data:

```json
{
  "id": "carbine_standard",
  "displayName": "Standard Carbine",
  "tags": ["ranged", "two_handed"],
  "aimTicks": 18,
  "attackTicks": 4,
  "reloadTicks": 32,
  "effectiveRange": 10,
  "maximumRange": 18,
  "baseAccuracy": 0.72,
  "damageProfile": "ballistic_medium",
  "magazineSize": 10
}
```

All content data must support:

- Schema validation.
- Versioning.
- Clear IDs.
- Editor-friendly display names.
- Localization later.
- Automated linting.

---

# 11. Scenario Editor Vision

A custom Unity editor should eventually allow designers to:

- Paint terrain.
- Set elevation.
- Place walls, doors, windows, and cover.
- Define deployment zones.
- Place objectives.
- Configure patrol routes.
- Add triggers.
- Configure lighting.
- Preview line of sight.
- Preview sound propagation.
- Validate unreachable areas.
- Run the scenario immediately.
- Watch AI-versus-AI simulations.
- Inspect replay events.

The editor is a later milestone, but architecture should avoid blocking it.

---

# 12. Art Direction and Asset Pipeline

## 12.1 Recommended First Presentation

Use placeholder art for the prototype.

Potential production directions:

### Option A: Isometric 2D Pre-Rendered Characters

Workflow:

```text
AI concept art
    -> approved character reference sheet
    -> Blender model or licensed base model
    -> rig and animate
    -> render consistent directions
    -> sprite sheet cleanup
    -> Unity import
```

Benefits:

- Strong visual identity.
- Consistent camera.
- Shared rigs.
- Suitable for historical and fantasy settings.

### Option B: Illustrated Tactical Tokens

Each unit uses:

- Top-down or isometric token.
- Portrait.
- Equipment icons.
- Direction indicator.
- Simple attack and movement effects.

Benefits:

- Fastest path to polished visuals.
- Strong tabletop feeling.
- Lower animation burden.
- Good mobile readability.

### Option C: Stylized 3D Miniatures

Characters resemble painted tabletop miniatures on small diorama-like maps.

Benefits:

- Commercially attractive.
- Works across settings.
- Modest animation can still look intentional.
- Camera distance hides small imperfections.

## 12.2 Visual Bible

Before generating many assets, define:

- Camera angle.
- Projection type.
- Lighting direction.
- Character proportions.
- Color philosophy.
- Material style.
- Edge treatment.
- Background transparency rules.
- Mobile readability requirements.
- UI icon style.
- Environment scale.
- Weapon scale.
- Portrait framing.

Illustrative art-direction language:

```text
Painted tabletop miniatures viewed from a fixed 35-degree isometric camera.
Restrained realism with slightly exaggerated silhouettes for mobile readability.
Weathered equipment, readable weapons, and soft directional light from the upper left.
Characters use approximately 6.5-head proportions with enlarged hands and equipment.
Natural earth tones are paired with restrained class or faction accent colors.
```

## 12.3 AI Art Uses

Good uses:

- Concept exploration.
- Character portraits.
- Faction symbols.
- Equipment icons.
- Loading screens.
- Store art.
- Scenario illustrations.
- Reference sheets.
- Environment mood paintings.
- Marketing art.

Riskier uses requiring cleanup:

- Frame-by-frame animation.
- Exact turnarounds generated independently.
- Seamless tile sets.
- Repeated complex uniforms.
- Weapons requiring precise historical consistency.
- Hands interacting with equipment.

---

# 13. Audio Direction

Audio should communicate tactical state, not merely decorate it.

Important categories:

- Footsteps by surface.
- Doors.
- Weapon reports.
- Reloading.
- Armor impact.
- Spell preparation and release.
- Suppression or panic cues.
- Unit acknowledgements.
- Environmental ambience.
- Objective cues.
- Timeline playback cues.

Sound may also become part of game simulation through hearing and noise propagation.

Audio presentation and simulation data must remain separate.

---

# 14. AI-Assisted Development Workflow

AI coding agents are useful for:

- Project scaffolding.
- C# domain models.
- NUnit tests.
- Serialization.
- Importers.
- Data validators.
- Unity editor tooling.
- Documentation.
- Refactoring.
- Replay inspectors.
- Scenario templates.
- Balance simulation harnesses.
- Identifying edge cases.

Recommended workflow:

```text
Design discussion
    -> accepted design document
    -> implementation task specification
    -> coding agent plan
    -> implementation and tests
    -> review agent findings
    -> corrections
    -> documentation update
```

Do not rely on a single large prompt as the only source of project knowledge.

Project understanding must live in committed repository documents.

---

# 15. Repository Documentation Structure

Recommended starting structure:

```text
/docs
  /game-design
    vision.md
    core-game-loop.md
    tactical-rules.md
    fantasy-module.md
    historical-module.md
    terminology.md

  /technical-design
    architecture.md
    simulation-engine.md
    timeline-resolution.md
    movement.md
    visibility-and-los.md
    combat-system.md
    reactions.md
    save-and-replay.md
    content-schema.md

  /art
    art-direction.md
    asset-pipeline.md
    character-guidelines.md
    environment-guidelines.md

  /decisions
    0001-unity-and-csharp.md
    0002-deterministic-core.md
    0003-simultaneous-resolution.md
    0004-data-driven-content.md

  /planning
    prototype-backlog.md
    milestones.md
    risks.md

AGENTS.md
README.md
```

---

# 16. AGENTS.md Baseline

The coding agent should create an `AGENTS.md` containing at least the following principles.

## Project Vision

This project is a modern spiritual successor to the design philosophy of *Computer Ambush*.

The core experience is a detailed, squad-level tactical strategy game in which the player gives orders to individual characters and those orders resolve over a shared timeline.

The project will support multiple settings through one reusable tactical engine.

## Core Design Principles

1. Individual characters matter.
2. Actions consume measurable time.
3. Friendly and enemy actions resolve simultaneously.
4. Visibility, facing, cover, sound, and interruption matter.
5. The player plans actions, then watches the timeline resolve.
6. Rules must be deterministic and testable.
7. Game content must be data-driven.
8. The simulation must not depend on Unity rendering code.
9. The same engine must support multiple settings.
10. Build the smallest playable version before adding content.

## Technical Direction

- Engine: Unity.
- Language: C#.
- Tests: NUnit or Unity Test Framework.
- Core simulation: plain C# assemblies.
- Rendering and input: Unity-specific layer.
- Content: JSON and/or ScriptableObjects.
- Architecture: deterministic, event-driven simulation.
- Randomness: seeded and reproducible.
- Multiplayer: not part of the initial prototype.
- Runtime LLM calls: prohibited for tactical resolution.

## Architecture Rule

The simulation layer must not reference Unity presentation or scene types.

Unity renders the simulation. Unity does not define the simulation.

## Development Rules

Before implementing a feature:

1. Read the relevant design document.
2. State assumptions.
3. Identify affected domain models.
4. Add or update tests.
5. Preserve deterministic behavior.
6. Avoid Unity dependencies in core simulation.
7. Update documentation when behavior changes.

## Definition of Done

A feature is complete when:

- Its behavior is documented.
- Core logic has automated tests.
- Deterministic replay is preserved.
- Invalid states are handled.
- The feature works with placeholder presentation where applicable.
- No unrelated systems were added.

## Agent Behavior

Do not silently invent major game rules.

When a rule is unclear:

- Check design documents.
- Prefer the smallest implementation.
- Record assumptions.
- Flag significant design decisions for review.

Do not optimize prematurely.

Prioritize correctness, readability, determinism, and testability.

## Source-of-Truth Priority

When instructions conflict, use this order:

1. Direct task instructions from Justin.
2. `AGENTS.md`.
3. Accepted architecture decision records.
4. Technical design documents.
5. Game design documents.
6. Existing implementation.
7. Agent assumptions.

---

# 17. Architecture Decision Records

## ADR 0001: Unity and C#

### Status

Accepted for prototype.

### Decision

Use Unity and C# for the client and presentation layer.

### Reasons

- Mobile support.
- Familiar language.
- Mature tooling.
- Large ecosystem.
- Strong editor extension support.

### Consequences

- Unity-specific code must remain outside the deterministic core.
- Licensing must be reviewed before commercial release.

## ADR 0002: Deterministic Simulation Core

### Status

Accepted.

### Decision

All tactical outcomes will be resolved by a deterministic C# simulation using a seeded random-number provider.

The simulation must execute without loading a Unity scene.

### Reasons

- Automated testing.
- Reliable replay.
- Balance simulation.
- Save-game compatibility.
- Easier debugging.
- Shared engine across multiple games.
- Possible asynchronous multiplayer later.

### Consequences

- Unity physics will not determine gameplay outcomes.
- Animation may interpolate results but cannot change them.
- Randomness must be centralized and controlled.

## ADR 0003: Simultaneous Timeline Resolution

### Status

Accepted.

### Decision

Units from all factions resolve actions on one shared tactical timeline.

### Reasons

- It preserves the core identity of the game.
- Timing becomes a strategic resource.
- It creates emergent interactions.
- It supports reactions and interruption.

### Consequences

- Actions require explicit timing.
- Conflict-resolution rules must be deterministic.
- Playback and event logs are essential.

## ADR 0004: Data-Driven Content

### Status

Accepted.

### Decision

Scenarios, units, weapons, abilities, terrain, effects, and objectives should be defined as data wherever practical.

### Reasons

- Multiple game settings.
- Faster content creation.
- Easier balancing.
- Better tooling.
- Potential mod support.

### Consequences

- Schemas and validators are required.
- Content versions must be tracked.

---

# 18. Prototype Backlog

## Milestone 0: Repository Initialization

- [ ] Create Unity project using a stable supported version.
- [ ] Initialize Git repository.
- [ ] Add `.gitignore` suitable for Unity.
- [ ] Create solution and assembly definitions.
- [ ] Create `Game.Core`, `Game.Presentation.Unity`, `Game.Editor`, and `Game.Tests` boundaries.
- [ ] Add `README.md`.
- [ ] Add `AGENTS.md`.
- [ ] Add documentation folders.
- [ ] Copy this blueprint into `/docs/project-blueprint.md`.
- [ ] Add architecture decision records.
- [ ] Configure test execution.
- [ ] Create first CI workflow if credentials and Unity license setup permit.

## Milestone 1: Deterministic Timeline

- [ ] Game state model.
- [ ] Unit model.
- [ ] Action command model.
- [ ] Simulation clock.
- [ ] Configurable ticks per round.
- [ ] Action start and completion events.
- [ ] Seeded random provider.
- [ ] Replay event log.
- [ ] Final-state checksum.
- [ ] Unit tests.
- [ ] Deterministic replay test.

## Milestone 2: Grid Movement

- [ ] Grid position.
- [ ] Facing.
- [ ] Terrain occupancy.
- [ ] Movement path.
- [ ] Speed.
- [ ] Move event generation.
- [ ] Collision rules.
- [ ] Destination invalidation.
- [ ] Interruption.
- [ ] Tests for overlapping movement.

## Milestone 3: Minimal Presentation

- [ ] One test scene.
- [ ] Placeholder grid.
- [ ] Placeholder units.
- [ ] Camera controls.
- [ ] Unit selection.
- [ ] Queue movement.
- [ ] Start round.
- [ ] Playback events.
- [ ] Pause and speed control.

## Milestone 4: Visibility

- [ ] Facing arcs.
- [ ] Obstacle blocking.
- [ ] Visibility gain and loss events.
- [ ] Faction knowledge.
- [ ] Basic fog of war.
- [ ] Visibility debug overlay.

## Milestone 5: Combat

- [ ] Aim action.
- [ ] Attack action.
- [ ] Weapon profile.
- [ ] Cover.
- [ ] Hit resolution.
- [ ] Damage.
- [ ] Incapacitation.
- [ ] Combat explanation event data.

## Milestone 6: Reactions

- [ ] Guard direction.
- [ ] React on visibility.
- [ ] Reaction delay.
- [ ] Action interruption.
- [ ] Resume or cancel policy.
- [ ] Tests for simultaneous triggers.

## Milestone 7: First Scenario

- [ ] Scenario data schema.
- [ ] Deployment zones.
- [ ] One objective.
- [ ] Four player units.
- [ ] Four enemies.
- [ ] Minimal enemy planning.
- [ ] Win and loss resolution.

---

# 19. Testing Strategy

Core simulation should have automated tests for:

- Action ordering.
- Same-tick event ordering.
- Overlapping movement.
- Invalid commands.
- Unit incapacitation during an action.
- Line-of-sight changes.
- Reactions.
- Door state changes.
- Seed reproducibility.
- Replay checksum equality.
- Serialization round trips.
- Content validation.
- Version compatibility rules.

Prefer behavior-focused tests.

Example:

```text
Given two units moving toward the same tile,
when both are scheduled to enter on the same tick,
then the documented movement-conflict policy is applied,
and replaying with the same inputs produces the same result and event sequence.
```

The test suite should contain explicit golden replay cases once event schemas stabilize.

---

# 20. Balance and Simulation Harness

Because the core simulation is headless, the project should eventually support large automated simulation batches.

Potential uses:

- AI-versus-AI matches.
- Weapon balance.
- Scenario difficulty estimates.
- Win-rate analysis.
- Detection of dominant strategies.
- Action-duration tuning.
- Reaction timing tuning.
- Map fairness.
- Regression detection.

The simulation harness should report:

- Win rates.
- Average round count.
- Casualties.
- Damage distribution.
- Ability or weapon usage.
- Objective completion timing.
- Invalid or stalled states.
- Determinism failures.

Do not optimize for massive scale until the rule model is correct.

---

# 21. Multiplayer Direction

Multiplayer is not part of the first prototype.

If asynchronous multiplayer is added later, a turn can be represented by:

- Initial deterministic state.
- Player command bundle.
- Opponent command bundle.
- Random seed.
- Simulation version.
- Content version.
- Final checksum.

The server may validate inputs, resolve the turn, and return the replay rather than stream combat in real time.

Security considerations:

- Validate command legality server-side.
- Protect hidden-information state.
- Version simulation logic.
- Prevent clients from choosing seeds.
- Sign or validate replay records.

---

# 22. Mobile UX Considerations

The game is intended to be viable on mobile, but the earliest prototype may be developed and tested on desktop.

Mobile UX principles:

- Large touch targets.
- Clear unit selection.
- Easy path editing.
- Explicit confirmation before committing a round.
- Timeline visualization that remains readable on a phone.
- Tooltips or detail panels that do not obscure the battlefield.
- Accessible text sizing.
- Color-independent status indicators.
- Battery-conscious playback and simulation.
- Save and resume at any time outside active resolution.

Potential interaction:

1. Tap unit.
2. Tap or drag destination.
3. Add action from contextual action bar.
4. Adjust action sequence in timeline tray.
5. Preview estimated timing and path.
6. Commit all orders.
7. Watch resolution.
8. Review important events.

---

# 23. Legal and Intellectual Property Guardrails

The project may be inspired by mechanics and historical game-design ideas, but it should not copy protected expression.

Do not copy:

- Original source code.
- Original art.
- Original maps.
- Original scenario text.
- Original rulebook wording.
- Original character names.
- Original UI layout where distinctive.
- Original music or sound.
- Trademarks or branding.

Use an original project name and original presentation.

The fantasy game should not market itself as Dungeons & Dragons unless properly licensed. Use original classes, abilities, spells, monsters, terminology, settings, and lore.

Before commercial launch, obtain appropriate legal review for:

- Project naming.
- Store descriptions.
- Historical references.
- Third-party assets.
- AI-generated content terms.
- Music and sound licenses.
- Open-source dependencies.

---

# 24. Risks

## Design Risks

- Excessive simulation complexity before proving fun.
- Too much hidden information causing frustration.
- Long planning phases.
- Slow or confusing playback.
- Rules that are realistic but not enjoyable.
- Too many modifiers.
- Difficult mobile controls.

## Technical Risks

- Unity dependencies leaking into core simulation.
- Nondeterministic collection ordering.
- Floating-point divergence.
- Event schema instability.
- Overengineered content system.
- Difficult save compatibility.
- Too much custom editor work too early.

## Production Risks

- Inconsistent AI art.
- Animation scope explosion.
- Large content requirements.
- Premature multiplayer.
- Premature monetization.
- Unclear project identity between historical and fantasy settings.

Mitigation:

- Prototype with shapes.
- Keep milestones small.
- Test headlessly.
- Document rules.
- Use integer or fixed-point approaches where determinism requires it.
- Build one compelling scenario before broad content.

---

# 25. Definition of the First Successful Prototype

The first prototype is successful when:

- The player can issue timed orders to multiple units.
- Enemy units also have plans.
- Both sides resolve on one timeline.
- Movement and visibility can change during resolution.
- At least one reaction can interrupt a plan.
- The player can watch and understand the result.
- Replaying the same inputs produces the same outcome.
- The core runs without a Unity scene.
- Automated tests cover core rules.
- The scenario is enjoyable with placeholder visuals.

The prototype is not judged by final art, content volume, progression, monetization, or polish.

---

# 26. Agent Workflow

## Planning Agent

Responsibilities:

- Read `AGENTS.md` and relevant design documents.
- Restate the requested behavior.
- Identify assumptions.
- Identify design gaps.
- Propose domain-model changes.
- Produce a scoped implementation plan.

## Implementation Agent

Responsibilities:

- Follow accepted design.
- Implement only requested scope.
- Add tests.
- Preserve determinism.
- Avoid Unity dependencies in core.
- Update documentation.

## Review Agent

Review questions:

- Does the simulation depend on Unity?
- Is behavior deterministic?
- Can the round be replayed?
- Did the implementation invent undocumented rules?
- Did the change exceed scope?
- Are tests validating behavior rather than implementation details?
- Will the design support historical and fantasy modules?
- Are concepts hard-coded that should be data-driven?
- Are event order and same-tick conflicts explicit?
- Is failure behavior understandable?

The review agent should produce findings ordered by severity before rewriting code.

---

# 27. Initial Coding-Agent Instruction

Use the following instruction after downloading this file.

```text
You are initializing and implementing the first prototype of a new Unity tactical strategy game.

The complete project blueprint is in my Downloads folder in a file named:
TACTICAL_GAME_PROJECT_BLUEPRINT.md

First, locate and read that file in full. Treat it as the primary project brief for initialization. Do not start implementing until you have summarized the architecture, prototype scope, and source-of-truth rules from the file.

Create a new repository and Unity project for this game. Use the current stable Unity LTS version available on this machine unless an existing supported version is already standardized locally. Use C#.

Repository initialization requirements:

1. Initialize Git.
2. Add an appropriate Unity .gitignore.
3. Create README.md.
4. Create AGENTS.md based on the blueprint.
5. Copy the downloaded blueprint into docs/project-blueprint.md.
6. Create the documentation structure described in the blueprint.
7. Create architecture decision records for Unity/C#, deterministic core simulation, simultaneous timeline resolution, and data-driven content.
8. Create clear assembly or project boundaries for:
   - Game.Core
   - Game.Presentation.Unity
   - Game.Editor
   - Game.Tests
9. Ensure Game.Core is plain C# and has no Unity scene, rendering, GameObject, MonoBehaviour, Transform, Animator, Unity physics, frame-time, or input dependencies.
10. Configure NUnit or Unity Test Framework tests so the deterministic core can be tested headlessly where practical.

Then implement Milestone 1 only: Deterministic Timeline.

Milestone 1 scope:

- Configurable ticks per round, defaulting to 100.
- Immutable or carefully controlled game-state model.
- Unit model with ID, faction, position, facing, and basic active/incapacitated state.
- Command bundle per faction.
- Tactical actions with action ID, unit ID, type, start tick, and duration.
- Initial action types: Wait, Rotate, Move, Aim, Attack.
- Shared timeline resolution across all units and factions.
- Explicit same-tick event ordering policy.
- Domain events for round start, action start, action completion, action failure, and round completion.
- Central seeded random provider, even if Milestone 1 uses little or no randomness.
- Ordered replay event log.
- Deterministic final-state checksum.
- Serialization for the minimum replay inputs and outputs.

Do not implement yet:

- Final rendering.
- Mobile UI.
- Full pathfinding.
- Full combat.
- Damage.
- Advanced line of sight.
- Reactions.
- Inventory.
- Progression.
- Multiplayer.
- Procedural generation.
- Final art.
- Monetization.

Testing requirements:

- Action ordering.
- Multiple units acting on the same shared timeline.
- Same-tick event ordering.
- Invalid action timing.
- Actions that exceed the round duration.
- Inactive or incapacitated units attempting actions.
- Serialization round trip.
- Same inputs plus same seed produce identical event logs and checksums.
- Different command inputs produce different appropriate results.

Before writing code, produce a concise implementation plan containing:

1. The Unity version selected.
2. The repository and project structure.
3. The domain models to create.
4. The event-ordering policy.
5. The files expected to change.
6. Any assumptions not explicitly resolved by the blueprint.

Record important assumptions in docs/decisions or docs/technical-design rather than leaving them only in chat output.

After implementation:

1. Run all tests.
2. Report test results.
3. Show the resulting repository tree.
4. Summarize the architecture.
5. List every assumption made.
6. Identify the next smallest milestone, but do not implement it unless explicitly instructed.

Prioritize correctness, determinism, readability, testability, and scope discipline. Do not silently invent major game rules.
```

---

# 28. Suggested Follow-Up Agent Prompts

## Review Prompt

```text
Review the current repository against AGENTS.md, docs/project-blueprint.md, and the accepted architecture decision records.

Focus on:

- Unity dependencies leaking into Game.Core.
- Nondeterministic behavior.
- Unclear same-tick event ordering.
- Replay incompleteness.
- Missing or weak tests.
- Rules invented without documentation.
- Scope beyond Milestone 1.
- Hard-coded assumptions that should be configurable.
- Architecture that would prevent historical and fantasy modules from sharing the engine.

Do not rewrite code yet. Produce findings ordered by severity, with file references and concrete remediation recommendations.
```

## Milestone 2 Prompt

```text
Read AGENTS.md, docs/project-blueprint.md, the movement design document, and all accepted architecture decision records.

Implement Milestone 2: Grid Movement only.

Before coding, identify unresolved movement rules and propose the smallest deterministic policies for review. Do not implement undocumented swap, crossing, collision, or destination-invalidation behavior without recording an explicit decision.

Add tests for simultaneous movement, same-tick tile entry, blocked destinations, movement interruption, and deterministic replay.
```

---

# 29. Immediate Human Decisions Still Needed

The project can begin without resolving all of these, but they should eventually be decided explicitly:

- Final project name.
- Historical game first or fantasy game first.
- Square grid versus hex grid.
- Ten-second rounds versus another duration.
- Tick count per round.
- Whether movement uses tile-by-tile timing or path-segment timing.
- Same-tick event precedence.
- Friendly-unit crossing rules.
- Opposing-unit crossing rules.
- Degree of simulation detail.
- Exact injury model.
- Whether probabilities are fully visible.
- Preferred art direction.
- Portrait versus miniature emphasis.
- Premium, free-to-play, or other commercial model.

Agents must not block project initialization on these decisions. They should implement configurable placeholders or explicitly documented minimal assumptions.

---

# 30. Closing Direction

Build the engine first.

Prove the tactical loop with placeholder visuals.

Keep the simulation deterministic and independent from Unity presentation.

Turn natural-language design discussions into committed documents.

Use AI to accelerate implementation, testing, art exploration, and content tooling, but do not let AI invent the game's identity or silently define its rules.

The long-term opportunity is larger than one remake-inspired project. A carefully designed simultaneous-tactics engine can become the foundation for multiple original games.
