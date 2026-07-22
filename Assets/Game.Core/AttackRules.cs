#nullable enable

using System;

namespace TacticalStrategyGame.Core
{

/// <summary>Pure direct-attack resolution. A supplied seeded roll evaluates accuracy; callers without a roll receive a legal-attack preview.</summary>
public static class AttackRules
{
    public static AttackResolution Resolve(UnitState attacker, UnitState target, AttackProfile profile, GridMapDefinition map, int? accuracyRoll = null)
    {
        var distance = GridDistance.Manhattan(attacker.Position, target.Position);
        var observation = VisibilityRules.Observe(map, attacker, target);
        if (!observation.IsObservable)
            return new AttackResolution(distance, null, observation.FailureDetail);
        if (distance < profile.MinimumRange || distance > profile.MaximumRange)
            return new AttackResolution(distance, null, $"Target distance {distance} is outside attack range {profile.MinimumRange}-{profile.MaximumRange}.");
        if (profile.RequiresLineOfSight && !VisibilityRules.HasLineOfSight(map, attacker.Position, target.Position))
            return new AttackResolution(distance, null, "Target line of sight is blocked.");
        if (target.ActivityState != UnitActivityState.Active)
            return new AttackResolution(distance, null, "Target is not active.");

        if (accuracyRoll is not null && (accuracyRoll < 1 || accuracyRoll > 100))
            throw new ArgumentOutOfRangeException(nameof(accuracyRoll), "Accuracy rolls must be between 1 and 100 inclusive.");
        if (accuracyRoll is not null && accuracyRoll > profile.AccuracyPercent)
            return new AttackResolution(distance, null, CoverMitigation: TerrainProtectionRules.At(map, target.Position).CoverValue,
                ArmorMitigation: target.ArmorValue, AccuracyPercent: profile.AccuracyPercent, AccuracyRoll: accuracyRoll, Hit: false);

        var coverMitigation = TerrainProtectionRules.At(map, target.Position).CoverValue;
        var armorMitigation = target.ArmorValue;
        var effectiveDamage = System.Math.Max(1, profile.Damage - coverMitigation - armorMitigation);
        var application = EffectRules.Apply(target, new EffectDefinition(profile.Id, -effectiveDamage));
        return new AttackResolution(distance, application, CoverMitigation: coverMitigation, EffectiveDamage: effectiveDamage, ArmorMitigation: armorMitigation,
            AccuracyPercent: profile.AccuracyPercent, AccuracyRoll: accuracyRoll, Hit: true);
    }

    internal static AttackResolution ResolveAreaImpact(UnitState target, AttackProfile profile, GridMapDefinition map, int distanceFromCenter, int? accuracyRoll)
    {
        if (accuracyRoll is not null && accuracyRoll > profile.AccuracyPercent)
            return new AttackResolution(0, null, CoverMitigation: TerrainProtectionRules.At(map, target.Position).CoverValue, ArmorMitigation: target.ArmorValue, AccuracyPercent: profile.AccuracyPercent, AccuracyRoll: accuracyRoll, Hit: false);

        var coverMitigation = TerrainProtectionRules.At(map, target.Position).CoverValue;
        var armorMitigation = target.ArmorValue;
        var effectiveDamage = System.Math.Max(1, profile.Damage - distanceFromCenter * profile.AreaFalloffDamagePerTile - coverMitigation - armorMitigation);
        var application = EffectRules.Apply(target, new EffectDefinition(profile.Id, -effectiveDamage));
        return new AttackResolution(0, application, CoverMitigation: coverMitigation, EffectiveDamage: effectiveDamage, ArmorMitigation: armorMitigation,
            AccuracyPercent: profile.AccuracyPercent, AccuracyRoll: accuracyRoll, Hit: true);
    }
}

public sealed record AttackResolution(int Distance, EffectApplication? Application, string? FailureDetail = null, int CoverMitigation = 0, int EffectiveDamage = 0, int AccuracyPercent = 100, int? AccuracyRoll = null, bool Hit = true, int ArmorMitigation = 0);

}
