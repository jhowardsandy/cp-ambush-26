# Combat System

The first direct-fire combat slice is implemented. `Attack` names an opposing `TargetUnitId` and data-defined `AttackProfileId`. At completion, `AttackRules` checks inclusive Manhattan range and required terrain line of sight against the current positions, then applies fixed damage through the generic vitality effect calculation. `AttackResolved` exposes structured attacker/target positions and outcome data for presentation/replay; an illegal-at-resolution attack emits `ActionFailed`.

The first profile is guaranteed hit once legal. Accuracy rolls, cover, armor, evasion, ammunition, facing, projectile travel, area effects, and reaction fire are deferred. `Aim` remains timing-only. See `../rulebook/direct-attacks.md` and `effect-system.md`.
