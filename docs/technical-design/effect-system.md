# Effect System

The core owns generic `EffectDefinition` records and pure `EffectRules.Apply` calculations. A definition has a stable ID and signed integer `VitalityDelta`; setting content supplies the meaning (for example, a med kit, clerical healing, grenade impact, or fireball impact).

`TacticalActionType.ApplyEffect` resolves at its normal completion tick against its `TargetUnitId`. Validation occurs before resolution, so an unknown target or effect definition yields diagnostics rather than a partial simulation. A successful application clamps hit points to `[0, MaxHitPoints]`, incapacitates a unit at zero, records a calculation breakdown in `EffectApplied`, then completes the action.

Effect actions are deliberately not attacks. They have no range, visibility, faction policy, chance, projectile path, area, resource cost, status duration, resistance, armor, cover, or revival behavior yet. Those concerns will compose with this calculation layer through separate accepted rules.
