#nullable enable

using System;

namespace TacticalStrategyGame.Core
{

/// <summary>Pure, deterministic vitality effect calculations shared by all setting packages.</summary>
public static class EffectRules
{
    public static EffectApplication Apply(UnitState target, EffectDefinition effect)
    {
        var afterHitPoints = Math.Clamp(target.HitPoints + effect.VitalityDelta, 0, target.MaxHitPoints);
        var activityState = afterHitPoints == 0 ? UnitActivityState.Incapacitated : target.ActivityState;
        var updatedTarget = target with { HitPoints = afterHitPoints, ActivityState = activityState };
        return new EffectApplication(updatedTarget, afterHitPoints - target.HitPoints);
    }
}

public sealed record EffectApplication(UnitState Target, int AppliedVitalityDelta);

}
