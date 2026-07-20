#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

/// <summary>Setting-neutral rectangular tactical map definition.</summary>
public sealed record GridMapDefinition(string Id, int Width, int Height)
{
    public bool Contains(GridPosition position) =>
        position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
}

/// <summary>Reusable, data-serializable starting point for one tactical encounter.</summary>
public sealed record ScenarioDefinition(
    string Id,
    GridMapDefinition Map,
    GameState InitialState,
    string ContentVersion = "1");

public static class ScenarioFactory
{
    public static SimulationRequest CreateRequest(
        ScenarioDefinition scenario,
        IReadOnlyList<CommandBundle> commandBundles,
        RoundConfiguration configuration,
        uint randomSeed,
        string simulationVersion = "1") =>
        new(scenario.InitialState, commandBundles, configuration, randomSeed, simulationVersion, scenario.ContentVersion, scenario);
}

public static class ScenarioValidator
{
    public static IReadOnlyList<ValidationDiagnostic> Validate(ScenarioDefinition scenario)
    {
        var diagnostics = new List<ValidationDiagnostic>();
        if (String.IsNullOrWhiteSpace(scenario.Id))
            diagnostics.Add(new("missing-scenario-id", "Scenario ID is required."));
        if (String.IsNullOrWhiteSpace(scenario.Map.Id))
            diagnostics.Add(new("missing-map-id", "Map ID is required."));
        if (scenario.Map.Width <= 0 || scenario.Map.Height <= 0)
            diagnostics.Add(new("invalid-map-size", "Map width and height must both be positive."));

        foreach (var unit in scenario.InitialState.Units)
        {
            if (!scenario.Map.Contains(unit.Position))
                diagnostics.Add(new("unit-out-of-bounds", "Scenario unit position must be inside its map."));
        }

        return diagnostics;
    }
}

}
