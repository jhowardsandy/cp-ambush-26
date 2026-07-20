#nullable enable

namespace TacticalStrategyGame.Core
{

/// <summary>Pure direct-attack resolution. Accuracy, cover, armor, and projectile travel are separate future rules.</summary>
public static class AttackRules
{
    public static AttackResolution Resolve(UnitState attacker, UnitState target, AttackProfile profile, GridMapDefinition map)
    {
        var distance = GridDistance.Manhattan(attacker.Position, target.Position);
        if (distance < profile.MinimumRange || distance > profile.MaximumRange)
            return new AttackResolution(distance, null, $"Target distance {distance} is outside attack range {profile.MinimumRange}-{profile.MaximumRange}.");
        if (profile.RequiresLineOfSight && !VisibilityRules.HasLineOfSight(map, attacker.Position, target.Position))
            return new AttackResolution(distance, null, "Target line of sight is blocked.");
        if (target.ActivityState != UnitActivityState.Active)
            return new AttackResolution(distance, null, "Target is not active.");

        var application = EffectRules.Apply(target, new EffectDefinition(profile.Id, -profile.Damage));
        return new AttackResolution(distance, application);
    }
}

public sealed record AttackResolution(int Distance, EffectApplication? Application, string? FailureDetail = null);

}
