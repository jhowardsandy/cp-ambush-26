# Combat System

`Aim` and `Attack` are still timing-only actions and do not target, roll, hit, or apply weapon damage. The generic vitality effect foundation is implemented separately: `ApplyEffect` can apply a named signed vitality change to a target at completion, with clamp and incapacity rules. See `effect-system.md`.
