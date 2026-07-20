#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public static class MovementRules
{
    public static IReadOnlyList<GridPosition> PathFor(TacticalAction action)
    {
        if (action.Path != null)
            return action.Path;

        return action.Destination == null
            ? Array.Empty<GridPosition>()
            : new[] { action.Destination };
    }

    public static bool IsCardinalStep(GridPosition from, GridPosition to) =>
        GridDistance.Manhattan(from, to) == 1;

    public static int DurationFor(TacticalAction action, GridMapDefinition? map)
    {
        return PathFor(action).Sum(position => map?.CellAt(position).MovementTicks ?? 1);
    }

    public static int StepIndexAtTick(TacticalAction action, GridMapDefinition? map, int tick)
    {
        var elapsed = 0;
        var path = PathFor(action);
        for (var index = 0; index < path.Count; index++)
        {
            elapsed += map?.CellAt(path[index]).MovementTicks ?? 1;
            if (action.StartTick + elapsed == tick)
                return index;
        }

        return -1;
    }
}

}
