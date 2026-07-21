#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{
public static class ActionPointRules
{
    public static int CostFor(TacticalAction action, GridMapDefinition? map, IReadOnlyList<EffectDefinition> effects, IReadOnlyList<AttackProfile> attacks) => action.Type switch
    {
        TacticalActionType.Move => MovementRules.PathFor(action).Sum(position => map?.CellAt(position).ActionPointCost ?? 1),
        TacticalActionType.Attack => attacks.FirstOrDefault(profile => StringComparer.Ordinal.Equals(profile.Id, action.AttackProfileId))?.ActionPointCost ?? 0,
        TacticalActionType.ApplyEffect => effects.FirstOrDefault(effect => StringComparer.Ordinal.Equals(effect.Id, action.EffectId))?.ActionPointCost ?? 0,
        TacticalActionType.Rotate or TacticalActionType.Aim => 1,
        _ => 0
    };
}
}
