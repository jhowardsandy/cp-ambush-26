#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public enum Facing { North, East, South, West }
public enum UnitActivityState { Active, Incapacitated }
public enum TacticalActionType { Wait, Rotate, Move, Aim, Attack }

public sealed record GridPosition(int X, int Y);

public sealed record UnitState(
    Guid Id,
    string FactionId,
    GridPosition Position,
    Facing Facing,
    UnitActivityState ActivityState);

public sealed record TacticalAction(
    Guid ActionId,
    Guid UnitId,
    TacticalActionType Type,
    int StartTick,
    int DurationTicks,
    GridPosition? Destination = null,
    Facing? Facing = null);

public sealed record CommandBundle(string FactionId, IReadOnlyList<TacticalAction> Actions);

public sealed record RoundConfiguration(int TicksPerRound = 100)
{
    public void Validate()
    {
        if (TicksPerRound <= 0)
            throw new ArgumentOutOfRangeException(nameof(TicksPerRound), "Ticks per round must be positive.");
    }
}

public sealed record GameState(IReadOnlyList<UnitState> Units)
{
    public UnitState? FindUnit(Guid unitId) => Units.SingleOrDefault(unit => unit.Id == unitId);

    public GameState WithUnit(UnitState replacement) => new(
        Units.Select(unit => unit.Id == replacement.Id ? replacement : unit).ToArray());
}

public sealed record SimulationRequest(
    GameState InitialState,
    IReadOnlyList<CommandBundle> CommandBundles,
    RoundConfiguration Configuration,
    uint RandomSeed,
    string SimulationVersion = "1",
    string ContentVersion = "1");

public sealed record ValidationDiagnostic(string Code, string Message, Guid? ActionId = null);

}
