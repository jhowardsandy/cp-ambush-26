#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
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
        Assert.That(result.FinalStateChecksum, Is.EqualTo("7222B46B11F8AC8DB16872404E44CA6240DFD8283F70D3087E427BF96263EFEE"));
    }

    [Test]
    public void Scenario_json_round_trip_preserves_map_terrain_and_units()
    {
        var scenario = new ScenarioDefinition("json-scenario", new GridMapDefinition("json-map", 4, 3, new[]
        {
            new TerrainCellDefinition(new GridPosition(1, 1), MovementTicks: 2),
            new TerrainCellDefinition(new GridPosition(2, 2), IsPassable: false)
        }, new[]
        {
            new MapAreaDefinition("rescue-building", new[] { new GridPosition(1, 1), new GridPosition(1, 2) }),
            new MapAreaDefinition("reinforcement-entry", new[] { new GridPosition(3, 0) })
        }), State(new GridPosition(0, 0), new GridPosition(3, 2)), "content-json-1", new[]
        {
            new ObjectiveDefinition("incapacitate-red", ObjectiveType.IncapacitateAllOpposingUnits, "blue")
        });

        var decoded = ScenarioSerializer.Deserialize(ScenarioSerializer.Serialize(scenario));

        Assert.That(decoded.Id, Is.EqualTo(scenario.Id));
        Assert.That(decoded.Map.Id, Is.EqualTo(scenario.Map.Id));
        Assert.That(decoded.Map.Width, Is.EqualTo(scenario.Map.Width));
        Assert.That(decoded.Map.Height, Is.EqualTo(scenario.Map.Height));
        Assert.That(decoded.Map.Terrain, Is.EqualTo(scenario.Map.Terrain));
        Assert.That(decoded.Map.Areas!.Select(area => area.Id), Is.EqualTo(scenario.Map.Areas!.Select(area => area.Id)));
        Assert.That(decoded.Map.Areas[0].Tiles, Is.EqualTo(scenario.Map.Areas[0].Tiles));
        Assert.That(decoded.Map.Areas[1].Tiles, Is.EqualTo(scenario.Map.Areas[1].Tiles));
        Assert.That(decoded.InitialState.Units, Is.EqualTo(scenario.InitialState.Units));
        Assert.That(decoded.ContentVersion, Is.EqualTo(scenario.ContentVersion));
        Assert.That(decoded.Objectives, Is.EqualTo(scenario.Objectives));
    }

    [Test]
    public void Scenario_rejects_invalid_named_map_areas()
    {
        var scenario = new ScenarioDefinition("invalid-areas", new GridMapDefinition("invalid-areas-map", 3, 3, Areas: new[]
        {
            new MapAreaDefinition("", Array.Empty<GridPosition>()),
            new MapAreaDefinition("duplicate", new[] { new GridPosition(0, 0), new GridPosition(0, 0) }),
            new MapAreaDefinition("duplicate", new[] { new GridPosition(4, 0) })
        }), DefaultState());
        var request = ScenarioFactory.CreateRequest(scenario, Array.Empty<CommandBundle>(), new RoundConfiguration(3), 1234u);

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("missing-map-area-id"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("empty-map-area"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("duplicate-map-area-tile"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("map-area-out-of-bounds"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("duplicate-map-area-id"));
    }

    [Test]
    public void Unit_definition_creates_initial_unit_state_and_round_trips_in_scenario_json()
    {
        var definition = new UnitDefinition("scout", 7, 5, 1, new[] { "scout", "light" }, new[] { "carbine" }, new[] { "first-aid" }, new[] { new NumericAttributeDefinition("evasion", 2) });
        var unit = definition.CreateInitialState(BlueUnit, "blue", new GridPosition(0, 0), Facing.East);
        var scenario = new ScenarioDefinition("unit-content", new GridMapDefinition("unit-content-map", 3, 3), new GameState(new[]
        {
            unit,
            new UnitState(RedUnit, "red", new GridPosition(2, 2), Facing.West, UnitActivityState.Active)
        }), UnitDefinitions: new[] { definition }, FactionDefinitions: new[]
        {
            new FactionDefinition("blue", new[] { "scout" }),
            new FactionDefinition("red")
        });

        var decoded = ScenarioSerializer.Deserialize(ScenarioSerializer.Serialize(scenario));

        Assert.That(unit.HitPoints, Is.EqualTo(7));
        Assert.That(unit.MaxHitPoints, Is.EqualTo(7));
        Assert.That(unit.UnitDefinitionId, Is.EqualTo("scout"));
        Assert.That(decoded.UnitDefinitions!.Count, Is.EqualTo(1));
        Assert.That(decoded.UnitDefinitions[0].Id, Is.EqualTo("scout"));
        Assert.That(decoded.UnitDefinitions[0].MaxHitPoints, Is.EqualTo(7));
        Assert.That(decoded.UnitDefinitions[0].VisionRange, Is.EqualTo(5));
        Assert.That(decoded.UnitDefinitions[0].RoleTags, Is.EqualTo(new[] { "scout", "light" }));
        Assert.That(decoded.UnitDefinitions[0].AttackProfileIds, Is.EqualTo(new[] { "carbine" }));
        Assert.That(decoded.UnitDefinitions[0].EffectIds, Is.EqualTo(new[] { "first-aid" }));
        Assert.That(decoded.UnitDefinitions[0].Attributes!.Select(attribute => new { attribute.Id, attribute.Value }),
            Is.EqualTo(new[] { new { Id = "evasion", Value = 2 } }));
        Assert.That(decoded.FactionDefinitions!.Select(faction => faction.Id), Is.EqualTo(new[] { "blue", "red" }));
        Assert.That(ScenarioValidator.Validate(decoded), Is.Empty);
    }

    [Test]
    public void Scenario_rejects_invalid_or_unknown_unit_definition_references()
    {
        var invalidDefinition = new UnitDefinition("", 0, -1, 0, new[] { "role", "role" }, new[] { "" });
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Active, UnitDefinitionId: "missing"),
            new UnitState(RedUnit, "red", new GridPosition(1, 0), Facing.South, UnitActivityState.Active)
        });
        var scenario = new ScenarioDefinition("invalid-unit-content", new GridMapDefinition("invalid-unit-content-map", 3, 1), state, UnitDefinitions: new[] { invalidDefinition });
        var result = new TimelineResolver().Resolve(ScenarioFactory.CreateRequest(scenario, Array.Empty<CommandBundle>(), new RoundConfiguration(3), 1234u));

        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("missing-unit-definition-id"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("non-positive-unit-definition-hit-points"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("negative-unit-definition-vision"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("non-positive-unit-definition-movement"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("invalid-unit-role-tag"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("invalid-unit-attack-profile-id"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("unknown-unit-definition"));
    }

    [Test]
    public void Scenario_rejects_unknown_factions_and_units_not_allowed_by_faction()
    {
        var definition = new UnitDefinition("scout", 8, 5);
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Active, HitPoints: 8, MaxHitPoints: 8, UnitDefinitionId: "scout"),
            new UnitState(RedUnit, "red", new GridPosition(1, 0), Facing.South, UnitActivityState.Active, HitPoints: 8, MaxHitPoints: 8, UnitDefinitionId: "scout")
        });
        var scenario = new ScenarioDefinition("faction-validation", new GridMapDefinition("faction-validation-map", 3, 1), state,
            UnitDefinitions: new[] { definition },
            FactionDefinitions: new[] { new FactionDefinition("blue", new[] { "rifle" }) });
        var result = new TimelineResolver().Resolve(ScenarioFactory.CreateRequest(scenario, Array.Empty<CommandBundle>(), new RoundConfiguration(3), 1234u));

        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("unit-not-allowed-for-faction"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("unknown-faction-definition"));
    }

    [Test]
    public void Larger_squad_skirmish_fixture_loads_as_a_valid_portable_scenario()
    {
        var fixturePath = Path.Combine(Directory.GetCurrentDirectory(), "docs", "examples", "scenarios", "iron-timeline-squad-skirmish-01.json");
        var scenario = ScenarioSerializer.Deserialize(File.ReadAllText(fixturePath));

        Assert.That(scenario.Map.Width, Is.EqualTo(16));
        Assert.That(scenario.Map.Height, Is.EqualTo(12));
        Assert.That(scenario.InitialState.Units.Count, Is.EqualTo(8));
        Assert.That(scenario.InitialState.Units.Count(unit => unit.FactionId == "blue"), Is.EqualTo(4));
        Assert.That(scenario.InitialState.Units.Count(unit => unit.FactionId == "red"), Is.EqualTo(4));
        Assert.That(scenario.Map.AreaById("old-town-search-zone"), Is.Not.Null);
        Assert.That(scenario.Map.AreaById("red-reinforcement-entry"), Is.Not.Null);
        Assert.That(scenario.UnitDefinitions, Has.Count.EqualTo(4));
        Assert.That(ScenarioValidator.Validate(scenario), Is.Empty);
    }

    [Test]
    public void Line_of_sight_is_blocked_by_an_intermediate_terrain_cell()
    {
        var map = new GridMapDefinition("los-map", 5, 1, new[]
        {
            new TerrainCellDefinition(new GridPosition(2, 0), BlocksLineOfSight: true)
        });

        Assert.That(VisibilityRules.HasLineOfSight(map, new GridPosition(0, 0), new GridPosition(4, 0)), Is.False);
        Assert.That(VisibilityRules.HasLineOfSight(map, new GridPosition(0, 0), new GridPosition(1, 0)), Is.True);
    }

    [Test]
    public void Line_of_sight_is_symmetric_and_excludes_origin_and_target_as_blockers()
    {
        var map = new GridMapDefinition("symmetric-los-map", 5, 5, new[]
        {
            new TerrainCellDefinition(new GridPosition(2, 2), BlocksLineOfSight: true)
        });
        var origin = new GridPosition(0, 0);
        var target = new GridPosition(4, 4);

        Assert.That(VisibilityRules.HasLineOfSight(map, origin, target), Is.EqualTo(VisibilityRules.HasLineOfSight(map, target, origin)));
        Assert.That(VisibilityRules.HasLineOfSight(map, new GridPosition(2, 2), target), Is.True);
    }

    [Test]
    public void Faction_visibility_reveals_an_enemy_within_range_and_clear_line_of_sight()
    {
        var map = new GridMapDefinition("perception-map", 6, 1);
        var state = State(new GridPosition(0, 0), new GridPosition(3, 0));

        var snapshot = PerceptionRules.Evaluate(map, state, "blue", visionRange: 3);

        Assert.That(snapshot.VisibleUnitIds, Is.EqualTo(new[] { BlueUnit, RedUnit }));
        Assert.That(snapshot.CanSee(RedUnit), Is.True);
    }

    [Test]
    public void Faction_visibility_does_not_reveal_an_enemy_behind_a_blocker_or_outside_range()
    {
        var blockedMap = new GridMapDefinition("blocked-perception-map", 6, 1, new[]
        {
            new TerrainCellDefinition(new GridPosition(2, 0), BlocksLineOfSight: true)
        });
        var state = State(new GridPosition(0, 0), new GridPosition(4, 0));

        var blocked = PerceptionRules.Evaluate(blockedMap, state, "blue", visionRange: 4);
        var outOfRange = PerceptionRules.Evaluate(new GridMapDefinition("range-perception-map", 6, 1), state, "blue", visionRange: 3);

        Assert.That(blocked.CanSee(RedUnit), Is.False);
        Assert.That(outOfRange.CanSee(RedUnit), Is.False);
    }

    [Test]
    public void Faction_visibility_always_includes_friendly_units_but_an_incapacitated_observer_reveals_no_enemy()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Incapacitated),
            new UnitState(RedUnit, "red", new GridPosition(1, 0), Facing.South, UnitActivityState.Active)
        });

        var snapshot = PerceptionRules.Evaluate(new GridMapDefinition("incapacitated-observer-map", 3, 1), state, "blue", visionRange: 1);

        Assert.That(snapshot.VisibleUnitIds, Is.EqualTo(new[] { BlueUnit }));
        Assert.That(snapshot.CanSee(RedUnit), Is.False);
    }

    [Test]
    public void Encounter_carries_a_valid_round_result_forward_for_next_round_orders()
    {
        var initialState = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Active, HitPoints: 8, MaxHitPoints: 10),
            new UnitState(RedUnit, "red", new GridPosition(3, 0), Facing.South, UnitActivityState.Active)
        });
        var encounter = new EncounterState(new EncounterDefinition("round-loop", new GridMapDefinition("round-loop-map", 5, 1), "round-loop-content"), initialState);
        var move = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(1, 0) });

        var firstRound = EncounterResolver.ResolveRound(encounter, new[] { Bundle("blue", move) }, new RoundConfiguration(3), 1u);
        var heal = new TacticalAction(SecondAction, BlueUnit, TacticalActionType.ApplyEffect, 0, 1, TargetUnitId: BlueUnit, EffectId: "aid");
        var secondRound = EncounterResolver.ResolveRound(firstRound.NextState, new[] { Bundle("blue", heal) }, new RoundConfiguration(3), 2u,
            effects: new[] { new EffectDefinition("aid", 3) });

        Assert.That(firstRound.NextState.CompletedRounds, Is.EqualTo(1));
        Assert.That(firstRound.NextState.CurrentState.FindUnit(BlueUnit)!.Position, Is.EqualTo(new GridPosition(1, 0)));
        Assert.That(secondRound.NextState.CompletedRounds, Is.EqualTo(2));
        Assert.That(secondRound.NextState.CurrentState.FindUnit(BlueUnit)!.Position, Is.EqualTo(new GridPosition(1, 0)));
        Assert.That(secondRound.NextState.CurrentState.FindUnit(BlueUnit)!.HitPoints, Is.EqualTo(10));
    }

    [Test]
    public void Encounter_does_not_advance_when_submitted_orders_are_invalid()
    {
        var encounter = new EncounterState(new EncounterDefinition("invalid-round-loop", new GridMapDefinition("invalid-round-loop-map", 3, 1)), DefaultState(), CompletedRounds: 2);
        var invalidMove = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(2, 0) });

        var result = EncounterResolver.ResolveRound(encounter, new[] { Bundle("blue", invalidMove) }, new RoundConfiguration(3), 3u);

        Assert.That(result.Resolution.IsValid, Is.False);
        Assert.That(result.NextState, Is.EqualTo(encounter));
    }

    [Test]
    public void Direct_attack_within_range_and_line_of_sight_damages_and_incapacities_target()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.East, UnitActivityState.Active),
            new UnitState(RedUnit, "red", new GridPosition(3, 0), Facing.West, UnitActivityState.Active, HitPoints: 4, MaxHitPoints: 10)
        });
        var scenario = new ScenarioDefinition("direct-attack", new GridMapDefinition("direct-attack-map", 5, 1), state);
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Attack, 0, 1, TargetUnitId: RedUnit, AttackProfileId: "training-rifle");
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", action) }, new RoundConfiguration(3), 1234u,
            attackProfiles: new[] { new AttackProfile("training-rifle", 1, 3, 5) });

        var result = new TimelineResolver().Resolve(request);

        var target = result.FinalState.FindUnit(RedUnit)!;
        Assert.That(target.HitPoints, Is.EqualTo(0));
        Assert.That(target.ActivityState, Is.EqualTo(UnitActivityState.Incapacitated));
        var attack = result.Events.Single(@event => @event.Type == DomainEventType.AttackResolved);
        Assert.That(attack.UnitId, Is.EqualTo(BlueUnit));
        Assert.That(attack.TargetUnitId, Is.EqualTo(RedUnit));
        Assert.That(attack.FromPosition, Is.EqualTo(new GridPosition(0, 0)));
        Assert.That(attack.ToPosition, Is.EqualTo(new GridPosition(3, 0)));
        Assert.That(attack.Detail, Is.EqualTo("attack=training-rifle; distance=3; damage=5; before=4; applied=-4; after=0"));
    }

    [Test]
    public void Direct_attack_fails_at_resolution_when_line_of_sight_is_blocked()
    {
        var state = State(new GridPosition(0, 0), new GridPosition(3, 0));
        var scenario = new ScenarioDefinition("blocked-attack", new GridMapDefinition("blocked-attack-map", 5, 1, new[]
        {
            new TerrainCellDefinition(new GridPosition(1, 0), BlocksLineOfSight: true)
        }), state);
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Attack, 0, 1, TargetUnitId: RedUnit, AttackProfileId: "blocked-rifle");
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", action) }, new RoundConfiguration(3), 1234u,
            attackProfiles: new[] { new AttackProfile("blocked-rifle", 1, 4, 5) });

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FinalState.FindUnit(RedUnit)!.HitPoints, Is.EqualTo(10));
        Assert.That(result.Events.Where(@event => @event.ActionId == FirstAction).Select(@event => @event.Type),
            Is.EqualTo(new[] { DomainEventType.ActionStarted, DomainEventType.ActionFailed }));
        Assert.That(result.Events.Single(@event => @event.Type == DomainEventType.ActionFailed).Detail, Is.EqualTo("Target line of sight is blocked."));
    }

    [Test]
    public void Direct_attack_fails_when_target_moves_out_of_range_before_attack_completes()
    {
        var state = State(new GridPosition(0, 0), new GridPosition(2, 0));
        var scenario = new ScenarioDefinition("moving-target-attack", new GridMapDefinition("moving-target-map", 5, 1), state);
        var attack = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Attack, 0, 2, TargetUnitId: RedUnit, AttackProfileId: "short-rifle");
        var escapeMove = new TacticalAction(SecondAction, RedUnit, TacticalActionType.Move, 0, 2, Path: new[] { new GridPosition(3, 0), new GridPosition(4, 0) });
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", attack), Bundle("red", escapeMove) }, new RoundConfiguration(3), 1234u,
            attackProfiles: new[] { new AttackProfile("short-rifle", 1, 3, 5) });

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FinalState.FindUnit(RedUnit)!.Position, Is.EqualTo(new GridPosition(4, 0)));
        Assert.That(result.FinalState.FindUnit(RedUnit)!.HitPoints, Is.EqualTo(10));
        Assert.That(result.Events.Where(@event => @event.ActionId == FirstAction).Select(@event => @event.Type),
            Is.EqualTo(new[] { DomainEventType.ActionStarted, DomainEventType.ActionFailed }));
        Assert.That(result.Events.Single(@event => @event.ActionId == FirstAction && @event.Type == DomainEventType.ActionFailed).Detail,
            Is.EqualTo("Target distance 4 is outside attack range 1-3."));
    }

    [Test]
    public void Direct_attack_succeeds_when_target_moves_into_range_before_attack_completes()
    {
        var state = State(new GridPosition(0, 0), new GridPosition(4, 0));
        var scenario = new ScenarioDefinition("approaching-target-attack", new GridMapDefinition("approaching-target-map", 5, 1), state);
        var attack = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Attack, 0, 2, TargetUnitId: RedUnit, AttackProfileId: "short-rifle");
        var approachMove = new TacticalAction(SecondAction, RedUnit, TacticalActionType.Move, 0, 2, Path: new[] { new GridPosition(3, 0), new GridPosition(2, 0) });
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", attack), Bundle("red", approachMove) }, new RoundConfiguration(3), 1234u,
            attackProfiles: new[] { new AttackProfile("short-rifle", 1, 3, 5) });

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FinalState.FindUnit(RedUnit)!.Position, Is.EqualTo(new GridPosition(2, 0)));
        Assert.That(result.FinalState.FindUnit(RedUnit)!.HitPoints, Is.EqualTo(5));
        Assert.That(result.Events.Where(@event => @event.ActionId == FirstAction).Select(@event => @event.Type),
            Is.EqualTo(new[] { DomainEventType.ActionStarted, DomainEventType.AttackResolved, DomainEventType.ActionCompleted }));
        Assert.That(result.Events.Single(@event => @event.ActionId == FirstAction && @event.Type == DomainEventType.AttackResolved).Detail,
            Is.EqualTo("attack=short-rifle; distance=2; damage=5; before=10; applied=-5; after=5"));
    }

    [Test]
    public void Direct_attack_rejects_missing_profile_and_friendly_target()
    {
        var scenario = new ScenarioDefinition("invalid-attack", new GridMapDefinition("invalid-attack-map", 3, 1), DefaultState());
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Attack, 0, 1, TargetUnitId: BlueUnit, AttackProfileId: "missing");
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", action) }, new RoundConfiguration(3), 1234u);

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("friendly-attack-target"));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("unknown-attack-profile-id"));
    }

    [Test]
    public void Planned_actions_cannot_exceed_the_unit_action_point_budget()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.East, UnitActivityState.Active, ActionPointBudget: 2),
            new UnitState(RedUnit, "red", new GridPosition(2, 0), Facing.West, UnitActivityState.Active)
        });
        var scenario = new ScenarioDefinition("ap-budget", new GridMapDefinition("ap-budget-map", 4, 1), state);
        var move = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, Path: new[] { new GridPosition(1, 0) });
        var attack = new TacticalAction(SecondAction, BlueUnit, TacticalActionType.Attack, 1, 1, TargetUnitId: RedUnit, AttackProfileId: "rifle");
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", move, attack) }, new RoundConfiguration(3), 1234u,
            attackProfiles: new[] { new AttackProfile("rifle", 1, 3, 5, ActionPointCost: 2) });

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("action-point-budget-exceeded"));
    }

    [Test]
    public void Adjacent_posture_change_updates_state_and_emits_event()
    {
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.ChangePosture, 0, 1, Posture: UnitPosture.Crouched);
        var result = new TimelineResolver().Resolve(Request(Bundle("blue", action)));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FinalState.FindUnit(BlueUnit)!.Posture, Is.EqualTo(UnitPosture.Crouched));
        var posture = result.Events.Single(@event => @event.Type == DomainEventType.PostureChanged);
        Assert.That(posture.PostureAfter, Is.EqualTo(UnitPosture.Crouched));
        Assert.That(posture.Detail, Is.EqualTo("posture=Crouched"));
    }

    [Test]
    public void Posture_change_cannot_skip_an_intermediate_posture()
    {
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.ChangePosture, 0, 1, Posture: UnitPosture.Prone);
        var result = new TimelineResolver().Resolve(Request(Bundle("blue", action)));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("invalid-posture-transition"));
    }

    [Test]
    public void Golden_replay_direct_attack_has_stable_event_sequence_and_checksum()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.East, UnitActivityState.Active),
            new UnitState(RedUnit, "red", new GridPosition(3, 0), Facing.West, UnitActivityState.Active, HitPoints: 4, MaxHitPoints: 10)
        });
        var scenario = new ScenarioDefinition("golden-direct-attack", new GridMapDefinition("golden-direct-attack-map", 5, 1), state, "golden-attack-1");
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Attack, 1, 2, TargetUnitId: RedUnit, AttackProfileId: "golden-rifle");
        var request = ScenarioFactory.CreateRequest(scenario, new[] { Bundle("blue", action) }, new RoundConfiguration(4), 20260720u, "golden-1",
            attackProfiles: new[] { new AttackProfile("golden-rifle", 1, 3, 5) });

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.Events.Select(@event => new { @event.Tick, @event.Type, @event.UnitId, @event.TargetUnitId, @event.ActionId, @event.Detail }), Is.EqualTo(new[]
        {
            new { Tick = 0, Type = DomainEventType.RoundStarted, UnitId = (Guid?)null, TargetUnitId = (Guid?)null, ActionId = (Guid?)null, Detail = (string?)null },
            new { Tick = 1, Type = DomainEventType.ActionStarted, UnitId = (Guid?)BlueUnit, TargetUnitId = (Guid?)null, ActionId = (Guid?)FirstAction, Detail = (string?)null },
            new { Tick = 3, Type = DomainEventType.AttackResolved, UnitId = (Guid?)BlueUnit, TargetUnitId = (Guid?)RedUnit, ActionId = (Guid?)FirstAction, Detail = "attack=golden-rifle; distance=3; damage=5; before=4; applied=-4; after=0" },
            new { Tick = 3, Type = DomainEventType.ActionCompleted, UnitId = (Guid?)BlueUnit, TargetUnitId = (Guid?)null, ActionId = (Guid?)FirstAction, Detail = (string?)null },
            new { Tick = 4, Type = DomainEventType.RoundCompleted, UnitId = (Guid?)null, TargetUnitId = (Guid?)null, ActionId = (Guid?)null, Detail = (string?)null }
        }));
        Assert.That(result.FinalStateChecksum, Is.EqualTo("C46DF81F698505C1ADE95EA668ADC11C04637C510F41061E1303A015CEB8A8E7"));
    }

    [Test]
    public void Eliminate_all_opponents_objective_completes_when_last_enemy_is_incapacitated()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.East, UnitActivityState.Active),
            new UnitState(RedUnit, "red", new GridPosition(2, 0), Facing.West, UnitActivityState.Active, HitPoints: 3, MaxHitPoints: 10)
        });
        var definition = new EncounterDefinition("eliminate-objective", new GridMapDefinition("eliminate-objective-map", 4, 1), Objectives: new[]
        {
            new ObjectiveDefinition("incapacitate-red", ObjectiveType.IncapacitateAllOpposingUnits, "blue")
        });
        var encounter = new EncounterState(definition, state);
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Attack, 0, 1, TargetUnitId: RedUnit, AttackProfileId: "objective-rifle");

        var result = EncounterResolver.ResolveRound(encounter, new[] { Bundle("blue", action) }, new RoundConfiguration(3), 1234u,
            attackProfiles: new[] { new AttackProfile("objective-rifle", 1, 2, 5) });

        Assert.That(result.NextState.Outcome, Is.EqualTo(new EncounterOutcome(true, "blue", "objective=incapacitate-red; winner=blue; opposing-active-units=0")));
        Assert.That(result.NextState.CurrentState.FindUnit(RedUnit)!.ActivityState, Is.EqualTo(UnitActivityState.Incapacitated));
    }

    [Test]
    public void Eliminate_all_opponents_objective_remains_incomplete_while_an_enemy_is_active()
    {
        var state = DefaultState();
        var outcome = ObjectiveRules.Evaluate(new[]
        {
            new ObjectiveDefinition("incapacitate-red", ObjectiveType.IncapacitateAllOpposingUnits, "blue")
        }, state);

        Assert.That(outcome, Is.EqualTo(new EncounterOutcome(false)));
    }

    [Test]
    public void Completed_encounter_cannot_resolve_another_round()
    {
        var encounter = new EncounterState(
            new EncounterDefinition("completed-encounter", new GridMapDefinition("completed-encounter-map", 3, 1)),
            DefaultState(),
            CompletedRounds: 3,
            Outcome: new EncounterOutcome(true, "blue", "test-complete"));

        Assert.That(() => EncounterResolver.ResolveRound(encounter, Array.Empty<CommandBundle>(), new RoundConfiguration(3), 1234u),
            Throws.InvalidOperationException);
    }

    [Test]
    public void Healing_effect_clamps_to_maximum_and_emits_its_calculation()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Active, HitPoints: 8, MaxHitPoints: 10),
            new UnitState(RedUnit, "red", new GridPosition(1, 0), Facing.South, UnitActivityState.Active)
        });
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.ApplyEffect, 1, 1, TargetUnitId: BlueUnit, EffectId: "field-med-kit");
        var request = new SimulationRequest(state, new[] { Bundle("blue", action) }, new RoundConfiguration(), 1234u,
            Effects: new[] { new EffectDefinition("field-med-kit", 5) });

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.FinalState.FindUnit(BlueUnit)!.HitPoints, Is.EqualTo(10));
        var effect = result.Events.Single(@event => @event.Type == DomainEventType.EffectApplied);
        Assert.That(effect.Tick, Is.EqualTo(2));
        Assert.That(effect.UnitId, Is.EqualTo(BlueUnit));
        Assert.That(effect.Detail, Is.EqualTo("effect=field-med-kit; before=8; requested=5; applied=2; after=10"));
        Assert.That(effect.HitPointsAfter, Is.EqualTo(10));
        Assert.That(effect.ActivityStateAfter, Is.EqualTo(UnitActivityState.Active));
    }

    [Test]
    public void Damaging_effect_clamps_at_zero_and_incapacitates_the_target()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Active),
            new UnitState(RedUnit, "red", new GridPosition(1, 0), Facing.South, UnitActivityState.Active, HitPoints: 3, MaxHitPoints: 10)
        });
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.ApplyEffect, 0, 1, TargetUnitId: RedUnit, EffectId: "training-impact");
        var request = new SimulationRequest(state, new[] { Bundle("blue", action) }, new RoundConfiguration(), 1234u,
            Effects: new[] { new EffectDefinition("training-impact", -5) });

        var result = new TimelineResolver().Resolve(request);

        var target = result.FinalState.FindUnit(RedUnit)!;
        Assert.That(target.HitPoints, Is.EqualTo(0));
        Assert.That(target.ActivityState, Is.EqualTo(UnitActivityState.Incapacitated));
        Assert.That(result.Events.Single(@event => @event.Type == DomainEventType.EffectApplied).Detail,
            Is.EqualTo("effect=training-impact; before=3; requested=-5; applied=-3; after=0"));
    }

    [Test]
    public void Effect_action_with_an_unknown_definition_is_rejected()
    {
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.ApplyEffect, 0, 1, TargetUnitId: RedUnit, EffectId: "not-in-content");
        var result = new TimelineResolver().Resolve(Request(Bundle("blue", action)));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Does.Contain("unknown-effect-id"));
    }

    [Test]
    public void Effect_content_round_trips_in_a_replay()
    {
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.ApplyEffect, 0, 1, TargetUnitId: BlueUnit, EffectId: "aid");
        var inputs = new SimulationRequest(DefaultState(), new[] { Bundle("blue", action) }, new RoundConfiguration(), 1234u,
            Effects: new[] { new EffectDefinition("aid", 2) });
        var output = new TimelineResolver().Resolve(inputs);

        var decoded = ReplaySerializer.Deserialize(ReplaySerializer.Serialize(new ReplayRecord(inputs, output)));

        Assert.That(decoded.Inputs.Effects, Is.EqualTo(inputs.Effects));
    }

    [Test]
    public void Golden_replay_vitality_effect_has_stable_event_sequence_and_checksum()
    {
        var state = new GameState(new[]
        {
            new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Active, HitPoints: 4, MaxHitPoints: 10),
            new UnitState(RedUnit, "red", new GridPosition(2, 0), Facing.South, UnitActivityState.Active)
        });
        var action = new TacticalAction(FirstAction, BlueUnit, TacticalActionType.ApplyEffect, 1, 2, TargetUnitId: BlueUnit, EffectId: "golden-aid");
        var request = new SimulationRequest(state, new[] { Bundle("blue", action) }, new RoundConfiguration(4), 20260720u,
            SimulationVersion: "golden-1", ContentVersion: "golden-effects-1", Effects: new[] { new EffectDefinition("golden-aid", 3) });

        var result = new TimelineResolver().Resolve(request);

        Assert.That(result.Events.Select(@event => new { @event.Tick, @event.Type, @event.UnitId, @event.ActionId, @event.Detail }), Is.EqualTo(new[]
        {
            new { Tick = 0, Type = DomainEventType.RoundStarted, UnitId = (Guid?)null, ActionId = (Guid?)null, Detail = (string?)null },
            new { Tick = 1, Type = DomainEventType.ActionStarted, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction, Detail = (string?)null },
            new { Tick = 3, Type = DomainEventType.EffectApplied, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction, Detail = "effect=golden-aid; before=4; requested=3; applied=3; after=7" },
            new { Tick = 3, Type = DomainEventType.ActionCompleted, UnitId = (Guid?)BlueUnit, ActionId = (Guid?)FirstAction, Detail = (string?)null },
            new { Tick = 4, Type = DomainEventType.RoundCompleted, UnitId = (Guid?)null, ActionId = (Guid?)null, Detail = (string?)null }
        }));
        Assert.That(result.FinalStateChecksum, Is.EqualTo("B566038CC280E781776128426632E8D07B6417F9E28E2B04B69F5D803E6599D3"));
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
