#nullable enable

namespace TacticalStrategyGame.Core
{

/// <summary>Deterministic grid line-of-sight for objective map geometry.</summary>
public static class VisibilityRules
{
    public static ObservationResult Observe(GridMapDefinition map, UnitState observer, UnitState target)
    {
        var distance = GridDistance.Manhattan(observer.Position, target.Position);
        var terrainConcealment = TerrainProtectionRules.At(map, target.Position).ConcealmentValue;
        var postureConcealment = ConcealmentFromPosture(target.Posture);
        var concealment = terrainConcealment + postureConcealment;
        var effectiveRange = System.Math.Max(0, observer.VisionRange - concealment);
        return distance <= effectiveRange
            ? new ObservationResult(true, distance, effectiveRange, concealment, TerrainConcealment: terrainConcealment, PostureConcealment: postureConcealment)
            : new ObservationResult(false, distance, effectiveRange, concealment, $"Target distance {distance} exceeds observable range {effectiveRange} after concealment {concealment} (terrain {terrainConcealment}; posture {postureConcealment}).", terrainConcealment, postureConcealment);
    }

    public static int ConcealmentFromPosture(UnitPosture posture) => posture switch
    {
        UnitPosture.Crouched => 1,
        UnitPosture.Prone => 2,
        _ => 0
    };
    public static bool HasLineOfSight(GridMapDefinition map, GridPosition origin, GridPosition target)
    {
        if (!map.Contains(origin) || !map.Contains(target))
            return false;

        var x = origin.X;
        var y = origin.Y;
        var deltaX = System.Math.Abs(target.X - origin.X);
        var deltaY = System.Math.Abs(target.Y - origin.Y);
        var stepX = origin.X < target.X ? 1 : -1;
        var stepY = origin.Y < target.Y ? 1 : -1;
        var error = deltaX - deltaY;

        while (x != target.X || y != target.Y)
        {
            var twiceError = 2 * error;
            if (twiceError > -deltaY)
            {
                error -= deltaY;
                x += stepX;
            }
            if (twiceError < deltaX)
            {
                error += deltaX;
                y += stepY;
            }

            if (x == target.X && y == target.Y)
                return true;

            if (map.CellAt(new GridPosition(x, y)).BlocksLineOfSight)
                return false;
        }

        return true;
    }
}

public sealed record ObservationResult(bool IsObservable, int Distance, int EffectiveRange, int Concealment, string? FailureDetail = null, int TerrainConcealment = 0, int PostureConcealment = 0);

}
