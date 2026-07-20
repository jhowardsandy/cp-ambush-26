#nullable enable

using System;
using Newtonsoft.Json;

namespace TacticalStrategyGame.Core
{

public static class ScenarioSerializer
{
    public static string Serialize(ScenarioDefinition scenario) =>
        JsonConvert.SerializeObject(scenario, Formatting.Indented);

    public static ScenarioDefinition Deserialize(string json) =>
        JsonConvert.DeserializeObject<ScenarioDefinition>(json)
        ?? throw new InvalidOperationException("Scenario JSON did not contain a scenario definition.");
}

}
