# Overwatch

## RXN-OW-001: One-shot movement-triggered overwatch

Status: accepted.

`EnterOverwatch` costs 2 AP and completes after one tick. It names a valid direct-attack profile and one cardinal facing, arming a forward 90-degree grid cone for the rest of that round. When a scenario supplies unit definitions, the acting unit must carry the `overwatch` skill. After the order is armed, the resolver checks each movement-entry event. An active opposing unit that enters the cone and is legal for the named attack profile (range and terrain line of sight) may trigger the reaction.

Each armed unit fires at most once. The resolver orders armed watchers by faction ID then unit ID; a watcher chooses the first legal entering enemy by that same deterministic identity order. The reaction uses the ordinary direct-attack calculation and emits `ReactionAttackResolved`. Arming and the reaction are both recorded as events. Overwatch expires at the end of the round whether it fired or not.

This first rule has no posture/scoped requirement, accuracy, cover/concealment modifier, ammunition, target selector customization, multi-shot capability, or interruption/resume behavior. Those are explicit follow-up rules.

Evidence: `Overwatch_fires_once_when_an_enemy_enters_the_armed_watch_cone`; `Overwatch_requires_the_overwatch_skill_when_a_unit_catalog_is_present`.
