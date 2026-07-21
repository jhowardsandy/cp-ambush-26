#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public enum ObjectiveType { IncapacitateAllOpposingUnits, HoldAreaForRounds }

/// <summary>Versioned encounter objective content. Additional objective types require their own accepted rules.</summary>
public sealed record ObjectiveDefinition(string Id, ObjectiveType Type, string WinningFactionId, string? AreaId = null, int RequiredControlRounds = 1);

public sealed record EncounterOutcome(bool IsComplete, string? WinningFactionId = null, string? Detail = null);
public sealed record ObjectiveProgress(string ObjectiveId, int ControlledRounds = 0);
public sealed record ObjectiveEvaluation(IReadOnlyList<ObjectiveProgress> Progress, EncounterOutcome? Outcome);

public static class ObjectiveRules
{
    public static EncounterOutcome? Evaluate(IReadOnlyList<ObjectiveDefinition>? objectives, GameState state) =>
        Evaluate(objectives, null, state, null).Outcome;

    public static ObjectiveEvaluation Evaluate(
        IReadOnlyList<ObjectiveDefinition>? objectives,
        GridMapDefinition? map,
        GameState state,
        IReadOnlyList<ObjectiveProgress>? priorProgress)
    {
        if (objectives == null || objectives.Count == 0)
            return new ObjectiveEvaluation(Array.Empty<ObjectiveProgress>(), null);

        var progress = new List<ObjectiveProgress>();

        foreach (var objective in objectives.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
        {
            if (objective.Type == ObjectiveType.IncapacitateAllOpposingUnits)
            {
                var winningFactionHasActiveUnit = state.Units.Any(unit =>
                    StringComparer.Ordinal.Equals(unit.FactionId, objective.WinningFactionId) &&
                    unit.ActivityState == UnitActivityState.Active);
                var opposingActiveUnits = state.Units.Count(unit =>
                    !StringComparer.Ordinal.Equals(unit.FactionId, objective.WinningFactionId) &&
                    unit.ActivityState == UnitActivityState.Active);

                if (winningFactionHasActiveUnit && opposingActiveUnits == 0)
                    return new ObjectiveEvaluation(progress, new EncounterOutcome(true, objective.WinningFactionId,
                        $"objective={objective.Id}; winner={objective.WinningFactionId}; opposing-active-units=0"));
                continue;
            }

            if (objective.Type != ObjectiveType.HoldAreaForRounds || map?.AreaById(objective.AreaId ?? String.Empty) is not { } area)
                continue;
            var controlled = state.Units.Any(unit => unit.ActivityState == UnitActivityState.Active && StringComparer.Ordinal.Equals(unit.FactionId, objective.WinningFactionId) && area.Tiles.Contains(unit.Position));
            var contested = state.Units.Any(unit => unit.ActivityState == UnitActivityState.Active && !StringComparer.Ordinal.Equals(unit.FactionId, objective.WinningFactionId) && area.Tiles.Contains(unit.Position));
            var previous = priorProgress?.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.ObjectiveId, objective.Id))?.ControlledRounds ?? 0;
            var rounds = controlled && !contested ? previous + 1 : 0;
            progress.Add(new ObjectiveProgress(objective.Id, rounds));
            if (rounds >= objective.RequiredControlRounds)
                return new ObjectiveEvaluation(progress, new EncounterOutcome(true, objective.WinningFactionId,
                    $"objective={objective.Id}; winner={objective.WinningFactionId}; area={objective.AreaId}; held-rounds={rounds}/{objective.RequiredControlRounds}"));
        }

        return new ObjectiveEvaluation(progress, new EncounterOutcome(false));
    }
}

}
