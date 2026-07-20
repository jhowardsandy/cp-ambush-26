#nullable enable

namespace TacticalStrategyGame.Core
{

/// <summary>Deterministic grid line-of-sight for objective map geometry.</summary>
public static class VisibilityRules
{
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

}
