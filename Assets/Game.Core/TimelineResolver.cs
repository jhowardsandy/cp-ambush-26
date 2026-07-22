#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticalStrategyGame.Core
{

public sealed class TimelineResolver
{
    public SimulationResult Resolve(SimulationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        request.Configuration.Validate();

        var diagnostics = Validate(request);
        if (diagnostics.Count > 0)
            return new SimulationResult(request.InitialState, Array.Empty<DomainEvent>(), diagnostics, StateChecksum.Calculate(request.InitialState));

        var scheduled = request.CommandBundles
            .SelectMany(bundle => bundle.Actions.Select(action => new ScheduledAction(bundle.FactionId, action)))
            .ToArray();
        var events = new List<DomainEvent>();
        var failedActions = new HashSet<Guid>();
        var startedActions = new HashSet<Guid>();
        var completedActions = new HashSet<Guid>();
        var timelineDelays = new Dictionary<Guid, int>();
        var random = new Random(unchecked((int)request.RandomSeed));
        var state = request.InitialState;
        var sequence = 0;

        AddEvent(0, DomainEventType.RoundStarted, string.Empty);

        for (var tick = 0; tick <= request.Configuration.TicksPerRound; tick++)
        {
            foreach (var item in scheduled.Where(item => !startedActions.Contains(item.Action.ActionId) && item.Action.StartTick + DelayFor(item.Action.UnitId) <= tick).OrderBy(item => item, ScheduledActionComparer.Instance))
            {
                startedActions.Add(item.Action.ActionId);
                var unit = state.FindUnit(item.Action.UnitId)!;
                if (unit.ActivityState != UnitActivityState.Active)
                {
                    failedActions.Add(item.Action.ActionId);
                    AddEvent(tick, DomainEventType.ActionFailed, item.FactionId, unit.Id, item.Action.ActionId, "Unit is not active.");
                    continue;
                }

                AddEvent(tick, DomainEventType.ActionStarted, item.FactionId, unit.Id, item.Action.ActionId);
            }

            ResolveMovementSteps(tick);
            ResolveOverwatchReactions(tick);

            foreach (var item in scheduled.Where(item => !completedActions.Contains(item.Action.ActionId) && item.Action.StartTick + item.Action.DurationTicks + DelayFor(item.Action.UnitId) == tick).OrderBy(item => item, ScheduledActionComparer.Instance))
            {
                if (failedActions.Contains(item.Action.ActionId))
                    continue;

                var unit = state.FindUnit(item.Action.UnitId)!;
                var completion = ApplyCompletion(state, unit, item.Action, request.Effects, request.AttackProfiles, request.Scenario?.Map, random);
                if (completion.FailureDetail is not null)
                {
                    failedActions.Add(item.Action.ActionId);
                    AddEvent(tick, DomainEventType.ActionFailed, item.FactionId, unit.Id, item.Action.ActionId, completion.FailureDetail);
                    continue;
                }
                state = completion.State;
                completedActions.Add(item.Action.ActionId);
                if (completion.Effect is not null)
                {
                    AddEvent(tick, DomainEventType.EffectApplied, item.FactionId, completion.Effect.Target.Id, item.Action.ActionId,
                        $"effect={completion.EffectDefinition!.Id}; before={completion.BeforeHitPoints}; requested={completion.EffectDefinition.VitalityDelta}; applied={completion.Effect.AppliedVitalityDelta}; after={completion.Effect.Target.HitPoints}{completion.InventoryConsumptionDetail}",
                        hitPointsAfter: completion.Effect.Target.HitPoints, activityStateAfter: completion.Effect.Target.ActivityState);
                }
                if (completion.Attack is not null)
                {
                    var targetAfter = state.FindUnit(item.Action.TargetUnitId!.Value)!;
                    AddEvent(tick, DomainEventType.AttackResolved, item.FactionId, unit.Id, item.Action.ActionId,
                        $"{AttackDetail("attack", completion.AttackProfile!, completion.Attack, completion.BeforeHitPoints, targetAfter.HitPoints)}{completion.InventoryConsumptionDetail}",
                        fromPosition: unit.Position, toPosition: targetAfter.Position,
                        hitPointsAfter: targetAfter.HitPoints, activityStateAfter: targetAfter.ActivityState,
                        targetUnitId: targetAfter.Id);
                }
                if (item.Action.Type == TacticalActionType.ChangePosture && item.Action.Posture is not null)
                    AddEvent(tick, DomainEventType.PostureChanged, item.FactionId, unit.Id, item.Action.ActionId,
                        $"posture={item.Action.Posture}", postureAfter: item.Action.Posture);
                if (item.Action.Type == TacticalActionType.EnterOverwatch && item.Action.Facing is not null)
                    AddEvent(tick, DomainEventType.OverwatchArmed, item.FactionId, unit.Id, item.Action.ActionId,
                        $"facing={item.Action.Facing}; profile={item.Action.AttackProfileId}");
                AddEvent(tick, DomainEventType.ActionCompleted, item.FactionId, unit.Id, item.Action.ActionId);
            }
        }

        foreach (var item in scheduled.Where(item => startedActions.Contains(item.Action.ActionId) && !completedActions.Contains(item.Action.ActionId) && !failedActions.Contains(item.Action.ActionId)))
        {
            failedActions.Add(item.Action.ActionId);
            AddEvent(request.Configuration.TicksPerRound, DomainEventType.ActionFailed, item.FactionId, item.Action.UnitId, item.Action.ActionId, "Action was delayed beyond the round.");
        }

        AddEvent(request.Configuration.TicksPerRound, DomainEventType.RoundCompleted, string.Empty);
        state = new GameState(state.Units.Select(unit => unit with { Overwatch = null }).ToArray());
        return new SimulationResult(state, events, diagnostics, StateChecksum.Calculate(state));

        void ResolveMovementSteps(int tick)
        {
            var intents = scheduled
                .Where(item => item.Action.Type == TacticalActionType.Move && !failedActions.Contains(item.Action.ActionId))
                .Select(item => new MovementIntent(item, MovementRules.StepIndexAtTick(item.Action, request.Scenario?.Map, tick - DelayFor(item.Action.UnitId))))
                .Where(intent => intent.StepIndex >= 0)
                .OrderBy(intent => intent.Scheduled, ScheduledActionComparer.Instance)
                .ToArray();

            if (intents.Length == 0)
                return;

            var occupiedAtTickStart = state.Units.ToDictionary(unit => unit.Position, unit => unit.Id);
            var winners = new HashSet<Guid>();
            foreach (var group in intents.GroupBy(intent => intent.Destination).Where(group => group.Count() > 1))
            {
                var contenders = group.OrderBy(intent => intent.Scheduled, ScheduledActionComparer.Instance).ToArray();
                var winner = contenders[random.Next(contenders.Length)];
                winners.Add(winner.Scheduled.Action.ActionId);
                foreach (var loser in contenders.Where(intent => intent.Scheduled.Action.ActionId != winner.Scheduled.Action.ActionId))
                {
                    timelineDelays[loser.Scheduled.Action.UnitId] = DelayFor(loser.Scheduled.Action.UnitId) + 1;
                    AddEvent(tick, DomainEventType.MovementDelayed, loser.Scheduled.FactionId, loser.Scheduled.Action.UnitId, loser.Scheduled.Action.ActionId,
                        $"Destination ({loser.Destination.X},{loser.Destination.Y}) contested; seeded tie-break selected another mover. Timeline delayed by 1 tick.");
                }
            }
            var movingFrom = intents.Where(intent => !intents.GroupBy(candidate => candidate.Destination).Any(group => group.Count() > 1 && !winners.Contains(intent.Scheduled.Action.ActionId)))
                .ToDictionary(intent => state.FindUnit(intent.Scheduled.Action.UnitId)!.Position, intent => intent);

            foreach (var intent in intents)
            {
                if (intents.GroupBy(candidate => candidate.Destination).Any(group => group.Count() > 1 && !winners.Contains(intent.Scheduled.Action.ActionId))) continue;

                if (occupiedAtTickStart.TryGetValue(intent.Destination, out var occupantId) && (!movingFrom.TryGetValue(intent.Destination, out var departing) || departing.Destination == state.FindUnit(intent.Scheduled.Action.UnitId)!.Position))
                {
                    Fail(tick, intent, "Destination was occupied at the start of this tick.");
                    continue;
                }

                var unit = state.FindUnit(intent.Scheduled.Action.UnitId)!;
                state = state.WithUnit(unit with { Position = intent.Destination });
                AddEvent(tick, DomainEventType.UnitExitedTile, intent.Scheduled.FactionId, unit.Id, intent.Scheduled.Action.ActionId, null, unit.Position, intent.Destination);
                AddEvent(tick, DomainEventType.UnitEnteredTile, intent.Scheduled.FactionId, unit.Id, intent.Scheduled.Action.ActionId, null, unit.Position, intent.Destination);
            }
        }

        void ResolveOverwatchReactions(int tick)
        {
            var enteredUnitIds = events.Where(@event => @event.Tick == tick && @event.Type == DomainEventType.UnitEnteredTile && @event.UnitId.HasValue)
                .Select(@event => @event.UnitId!.Value).Distinct().ToArray();
            if (enteredUnitIds.Length == 0)
                return;

            foreach (var watcher in state.Units.Where(unit => unit.ActivityState == UnitActivityState.Active && unit.Overwatch is { HasFired: false })
                         .OrderBy(unit => unit.FactionId, StringComparer.Ordinal).ThenBy(unit => unit.Id))
            {
                var profile = (request.AttackProfiles ?? Array.Empty<AttackProfile>()).Single(profile => StringComparer.Ordinal.Equals(profile.Id, watcher.Overwatch!.AttackProfileId));
                var target = enteredUnitIds.Select(unitId => state.FindUnit(unitId)!)
                    .Where(candidate => candidate.ActivityState == UnitActivityState.Active && !StringComparer.Ordinal.Equals(candidate.FactionId, watcher.FactionId))
                    .Where(candidate => IsInsideWatchCone(watcher.Position, watcher.Overwatch!.Facing, candidate.Position))
                    .OrderBy(candidate => candidate.FactionId, StringComparer.Ordinal).ThenBy(candidate => candidate.Id)
                    .FirstOrDefault(candidate => AttackRules.Resolve(watcher, candidate, profile, request.Scenario!.Map).FailureDetail is null);
                if (target is null)
                    continue;

                var resolution = AttackRules.Resolve(watcher, target, profile, request.Scenario!.Map, random.Next(1, 101));
                var targetAfter = resolution.Application?.Target ?? target;
                state = state.WithUnit(targetAfter).WithUnit(watcher with { Overwatch = watcher.Overwatch! with { HasFired = true } });
                var ammunitionDetail = InventoryConsumptionDetail(state.FindUnit(watcher.Id)!, profile.AmmunitionItemId, profile.AmmunitionQuantityCost);
                state = ConsumeInventory(state, watcher.Id, profile.AmmunitionItemId, profile.AmmunitionQuantityCost);
                AddEvent(tick, DomainEventType.ReactionAttackResolved, watcher.FactionId, watcher.Id, watcher.Overwatch!.ActionId,
                    $"{AttackDetail("reaction", profile, resolution, target.HitPoints, targetAfter.HitPoints)}; target={target.Id}{ammunitionDetail}",
                    fromPosition: watcher.Position, toPosition: target.Position, hitPointsAfter: targetAfter.HitPoints, activityStateAfter: targetAfter.ActivityState,
                    targetUnitId: target.Id);
            }
        }

        static bool IsInsideWatchCone(GridPosition origin, Facing facing, GridPosition target) => facing switch
        {
            Facing.North => target.Y > origin.Y && Math.Abs(target.X - origin.X) <= target.Y - origin.Y,
            Facing.South => target.Y < origin.Y && Math.Abs(target.X - origin.X) <= origin.Y - target.Y,
            Facing.East => target.X > origin.X && Math.Abs(target.Y - origin.Y) <= target.X - origin.X,
            Facing.West => target.X < origin.X && Math.Abs(target.Y - origin.Y) <= origin.X - target.X,
            _ => false
        };

        int DelayFor(Guid unitId) => timelineDelays.TryGetValue(unitId, out var delay) ? delay : 0;

        void Fail(int tick, MovementIntent intent, string detail)
        {
            failedActions.Add(intent.Scheduled.Action.ActionId);
            AddEvent(tick, DomainEventType.ActionFailed, intent.Scheduled.FactionId, intent.Scheduled.Action.UnitId, intent.Scheduled.Action.ActionId, detail);
        }

        void AddEvent(int tick, DomainEventType type, string factionId, Guid? unitId = null, Guid? actionId = null, string? detail = null, GridPosition? fromPosition = null, GridPosition? toPosition = null, int? hitPointsAfter = null, UnitActivityState? activityStateAfter = null, Guid? targetUnitId = null, UnitPosture? postureAfter = null) =>
            events.Add(new DomainEvent(sequence++, tick, type, factionId, unitId, actionId, detail, fromPosition, toPosition, hitPointsAfter, activityStateAfter, targetUnitId, postureAfter));
    }

    private static CompletionResult ApplyCompletion(GameState state, UnitState unit, TacticalAction action, IReadOnlyList<EffectDefinition>? effects, IReadOnlyList<AttackProfile>? attackProfiles, GridMapDefinition? map, Random random)
    {
        if (action.Type == TacticalActionType.Rotate && action.Facing is not null)
            return new CompletionResult(state.WithUnit(unit with { Facing = action.Facing.Value }));

        if (action.Type == TacticalActionType.ChangePosture && action.Posture is not null)
            return new CompletionResult(state.WithUnit(unit with { Posture = action.Posture.Value }));

        if (action.Type == TacticalActionType.EnterOverwatch && action.Facing is not null)
            return new CompletionResult(state.WithUnit(unit with { Overwatch = new OverwatchState(action.ActionId, action.Facing.Value, action.AttackProfileId!) }));

        if (action.Type == TacticalActionType.ApplyEffect)
        {
            var definition = effects!.Single(effect => StringComparer.Ordinal.Equals(effect.Id, action.EffectId));
            var target = state.FindUnit(action.TargetUnitId!.Value)!;
            var resolution = EffectRules.Resolve(unit, target, definition, map);
            if (resolution.FailureDetail is not null)
                return new CompletionResult(state, FailureDetail: resolution.FailureDetail);
            var application = resolution.Application!;
            var afterEffect = state.WithUnit(application.Target);
            return new CompletionResult(ConsumeInventory(afterEffect, unit.Id, definition.RequiredInventoryItemId, definition.InventoryQuantityCost), application, definition, target.HitPoints,
                InventoryConsumptionDetail: InventoryConsumptionDetail(afterEffect.FindUnit(unit.Id)!, definition.RequiredInventoryItemId, definition.InventoryQuantityCost));
        }

        if (action.Type == TacticalActionType.Attack)
        {
            var profile = attackProfiles!.Single(candidate => StringComparer.Ordinal.Equals(candidate.Id, action.AttackProfileId));
            var target = state.FindUnit(action.TargetUnitId!.Value)!;
            var preview = AttackRules.Resolve(unit, target, profile, map!);
            if (preview.FailureDetail is not null)
                return new CompletionResult(state, FailureDetail: preview.FailureDetail);
            var resolution = AttackRules.Resolve(unit, target, profile, map!, random.Next(1, 101));
            var afterAttack = state.WithUnit(resolution.Application?.Target ?? target);
            var ammunitionDetail = InventoryConsumptionDetail(afterAttack.FindUnit(unit.Id)!, profile.AmmunitionItemId, profile.AmmunitionQuantityCost);
            return new CompletionResult(ConsumeInventory(afterAttack, unit.Id, profile.AmmunitionItemId, profile.AmmunitionQuantityCost), Attack: resolution, AttackProfile: profile, BeforeHitPoints: target.HitPoints,
                InventoryConsumptionDetail: ammunitionDetail);
        }

        return new CompletionResult(state);
    }

    private static GameState ConsumeInventory(GameState state, Guid unitId, string? itemId, int quantity)
    {
        if (String.IsNullOrWhiteSpace(itemId) || quantity <= 0) return state;
        return state.WithUnit(InventoryRules.Consume(state.FindUnit(unitId)!, itemId, quantity));
    }

    private static string InventoryConsumptionDetail(UnitState unit, string? itemId, int quantity) =>
        String.IsNullOrWhiteSpace(itemId) || quantity <= 0 ? String.Empty : $"; item={itemId}; spent={quantity}; remaining={InventoryRules.QuantityOf(unit, itemId) - quantity}";

    private static string AttackDetail(string kind, AttackProfile profile, AttackResolution resolution, int beforeHitPoints, int afterHitPoints) =>
        $"{kind}={profile.Id}; distance={resolution.Distance}; accuracy={resolution.AccuracyPercent}; roll={resolution.AccuracyRoll}; result={(resolution.Hit ? "hit" : "miss")}; damage={profile.Damage}; cover={resolution.CoverMitigation}; armor={resolution.ArmorMitigation}; effective={resolution.EffectiveDamage}; before={beforeHitPoints}; applied={resolution.Application?.AppliedVitalityDelta ?? 0}; after={afterHitPoints}";

    private static IReadOnlyList<ValidationDiagnostic> Validate(SimulationRequest request)
    {
        var diagnostics = new List<ValidationDiagnostic>();
        var units = request.InitialState.Units.ToDictionary(unit => unit.Id);
        foreach (var unit in request.InitialState.Units)
        {
            if (unit.MaxHitPoints <= 0)
                diagnostics.Add(new("non-positive-max-hit-points", "Unit maximum hit points must be positive."));
            else if (unit.HitPoints < 0 || unit.HitPoints > unit.MaxHitPoints)
                diagnostics.Add(new("invalid-hit-points", "Unit hit points must be between zero and maximum hit points."));
        }
        var effects = request.Effects ?? Array.Empty<EffectDefinition>();
        var attackProfiles = request.AttackProfiles ?? Array.Empty<AttackProfile>();
        foreach (var effect in effects)
        {
            if (String.IsNullOrWhiteSpace(effect.Id))
                diagnostics.Add(new("missing-effect-id", "Effect definitions require a stable non-empty ID."));
            if (effect.VitalityDelta == 0)
                diagnostics.Add(new("zero-vitality-effect", "An effect vitality change cannot be zero."));
            if (effect.MinimumRange < 0 || effect.MaximumRange < effect.MinimumRange)
                diagnostics.Add(new("invalid-effect-range", "Effect ranges must be non-negative and ordered."));
            ValidateRequirement(effect.RequiredSkillId, effect.RequiredInventoryItemId, effect.InventoryQuantityCost, "effect", diagnostics);
        }
        if (effects.GroupBy(effect => effect.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new("duplicate-effect-id", "Effect definition IDs must be unique."));
        foreach (var profile in attackProfiles)
        {
            if (String.IsNullOrWhiteSpace(profile.Id))
                diagnostics.Add(new("missing-attack-profile-id", "Attack profiles require a stable non-empty ID."));
            if (profile.MinimumRange < 0 || profile.MaximumRange < profile.MinimumRange)
                diagnostics.Add(new("invalid-attack-range", "Attack profile ranges must be non-negative and ordered."));
            if (profile.Damage <= 0)
                diagnostics.Add(new("non-positive-attack-damage", "Attack profile damage must be positive."));
            if (profile.AccuracyPercent < 0 || profile.AccuracyPercent > 100)
                diagnostics.Add(new("invalid-attack-accuracy", "Attack profile accuracy must be between 0 and 100 inclusive."));
            ValidateRequirement(profile.RequiredSkillId, profile.RequiredInventoryItemId, profile.InventoryQuantityCost, "attack profile", diagnostics);
            ValidateRequirement(null, profile.AmmunitionItemId, profile.AmmunitionQuantityCost, "attack ammunition", diagnostics);
        }
        if (attackProfiles.GroupBy(profile => profile.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
            diagnostics.Add(new("duplicate-attack-profile-id", "Attack profile IDs must be unique."));
        if (request.Scenario != null)
        {
            diagnostics.AddRange(ScenarioValidator.Validate(request.Scenario));
            foreach (var unit in request.InitialState.Units.Where(unit => !request.Scenario.Map.Contains(unit.Position)))
                diagnostics.Add(new("request-unit-out-of-bounds", "Simulation request unit position must be inside the scenario map."));
        }
        if (request.InitialState.Units.GroupBy(unit => unit.Position).Any(group => group.Count() > 1))
            diagnostics.Add(new("overlapping-initial-positions", "Initial state cannot place more than one unit in a tile."));

        foreach (var item in request.CommandBundles.SelectMany(bundle => bundle.Actions.Select(action => new ScheduledAction(bundle.FactionId, action))))
        {
            var action = item.Action;
            if (!units.TryGetValue(action.UnitId, out var unit))
                diagnostics.Add(new("unknown-unit", "Action references an unknown unit.", action.ActionId));
            else if (!StringComparer.Ordinal.Equals(unit.FactionId, item.FactionId))
                diagnostics.Add(new("wrong-faction", "A command bundle may only command its own faction's unit.", action.ActionId));
            if (action.StartTick < 0)
                diagnostics.Add(new("negative-start", "Action start tick cannot be negative.", action.ActionId));
            if (action.DurationTicks <= 0)
                diagnostics.Add(new("non-positive-duration", "Action duration must be positive.", action.ActionId));
            if (action.StartTick + action.DurationTicks > request.Configuration.TicksPerRound)
                diagnostics.Add(new("round-overrun", "Action completion must be within the round.", action.ActionId));
            if (action.Type == TacticalActionType.Move)
                ValidateMovePath(action, unit, request.Scenario?.Map, diagnostics);
            if (action.Type == TacticalActionType.Rotate && action.Facing is null)
                diagnostics.Add(new("missing-facing", "Rotate requires a facing.", action.ActionId));
            if (action.Type == TacticalActionType.ChangePosture)
                ValidatePostureAction(action, unit, diagnostics);
            if (action.Type == TacticalActionType.EnterOverwatch)
                ValidateOverwatchAction(action, unit, request.Scenario?.UnitDefinitions, attackProfiles, request.Scenario?.Map, diagnostics);
            if (action.Type == TacticalActionType.ApplyEffect)
                ValidateEffectAction(action, unit, units, effects, request.Scenario?.Map, diagnostics);
            if (action.Type == TacticalActionType.Attack)
                ValidateAttackAction(action, unit, units, attackProfiles, request.Scenario?.Map, diagnostics);
            ValidateActionEntitlement(action, unit, request.Scenario?.UnitDefinitions, effects, attackProfiles, diagnostics);
        }

        foreach (var group in request.CommandBundles.SelectMany(bundle => bundle.Actions).GroupBy(action => action.UnitId))
        {
            var ordered = group.OrderBy(action => action.StartTick).ThenBy(action => action.DurationTicks).ThenBy(action => action.ActionId.ToString("N"), StringComparer.Ordinal).ToArray();
            for (var index = 1; index < ordered.Length; index++)
            {
                if (ordered[index - 1].StartTick + ordered[index - 1].DurationTicks > ordered[index].StartTick)
                    diagnostics.Add(new("overlapping-actions", "A unit cannot have overlapping actions.", ordered[index].ActionId));
            }

            if (group.Count(action => action.Type == TacticalActionType.Move) > 1)
                diagnostics.Add(new("multiple-move-actions", "Milestone 2 permits at most one Move action per unit per round.", group.First(action => action.Type == TacticalActionType.Move).ActionId));

            if (units.TryGetValue(group.Key, out var unit))
            {
                var spent = group.Sum(action => ActionPointRules.CostFor(action, request.Scenario?.Map, effects, attackProfiles));
                if (spent > unit.ActionPointBudget)
                    diagnostics.Add(new("action-point-budget-exceeded", $"Planned actions cost {spent} AP but the unit budget is {unit.ActionPointBudget} AP.", group.First().ActionId));
                ValidatePlannedInventory(group, unit, effects, attackProfiles, diagnostics);
            }
        }

        return diagnostics;
    }

    private static void ValidateRequirement(string? skillId, string? itemId, int quantityCost, string contentKind, ICollection<ValidationDiagnostic> diagnostics)
    {
        if (quantityCost < 0)
            diagnostics.Add(new("negative-inventory-cost", $"{contentKind} inventory quantity cost cannot be negative."));
        if (quantityCost > 0 && String.IsNullOrWhiteSpace(itemId))
            diagnostics.Add(new("inventory-cost-without-item", $"{contentKind} inventory quantity cost requires an inventory item ID."));
    }

    private static void ValidateActionEntitlement(TacticalAction action, UnitState? unit, IReadOnlyList<UnitDefinition>? definitions, IReadOnlyList<EffectDefinition> effects, IReadOnlyList<AttackProfile> profiles, ICollection<ValidationDiagnostic> diagnostics)
    {
        if (unit is null || action.Type is not (TacticalActionType.Attack or TacticalActionType.EnterOverwatch or TacticalActionType.ApplyEffect)) return;
        var requirement = action.Type is TacticalActionType.Attack or TacticalActionType.EnterOverwatch
            ? profiles.FirstOrDefault(profile => StringComparer.Ordinal.Equals(profile.Id, action.AttackProfileId))
            : null;
        var effect = action.Type == TacticalActionType.ApplyEffect
            ? effects.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, action.EffectId))
            : null;
        var skillId = requirement?.RequiredSkillId ?? effect?.RequiredSkillId;
        var itemId = requirement?.RequiredInventoryItemId ?? effect?.RequiredInventoryItemId;
        var ammunitionItemId = requirement?.AmmunitionItemId;
        if (String.IsNullOrWhiteSpace(skillId) && String.IsNullOrWhiteSpace(itemId) && String.IsNullOrWhiteSpace(ammunitionItemId)) return;

        var definition = definitions?.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, unit.UnitDefinitionId));
        if (definition is null)
        {
            diagnostics.Add(new("action-requires-unit-definition", "A capability- or inventory-gated action requires a unit definition.", action.ActionId));
            return;
        }
        if (!String.IsNullOrWhiteSpace(skillId) && !(definition.SkillIds ?? Array.Empty<string>()).Contains(skillId, StringComparer.Ordinal))
            diagnostics.Add(new("missing-required-skill", $"Unit does not have required skill '{skillId}'.", action.ActionId));
        if (!String.IsNullOrWhiteSpace(itemId) && InventoryRules.QuantityOf(unit, itemId) == 0)
            diagnostics.Add(new("missing-required-inventory-item", $"Unit does not carry required inventory item '{itemId}'.", action.ActionId));
        if (!String.IsNullOrWhiteSpace(ammunitionItemId) && InventoryRules.QuantityOf(unit, ammunitionItemId) < requirement!.AmmunitionQuantityCost)
            diagnostics.Add(new("missing-required-ammunition", $"Unit does not carry enough ammunition '{ammunitionItemId}'.", action.ActionId));
    }

    private static void ValidatePlannedInventory(IGrouping<Guid, TacticalAction> actions, UnitState unit, IReadOnlyList<EffectDefinition> effects, IReadOnlyList<AttackProfile> profiles, ICollection<ValidationDiagnostic> diagnostics)
    {
        var requested = actions
            .SelectMany(action => InventoryCostsFor(action, effects, profiles))
            .Where(item => item.Quantity > 0)
            .GroupBy(item => item.ItemId, StringComparer.Ordinal);
        foreach (var item in requested)
            if (item.Sum(entry => entry.Quantity) > InventoryRules.QuantityOf(unit, item.Key))
                diagnostics.Add(new("inventory-quantity-exceeded", $"Planned actions require {item.Sum(entry => entry.Quantity)} '{item.Key}' but the unit has {InventoryRules.QuantityOf(unit, item.Key)}.", actions.First().ActionId));
    }

    private static IEnumerable<InventoryItemDefinition> InventoryCostsFor(TacticalAction action, IReadOnlyList<EffectDefinition> effects, IReadOnlyList<AttackProfile> profiles)
    {
        if (action.Type is TacticalActionType.Attack or TacticalActionType.EnterOverwatch)
        {
            var profile = profiles.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, action.AttackProfileId));
            if (profile?.RequiredInventoryItemId is { } requiredItem && profile.InventoryQuantityCost > 0)
                yield return new InventoryItemDefinition(requiredItem, profile.InventoryQuantityCost);
            if (profile?.AmmunitionItemId is { } ammunitionItem && profile.AmmunitionQuantityCost > 0)
                yield return new InventoryItemDefinition(ammunitionItem, profile.AmmunitionQuantityCost);
            yield break;
        }

        if (action.Type != TacticalActionType.ApplyEffect) yield break;
        var effect = effects.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, action.EffectId));
        if (effect?.RequiredInventoryItemId is { } effectItem && effect.InventoryQuantityCost > 0)
            yield return new InventoryItemDefinition(effectItem, effect.InventoryQuantityCost);
    }

    private static void ValidateAttackAction(TacticalAction action, UnitState? attacker, IReadOnlyDictionary<Guid, UnitState> units, IReadOnlyList<AttackProfile> profiles, GridMapDefinition? map, ICollection<ValidationDiagnostic> diagnostics)
    {
        if (map == null)
            diagnostics.Add(new("attack-requires-map", "Attack requires a scenario map for range and line-of-sight evaluation.", action.ActionId));
        if (action.TargetUnitId is null)
            diagnostics.Add(new("missing-attack-target", "Attack requires a target unit.", action.ActionId));
        else if (!units.TryGetValue(action.TargetUnitId.Value, out var target))
            diagnostics.Add(new("unknown-attack-target", "Attack target must be a unit in the initial state.", action.ActionId));
        else if (attacker is not null && StringComparer.Ordinal.Equals(attacker.FactionId, target.FactionId))
            diagnostics.Add(new("friendly-attack-target", "Attack target must belong to an opposing faction.", action.ActionId));

        if (String.IsNullOrWhiteSpace(action.AttackProfileId))
            diagnostics.Add(new("missing-attack-profile-id", "Attack requires an attack profile ID.", action.ActionId));
        else if (!profiles.Any(profile => StringComparer.Ordinal.Equals(profile.Id, action.AttackProfileId)))
            diagnostics.Add(new("unknown-attack-profile-id", "Attack references an unknown attack profile.", action.ActionId));
    }

    private static void ValidateEffectAction(TacticalAction action, UnitState? source, IReadOnlyDictionary<Guid, UnitState> units, IReadOnlyList<EffectDefinition> effects, GridMapDefinition? map, ICollection<ValidationDiagnostic> diagnostics)
    {
        if (action.TargetUnitId is null)
            diagnostics.Add(new("missing-effect-target", "ApplyEffect requires a target unit.", action.ActionId));
        else if (!units.TryGetValue(action.TargetUnitId.Value, out var target))
            diagnostics.Add(new("unknown-effect-target", "ApplyEffect target must be a unit in the initial state.", action.ActionId));

        else if (!String.IsNullOrWhiteSpace(action.EffectId))
        {
            var effect = effects.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.Id, action.EffectId));
            if (effect?.RequiresLineOfSight == true && map is null)
                diagnostics.Add(new("effect-requires-map", "Line-of-sight effect requires a scenario map.", action.ActionId));
            if (effect?.TargetPolicy == EffectTargetPolicy.Self && source is not null && source.Id != target.Id)
                diagnostics.Add(new("invalid-effect-target-policy", "Effect requires the acting unit to target itself.", action.ActionId));
            if (effect?.TargetPolicy == EffectTargetPolicy.Friendly && source is not null && !StringComparer.Ordinal.Equals(source.FactionId, target.FactionId))
                diagnostics.Add(new("invalid-effect-target-policy", "Effect requires a friendly target.", action.ActionId));
            if (effect?.TargetPolicy == EffectTargetPolicy.Hostile && source is not null && StringComparer.Ordinal.Equals(source.FactionId, target.FactionId))
                diagnostics.Add(new("invalid-effect-target-policy", "Effect requires an opposing target.", action.ActionId));
        }

        if (String.IsNullOrWhiteSpace(action.EffectId))
            diagnostics.Add(new("missing-effect-id", "ApplyEffect requires an effect ID.", action.ActionId));
        else if (!effects.Any(effect => StringComparer.Ordinal.Equals(effect.Id, action.EffectId)))
            diagnostics.Add(new("unknown-effect-id", "ApplyEffect references an unknown effect definition.", action.ActionId));
    }

    private static void ValidatePostureAction(TacticalAction action, UnitState? unit, ICollection<ValidationDiagnostic> diagnostics)
    {
        if (action.Posture is null)
            diagnostics.Add(new("missing-posture", "ChangePosture requires a destination posture.", action.ActionId));
        else if (unit is not null && Math.Abs((int)action.Posture.Value - (int)unit.Posture) != 1)
            diagnostics.Add(new("invalid-posture-transition", "Posture changes must move one step between standing, crouched, and prone.", action.ActionId));
    }

    private static void ValidateOverwatchAction(TacticalAction action, UnitState? unit, IReadOnlyList<UnitDefinition>? definitions, IReadOnlyList<AttackProfile> profiles, GridMapDefinition? map, ICollection<ValidationDiagnostic> diagnostics)
    {
        if (map is null)
            diagnostics.Add(new("overwatch-requires-map", "EnterOverwatch requires a scenario map.", action.ActionId));
        if (action.Facing is null)
            diagnostics.Add(new("overwatch-requires-facing", "EnterOverwatch requires a watched facing.", action.ActionId));
        if (String.IsNullOrWhiteSpace(action.AttackProfileId))
            diagnostics.Add(new("overwatch-requires-profile", "EnterOverwatch requires an attack profile.", action.ActionId));
        else if (!profiles.Any(profile => StringComparer.Ordinal.Equals(profile.Id, action.AttackProfileId)))
            diagnostics.Add(new("unknown-overwatch-profile-id", "EnterOverwatch references an unknown attack profile.", action.ActionId));
        var definition = definitions?.FirstOrDefault(candidate => unit is not null && StringComparer.Ordinal.Equals(candidate.Id, unit.UnitDefinitionId));
        if (definition is not null && !(definition.SkillIds ?? Array.Empty<string>()).Contains("overwatch", StringComparer.Ordinal))
            diagnostics.Add(new("missing-overwatch-skill", "Unit does not have the overwatch skill.", action.ActionId));
    }

    private static void ValidateMovePath(TacticalAction action, UnitState? unit, GridMapDefinition? map, ICollection<ValidationDiagnostic> diagnostics)
    {
        var path = MovementRules.PathFor(action);
        if (path.Count == 0)
        {
            diagnostics.Add(new("missing-path", "Move requires a non-empty explicit path or destination.", action.ActionId));
            return;
        }

        if (MovementRules.DurationFor(action, map) != action.DurationTicks)
            diagnostics.Add(new("movement-duration-mismatch", "Move duration must equal the summed movement ticks of its path.", action.ActionId));

        if (unit == null)
            return;

        var previous = unit.Position;
        var visited = new HashSet<GridPosition>();
        foreach (var position in path)
        {
            if (!MovementRules.IsCardinalStep(previous, position))
                diagnostics.Add(new("invalid-movement-step", "Each movement path step must be exactly one cardinal tile.", action.ActionId));
            if (!visited.Add(position))
                diagnostics.Add(new("repeated-path-position", "A movement path cannot revisit a tile during one action.", action.ActionId));
            if (map != null && !map.Contains(position))
                diagnostics.Add(new("movement-out-of-bounds", "Movement path must remain inside the scenario map.", action.ActionId));
            if (map != null && !map.CellAt(position).IsPassable)
                diagnostics.Add(new("impassable-movement-path", "Movement path cannot enter an impassable terrain tile.", action.ActionId));
            previous = position;
        }
    }

    private sealed record ScheduledAction(string FactionId, TacticalAction Action);

    private sealed record MovementIntent(ScheduledAction Scheduled, int StepIndex)
    {
        public GridPosition Destination => MovementRules.PathFor(Scheduled.Action)[StepIndex];
    }

    private sealed record CompletionResult(GameState State, EffectApplication? Effect = null, EffectDefinition? EffectDefinition = null, int BeforeHitPoints = 0, AttackResolution? Attack = null, AttackProfile? AttackProfile = null, string? FailureDetail = null, string InventoryConsumptionDetail = "");

    private sealed class ScheduledActionComparer : IComparer<ScheduledAction>
    {
        public static readonly ScheduledActionComparer Instance = new();
        public int Compare(ScheduledAction? left, ScheduledAction? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;
            var faction = StringComparer.Ordinal.Compare(left.FactionId, right.FactionId);
            if (faction != 0) return faction;
            var unit = StringComparer.Ordinal.Compare(left.Action.UnitId.ToString("N"), right.Action.UnitId.ToString("N"));
            return unit != 0 ? unit : StringComparer.Ordinal.Compare(left.Action.ActionId.ToString("N"), right.Action.ActionId.ToString("N"));
        }
    }
}

}
