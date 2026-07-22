#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

/// <summary>Content shared across all rounds of one reusable tactical encounter.</summary>
public sealed record EncounterDefinition(
    string Id,
    GridMapDefinition Map,
    string ContentVersion = "1",
    IReadOnlyList<ObjectiveDefinition>? Objectives = null,
    IReadOnlyList<UnitDefinition>? UnitDefinitions = null,
    IReadOnlyList<FactionDefinition>? FactionDefinitions = null);

/// <summary>Authoritative state at the planning boundary before a new round is ordered.</summary>
public sealed record EncounterState(EncounterDefinition Definition, GameState CurrentState, int CompletedRounds = 0, EncounterOutcome? Outcome = null, IReadOnlyList<ObjectiveProgress>? ObjectiveProgress = null, IReadOnlyList<FactionKnowledgeState>? FactionKnowledge = null);

public sealed record EncounterRoundResult(EncounterState NextState, SimulationResult Resolution);

/// <summary>Builds each round from the previous resolved state; presentation never carries state forward itself.</summary>
public static class EncounterResolver
{
    public static EncounterRoundResult ResolveRound(
        EncounterState encounter,
        IReadOnlyList<CommandBundle> commandBundles,
        RoundConfiguration configuration,
        uint randomSeed,
        string simulationVersion = "1",
        IReadOnlyList<EffectDefinition>? effects = null,
        IReadOnlyList<AttackProfile>? attackProfiles = null)
    {
        if (encounter == null) throw new ArgumentNullException(nameof(encounter));
        if (commandBundles == null) throw new ArgumentNullException(nameof(commandBundles));
        if (encounter.Outcome?.IsComplete == true)
            throw new InvalidOperationException("A completed encounter cannot resolve another round.");

        var scenario = new ScenarioDefinition(encounter.Definition.Id, encounter.Definition.Map, encounter.CurrentState, encounter.Definition.ContentVersion,
            encounter.Definition.Objectives, encounter.Definition.UnitDefinitions, encounter.Definition.FactionDefinitions);
        var request = ScenarioFactory.CreateRequest(scenario, commandBundles, configuration, randomSeed, simulationVersion, effects, attackProfiles);
        var resolution = new TimelineResolver().Resolve(request);
        var evaluation = resolution.IsValid
            ? ObjectiveRules.Evaluate(encounter.Definition.Objectives, encounter.Definition.Map, resolution.FinalState, encounter.ObjectiveProgress)
            : null;
        var outcome = resolution.IsValid ? evaluation!.Outcome : encounter.Outcome;
        if (!resolution.IsValid)
            return new EncounterRoundResult(encounter, resolution);

        var completedRounds = encounter.CompletedRounds + 1;
        resolution = AppendRescueEvents(encounter, evaluation!.Progress, configuration, resolution);
        var knowledge = UpdateKnowledge(encounter, resolution.FinalState, completedRounds, configuration, resolution);
        var nextState = encounter with { CurrentState = resolution.FinalState, CompletedRounds = completedRounds, Outcome = outcome, ObjectiveProgress = evaluation!.Progress, FactionKnowledge = knowledge.Knowledge };
        return new EncounterRoundResult(nextState, knowledge.Resolution);
    }

    private static (IReadOnlyList<FactionKnowledgeState> Knowledge, SimulationResult Resolution) UpdateKnowledge(EncounterState encounter, GameState finalState, int completedRounds, RoundConfiguration configuration, SimulationResult resolution)
    {
        var previous = encounter.FactionKnowledge ?? Array.Empty<FactionKnowledgeState>();
        var events = resolution.Events.ToList();
        var knowledge = new List<FactionKnowledgeState>();
        foreach (var factionId in finalState.Units.Select(unit => unit.FactionId).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
        {
            var snapshot = PerceptionRules.Evaluate(encounter.Definition.Map, finalState, factionId);
            var visibleEnemies = snapshot.VisibleUnitIds.Where(id => !StringComparer.Ordinal.Equals(finalState.FindUnit(id)!.FactionId, factionId)).OrderBy(id => id).ToArray();
            var prior = previous.FirstOrDefault(item => StringComparer.Ordinal.Equals(item.FactionId, factionId));
            var priorVisible = (prior?.VisibleEnemyUnitIds ?? Array.Empty<Guid>()).ToHashSet();
            var contacts = (prior?.Contacts ?? Array.Empty<KnownContact>()).ToDictionary(contact => contact.UnitId);
            foreach (var unitId in visibleEnemies)
            {
                var target = finalState.FindUnit(unitId)!;
                contacts[unitId] = new KnownContact(unitId, target.Position, completedRounds);
                if (!priorVisible.Contains(unitId))
                    events.Add(new DomainEvent(events.Count, configuration.TicksPerRound, DomainEventType.ContactRevealed, factionId, unitId, Detail: $"contact={unitId}; position=({target.Position.X},{target.Position.Y}); observed-round={completedRounds}", ToPosition: target.Position));
            }
            foreach (var unitId in priorVisible.Except(visibleEnemies).OrderBy(id => id))
            {
                var contact = contacts[unitId];
                events.Add(new DomainEvent(events.Count, configuration.TicksPerRound, DomainEventType.ContactLost, factionId, unitId, Detail: $"contact={unitId}; last-known=({contact.LastKnownPosition.X},{contact.LastKnownPosition.Y}); observed-round={contact.LastObservedRound}", ToPosition: contact.LastKnownPosition));
            }
            knowledge.Add(new FactionKnowledgeState(factionId, visibleEnemies, contacts.Values.OrderBy(contact => contact.UnitId).ToArray()));
        }
        return (knowledge, resolution with { Events = events });
    }

    private static SimulationResult AppendRescueEvents(EncounterState encounter, IReadOnlyList<ObjectiveProgress> progress, RoundConfiguration configuration, SimulationResult resolution)
    {
        var events = resolution.Events.ToList();
        foreach (var objective in encounter.Definition.Objectives?.Where(item => item.Type == ObjectiveType.RescueAndExtract) ?? Array.Empty<ObjectiveDefinition>())
        {
            var prior = encounter.ObjectiveProgress?.FirstOrDefault(item => item.ObjectiveId == objective.Id)?.RescuerUnitId;
            var current = progress.FirstOrDefault(item => item.ObjectiveId == objective.Id)?.RescuerUnitId;
            if (!prior.HasValue && current.HasValue)
                events.Add(new DomainEvent(events.Count, configuration.TicksPerRound, DomainEventType.RescueDiscovered, objective.WinningFactionId, current, Detail: $"objective={objective.Id}; rescue-area={objective.AreaId}; rescuer={current}"));
            if (resolution.FinalState.FindUnit(current ?? Guid.Empty) is { } rescuer && encounter.Definition.Map.AreaById(objective.ExtractionAreaId ?? String.Empty)?.Tiles.Contains(rescuer.Position) == true)
                events.Add(new DomainEvent(events.Count, configuration.TicksPerRound, DomainEventType.RescueExtracted, objective.WinningFactionId, rescuer.Id, Detail: $"objective={objective.Id}; extraction-area={objective.ExtractionAreaId}; rescuer={rescuer.Id}"));
        }
        return resolution with { Events = events };
    }
}

}
