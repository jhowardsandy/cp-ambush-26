# Timeline Resolution

The default round is 100 configurable ticks. Each action has a non-negative start tick and positive duration, and must complete within the round. A unit may not have overlapping actions.

At each tick, the resolver processes action starts before completions. Within one phase, actions are sorted by ordinal faction ID, canonical unit GUID, then canonical action GUID. Round start is emitted first at tick zero and round completion last at the final tick. An inactive or incapacitated unit emits `ActionFailed` at the scheduled start and its action does not complete.

Milestone 1 actions have only the following state effects: `Move` changes position at completion when supplied a destination; `Rotate` changes facing at completion when supplied a facing. `Wait`, `Aim`, and `Attack` currently contribute timing events only.
