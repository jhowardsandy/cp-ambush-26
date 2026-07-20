#nullable enable

using System;
using System.Collections.Generic;

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
}

}
