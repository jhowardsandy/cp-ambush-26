#nullable enable

using System;
using System.Collections.Generic;

namespace TacticalStrategyGame.Core
{

public enum DomainEventType
{
    RoundStarted,
    ActionStarted,
    UnitExitedTile,
    UnitEnteredTile,
    EffectApplied,
    ActionCompleted,
    ActionFailed,
    RoundCompleted
}

public sealed record DomainEvent(
    int Sequence,
    int Tick,
    DomainEventType Type,
    string FactionId,
    Guid? UnitId = null,
    Guid? ActionId = null,
    string? Detail = null,
    GridPosition? FromPosition = null,
    GridPosition? ToPosition = null,
    int? HitPointsAfter = null,
    UnitActivityState? ActivityStateAfter = null);

public sealed record SimulationResult(
    GameState FinalState,
    IReadOnlyList<DomainEvent> Events,
    IReadOnlyList<ValidationDiagnostic> Diagnostics,
    string FinalStateChecksum)
{
    public bool IsValid => Diagnostics.Count == 0;
}

}
