#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public enum Facing { North, East, South, West }
public enum UnitActivityState { Active, Incapacitated }
public enum TacticalActionType { Wait, Rotate, Move, Aim, Attack, ApplyEffect }

public sealed record GridPosition(int X, int Y);

public static class GridDistance
{
    public static int Manhattan(GridPosition from, GridPosition to) =>
        Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
}

public sealed record UnitState(
    Guid Id,
    string FactionId,
    GridPosition Position,
    Facing Facing,
    UnitActivityState ActivityState,
    int HitPoints = 10,
    int MaxHitPoints = 10,
    string? UnitDefinitionId = null,
    int ActionPointBudget = 6);

public sealed record TacticalAction(
    Guid ActionId,
    Guid UnitId,
    TacticalActionType Type,
    int StartTick,
    int DurationTicks,
    GridPosition? Destination = null,
    Facing? Facing = null,
    IReadOnlyList<GridPosition>? Path = null,
    Guid? TargetUnitId = null,
    string? EffectId = null,
    string? AttackProfileId = null);

/// <summary>Setting-neutral, versioned vitality change. Positive values heal; negative values damage.</summary>
public sealed record EffectDefinition(string Id, int VitalityDelta, int ActionPointCost = 2);

/// <summary>Data-defined direct attack with deterministic, guaranteed-hit damage in the first combat slice.</summary>
public sealed record AttackProfile(string Id, int MinimumRange, int MaximumRange, int Damage, bool RequiresLineOfSight = true, int ActionPointCost = 2);

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
    string ContentVersion = "1",
    ScenarioDefinition? Scenario = null,
    IReadOnlyList<EffectDefinition>? Effects = null,
    IReadOnlyList<AttackProfile>? AttackProfiles = null);

public sealed record ValidationDiagnostic(string Code, string Message, Guid? ActionId = null);

}
