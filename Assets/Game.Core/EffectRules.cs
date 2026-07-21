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

    public static EffectResolution Resolve(UnitState source, UnitState target, EffectDefinition effect, GridMapDefinition? map)
    {
        if (effect.TargetPolicy == EffectTargetPolicy.Self && source.Id != target.Id)
            return new EffectResolution(0, null, "Effect requires the acting unit to target itself.");
        if (effect.TargetPolicy == EffectTargetPolicy.Friendly && !StringComparer.Ordinal.Equals(source.FactionId, target.FactionId))
            return new EffectResolution(0, null, "Effect requires a friendly target.");
        if (effect.TargetPolicy == EffectTargetPolicy.Hostile && StringComparer.Ordinal.Equals(source.FactionId, target.FactionId))
            return new EffectResolution(0, null, "Effect requires an opposing target.");
        var distance = GridDistance.Manhattan(source.Position, target.Position);
        if (distance < effect.MinimumRange || distance > effect.MaximumRange)
            return new EffectResolution(distance, null, $"Target distance {distance} is outside effect range {effect.MinimumRange}-{effect.MaximumRange}.");
        if (effect.RequiresLineOfSight && (map is null || !VisibilityRules.HasLineOfSight(map, source.Position, target.Position)))
            return new EffectResolution(distance, null, "Target line of sight is blocked.");
        return new EffectResolution(distance, Apply(target, effect));
    }
}

public sealed record EffectApplication(UnitState Target, int AppliedVitalityDelta);
public sealed record EffectResolution(int Distance, EffectApplication? Application, string? FailureDetail = null);

}
