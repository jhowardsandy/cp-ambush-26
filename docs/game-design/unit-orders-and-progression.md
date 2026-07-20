# Unit Orders, Roles, Equipment, and Progression

Status: proposed design directions. Nothing in this document is an accepted simulation rule yet.

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
