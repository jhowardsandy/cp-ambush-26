#nullable enable

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace TacticalStrategyGame.Core
{

public sealed record ReplayRecord(SimulationRequest Inputs, SimulationResult Output);

public static class ReplaySerializer
{
    public static string Serialize(ReplayRecord replay) => JsonConvert.SerializeObject(replay, Formatting.Indented);
    public static ReplayRecord Deserialize(string json) => JsonConvert.DeserializeObject<ReplayRecord>(json)
        ?? throw new InvalidOperationException("Replay JSON did not contain a replay record.");
}

public static class StateChecksum
{
    public static string Calculate(GameState state)
    {
        var canonical = new StringBuilder();
        foreach (var unit in state.Units.OrderBy(unit => unit.Id.ToString("N"), StringComparer.Ordinal))
        {
            canonical.Append(unit.Id.ToString("N")).Append('|')
                .Append(unit.FactionId).Append('|')
                .Append(unit.Position.X).Append('|').Append(unit.Position.Y).Append('|')
                .Append((int)unit.Facing).Append('|').Append((int)unit.ActivityState).Append('|')
                .Append(unit.HitPoints).Append('|').Append(unit.MaxHitPoints).Append('|')
                .Append(unit.ActionPointBudget).Append('|').Append((int)unit.Posture).Append('|').Append(unit.VisionRange);
            var inventory = (unit.Inventory ?? Array.Empty<InventoryItemState>()).OrderBy(item => item.ItemId, StringComparer.Ordinal).ToArray();
            if (inventory.Length > 0)
            {
                canonical.Append('|');
                foreach (var item in inventory)
                    canonical.Append(item.ItemId).Append(':').Append(item.Quantity).Append(',');
            }
            canonical.Append('\n');
        }

        using (var sha256 = SHA256.Create())
        {
            return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()))).Replace("-", string.Empty);
        }
    }
}

}
