#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TacticalStrategyGame.Core;

namespace TacticalStrategyGame.Tests
{

public sealed class TimelineResolverTests
{
    private static readonly Guid BlueUnit = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RedUnit = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FirstAction = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SecondAction = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Test]
    public void Orders_same_tick_by_faction_then_unit_then_action_id()
    {
        var request = Request(
            Bundle("red", new TacticalAction(SecondAction, RedUnit, TacticalActionType.Wait, 5, 1)),
            Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Wait, 5, 1)));

        var starts = new TimelineResolver().Resolve(request).Events.Where(e => e.Type == DomainEventType.ActionStarted).ToArray();

        Assert.That(starts.Select(e => e.FactionId), Is.EqualTo(new[] { "blue", "red" }));
    }

    [Test]
    public void Units_from_multiple_factions_share_one_timeline()
    {
        var result = new TimelineResolver().Resolve(Request(
            Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Wait, 2, 4)),
            Bundle("red", new TacticalAction(SecondAction, RedUnit, TacticalActionType.Wait, 3, 2))));

        Assert.That(result.Events.Where(e => e.Type == DomainEventType.ActionStarted).Select(e => e.Tick), Is.EqualTo(new[] { 2, 3 }));
        Assert.That(result.Events.Where(e => e.Type == DomainEventType.ActionCompleted).Select(e => e.Tick), Is.EqualTo(new[] { 5, 6 }));
    }

    [Test]
    public void Starts_precede_completions_on_the_same_tick()
    {
        var result = new TimelineResolver().Resolve(Request(
            Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Wait, 0, 5)),
            Bundle("red", new TacticalAction(SecondAction, RedUnit, TacticalActionType.Wait, 5, 1))));

        var eventsAtFive = result.Events.Where(e => e.Tick == 5).ToArray();
        Assert.That(eventsAtFive.Select(e => e.Type), Is.EqualTo(new[] { DomainEventType.ActionStarted, DomainEventType.ActionCompleted }));
    }

    [TestCase(-1, 1, "negative-start")]
    [TestCase(0, 0, "non-positive-duration")]
    public void Invalid_action_timing_is_rejected(int start, int duration, string expectedDiagnostic)
    {
        var result = new TimelineResolver().Resolve(Request(Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Wait, start, duration))));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain(expectedDiagnostic));
    }

    [Test]
    public void Action_beyond_round_is_rejected()
    {
        var result = new TimelineResolver().Resolve(Request(Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Wait, 99, 2))));

        Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain("round-overrun"));
    }

    [Test]
    public void Incapacitated_unit_action_fails_without_completion()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Incapacitated),
            new UnitState(RedUnit, "red", new GridPosition(1, 0), Facing.South, UnitActivityState.Active)
        });
        var request = Request(state, Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, new GridPosition(1, 0))));

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.Events.Select(e => e.Type), Does.Contain(DomainEventType.ActionFailed));
        Assert.That(result.Events.Where(e => e.ActionId == FirstAction).Select(e => e.Type), Has.None.EqualTo(DomainEventType.ActionCompleted));
        Assert.That(result.FinalState.FindUnit(BlueUnit)!.Position, Is.EqualTo(new GridPosition(0, 0)));
    }

    [Test]
    public void Replay_serialization_round_trips()
    {
        var inputs = Request(Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Rotate, 0, 1, Facing: Facing.East)));
        var result = new TimelineResolver().Resolve(inputs);

        var decoded = ReplaySerializer.Deserialize(ReplaySerializer.Serialize(new ReplayRecord(inputs, result)));

        Assert.That(decoded.Output.FinalStateChecksum, Is.EqualTo(result.FinalStateChecksum));
        Assert.That(decoded.Inputs.RandomSeed, Is.EqualTo(inputs.RandomSeed));
    }

    [Test]
    public void Identical_inputs_produce_identical_event_log_and_checksum()
    {
        var request = Request(Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 3, new GridPosition(3, 1))));
        var resolver = new TimelineResolver();

        var first = resolver.Resolve(request);
        var second = resolver.Resolve(request);

        Assert.That(second.Events, Is.EqualTo(first.Events));
        Assert.That(second.FinalStateChecksum, Is.EqualTo(first.FinalStateChecksum));
    }

    [Test]
    public void Different_commands_produce_different_results()
    {
        var first = new TimelineResolver().Resolve(Request(Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, new GridPosition(1, 0)))));
        var second = new TimelineResolver().Resolve(Request(Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, new GridPosition(0, 1)))));

        Assert.That(second.FinalStateChecksum, Is.Not.EqualTo(first.FinalStateChecksum));
    }

    [Test]
    public void Move_enters_each_cardinal_path_tile_on_its_matching_tick()
    {
        var path = new[] { new GridPosition(1, 0), new GridPosition(1, 1), new GridPosition(2, 1) };
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 2, 3, Path: path);

        var state = State(new GridPosition(0, 0), new GridPosition(10, 0));
        var result = new TimelineResolver().Resolve(Request(state, Bundle("blue", action)));

        var entries = result.Events.Where(e => e.Type == DomainEventType.UnitEnteredTile).ToArray();
        Assert.That(entries.Select(e => e.Tick), Is.EqualTo(new[] { 3, 4, 5 }));
        Assert.That(entries.Select(e => e.ToPosition), Is.EqualTo(path));
        Assert.That(result.FinalState.FindUnit(BlueUnit)!.Position, Is.EqualTo(new GridPosition(2, 1)));
        Assert.That(result.FinalState.FindUnit(BlueUnit)!.Facing, Is.EqualTo(Facing.North));
    }

    [Test]
    public void Diagonal_path_step_is_rejected()
    {
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(1, 1) });

        var result = new TimelineResolver().Resolve(Request(Bundle("blue", action)));

        Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain("invalid-movement-step"));
    }

    [Test]
    public void Move_duration_must_match_number_of_path_tiles()
    {
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 3, Path: new[] { new GridPosition(1, 0), new GridPosition(2, 0) });

        var result = new TimelineResolver().Resolve(Request(Bundle("blue", action)));

        Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain("movement-duration-mismatch"));
    }

    [Test]
    public void Simultaneous_contested_destination_fails_all_movers_without_priority()
    {
        var blue = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(0, 1) });
        var red = new TacticalAction(SecondAction, RedUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(0, 1) });

        var result = new TimelineResolver().Resolve(Request(State(new GridPosition(0, 0), new GridPosition(0, 2)), Bundle("blue", blue), Bundle("red", red)));

        Assert.That(result.Events.Where(e => e.Type == DomainEventType.ActionFailed).Select(e => e.ActionId), Is.EquivalentTo(new[] { FirstAction, SecondAction }));
        Assert.That(result.FinalState.FindUnit(BlueUnit)!.Position, Is.EqualTo(new GridPosition(0, 0)));
        Assert.That(result.FinalState.FindUnit(RedUnit)!.Position, Is.EqualTo(new GridPosition(0, 2)));
    }

    [Test]
    public void Move_into_occupied_tile_fails_even_when_occupant_leaves_on_same_tick()
    {
        var blue = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(1, 0) });
        var red = new TacticalAction(SecondAction, RedUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(2, 0) });

        var result = new TimelineResolver().Resolve(Request(Bundle("blue", blue), Bundle("red", red)));

        Assert.That(result.Events.Where(e => e.ActionId == FirstAction).Select(e => e.Type), Does.Contain(DomainEventType.ActionFailed));
        Assert.That(result.FinalState.FindUnit(BlueUnit)!.Position, Is.EqualTo(new GridPosition(0, 0)));
        Assert.That(result.FinalState.FindUnit(RedUnit)!.Position, Is.EqualTo(new GridPosition(2, 0)));
    }

    [Test]
    public void Swap_attempt_fails_for_both_units()
    {
        var blue = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(1, 0) });
        var red = new TacticalAction(SecondAction, RedUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(0, 0) });

        var result = new TimelineResolver().Resolve(Request(Bundle("blue", blue), Bundle("red", red)));

        Assert.That(result.Events.Where(e => e.Type == DomainEventType.ActionFailed).Select(e => e.ActionId), Is.EquivalentTo(new[] { FirstAction, SecondAction }));
        Assert.That(result.FinalState.Units.Select(unit => unit.Position), Is.EquivalentTo(new[] { new GridPosition(0, 0), new GridPosition(1, 0) }));
    }

    [Test]
    public void Scenario_request_rejects_units_and_movement_outside_its_map()
    {
        var scenario = new ScenarioDefinition("test-map", new GridMapDefinition("map-2x2", 2, 2), DefaultState());
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(2, 0) });
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", action) }, new RoundConfiguration(), 1234u);

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain("movement-out-of-bounds"));
    }

    [Test]
    public void Scenario_definition_validates_map_dimensions_and_starting_positions()
    {
        var scenario = new ScenarioDefinition("bad-map", new GridMapDefinition("bad", 0, 1), State(new GridPosition(0, 0), new GridPosition(2, 0)));

        var diagnostics = ScenarioValidator.Validate(scenario);

        Assert.That(diagnostics.Select(d => d.Code), Does.Contain("invalid-map-size"));
        Assert.That(diagnostics.Select(d => d.Code), Does.Contain("unit-out-of-bounds"));
    }

    [Test]
    public void Replay_round_trip_preserves_scenario_definition()
    {
        var scenario = new ScenarioDefinition("arena", new GridMapDefinition("arena-map", 4, 4), DefaultState(), "content-7");
        var inputs = ScenarioFactory.CreateRequest(scenario, Array.Empty<CommandBundle>(), new RoundConfiguration(), 44u);
        var result = new TimelineResolver().Resolve(inputs);

        var decoded = ReplaySerializer.Deserialize(ReplaySerializer.Serialize(new ReplayRecord(inputs, result)));

        Assert.That(decoded.Inputs.Scenario!.Map, Is.EqualTo(scenario.Map));
        Assert.That(decoded.Inputs.ContentVersion, Is.EqualTo("content-7"));
    }

    [Test]
    public void Terrain_movement_ticks_delay_entry_and_determine_action_duration()
    {
        var map = new GridMapDefinition("terrain-map", 4, 4, new[]
        {
            new TerrainCellDefinition(new GridPosition(1, 0), MovementTicks: 2)
        });
        var scenario = new ScenarioDefinition("terrain-scenario", map, State(new GridPosition(0, 0), new GridPosition(3, 3)));
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 3, Path: new[] { new GridPosition(1, 0), new GridPosition(2, 0) });
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", action) }, new RoundConfiguration(), 1234u);

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.Events.Where(e => e.Type == DomainEventType.UnitEnteredTile).Select(e => e.Tick), Is.EqualTo(new[] { 2, 3 }));
        Assert.That(result.FinalState.FindUnit(BlueUnit)!.Position, Is.EqualTo(new GridPosition(2, 0)));
    }

    [Test]
    public void Impassable_terrain_rejects_movement_path()
    {
        var map = new GridMapDefinition("blocked-map", 3, 3, new[]
        {
            new TerrainCellDefinition(new GridPosition(1, 0), IsPassable: false)
        });
        var scenario = new ScenarioDefinition("blocked-scenario", map, State(new GridPosition(0, 0), new GridPosition(2, 2)));
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(1, 0) });
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", action) }, new RoundConfiguration(), 1234u);

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain("impassable-movement-path"));
    }

    [Test]
    public void Golden_replay_terrain_delay_has_stable_events_and_checksum()
    {
        var map = new GridMapDefinition("golden-terrain-map", 4, 4, new[]
        {
            new TerrainCellDefinition(new GridPosition(1, 0), MovementTicks: 2)
        });
        var scenario = new ScenarioDefinition("golden-terrain-delay", map, State(new GridPosition(0, 0), new GridPosition(3, 3)), "golden-1");
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 3, Path: new[] { new GridPosition(1, 0), new GridPosition(2, 0) });
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", action) }, new RoundConfiguration(5), 20260720u, "golden-1");

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.Events.Select(@event => new { @event.Tick, @event.Type, @event.UnitId, @event.ActionId }), Is.EqualTo(new[]
        {
            new { Tick = 0, Type = DomainEventType.RoundStarted, UnitId = (Guid?)null, ActionId = (Guid?)null },
            new { Tick = 0, Type = DomainEventType.ActionStarted, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction },
            new { Tick = 2, Type = DomainEventType.UnitExitedTile, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction },
            new { Tick = 2, Type = DomainEventType.UnitEnteredTile, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction },
            new { Tick = 3, Type = DomainEventType.UnitExitedTile, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction },
            new { Tick = 3, Type = DomainEventType.UnitEnteredTile, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction },
            new { Tick = 3, Type = DomainEventType.ActionCompleted, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction },
            new { Tick = 5, Type = DomainEventType.RoundCompleted, UnitId = (Guid?)null, ActionId = (Guid?)null }
        }));
        Assert.That(result.FinalStateChecksum, Is.EqualTo("8A641DD03771B4B48B8223499FE694A48199F879F1C69B26F5F1B1167E98907E"));
    }

    [Test]
    public void Scenario_json_round_trip_preserves_map_terrain_and_units()
    {
        var scenario = new ScenarioDefinition("json-scenario", new GridMapDefinition("json-map", 4, 3, new[]
        {
            new TerrainCellDefinition(new GridPosition(1, 1), MovementTicks: 2),
            new TerrainCellDefinition(new GridPosition(2, 2), IsPassable: false)
        }), State(new GridPosition(0, 0), new GridPosition(3, 2)), "content-json-1");

        var decoded = ScenarioSerializer.Deserialize(ScenarioSerializer.Serialize(scenario));

        Assert.That(decoded.Id, Is.EqualTo(scenario.Id));
        Assert.That(decoded.Map.Id, Is.EqualTo(scenario.Map.Id));
        Assert.That(decoded.Map.Width, Is.EqualTo(scenario.Map.Width));
        Assert.That(decoded.Map.Height, Is.EqualTo(scenario.Map.Height));
        Assert.That(decoded.Map.Terrain, Is.EqualTo(scenario.Map.Terrain));
        Assert.That(decoded.InitialState.Units, Is.EqualTo(scenario.InitialState.Units));
        Assert.That(decoded.ContentVersion, Is.EqualTo(scenario.ContentVersion));
    }

    private static SimulationRequest Request(params CommandBundle[] bundles) => Request(DefaultState(), bundles);

    private static SimulationRequest Request(GameState state, params CommandBundle[] bundles) => new(state, bundles, new RoundConfiguration(), 1234u);

    private static CommandBundle Bundle(string faction, params TacticalAction[] actions) => new(faction, actions);

    private static GameState DefaultState() => new(new[]
    {
        new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Active),
        new UnitState(RedUnit, "red", new GridPosition(1, 0), Facing.South, UnitActivityState.Active)
    });

    private static GameState State(GridPosition bluePosition, GridPosition redPosition) => new(new[]
    {
        new UnitState(BlueUnit, "blue", bluePosition, Facing.North, UnitActivityState.Active),
        new UnitState(RedUnit, "red", redPosition, Facing.South, UnitActivityState.Active)
    });
}

}
