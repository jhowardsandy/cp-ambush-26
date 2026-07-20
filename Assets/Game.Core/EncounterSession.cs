#nullable enable

using System;
using System.Collections.Generic;

namespace TacticalStrategyGame.Core
{

/// <summary>Content shared across all rounds of one reusable tactical encounter.</summary>
public sealed record EncounterDefinition(string Id, GridMapDefinition Map, string ContentVersion = "1");

/// <summary>Authoritative state at the planning boundary before a new round is ordered.</summary>
public sealed record EncounterState(EncounterDefinition Definition, GameState CurrentState, int CompletedRounds = 0);

public sealed record EncounterRoundResult(EncounterState NextState, SimulationResult Resolution);

/// <summary>Builds each round from the previous resolved state; presentation never carries state forward itself.</summary>
public static class EncounterResolver
{
    public static EncounterRoundResult ResolveRound(
        EncounterState encounter,
        IReadOnlyList<CommandBundle> commandBundles,
        RoundConfiguration configuration,
        uint randomSeed,
        string simulationVersion = "1",
        IReadOnlyList<EffectDefinition>? effects = null,
        IReadOnlyList<AttackProfile>? attackProfiles = null)
    {
        if (encounter == null) throw new ArgumentNullException(nameof(encounter));
        if (commandBundles == null) throw new ArgumentNullException(nameof(commandBundles));

        var scenario = new ScenarioDefinition(encounter.Definition.Id, encounter.Definition.Map, encounter.CurrentState, encounter.Definition.ContentVersion);
        var request = ScenarioFactory.CreateRequest(scenario, commandBundles, configuration, randomSeed, simulationVersion, effects, attackProfiles);
        var resolution = new TimelineResolver().Resolve(request);
        var nextState = resolution.IsValid
            ? encounter with { CurrentState = resolution.FinalState, CompletedRounds = encounter.CompletedRounds + 1 }
            : encounter;
        return new EncounterRoundResult(nextState, resolution);
    }
}

}
