#nullable enable

using System;
using System.Collections.Generic;

namespace TacticalStrategyGame.Core
{

/// <summary>Deterministic acceptance fixture for the first complete objective showcase. It is intentionally separate from free PvE auto-play.</summary>
public static class SecureTheFordShowcase
{
    public static readonly Guid BlueRiflemanId = Guid.Parse("81000000-0000-0000-0000-000000000001");
    public static readonly Guid BlueMedicId = Guid.Parse("81000000-0000-0000-0000-000000000002");
    public static readonly Guid RedSkirmisherId = Guid.Parse("82000000-0000-0000-0000-000000000001");
    public static readonly Guid RedReserveId = Guid.Parse("82000000-0000-0000-0000-000000000002");

    public static EncounterState CreateEncounter()
    {
        var map = new GridMapDefinition("secure-the-ford-map", 10, 6, Areas: new[]
        {
            new MapAreaDefinition("central-ford", new[] { new GridPosition(4, 3) }),
            new MapAreaDefinition("blue-signal", new[] { new GridPosition(1, 1) }),
            new MapAreaDefinition("red-reinforcement-entry", new[] { new GridPosition(9, 5) })
        });
        var blueRifleman = StarterMilitaryContent.Rifleman.CreateInitialState(BlueRiflemanId, "blue", new GridPosition(4, 3), Facing.East);
        var blueMedic = StarterMilitaryContent.CombatMedic.CreateInitialState(BlueMedicId, "blue", new GridPosition(1, 1), Facing.East);
        var redSkirmisher = StarterMilitaryContent.Rifleman.CreateInitialState(RedSkirmisherId, "red", new GridPosition(7, 3), Facing.West) with { HitPoints = 5 };
        var redReserve = StarterMilitaryContent.CombatMedic.CreateInitialState(RedReserveId, "red", new GridPosition(9, 4), Facing.West);
        return new EncounterState(new EncounterDefinition("secure-the-ford-01", map, Objectives: new[]
        {
            new ObjectiveDefinition("blue-secures-ford", ObjectiveType.HoldAreaForRounds, "blue", "central-ford", RequiredControlRounds: 3)
        }, UnitDefinitions: new[] { StarterMilitaryContent.Rifleman, StarterMilitaryContent.CombatMedic, StarterMilitaryContent.Marksman }, FactionDefinitions: new[]
        {
            new FactionDefinition("blue", new[] { StarterMilitaryContent.Rifleman.Id, StarterMilitaryContent.CombatMedic.Id, StarterMilitaryContent.Marksman.Id }),
            new FactionDefinition("red", new[] { StarterMilitaryContent.Rifleman.Id, StarterMilitaryContent.CombatMedic.Id, StarterMilitaryContent.Marksman.Id })
        }, Reinforcements: new[]
        {
            new ReinforcementSchedule("red-round-two", 2, "red", StarterMilitaryContent.Rifleman.Id, "red-reinforcement-entry", "blue-signal", "blue")
        }), new GameState(new[] { blueRifleman, blueMedic, redSkirmisher, redReserve }));
    }

    public static IReadOnlyList<EncounterRoundResult> Run()
    {
        var encounter = CreateEncounter();
        var results = new List<EncounterRoundResult>();
        for (var round = 0; round < 3; round++)
        {
            var blueActions = round == 0 ? new[]
            {
                new TacticalAction(Guid.Parse("83000000-0000-0000-0000-000000000001"), BlueRiflemanId, TacticalActionType.Attack, 0, 1, AttackProfileId: StarterMilitaryContent.FragmentationGrenade.Id, TargetPosition: new GridPosition(7, 3))
            } : Array.Empty<TacticalAction>();
            var result = EncounterResolver.ResolveRound(encounter, new[] { new CommandBundle("blue", blueActions), new CommandBundle("red", Array.Empty<TacticalAction>()) }, new RoundConfiguration(10), (uint)(20260723 + round), attackProfiles: new[] { StarterMilitaryContent.ServiceRifle, StarterMilitaryContent.MarksmanRifle, StarterMilitaryContent.FragmentationGrenade }, effects: new[] { StarterMilitaryContent.FieldMedKit });
            results.Add(result); encounter = result.NextState;
        }
        return results;
    }
}

}
