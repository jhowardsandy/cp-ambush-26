# Unit Orders, Roles, Equipment, and Progression

Status: proposed design directions. Nothing in this document is an accepted simulation rule yet.

Update: the portable `UnitDefinition` catalog foundation is implemented for archetype ID, maximum vitality, vision, base movement timing, role tags, listed attack/effect IDs, and extensible named numeric attributes. `FactionDefinition` declares which unit definitions a country/culture/race-style content package can field. Their listed capability bindings and progression behavior remain proposed.

## Tactical order language

The player should plan intent, not merely issue one generic attack command. Candidate order families include:

| Order family | Example | Engine requirement before acceptance |
| --- | --- | --- |
| Explicit movement | Move along a chosen route | Implemented timed path foundation. |
| Sequenced action | Move, then heal; move, then attack | Implemented timeline sequencing; action legality still needs content/range rules. |
| Explicit target | Fire at a named visible unit | Target identity, legal target policy, range/LOS at resolution, and explainable outcome. |
| Conditional target | Fire at the nearest visible enemy | Deterministic selector, tie-break policy, visibility timing, and fallback behavior. |
| Priority target | A sniper prioritizes a commander when that commander becomes a legal target | Unit role/tags, priority list, detection trigger, interruption/resume policy, and fallback behavior. |
| Area/position target | Throw an explosive at a tile or area | Target geometry, projectile/effect travel, area shape, obstacle interaction, friendly-fire policy, and event explanations. |

Conditional orders are particularly valuable for the shared-timeline game: an order can name what the unit should attempt at resolution time without granting the presentation or AI discretion to alter the result. Every selector must be deterministic, visible in a replay, and explain why it chose or did not choose a target.

## Distance-sensitive attacks

Weapons and abilities should be defined as content profiles rather than hard-coded branches. A profile may eventually contain:

- usable range and distance bands;
- accuracy, damage, armor/cover interaction, and ammunition/resource cost by band;
- target restrictions and line-of-sight requirements;
- effect delivery (instant, ballistic, projectile, explosive area, healing, or magical); and
- an animation/sound presentation reference that never determines the result.

Guns, arrows, thrown grenades, and magical projectiles can therefore have different delivery and calculation rules while still using the same action, targeting, event, replay, and effect foundations.

## Unit identity and role

Units should be content-defined from composable data: base vitality, movement, vision, equipment slots, role tags, abilities, modifiers, and presentation references. Military examples may include rifle, scout, medic, support, sniper, commander, vehicle crew, or armored vehicle roles. A future original fantasy package can express analogous roles—such as warrior, archer, healer, mage, or creature—without changing the generic engine.

Role tags are candidate data for priority targeting and AI planning; they are not a reason to hard-code a special unit class into the core.

## Proposed round action-point economy

The current sandbox uses a 10-tick round as a technical planning bound; it is not yet a unit-speed rule. The proposed player-facing model is a per-unit action-point (AP) budget that refreshes at the start of each round. A unit's content definition would supply a base AP value, terrain traversal could spend AP per entered tile, and actions such as attacks, healing, reloads, stances, or abilities could spend explicit AP costs. Temporary effects or special abilities may grant, remove, or reserve AP only through named, versioned rules.

Before accepting this rule, define: the relationship between AP and timeline ticks; whether unused AP is lost or carried; exact costs for each action; interruption/refund policy for a failed action; modifier stacking/caps/duration; and the event/replay calculation that explains `starting AP -> spent/reserved/refunded AP -> remaining AP`. This must remain setting-neutral so a rifle, bow, fireball, medical kit, or healing spell follows the same budgeting contract while retaining its own content profile.

## Proposed posture, cover, and concealment

Units may later spend AP to change posture—for example standing, crouching, or prone/lying down—and spend AP again to stand. Posture can alter movement, visibility, targetability, cover/concealment interaction, and allowed actions only through explicit content and rule tables. Brush, walls, buildings, terrain height, and other map features should expose generic cover/concealment/elevation properties; military and fantasy content can then use the same engine to express foxholes, hedges, ruins, magical thickets, or barricades.

Before acceptance, define posture transition costs/timing, whether a unit may move or fire while changing posture, how multiple cover sources combine, observation versus attack visibility, directional or elevation policy, and the exact calculation/event explanation. The first rule must be deterministic and testable without Unity animation; animation is a consequence of the resolved posture event.

## Proposed overwatch, scoped zones, and reactions

An overwatch order is a constrained intent, not permission to make an additional unrestricted attack. For example, a sniper may need to be prone and scoped, choose a facing/arc or explicit watched zone, and reserve one reaction shot. During the shared timeline, the resolver evaluates a deterministic trigger whenever a qualifying enemy enters, leaves, or becomes visible within that zone. If the trigger is legal, the reaction shot is consumed; after firing, that unit cannot fire another reaction shot in the round unless a separate named rule grants one.

The first accepted reaction design must specify: eligible posture/weapon readiness; zone geometry and facing; which events trigger evaluation; perception/stealth/cover requirements; target-selection and tie-break rules; reaction timing and priority when several units qualify; interruption/resume policy for the moving unit; ammunition/AP reservation and refund policy; and a complete event/replay explanation. "Scoped" and "watching" must be simulation state, not merely an animation or UI label.

## Equipment, purchases, and progression

Future campaign and match-economy systems may permit purchasing or earning better equipment, abilities, and upgrades. Each change should be a versioned data transformation with:

- prerequisites, availability, cost/currency, and legal recipients;
- explicit additive/multiplicative modifiers, ordering, stacking, caps, and duration;
- a before/after calculation explanation and deterministic tests;
- serialization/content-version compatibility; and
- a separation between permanent progression and temporary in-encounter effects.

Examples include a stronger medical kit, a longer-range optic, protective equipment, improved movement gear, a new ability, or a fantasy equivalent. Balance and unlock economy are future design work; the first vertical slice should use a small, transparent fixed catalog.

## Delivery sequence

1. Player unit selection and round-order authoring over the existing encounter state.
2. Versioned unit, action, and equipment content schemas plus validators.
3. Explicit targeting, range, and line-of-sight legality.
4. Deterministic target selectors and PvE planner explanations.
5. Weapon/ability profiles, cover, delivery types, and combat calculations.
6. Scenario rewards, equipment purchases, and progression after the core encounter loop is trusted.

PvP will use the same content and command contracts. It must not have a separate combat or upgrade calculation path.

## Scenario pressure and rescue directions

Future scenarios may include civilians or important people located through a scan/search sequence and rescued through an explicit pickup, escort, or extraction objective. Search information must be represented as objective/faction knowledge, not inferred from presentation.

Campaign and PvE maps may also have deterministic reinforcement schedules, for example “every three completed rounds, spawn this unit group at this point.” A capturable spawn point can alter that schedule only through explicit control rules. Spawning must define stable generated IDs, exact round timing, placement when a spawn tile is blocked, ownership transition, and replay inputs before it is accepted as a game rule.

PvE scenarios may instead begin with a defensive player deployment: players place their units within a defined deployment area before the first resolution round, then hold, search, extract, survive, or repel an opposing force. Deployment zones, valid placement, facing/stance defaults, preparation limits, enemy knowledge, and objective timing must be scenario data and deterministic rules—not presentation-only setup—before defensive scenarios are implemented.
