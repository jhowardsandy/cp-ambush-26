#nullable enable

namespace TacticalStrategyGame.Core
{
public sealed record TerrainProtection(int CoverValue, int ConcealmentValue);

public static class TerrainProtectionRules
{
    public static TerrainProtection At(GridMapDefinition map, GridPosition position)
    {
        var terrain = map.CellAt(position);
        return new TerrainProtection(terrain.CoverValue, terrain.ConcealmentValue);
    }
}
}
