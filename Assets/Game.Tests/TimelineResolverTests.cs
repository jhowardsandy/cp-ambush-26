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
        var request = Request(state, Bundle("blue", new TacticalAction(FirstAction, BlueUnit, TacticalActionType.Move, 0, 1, new GridPosition(2, 0))));

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

    private static SimulationRequest Request(params CommandBundle[] bundles) => Request(DefaultState(), bundles);

    private static SimulationRequest Request(GameState state, params CommandBundle[] bundles) => new(state, bundles, new RoundConfiguration(), 1234u);

    private static CommandBundle Bundle(string faction, params TacticalAction[] actions) => new(faction, actions);

    private static GameState DefaultState() => new(new[]
    {
        new UnitState(BlueUnit, "blue", new GridPosition(0, 0), Facing.North, UnitActivityState.Active),
        new UnitState(RedUnit, "red", new GridPosition(1, 0), Facing.South, UnitActivityState.Active)
    });
}

}
