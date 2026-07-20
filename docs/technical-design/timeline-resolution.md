# Timeline Resolution

The default round is 100 configurable ticks. Each action has a non-negative start tick and positive duration, and must complete within the round. A unit may not have overlapping actions.

At each tick, the resolver processes action starts before completions. Within one phase, actions are sorted by ordinal faction ID, canonical unit GUID, then canonical action GUID. Round start is emitted first at tick zero and round completion last at the final tick. An inactive or incapacitated unit emits `ActionFailed` at the scheduled start and its action does not complete.

Implemented action effects are deliberately narrow and documented by rule chapter: `Move` enters each explicit path tile on its terrain-determined tick; `Rotate` updates facing at completion; `ApplyEffect` applies a named vitality effect at completion; and `Attack` resolves a named direct-fire profile at completion. `Wait` and `Aim` remain timing-only. See the movement, effects, and direct-attack rulebook chapters for legality and calculation details.
