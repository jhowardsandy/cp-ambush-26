#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TacticalStrategyGame.Core;
using UnityEngine;

namespace TacticalStrategyGame.Presentation.Unity
{
    /// <summary>Player-operated 4v4 graybox. It only renders commands and core events.</summary>
    public sealed class GrayboxPveController : MonoBehaviour
    {
        private readonly Dictionary<Guid, GameObject> _views = new();
        private readonly Dictionary<Guid, List<TacticalAction>> _blueOrders = new();
        private readonly List<string> _lines = new();
        private readonly Dictionary<Guid, Facing> _armedOverwatch = new();
        private readonly Dictionary<Guid, int> _displayHitPoints = new();
        private readonly List<FeedbackPulse> _feedback = new();
        private readonly Dictionary<Guid, TacticalDoctrine> _blueDoctrines = new();
        private readonly Dictionary<Guid, Guid> _followTargets = new();
        private readonly Dictionary<Guid, string> _doctrineExplanations = new();
        private ScenarioDefinition _scenario = null!;
        private EncounterState _encounter = null!;
        private Guid _selectedBlue;
        private Guid _selectedRed;
        private Guid _selectedHealTarget;
        private bool _resolving;
        private bool _autoPlaying;
        private bool _manualRoute;
        private string _message = string.Empty;
        private string _roundSummary = "No round resolved yet.";
        private static readonly IReadOnlyList<AttackProfile> AttackProfiles = new[] { StarterMilitaryContent.ServiceRifle, StarterMilitaryContent.MarksmanRifle };
        private static readonly EffectDefinition FieldMedKit = StarterMilitaryContent.FieldMedKit;

        private void Start()
        {
            BuildScenario();
            BuildViews();
            ResetEncounter();
        }

        private void Update()
        {
            if (_resolving || _autoPlaying || !Input.GetMouseButtonDown(0) || Input.mousePosition.y > Screen.height - 220) return;
            var ray = Camera.main!.ScreenPointToRay(Input.mousePosition);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var distance)) return;
            var point = ray.GetPoint(distance);
            var destination = new GridPosition(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.z));
            if (_scenario.Map.Contains(destination)) DraftMoveTo(destination);
        }

        private void BuildScenario()
        {
            var units = new[]
            {
                Unit("blue", 1, 1, 1, StarterMilitaryContent.Rifleman), Unit("blue", 2, 3, 1, StarterMilitaryContent.CombatMedic),
                Unit("blue", 3, 1, 3, StarterMilitaryContent.Marksman), Unit("blue", 4, 4, 2, StarterMilitaryContent.CombatMedic),
                Unit("red", 1, 14, 10, StarterMilitaryContent.Rifleman), Unit("red", 2, 12, 10, StarterMilitaryContent.CombatMedic),
                Unit("red", 3, 14, 8, StarterMilitaryContent.Marksman), Unit("red", 4, 11, 9, StarterMilitaryContent.CombatMedic)
            };
            _scenario = new ScenarioDefinition("riverside-crossing-4v4-01", new GridMapDefinition("riverside-crossing-16x12", 16, 12, new[]
            {
                new TerrainCellDefinition(new GridPosition(7, 5), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3), new TerrainCellDefinition(new GridPosition(8, 5), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(7, 6), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3), new TerrainCellDefinition(new GridPosition(8, 6), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(5, 5), IsPassable: false, BlocksLineOfSight: true, CoverValue: 2), new TerrainCellDefinition(new GridPosition(5, 6), IsPassable: false, BlocksLineOfSight: true, CoverValue: 2),
                new TerrainCellDefinition(new GridPosition(10, 5), IsPassable: false, BlocksLineOfSight: true, CoverValue: 2), new TerrainCellDefinition(new GridPosition(10, 6), IsPassable: false, BlocksLineOfSight: true, CoverValue: 2),
                new TerrainCellDefinition(new GridPosition(3, 4), MovementTicks: 2, ConcealmentValue: 2), new TerrainCellDefinition(new GridPosition(3, 5), MovementTicks: 2, ConcealmentValue: 2), new TerrainCellDefinition(new GridPosition(4, 5), MovementTicks: 2, ConcealmentValue: 2),
                new TerrainCellDefinition(new GridPosition(12, 6), MovementTicks: 2, ConcealmentValue: 2), new TerrainCellDefinition(new GridPosition(12, 7), MovementTicks: 2, ConcealmentValue: 2), new TerrainCellDefinition(new GridPosition(11, 6), MovementTicks: 2, ConcealmentValue: 2),
                new TerrainCellDefinition(new GridPosition(6, 3), CoverValue: 2), new TerrainCellDefinition(new GridPosition(7, 3), CoverValue: 2), new TerrainCellDefinition(new GridPosition(8, 8), CoverValue: 2), new TerrainCellDefinition(new GridPosition(9, 8), CoverValue: 2)
            }, new[]
            {
                new MapAreaDefinition("blue-deployment", new[] { new GridPosition(1, 1), new GridPosition(3, 1), new GridPosition(1, 3), new GridPosition(4, 2) }),
                new MapAreaDefinition("red-deployment", new[] { new GridPosition(14, 10), new GridPosition(12, 10), new GridPosition(14, 8), new GridPosition(11, 9) }),
                new MapAreaDefinition("central-crossing", new[] { new GridPosition(6, 4), new GridPosition(7, 4), new GridPosition(8, 4), new GridPosition(9, 4), new GridPosition(6, 7), new GridPosition(7, 7), new GridPosition(8, 7), new GridPosition(9, 7) }),
                new MapAreaDefinition("contact-rally", new[] { new GridPosition(9, 4) })
            }), new GameState(units), Objectives: new[]
            {
                new ObjectiveDefinition("hold-central-crossing", ObjectiveType.HoldAreaForRounds, "blue", "central-crossing", RequiredControlRounds: 3),
                new ObjectiveDefinition("eliminate-red", ObjectiveType.IncapacitateAllOpposingUnits, "blue")
            },
                UnitDefinitions: new[] { StarterMilitaryContent.Rifleman, StarterMilitaryContent.CombatMedic, StarterMilitaryContent.Marksman },
                FactionDefinitions: new[]
                {
                    new FactionDefinition("blue", new[] { StarterMilitaryContent.Rifleman.Id, StarterMilitaryContent.CombatMedic.Id, StarterMilitaryContent.Marksman.Id }),
                    new FactionDefinition("red", new[] { StarterMilitaryContent.Rifleman.Id, StarterMilitaryContent.CombatMedic.Id, StarterMilitaryContent.Marksman.Id })
                });
        }

        private static UnitState Unit(string faction, int number, int x, int y, UnitDefinition definition) =>
            definition.CreateInitialState(Guid.Parse($"{(faction == "blue" ? "1" : "2")}0000000-0000-0000-0000-00000000000{number}"), faction, new GridPosition(x, y), faction == "blue" ? Facing.North : Facing.South);

        private void BuildViews()
        {
            for (var x = 0; x < _scenario.Map.Width; x++)
            for (var y = 0; y < _scenario.Map.Height; y++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.transform.SetParent(transform, false); tile.transform.position = new Vector3(x, 0, y); tile.transform.localScale = new Vector3(.92f, .08f, .92f);
                var terrain = _scenario.Map.CellAt(new GridPosition(x, y));
                if (!terrain.IsPassable)
                {
                    tile.transform.position += Vector3.up * .28f;
                    tile.transform.localScale = new Vector3(.82f, .58f, .82f);
                }
                else if (terrain.CoverValue > 0)
                {
                    tile.transform.position += Vector3.up * .07f;
                    tile.transform.localScale = new Vector3(.88f, .18f, .88f);
                }
                tile.GetComponent<Renderer>().material.color = !terrain.IsPassable ? new Color(.34f, .25f, .16f) : terrain.ConcealmentValue > 0 ? new Color(.16f, .42f, .2f) : terrain.CoverValue > 0 ? new Color(.45f, .45f, .38f) : new Color(.22f, .27f, .32f);
            }
            foreach (var unit in _scenario.InitialState.Units)
            {
                var primitive = unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? PrimitiveType.Sphere : unit.UnitDefinitionId == StarterMilitaryContent.Marksman.Id ? PrimitiveType.Cylinder : PrimitiveType.Capsule;
                var view = GameObject.CreatePrimitive(primitive);
                view.transform.SetParent(transform, false); view.transform.localScale = Vector3.one * .42f;
                view.GetComponent<Renderer>().material.color = UnitColor(unit);
                _views.Add(unit.Id, view);
            }
            var cameraObject = new GameObject("Graybox PvE Camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 5.6f;
            cameraObject.transform.position = new Vector3(7.5f, 12, 5.5f); camera.orthographicSize = 8.1f; cameraObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            new GameObject("Graybox Light").AddComponent<Light>().type = LightType.Directional;
        }

        private void ResetEncounter()
        {
            StopAllCoroutines(); _resolving = false; _autoPlaying = false; _manualRoute = false; _blueOrders.Clear(); _lines.Clear(); _armedOverwatch.Clear(); _feedback.Clear(); _blueDoctrines.Clear(); _followTargets.Clear(); _doctrineExplanations.Clear(); _roundSummary = "No round resolved yet.";
            _encounter = new EncounterState(new EncounterDefinition(_scenario.Id, _scenario.Map, _scenario.ContentVersion, _scenario.Objectives, _scenario.UnitDefinitions, _scenario.FactionDefinitions), _scenario.InitialState);
            _selectedBlue = _scenario.InitialState.Units.First(unit => unit.FactionId == "blue").Id;
            _selectedRed = _scenario.InitialState.Units.First(unit => unit.FactionId == "red").Id;
            _selectedHealTarget = _selectedBlue;
            foreach (var unit in _scenario.InitialState.Units.Where(unit => unit.FactionId == "blue")) _blueDoctrines[unit.Id] = TacticalDoctrine.Aggressive;
            _message = "Draft one order for any Blue unit, then submit the round. Red uses deterministic PvE.";
            Render(_encounter.CurrentState);
        }

        private void DraftMove(GridPosition delta)
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            var priorActions = PlannedActions(unit.Id);
            var priorMove = priorActions.LastOrDefault(action => action.Type == TacticalActionType.Move);
            var origin = priorMove?.Path is { Count: > 0 } ? priorMove.Path[^1] : unit.Position;
            DraftMoveTo(new GridPosition(origin.X + delta.X, origin.Y + delta.Y));
        }

        private void DraftMoveTo(GridPosition destination)
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            if (unit.ActivityState != UnitActivityState.Active) { _message = "The selected unit is not active."; return; }
            PrepareManualOrder(unit);
            var actions = PlannedActions(unit.Id);
            if (actions.Any(action => action.Type == TacticalActionType.Move) && actions.Last().Type != TacticalActionType.Move)
            {
                _message = "This first planner permits one contiguous move path. Undo back to the move before adding more movement.";
                return;
            }
            var previousMove = actions.LastOrDefault(action => action.Type == TacticalActionType.Move);
            var origin = previousMove?.Path is { Count: > 0 } ? previousMove.Path[^1] : unit.Position;
            IReadOnlyList<GridPosition>? route;
            if (_manualRoute)
            {
                if (!MovementRules.IsCardinalStep(origin, destination) || !_scenario.Map.Contains(destination) || !_scenario.Map.CellAt(destination).IsPassable || _encounter.CurrentState.Units.Any(other => other.Id != unit.Id && other.Position == destination))
                {
                    _message = "Manual route mode requires the next adjacent, passable, unoccupied tile.";
                    return;
                }
                route = new[] { destination };
            }
            else route = FindRoute(origin, destination, unit.Id);
            if (route is null)
            {
                _message = "No clear route to that tile using the current known board. Try another destination.";
                return;
            }
            if (route.Count == 0) { _message = "That unit is already on this tile."; return; }
            if (actions.LastOrDefault()?.Type == TacticalActionType.Move && previousMove is not null)
            {
                var path = previousMove.Path!.Concat(route).ToArray();
                actions[^1] = previousMove with { Path = path, DurationTicks = MovementRules.DurationFor(previousMove with { Path = path }, _scenario.Map) };
            }
            else
            {
                var duration = route.Sum(position => _scenario.Map.CellAt(position).MovementTicks);
                QueueAction(unit, TacticalActionType.Move, duration, path: route);
            }
            _message = $"Previewing {route.Count} tile(s) to ({destination.X},{destination.Y}). The plan panel shows final AP and tick cost.";
        }

        private IReadOnlyList<GridPosition>? FindRoute(GridPosition origin, GridPosition destination, Guid movingUnitId)
        {
            var frontier = new Queue<GridPosition>();
            var previous = new Dictionary<GridPosition, GridPosition?> { [origin] = null };
            var occupied = _encounter.CurrentState.Units.Where(unit => unit.Id != movingUnitId).Select(unit => unit.Position).ToHashSet();
            frontier.Enqueue(origin);
            var steps = new[] { new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0) };
            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == destination) break;
                foreach (var step in steps)
                {
                    var next = new GridPosition(current.X + step.X, current.Y + step.Y);
                    if (!_scenario.Map.Contains(next) || !_scenario.Map.CellAt(next).IsPassable || occupied.Contains(next) || previous.ContainsKey(next)) continue;
                    previous[next] = current;
                    frontier.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(destination)) return null;
            var route = new List<GridPosition>();
            for (GridPosition? current = destination; current is not null && current != origin; current = previous[current]) route.Add(current);
            route.Reverse();
            return route;
        }

        private void DraftAttack()
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            var target = _encounter.CurrentState.FindUnit(_selectedRed)!;
            if (unit.ActivityState != UnitActivityState.Active || target.ActivityState != UnitActivityState.Active) { _message = "Both units must be active."; return; }
            PrepareManualOrder(unit);
            var profile = AttackProfileFor(unit);
            QueueAction(unit, TacticalActionType.Attack, 1, targetUnitId: target.Id, attackProfileId: profile.Id);
            var observation = VisibilityRules.Observe(_scenario.Map, unit, target);
            _message = observation.IsObservable
                ? $"Queued {profile.Id} attack: Blue {UnitNumber(unit.Id)} targets observable Red {UnitNumber(target.Id)}. Range and sight resolve later."
                : $"Queued speculative attack: Red {UnitNumber(target.Id)} is currently concealed/out of vision; observation is checked at resolution.";
        }

        private void DraftHealTarget()
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            var target = _encounter.CurrentState.FindUnit(_selectedHealTarget)!;
            if (unit.UnitDefinitionId != StarterMilitaryContent.CombatMedic.Id)
            {
                _message = "Only a Combat Medic has the field-medicine skill and med kits.";
                return;
            }
            if (InventoryRules.QuantityOf(unit, "med-kit") == 0)
            {
                _message = $"Blue {UnitNumber(unit.Id)} has no med kits remaining.";
                return;
            }
            if (target.ActivityState != UnitActivityState.Active)
            {
                _message = "The selected healing target is not active.";
                return;
            }
            PrepareManualOrder(unit);
            QueueAction(unit, TacticalActionType.ApplyEffect, 1, targetUnitId: target.Id, effectId: FieldMedKit.Id);
            _message = $"Queued heal: Blue {UnitNumber(unit.Id)} targets Blue {UnitNumber(target.Id)}. Range and sight are checked at resolution.";
        }

        private void DraftOverwatch(Facing facing)
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            var definition = _scenario.UnitDefinitions!.Single(candidate => candidate.Id == unit.UnitDefinitionId);
            if (!(definition.SkillIds ?? Array.Empty<string>()).Contains("overwatch", StringComparer.Ordinal))
            {
                _message = "This unit does not have the overwatch skill.";
                return;
            }
            PrepareManualOrder(unit);
            QueueAction(unit, TacticalActionType.EnterOverwatch, 1, attackProfileId: AttackProfileFor(unit).Id, facing: facing);
            _message = $"Queued {RoleName(unit)} overwatch for Blue {UnitNumber(unit.Id)}: 90° {facing} watch cone; one reaction shot if an enemy enters it.";
        }

        private List<TacticalAction> PlannedActions(Guid unitId)
        {
            if (_blueOrders.TryGetValue(unitId, out var actions)) return actions;
            actions = new List<TacticalAction>();
            _blueOrders.Add(unitId, actions);
            return actions;
        }

        private void QueueAction(UnitState unit, TacticalActionType type, int durationTicks, GridPosition? destination = null, IReadOnlyList<GridPosition>? path = null, Guid? targetUnitId = null, string? effectId = null, string? attackProfileId = null, Facing? facing = null)
        {
            var actions = PlannedActions(unit.Id);
            var startTick = actions.Count == 0 ? 0 : actions[^1].StartTick + actions[^1].DurationTicks;
            if (startTick + durationTicks > 10)
            {
                _message = "That action would extend beyond this 10-tick round. Undo or clear an earlier order.";
                return;
            }
            actions.Add(new TacticalAction(PlannedActionId(unit.Id, actions.Count + 1), unit.Id, type, startTick, durationTicks, destination, Facing: facing, Path: path, TargetUnitId: targetUnitId, EffectId: effectId, AttackProfileId: attackProfileId));
        }

        private static Guid PlannedActionId(Guid unitId, int sequence)
        {
            var canonical = unitId.ToString("N");
            return Guid.Parse(canonical[..28] + sequence.ToString("x1") + canonical[29..]);
        }

        private int PlannedActionCount => _blueOrders.Values.Sum(actions => actions.Count);

        private void PrepareManualOrder(UnitState unit)
        {
            if (_doctrineExplanations.Remove(unit.Id))
            {
                _blueOrders.Remove(unit.Id);
                _message = $"Manual order replaces Blue {UnitNumber(unit.Id)}'s doctrine-generated order.";
            }
        }

        private TacticalDoctrine DoctrineFor(Guid unitId) => _blueDoctrines.TryGetValue(unitId, out var doctrine) ? doctrine : TacticalDoctrine.Aggressive;

        private void SetDoctrine(TacticalDoctrine doctrine)
        {
            _blueDoctrines[_selectedBlue] = doctrine;
            _message = $"Blue {UnitNumber(_selectedBlue)} doctrine set to {DoctrineLabel(doctrine)}. Use Auto-plan selected to draft its inspectable order.";
        }

        private void AutoPlanSelected()
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            if (unit.ActivityState != UnitActivityState.Active) { _message = "The selected unit is not active."; return; }
            var plan = PvePlanner.Plan("blue", _encounter.CurrentState, _scenario.Map, AttackProfiles, FieldMedKit, _scenario.UnitDefinitions, ScoutObjectives, PlayerDoctrinePolicy);
            var decision = plan.Decisions.Single(candidate => candidate.UnitId == unit.Id);
            _blueOrders.Remove(unit.Id);
            var action = plan.Commands.Actions.SingleOrDefault(candidate => candidate.UnitId == unit.Id);
            if (action is not null) _blueOrders[unit.Id] = new List<TacticalAction> { action with { ActionId = PlannedActionId(unit.Id, 1) } };
            _doctrineExplanations[unit.Id] = decision.Explanation;
            _message = $"Blue {UnitNumber(unit.Id)} {DoctrineLabel(DoctrineFor(unit.Id))}: {decision.Explanation}";
        }

        private void ClearSelectedOrder()
        {
            _blueOrders.Remove(_selectedBlue);
            _doctrineExplanations.Remove(_selectedBlue);
            _message = $"Cleared Blue {UnitNumber(_selectedBlue)}'s drafted order.";
        }

        private void UndoLastBlueAction()
        {
            if (!_blueOrders.TryGetValue(_selectedBlue, out var actions) || actions.Count == 0)
            {
                _message = $"Blue {UnitNumber(_selectedBlue)} has no queued action to undo.";
                return;
            }
            var last = actions[^1];
            if (last.Type == TacticalActionType.Move && last.Path is { Count: > 1 })
            {
                var shortenedPath = last.Path.Take(last.Path.Count - 1).ToArray();
                actions[^1] = last with { Path = shortenedPath, DurationTicks = MovementRules.DurationFor(last with { Path = shortenedPath }, _scenario.Map) };
                _message = $"Removed Blue {UnitNumber(_selectedBlue)}'s last movement tile.";
                return;
            }
            actions.RemoveAt(actions.Count - 1);
            if (actions.Count == 0) { _blueOrders.Remove(_selectedBlue); _doctrineExplanations.Remove(_selectedBlue); }
            _message = $"Removed Blue {UnitNumber(_selectedBlue)}'s last queued action.";
        }

        private void Submit()
        {
            if (_resolving || _encounter.Outcome?.IsComplete == true) return;
            if (PlannedActionCount == 0)
            {
                _message = "Draft at least one Blue order first. Units without an order wait this round.";
                return;
            }
            StartCoroutine(Resolve());
        }

        private void StartAutoPlay()
        {
            if (_resolving || _autoPlaying) return;
            ResetEncounter();
            _autoPlaying = true;
            StartCoroutine(AutoPlay());
        }

        private IEnumerator AutoPlay()
        {
            const int maximumDemoRounds = 12;
            for (var round = 0; round < maximumDemoRounds && _encounter.Outcome?.IsComplete != true; round++)
            {
                _message = $"Auto-play demo: planning round {_encounter.CompletedRounds + 1}.";
                var blue = PvePlanner.Plan("blue", _encounter.CurrentState, _scenario.Map, AttackProfiles, FieldMedKit, _scenario.UnitDefinitions, ScoutObjectives, BlueHoldPolicy);
                yield return StartCoroutine(Resolve(blue.Commands, blue.Decisions));
                if (_encounter.Outcome?.IsComplete != true)
                    yield return new WaitForSeconds(.65f);
            }
            _autoPlaying = false;
            if (_encounter.Outcome?.IsComplete != true)
                _message = $"Auto-play demo paused after {maximumDemoRounds} rounds. Reset to replay.";
        }

        private IEnumerator Resolve()
        {
            yield return Resolve(new CommandBundle("blue", _blueOrders.Values.SelectMany(actions => actions).ToArray()));
        }

        private IEnumerator Resolve(CommandBundle blueCommands, IReadOnlyList<PveDecision>? blueDecisions = null)
        {
            _resolving = true; _armedOverwatch.Clear(); var before = _encounter.CurrentState; var red = PvePlanner.Plan("red", before, _scenario.Map, AttackProfiles, FieldMedKit, _scenario.UnitDefinitions, ScoutObjectives);
            var overwatchActions = blueCommands.Actions.Concat(red.Commands.Actions)
                .Where(action => action.Type == TacticalActionType.EnterOverwatch && action.Facing.HasValue)
                .ToDictionary(action => action.ActionId);
            var result = EncounterResolver.ResolveRound(_encounter, new[] { blueCommands, red.Commands }, new RoundConfiguration(10), (uint)(20260721 + _encounter.CompletedRounds), effects: new[] { FieldMedKit }, attackProfiles: AttackProfiles);
            _blueOrders.Clear(); _lines.Clear();
            foreach (var decision in blueDecisions ?? Array.Empty<PveDecision>()) _lines.Add($"PLAN BLUE {UnitNumber(decision.UnitId)} {decision.Decision}: {decision.Explanation}");
            foreach (var decision in red.Decisions) _lines.Add($"PLAN RED {UnitNumber(decision.UnitId)} {decision.Decision}: {decision.Explanation}");
            Render(before);
            foreach (var group in result.Resolution.Events.GroupBy(@event => @event.Tick).OrderBy(group => group.Key))
            {
                foreach (var @event in group)
                {
                    if (@event.Type == DomainEventType.UnitEnteredTile && @event.UnitId.HasValue && @event.ToPosition is not null) _views[@event.UnitId.Value].transform.position = new Vector3(@event.ToPosition.X, .3f, @event.ToPosition.Y);
                    if (@event.Type == DomainEventType.OverwatchArmed && @event.UnitId.HasValue && @event.ActionId.HasValue && overwatchActions.TryGetValue(@event.ActionId.Value, out var overwatch))
                        _armedOverwatch[@event.UnitId.Value] = overwatch.Facing!.Value;
                    if (@event.Type is DomainEventType.AttackResolved or DomainEventType.ReactionAttackResolved)
                    {
                        if (@event.FromPosition is not null && @event.ToPosition is not null)
                            yield return AnimateProjectile(@event.FromPosition, @event.ToPosition, @event.Type == DomainEventType.ReactionAttackResolved ? new Color(1f, .36f, .1f) : new Color(1f, .82f, .22f));
                        if (@event.Detail?.Contains("result=miss", StringComparison.Ordinal) == true && @event.TargetUnitId.HasValue)
                            AddFeedback(@event.TargetUnitId.Value, "MISS", new Color(.9f, .9f, .9f));
                        else
                            ApplyVitalityFeedback(@event, @event.TargetUnitId, "DAMAGE", new Color(1f, .25f, .2f));
                    }
                    else if (@event.Type == DomainEventType.EffectApplied)
                        ApplyVitalityFeedback(@event, @event.UnitId, "HEAL", new Color(.25f, 1f, .58f));
                    else if (@event.Type == DomainEventType.MovementDelayed && @event.UnitId.HasValue)
                        AddFeedback(@event.UnitId.Value, "DELAYED", new Color(1f, .25f, .78f));
                    else if (@event.Type == DomainEventType.ActionFailed && @event.UnitId.HasValue)
                        AddFeedback(@event.UnitId.Value, "FAILED", new Color(1f, .45f, .2f));
                    _lines.Add(EventLine(@event));
                }
                yield return new WaitForSeconds(.35f);
            }
            _encounter = result.NextState; Render(_encounter.CurrentState);
            _armedOverwatch.Clear();
            _lines.Add($"Checksum: {result.Resolution.FinalStateChecksum}");
            _roundSummary = RoundSummary(result.Resolution.Events, _encounter.CompletedRounds);
            _message = _encounter.Outcome?.IsComplete == true ? _encounter.Outcome.Detail : _autoPlaying ? $"Auto-play completed round {_encounter.CompletedRounds}." : $"Round {_encounter.CompletedRounds} complete. Draft next Blue orders.";
            _resolving = false;
        }

        private void Render(GameState state)
        {
            foreach (var unit in state.Units)
            {
                _displayHitPoints[unit.Id] = unit.HitPoints;
                _views[unit.Id].transform.position = new Vector3(unit.Position.X, .3f, unit.Position.Y);
                var color = unit.ActivityState == UnitActivityState.Incapacitated ? Color.gray : UnitColor(unit);
                if (unit.FactionId == "red" && !IsObservableByBlue(unit, state)) color = Color.Lerp(color, Color.black, .55f);
                _views[unit.Id].GetComponent<Renderer>().material.color = color;
            }
        }

        private static int UnitNumber(Guid id) => id.ToString("N")[31] - '0';
        private IReadOnlyList<GridPosition> ScoutObjectives => _scenario.Map.AreaById("contact-rally")?.Tiles
            ?? _scenario.Map.AreaById("central-crossing")?.Tiles
            ?? Array.Empty<GridPosition>();
        private PvePlanningPolicy BlueHoldPolicy => new(_scenario.Map.AreaById("central-crossing")?.Tiles, HoldWhenOccupied: true);
        private PvePlanningPolicy PlayerDoctrinePolicy => new(
            _scenario.Map.AreaById("central-crossing")?.Tiles,
            DoctrineAssignments: _blueDoctrines.Select(entry => new PveDoctrineAssignment(entry.Key, entry.Value, _followTargets.TryGetValue(entry.Key, out var target) ? target : null)).ToArray());
        private AttackProfile AttackProfileFor(UnitState unit)
        {
            var definition = _scenario.UnitDefinitions!.Single(candidate => candidate.Id == unit.UnitDefinitionId);
            return AttackProfiles.First(profile => (definition.AttackProfileIds ?? Array.Empty<string>()).Contains(profile.Id, StringComparer.Ordinal));
        }
        private static string RoleName(UnitState unit) => unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? "MEDIC" : unit.UnitDefinitionId == StarterMilitaryContent.Marksman.Id ? "MARKSMAN" : "RIFLE";
        private bool IsObservableByBlue(UnitState target, GameState state) => state.Units.Where(unit => unit.FactionId == "blue" && unit.ActivityState == UnitActivityState.Active)
            .Any(observer => VisibilityRules.Observe(_scenario.Map, observer, target).IsObservable);
        private static Color UnitColor(UnitState unit) => unit.FactionId == "blue"
            ? unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? new Color(.25f, .9f, .65f) : unit.UnitDefinitionId == StarterMilitaryContent.Marksman.Id ? new Color(.62f, .45f, 1f) : new Color(.2f, .62f, 1f)
            : unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? new Color(1f, .55f, .25f) : unit.UnitDefinitionId == StarterMilitaryContent.Marksman.Id ? new Color(.9f, .3f, .78f) : new Color(1f, .3f, .25f);

        private void ApplyVitalityFeedback(DomainEvent @event, Guid? targetId, string label, Color color)
        {
            if (!targetId.HasValue || !@event.HitPointsAfter.HasValue) return;
            _displayHitPoints[targetId.Value] = @event.HitPointsAfter.Value;
            var applied = DetailInteger(@event.Detail, "applied") ?? 0;
            AddFeedback(targetId.Value, applied == 0 ? label : $"{(applied > 0 ? "+" : String.Empty)}{applied} {label}", color);
            if (@event.ActivityStateAfter == UnitActivityState.Incapacitated)
            {
                _views[targetId.Value].GetComponent<Renderer>().material.color = Color.gray;
                AddFeedback(targetId.Value, "INCAPACITATED", Color.white);
            }
        }

        private void AddFeedback(Guid unitId, string text, Color color)
        {
            _feedback.RemoveAll(pulse => pulse.UnitId == unitId);
            _feedback.Add(new FeedbackPulse(unitId, text, color, Time.time + 1.6f));
        }

        private static int? DetailInteger(string? detail, string key)
        {
            if (String.IsNullOrEmpty(detail)) return null;
            var marker = key + "=";
            var start = detail.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return null;
            start += marker.Length;
            var end = detail.IndexOf(';', start);
            return Int32.TryParse(end < 0 ? detail[start..] : detail[start..end], out var value) ? value : null;
        }

        private static string EventLine(DomainEvent @event)
        {
            var label = @event.Type switch
            {
                DomainEventType.UnitEnteredTile => "MOVE",
                DomainEventType.MovementDelayed => "CLASH DELAY",
                DomainEventType.EffectApplied => "HEAL",
                DomainEventType.AttackResolved => "ATTACK",
                DomainEventType.ReactionAttackResolved => "OVERWATCH HIT",
                DomainEventType.OverwatchArmed => "OVERWATCH ARMED",
                DomainEventType.ActionFailed => "FAILED",
                _ => @event.Type.ToString()
            };
            return $"t{@event.Tick:00} {label} {@event.FactionId} {@event.Detail}";
        }

        private static string RoundSummary(IReadOnlyList<DomainEvent> events, int round) =>
            $"Round {round} result: {events.Count(@event => @event.Type == DomainEventType.UnitEnteredTile)} moves · {events.Count(@event => @event.Type == DomainEventType.AttackResolved)} attacks · {events.Count(@event => @event.Type == DomainEventType.EffectApplied)} heals · {events.Count(@event => @event.Type == DomainEventType.ReactionAttackResolved)} reactions · {events.Count(@event => @event.Type == DomainEventType.MovementDelayed)} delays · {events.Count(@event => @event.Type == DomainEventType.ActionFailed)} failed";

        private string ObjectiveStatus()
        {
            var objective = _scenario.Objectives!.First(candidate => candidate.Id == "hold-central-crossing");
            var heldRounds = _encounter.ObjectiveProgress?.FirstOrDefault(progress => progress.ObjectiveId == objective.Id)?.ControlledRounds ?? 0;
            return $"Objective: Blue holds Central Crossing uncontested {heldRounds}/{objective.RequiredControlRounds} completed rounds (or eliminate Red).";
        }

        private void DrawFeedback()
        {
            _feedback.RemoveAll(pulse => pulse.ExpiresAt <= Time.time || !_views.ContainsKey(pulse.UnitId));
            var previousColor = GUI.color;
            foreach (var pulse in _feedback)
            {
                var screen = Camera.main!.WorldToScreenPoint(_views[pulse.UnitId].transform.position + Vector3.up * 1.25f);
                GUI.color = pulse.Color;
                GUI.Label(new Rect(screen.x - 72, Screen.height - screen.y, 150, 22), pulse.Text);
            }
            GUI.color = previousColor;
        }

        private sealed class FeedbackPulse
        {
            public FeedbackPulse(Guid unitId, string text, Color color, float expiresAt) { UnitId = unitId; Text = text; Color = color; ExpiresAt = expiresAt; }
            public Guid UnitId { get; }
            public string Text { get; }
            public Color Color { get; }
            public float ExpiresAt { get; }
        }

        private TacticalAction? SelectedQueuedOverwatch() =>
            _blueOrders.TryGetValue(_selectedBlue, out var actions) ? actions.LastOrDefault(action => action.Type == TacticalActionType.EnterOverwatch) : null;

        private void DrawOverwatchOverlay()
        {
            DrawObjectiveOverlay();
            DrawPlannedRouteOverlay();
            var queued = SelectedQueuedOverwatch();
            if (queued?.Facing is not null)
                DrawWatchCone(_encounter.CurrentState.FindUnit(_selectedBlue)!, queued.Facing.Value, new Color(1f, .82f, .15f, .28f), "PLANNED");
            foreach (var armed in _armedOverwatch)
                DrawWatchCone(_encounter.CurrentState.FindUnit(armed.Key)!, armed.Value, new Color(1f, .5f, .1f, .34f), "ARMED");
        }

        private void DrawObjectiveOverlay()
        {
            var area = _scenario.Map.AreaById("central-crossing");
            if (area is null) return;
            var camera = Camera.main!;
            var tileSize = Mathf.Abs(camera.WorldToScreenPoint(new Vector3(1, .1f, 0)).x - camera.WorldToScreenPoint(new Vector3(0, .1f, 0)).x) * .76f;
            var previousColor = GUI.color;
            GUI.color = new Color(.22f, .95f, .72f, .16f);
            foreach (var position in area.Tiles)
            {
                var screen = camera.WorldToScreenPoint(new Vector3(position.X, .105f, position.Y));
                GUI.DrawTexture(new Rect(screen.x - tileSize / 2, Screen.height - screen.y - tileSize / 2, tileSize, tileSize), Texture2D.whiteTexture);
            }
            var labelPosition = camera.WorldToScreenPoint(new Vector3(area.Tiles[0].X, .12f, area.Tiles[0].Y));
            GUI.Label(new Rect(labelPosition.x - 34, Screen.height - labelPosition.y - 22, 100, 20), "HOLD AREA");
            GUI.color = previousColor;
        }

        private void DrawPlannedRouteOverlay()
        {
            if (!_blueOrders.TryGetValue(_selectedBlue, out var actions)) return;
            var move = actions.LastOrDefault(action => action.Type == TacticalActionType.Move);
            if (move?.Path is not { Count: > 0 }) return;
            var conflicts = PlannedMovementConflicts();
            var camera = Camera.main!;
            var tileSize = Mathf.Abs(camera.WorldToScreenPoint(new Vector3(1, .1f, 0)).x - camera.WorldToScreenPoint(new Vector3(0, .1f, 0)).x) * .62f;
            var priorColor = GUI.color;
            GUI.color = new Color(.2f, .9f, 1f, .42f);
            for (var index = 0; index < move.Path.Count; index++)
            {
                var position = move.Path[index];
                var screen = camera.WorldToScreenPoint(new Vector3(position.X, .13f, position.Y));
                var arrivalTick = MoveArrivalTick(move, index);
                GUI.color = conflicts.Contains((position, arrivalTick)) ? new Color(1f, .15f, .75f, .58f) : new Color(.2f, .9f, 1f, .42f);
                GUI.DrawTexture(new Rect(screen.x - tileSize / 2, Screen.height - screen.y - tileSize / 2, tileSize, tileSize), Texture2D.whiteTexture);
                GUI.Label(new Rect(screen.x - 9, Screen.height - screen.y - 9, 25, 20), (index + 1).ToString());
            }
            GUI.color = priorColor;
        }

        private HashSet<(GridPosition Position, int Tick)> PlannedMovementConflicts() => _blueOrders.Values.SelectMany(actions => actions)
            .Where(action => action.Type == TacticalActionType.Move)
            .SelectMany(action => MovementRules.PathFor(action).Select((position, index) => (Position: position, Tick: MoveArrivalTick(action, index))))
            .GroupBy(intent => intent)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        private int MoveArrivalTick(TacticalAction move, int index) => move.StartTick + MovementRules.PathFor(move).Take(index + 1).Sum(position => _scenario.Map.CellAt(position).MovementTicks);

        private void DrawWatchCone(UnitState unit, Facing facing, Color color, string label)
        {
            var camera = Camera.main!;
            var tileSize = Mathf.Abs(camera.WorldToScreenPoint(new Vector3(unit.Position.X + 1, .1f, unit.Position.Y)).x - camera.WorldToScreenPoint(new Vector3(unit.Position.X, .1f, unit.Position.Y)).x) * .82f;
            var priorColor = GUI.color;
            GUI.color = color;
            for (var x = 0; x < _scenario.Map.Width; x++)
            for (var y = 0; y < _scenario.Map.Height; y++)
            {
                var tile = new GridPosition(x, y);
                if (GridDistance.Manhattan(unit.Position, tile) > AttackProfileFor(unit).MaximumRange || !IsInsideWatchCone(unit.Position, facing, tile)) continue;
                var screen = camera.WorldToScreenPoint(new Vector3(x, .12f, y));
                GUI.DrawTexture(new Rect(screen.x - tileSize / 2, Screen.height - screen.y - tileSize / 2, tileSize, tileSize), Texture2D.whiteTexture);
            }
            GUI.color = priorColor;
            var unitScreen = camera.WorldToScreenPoint(_views[unit.Id].transform.position + Vector3.up * 1.05f);
            GUI.Label(new Rect(unitScreen.x - 42, Screen.height - unitScreen.y, 100, 20), $"{label} {FacingSymbol(facing)}");
        }

        private static bool IsInsideWatchCone(GridPosition origin, Facing facing, GridPosition target) => facing switch
        {
            Facing.North => target.Y > origin.Y && Math.Abs(target.X - origin.X) <= target.Y - origin.Y,
            Facing.South => target.Y < origin.Y && Math.Abs(target.X - origin.X) <= origin.Y - target.Y,
            Facing.East => target.X > origin.X && Math.Abs(target.Y - origin.Y) <= target.X - origin.X,
            Facing.West => target.X < origin.X && Math.Abs(target.Y - origin.Y) <= origin.X - target.X,
            _ => false
        };

        private static string FacingSymbol(Facing facing) => facing switch { Facing.North => "↑", Facing.East => "→", Facing.South => "↓", Facing.West => "←", _ => "?" };

        private IEnumerator AnimateProjectile(GridPosition from, GridPosition to, Color color)
        {
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Resolved attack projectile";
            projectile.transform.SetParent(transform, false); projectile.transform.localScale = Vector3.one * .20f;
            projectile.GetComponent<Renderer>().material.color = color;
            var start = new Vector3(from.X, .65f, from.Y);
            var end = new Vector3(to.X, .65f, to.Y);
            const float durationSeconds = .28f;
            for (var elapsed = 0f; elapsed < durationSeconds; elapsed += Time.deltaTime)
            {
                projectile.transform.position = Vector3.Lerp(start, end, elapsed / durationSeconds);
                yield return null;
            }
            projectile.transform.position = end;
            Destroy(projectile);
        }

        private string PlannedOrderDescription(UnitState unit)
        {
            if (!_blueOrders.TryGetValue(unit.Id, out var actions) || actions.Count == 0)
                return $"Doctrine {DoctrineLabel(DoctrineFor(unit.Id))} — no generated order; waits";
            var descriptions = actions.Select(action => ActionDescription(action)).ToArray();
            var spent = actions.Sum(action => ActionPointRules.CostFor(action, _scenario.Map, new[] { FieldMedKit }, AttackProfiles));
            var finalTick = actions.Max(action => action.StartTick + action.DurationTicks);
            var conflicts = PlannedMovementConflicts();
            var hasConflict = actions.Where(action => action.Type == TacticalActionType.Move).SelectMany(action => MovementRules.PathFor(action).Select((position, index) => (position, MoveArrivalTick(action, index)))).Any(intent => conflicts.Contains(intent));
            var rationale = _doctrineExplanations.TryGetValue(unit.Id, out var explanation) ? $" — {explanation}" : String.Empty;
            return $"{String.Join(" → ", descriptions)}  [{spent}/{unit.ActionPointBudget} AP; t0–{finalTick}/10]{(hasConflict ? " ⚠ seeded clash" : String.Empty)}{rationale}";
        }

        private static string DoctrineLabel(TacticalDoctrine doctrine) => doctrine switch
        {
            TacticalDoctrine.HoldObjective => "Hold objective",
            TacticalDoctrine.KeepRange => "Keep range",
            TacticalDoctrine.SupportFollow => "Support / follow",
            _ => "Aggressive"
        };

        private static string ActionDescription(TacticalAction action)
        {
            if (action.Type == TacticalActionType.Move && action.Path is { Count: > 0 })
            {
                var destination = action.Path[^1];
                return $"Move to ({destination.X},{destination.Y})";
            }
            if (action.Type == TacticalActionType.Attack && action.TargetUnitId.HasValue)
                return $"Attack Red {UnitNumber(action.TargetUnitId.Value)} — checked at resolution";
            if (action.Type == TacticalActionType.ApplyEffect && action.TargetUnitId.HasValue)
                return $"Heal Blue {UnitNumber(action.TargetUnitId.Value)} — range checked at resolution";
            if (action.Type == TacticalActionType.EnterOverwatch && action.Facing.HasValue)
                return $"Overwatch {action.Facing} — 90° cone, one reaction shot";
            return action.Type.ToString();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 1000, 196), "Riverside Crossing 4v4 — player Blue vs deterministic Red");
            if (GUI.Button(new Rect(24, 44, 120, 26), "Submit round")) Submit();
            if (GUI.Button(new Rect(154, 44, 80, 26), "Reset")) ResetEncounter();
            if (GUI.Button(new Rect(244, 44, 120, 26), "Auto-play demo")) StartAutoPlay();
            GUI.Label(new Rect(375, 44, 625, 22), _message);
            var blue = _encounter.CurrentState.Units.Where(unit => unit.FactionId == "blue").OrderBy(unit => unit.Id).ToArray();
            for (var i = 0; i < blue.Length; i++)
            {
                var marker = blue[i].Id == _selectedBlue ? "> " : string.Empty;
                if (GUI.Button(new Rect(24 + i * 74, 76, 68, 24), $"{marker}Blue {i + 1}")) _selectedBlue = blue[i].Id;
            }
            GUI.Label(new Rect(300, 78, 200, 20), "Click a map tile to preview route");
            if (GUI.Button(new Rect(505, 76, 100, 24), "Attack red")) DraftAttack();
            if (GUI.Button(new Rect(615, 76, 100, 24), "Next red")) _selectedRed = NextRed();
            if (GUI.Button(new Rect(725, 76, 60, 24), "Undo")) UndoLastBlueAction();
            if (GUI.Button(new Rect(795, 76, 65, 24), "Clear")) ClearSelectedOrder();
            if (GUI.Button(new Rect(300, 104, 115, 24), "Next heal target")) _selectedHealTarget = NextBlueHealTarget();
            if (GUI.Button(new Rect(425, 104, 115, 24), "Medic heal target")) DraftHealTarget();
            GUI.Label(new Rect(550, 106, 300, 20), $"Heal target: Blue {UnitNumber(_selectedHealTarget)}");
            if (GUI.Button(new Rect(850, 104, 140, 24), _manualRoute ? "Manual route: ON" : "Manual route: OFF")) _manualRoute = !_manualRoute;
            GUI.Label(new Rect(24, 134, 250, 20), "Overwatch zone (eligible unit):");
            if (GUI.Button(new Rect(280, 132, 45, 24), "N")) DraftOverwatch(Facing.North);
            if (GUI.Button(new Rect(335, 132, 45, 24), "E")) DraftOverwatch(Facing.East);
            if (GUI.Button(new Rect(390, 132, 45, 24), "S")) DraftOverwatch(Facing.South);
            if (GUI.Button(new Rect(445, 132, 45, 24), "W")) DraftOverwatch(Facing.West);
            GUI.Label(new Rect(24, 162, 120, 20), "Doctrine:");
            if (GUI.Button(new Rect(92, 158, 88, 24), "Aggressive")) SetDoctrine(TacticalDoctrine.Aggressive);
            if (GUI.Button(new Rect(188, 158, 92, 24), "Hold")) SetDoctrine(TacticalDoctrine.HoldObjective);
            if (GUI.Button(new Rect(288, 158, 92, 24), "Keep range")) SetDoctrine(TacticalDoctrine.KeepRange);
            if (GUI.Button(new Rect(388, 158, 100, 24), "Support")) SetDoctrine(TacticalDoctrine.SupportFollow);
            if (GUI.Button(new Rect(498, 158, 120, 24), "Next follow unit")) _followTargets[_selectedBlue] = NextBlueFollowTarget();
            if (GUI.Button(new Rect(628, 158, 130, 24), "Auto-plan selected")) AutoPlanSelected();
            var followLabel = _followTargets.TryGetValue(_selectedBlue, out var followTarget) ? $"Follow: Blue {UnitNumber(followTarget)}" : "Follow: nearest ally";
            GUI.Label(new Rect(768, 162, 220, 20), $"{DoctrineLabel(DoctrineFor(_selectedBlue))}; {followLabel}");
            GUI.Box(new Rect(12, 216, 1000, 112), $"Round plan — {PlannedActionCount} actions across {_blueOrders.Count} Blue units. Doctrine orders are ordinary editable actions; Undo removes the selected unit's last action.");
            for (var i = 0; i < blue.Length; i++)
            {
                var selected = blue[i].Id == _selectedBlue ? "> " : "  ";
                GUI.Label(new Rect(28, 242 + i * 20, 970, 20), $"{selected}Blue {i + 1}: {PlannedOrderDescription(blue[i])}");
            }
            GUI.Label(new Rect(560, 134, 440, 20), ObjectiveStatus());
            GUI.Box(new Rect(12, 336, 1000, 30), _roundSummary);
            var y = 374f; foreach (var line in _lines.Take(11)) { GUI.Label(new Rect(20, y, 990, 20), line); y += 19; }
            DrawOverwatchOverlay();
            foreach (var unit in _encounter.CurrentState.Units)
            {
                var screen = Camera.main!.WorldToScreenPoint(_views[unit.Id].transform.position + Vector3.up * .65f);
                var medKits = InventoryRules.QuantityOf(unit, "med-kit");
                var ammunitionItem = AttackProfileFor(unit).AmmunitionItemId;
                var ammunition = String.IsNullOrWhiteSpace(ammunitionItem) ? 0 : InventoryRules.QuantityOf(unit, ammunitionItem);
                var inventory = medKits > 0 || unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? $" kit:{medKits}" : string.Empty;
                inventory += !String.IsNullOrWhiteSpace(ammunitionItem) ? $" ammo:{ammunition}" : string.Empty;
                var visibility = unit.FactionId == "red" ? IsObservableByBlue(unit, _encounter.CurrentState) ? " OBS" : " HIDDEN" : string.Empty;
                var hitPoints = _displayHitPoints.TryGetValue(unit.Id, out var displayHitPoints) ? displayHitPoints : unit.HitPoints;
                GUI.Label(new Rect(screen.x - 70, Screen.height - screen.y, 180, 20), $"{unit.FactionId.ToUpperInvariant()} {UnitNumber(unit.Id)} {RoleName(unit)} {hitPoints}/{unit.MaxHitPoints}{inventory}{visibility}");
            }
            DrawFeedback();
        }

        private Guid NextRed()
        {
            var red = _encounter.CurrentState.Units.Where(unit => unit.FactionId == "red" && unit.ActivityState == UnitActivityState.Active).OrderBy(unit => unit.Id).ToArray();
            var index = Array.FindIndex(red, unit => unit.Id == _selectedRed); return red.Length == 0 ? _selectedRed : red[(index + 1 + red.Length) % red.Length].Id;
        }

        private Guid NextBlueHealTarget()
        {
            var blue = _encounter.CurrentState.Units.Where(unit => unit.FactionId == "blue" && unit.ActivityState == UnitActivityState.Active).OrderBy(unit => unit.Id).ToArray();
            var index = Array.FindIndex(blue, unit => unit.Id == _selectedHealTarget); return blue.Length == 0 ? _selectedHealTarget : blue[(index + 1 + blue.Length) % blue.Length].Id;
        }

        private Guid NextBlueFollowTarget()
        {
            var blue = _encounter.CurrentState.Units.Where(unit => unit.FactionId == "blue" && unit.Id != _selectedBlue && unit.ActivityState == UnitActivityState.Active).OrderBy(unit => unit.Id).ToArray();
            if (blue.Length == 0) return _selectedBlue;
            var current = _followTargets.TryGetValue(_selectedBlue, out var target) ? target : blue[^1].Id;
            var index = Array.FindIndex(blue, unit => unit.Id == current);
            return blue[(index + 1 + blue.Length) % blue.Length].Id;
        }
    }
}
