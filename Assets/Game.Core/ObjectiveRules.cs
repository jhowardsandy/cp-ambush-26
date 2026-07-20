#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public enum ObjectiveType { IncapacitateAllOpposingUnits }

/// <summary>Versioned encounter objective content. Additional objective types require their own accepted rules.</summary>
public sealed record ObjectiveDefinition(string Id, ObjectiveType Type, string WinningFactionId);

public sealed record EncounterOutcome(bool IsComplete, string? WinningFactionId = null, string? Detail = null);

public static class ObjectiveRules
{
    public static EncounterOutcome? Evaluate(IReadOnlyList<ObjectiveDefinition>? objectives, GameState state)
    {
        if (objectives == null || objectives.Count == 0)
            return null;

        foreach (var objective in objectives.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            if (objective.Type != ObjectiveType.IncapacitateAllOpposingUnits)
                continue;

            var winningFactionHasActiveUnit = state.Units.Any(unit =>
                StringComparer.Ordinal.Equals(unit.FactionId, objective.WinningFactionId) &&
                unit.ActivityState == UnitActivityState.Active);
            var opposingActiveUnits = state.Units.Count(unit =>
                !StringComparer.Ordinal.Equals(unit.FactionId, objective.WinningFactionId) &&
                unit.ActivityState == UnitActivityState.Active);

            if (winningFactionHasActiveUnit && opposingActiveUnits == 0)
            {
                return new EncounterOutcome(true, objective.WinningFactionId,
                    $"objective={objective.Id}; winner={objective.WinningFactionId}; opposing-active-units=0");
            }
        }

        return new EncounterOutcome(false);
    }
}

}
