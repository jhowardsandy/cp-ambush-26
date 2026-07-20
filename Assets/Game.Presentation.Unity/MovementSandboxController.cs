#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TacticalStrategyGame.Core;
using UnityEngine;

namespace TacticalStrategyGame.Presentation.Unity
{
    public sealed class MovementSandboxController : MonoBehaviour
    {
        private readonly Dictionary<Guid, GameObject> _unitViews = new();
        private readonly Dictionary<Guid, int> _displayHitPoints = new();
        private readonly Dictionary<Guid, int> _displayMaxHitPoints = new();
        private readonly Dictionary<Guid, UnitActivityState> _displayActivityStates = new();
        private readonly List<string> _eventLines = new();
        private ScenarioDefinition _scenario = null!;
        private EncounterState _encounter = null!;
        private SimulationResult? _result;
        private bool _isResolving;
        private bool _manualPlanningMode;
        private readonly List<TacticalAction> _draftedActions = new();
        private string _planningMessage = string.Empty;
        private const int DemonstrationRoundCount = 3;
        private static readonly AttackProfile SandboxRifle = new("sandbox-rifle", 1, 3, 10);

        private void Start()
        {
            BuildScene();
            ResetSandbox();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                StartRoundPlayback();
            if (Input.GetKeyDown(KeyCode.R))
                ResetSandbox();
        }

        private void BuildScene()
        {
            _scenario = new ScenarioDefinition(
                "movement-sandbox-01",
                new GridMapDefinition("sandbox-grid", 8, 6, new[]
                {
                    new TerrainCellDefinition(new GridPosition(2, 1), MovementTicks: 2),
                    new TerrainCellDefinition(new GridPosition(4, 3), IsPassable: false, BlocksLineOfSight: true)
                }),
                new GameState(new[]
                {
                    new UnitState(Guid.Parse("11111111-1111-1111-1111-111111111111"), "blue", new GridPosition(1, 1), Facing.East, UnitActivityState.Active, HitPoints: 8, MaxHitPoints: 10),
                    new UnitState(Guid.Parse("22222222-2222-2222-2222-222222222222"), "red", new GridPosition(6, 4), Facing.West, UnitActivityState.Active)
                }),
                Objectives: new[] { new ObjectiveDefinition("incapacitate-red", ObjectiveType.IncapacitateAllOpposingUnits, "blue") });

            for (var x = 0; x < _scenario.Map.Width; x++)
            for (var y = 0; y < _scenario.Map.Height; y++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Tile {x},{y}";
                tile.transform.SetParent(transform, false);
                tile.transform.position = new Vector3(x, 0, y);
                tile.transform.localScale = new Vector3(0.94f, 0.08f, 0.94f);
                var terrain = _scenario.Map.CellAt(new GridPosition(x, y));
                tile.GetComponent<Renderer>().material.color = !terrain.IsPassable
                    ? new Color(0.48f, 0.18f, 0.18f)
                    : terrain.MovementTicks > 1
                        ? new Color(0.50f, 0.38f, 0.16f)
                        : (x + y) % 2 == 0
                            ? new Color(0.16f, 0.20f, 0.24f)
                            : new Color(0.20f, 0.25f, 0.30f);
            }

            foreach (var unit in _scenario.InitialState.Units)
            {
                var view = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                view.name = $"{unit.FactionId} unit";
                view.transform.SetParent(transform, false);
                view.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
                view.GetComponent<Renderer>().material.color = unit.FactionId == "blue"
                    ? new Color(0.25f, 0.60f, 1.0f)
                    : new Color(1.0f, 0.32f, 0.28f);
                _unitViews.Add(unit.Id, view);
            }

            EnsureCameraAndLight();
        }

        private void ResetSandbox()
        {
            StopAllCoroutines();
            _isResolving = false;
            _result = null;
            _draftedActions.Clear();
            _encounter = new EncounterState(
                new EncounterDefinition(_scenario.Id, _scenario.Map, _scenario.ContentVersion, _scenario.Objectives),
                _scenario.InitialState);
            _eventLines.Clear();
            _planningMessage = _manualPlanningMode
                ? "Manual planning: build a blue path or draft an action, then submit it."
                : "Encounter reset. Round 1 is ready: scripted movement orders.";
            _eventLines.Add(_planningMessage);
            RenderState(_scenario.InitialState);
        }

        private void StartRoundPlayback()
        {
            if (!_isResolving && (_manualPlanningMode ? _draftedActions.Count > 0 && _encounter.Outcome?.IsComplete != true : _encounter.CompletedRounds < DemonstrationRoundCount))
                StartCoroutine(ResolveAndPlayback());
        }

        private IEnumerator ResolveAndPlayback()
        {
            _isResolving = true;
            var roundNumber = _encounter.CompletedRounds + 1;
            var stateBeforeRound = _encounter.CurrentState;
            var round = EncounterResolver.ResolveRound(
                _encounter,
                _manualPlanningMode ? CommandsForManualRound() : CommandsForRound(roundNumber),
                new RoundConfiguration(10),
                (uint)(20260720 + roundNumber),
                effects: new[] { new EffectDefinition("field-med-kit", 5) },
                attackProfiles: new[] { SandboxRifle });

            _result = round.Resolution;
            _encounter = round.NextState;
            _draftedActions.Clear();
            _eventLines.Clear();
            _eventLines.Add($"Resolving encounter round {roundNumber}.");
            RenderState(stateBeforeRound);

            foreach (var tickEvents in _result.Events.GroupBy(@event => @event.Tick).OrderBy(group => group.Key))
            {
                foreach (var @event in tickEvents)
                {
                    if (@event.Type == DomainEventType.UnitEnteredTile && @event.UnitId.HasValue && @event.ToPosition != null)
                        _unitViews[@event.UnitId.Value].transform.position = new Vector3(@event.ToPosition.X, 0.3f, @event.ToPosition.Y);

                    if (@event.Type == DomainEventType.AttackResolved && @event.FromPosition != null && @event.ToPosition != null)
                        yield return AnimateProjectile(@event.FromPosition, @event.ToPosition);

                    var affectedUnitId = @event.Type == DomainEventType.AttackResolved ? @event.TargetUnitId : @event.UnitId;
                    if ((@event.Type == DomainEventType.EffectApplied || @event.Type == DomainEventType.AttackResolved) && affectedUnitId.HasValue && @event.HitPointsAfter.HasValue && @event.ActivityStateAfter.HasValue)
                    {
                        _displayHitPoints[affectedUnitId.Value] = @event.HitPointsAfter.Value;
                        _displayActivityStates[affectedUnitId.Value] = @event.ActivityStateAfter.Value;
                        ApplyActivityAppearance(affectedUnitId.Value, @event.ActivityStateAfter.Value);
                    }

                    _eventLines.Add($"t{@event.Tick:00} {@event.Type} {@event.FactionId} {@event.Detail}");
                }

                yield return new WaitForSeconds(0.55f);
            }

            _eventLines.Add($"Checksum: {_result.FinalStateChecksum}");
            if (_encounter.Outcome?.IsComplete == true)
                _eventLines.Add($"OUTCOME: {_encounter.Outcome.Detail}");
            else if (_manualPlanningMode)
                _eventLines.Add("Manual planning: build the next blue path or draft an action.");
            else _eventLines.Add(_encounter.CompletedRounds < DemonstrationRoundCount
                ? $"Round {roundNumber} complete. Resolve round {_encounter.CompletedRounds + 1} for fresh orders."
                : "Three-round encounter demo complete. Reset to begin again.");
            _isResolving = false;
        }

        private static IReadOnlyList<CommandBundle> CommandsForRound(int roundNumber)
        {
            var blueUnit = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var redUnit = Guid.Parse("22222222-2222-2222-2222-222222222222");
            if (roundNumber == 1)
            {
                var blueMove = new TacticalAction(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), blueUnit, TacticalActionType.Move, 0, 4,
                    Path: new[] { new GridPosition(2, 1), new GridPosition(3, 1), new GridPosition(3, 2) });
                var redMove = new TacticalAction(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), redUnit, TacticalActionType.Move, 1, 2,
                    Path: new[] { new GridPosition(5, 4), new GridPosition(4, 4) });
                return new[] { new CommandBundle("blue", new[] { blueMove }), new CommandBundle("red", new[] { redMove }) };
            }

            if (roundNumber == 2)
            {
                var blueHeal = new TacticalAction(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), blueUnit, TacticalActionType.ApplyEffect, 0, 1,
                    TargetUnitId: blueUnit, EffectId: "field-med-kit");
                var redWait = new TacticalAction(
                    Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), redUnit, TacticalActionType.Wait, 0, 1);
                return new[] { new CommandBundle("blue", new[] { blueHeal }), new CommandBundle("red", new[] { redWait }) };
            }

            var blueAttack = new TacticalAction(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), blueUnit, TacticalActionType.Attack, 0, 2,
                TargetUnitId: redUnit, AttackProfileId: "sandbox-rifle");
            return new[] { new CommandBundle("blue", new[] { blueAttack }) };
        }

        private IReadOnlyList<CommandBundle> CommandsForManualRound()
        {
            var redUnit = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var redWait = new TacticalAction(
                Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff4"), redUnit, TacticalActionType.Wait, 0, 1);
            return new[] { new CommandBundle("blue", _draftedActions.ToArray()), new CommandBundle("red", new[] { redWait }) };
        }

        private void DraftMove(GridPosition delta)
        {
            var blue = _encounter.CurrentState.Units.Single(unit => unit.FactionId == "blue");
            if (blue.ActivityState != UnitActivityState.Active)
            {
                _planningMessage = "Blue is incapacitated and cannot receive an order.";
                return;
            }

            if (_draftedActions.Any(action => action.Type != TacticalActionType.Move))
            {
                _planningMessage = "Movement must be built before the drafted follow-up action. Undo or clear the draft to change it.";
                return;
            }

            var existingMove = _draftedActions.SingleOrDefault(action => action.Type == TacticalActionType.Move);
            var existingPath = existingMove is not null
                ? MovementRules.PathFor(existingMove).ToList()
                : new List<GridPosition>();
            var origin = existingPath.Count > 0 ? existingPath[^1] : blue.Position;
            var destination = new GridPosition(origin.X + delta.X, origin.Y + delta.Y);
            if (!_scenario.Map.Contains(destination) || !_scenario.Map.CellAt(destination).IsPassable || _encounter.CurrentState.Units.Any(unit => unit.Position == destination) || existingPath.Contains(destination))
            {
                _planningMessage = "That path step is not currently legal: it is outside the map, blocked, occupied, or already in this path.";
                return;
            }

            existingPath.Add(destination);
            var draft = new TacticalAction(Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff1"), blue.Id, TacticalActionType.Move, 0, 1, Path: existingPath);
            var move = draft with { DurationTicks = MovementRules.DurationFor(draft, _scenario.Map) };
            if (move.DurationTicks > 10)
            {
                existingPath.RemoveAt(existingPath.Count - 1);
                _planningMessage = "That path exceeds this round's 10-tick movement budget.";
                return;
            }

            if (existingMove is not null)
                _draftedActions.Remove(existingMove);
            _draftedActions.Insert(0, move);
            _planningMessage = $"Drafted {existingPath.Count}-tile path to ({destination.X}, {destination.Y}) for {move.DurationTicks}/10 tick(s).";
        }

        private void UndoLastDraft()
        {
            if (_draftedActions.Count == 0)
            {
                _planningMessage = "There is no drafted order to undo.";
                return;
            }

            var lastAction = _draftedActions[^1];
            if (lastAction.Type != TacticalActionType.Move)
            {
                _draftedActions.RemoveAt(_draftedActions.Count - 1);
                _planningMessage = "Removed the drafted follow-up action.";
                return;
            }

            var path = MovementRules.PathFor(lastAction).ToList();
            path.RemoveAt(path.Count - 1);
            if (path.Count == 0)
            {
                _draftedActions.Clear();
                _planningMessage = "Movement path cleared.";
                return;
            }

            var shortenedMove = lastAction with { Path = path, DurationTicks = MovementRules.DurationFor(lastAction with { Path = path }, _scenario.Map) };
            _draftedActions[^1] = shortenedMove;
            var destination = path[^1];
            _planningMessage = $"Path shortened to {path.Count} tile(s), ending at ({destination.X}, {destination.Y}) for {shortenedMove.DurationTicks}/10 tick(s).";
        }

        private void DraftHeal()
        {
            var blue = _encounter.CurrentState.Units.Single(unit => unit.FactionId == "blue");
            if (blue.HitPoints >= blue.MaxHitPoints)
            {
                _planningMessage = "Blue is already at full vitality; healing would have no effect.";
                return;
            }
            if (!CanAppendFollowUpAction(1))
                return;
            var startTick = DraftEndTick();
            _draftedActions.Add(new TacticalAction(Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff2"), blue.Id, TacticalActionType.ApplyEffect, startTick, 1,
                TargetUnitId: blue.Id, EffectId: "field-med-kit");
            _planningMessage = $"Drafted field-med-kit heal to resolve at tick {startTick + 1}.";
        }

        private void DraftAttack()
        {
            var blue = _encounter.CurrentState.Units.Single(unit => unit.FactionId == "blue");
            var red = _encounter.CurrentState.Units.Single(unit => unit.FactionId == "red");
            if (!CanAppendFollowUpAction(2))
                return;
            var startTick = DraftEndTick();
            _draftedActions.Add(new TacticalAction(Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff3"), blue.Id, TacticalActionType.Attack, startTick, 2,
                TargetUnitId: red.Id, AttackProfileId: "sandbox-rifle");
            _planningMessage = $"Drafted speculative attack on red to resolve at tick {startTick + 2}; range and sight are checked only then.";
        }

        private int DraftEndTick() => _draftedActions.Count == 0 ? 0 : _draftedActions.Max(action => action.StartTick + action.DurationTicks);

        private bool CanAppendFollowUpAction(int durationTicks)
        {
            if (_draftedActions.Any(action => action.Type != TacticalActionType.Move))
            {
                _planningMessage = "This first planner supports one movement path followed by one action. Undo or clear the draft to change it.";
                return false;
            }

            if (DraftEndTick() + durationTicks > 10)
            {
                _planningMessage = "There are not enough ticks left in this round for that follow-up action.";
                return false;
            }

            return true;
        }

        private IEnumerator AnimateProjectile(GridPosition from, GridPosition to)
        {
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Direct attack projectile";
            projectile.transform.SetParent(transform, false);
            projectile.transform.localScale = Vector3.one * 0.20f;
            projectile.GetComponent<Renderer>().material.color = new Color(1.0f, 0.82f, 0.22f);

            var start = new Vector3(from.X, 0.65f, from.Y);
            var end = new Vector3(to.X, 0.65f, to.Y);
            const float durationSeconds = 0.35f;
            for (var elapsed = 0f; elapsed < durationSeconds; elapsed += Time.deltaTime)
            {
                projectile.transform.position = Vector3.Lerp(start, end, elapsed / durationSeconds);
                yield return null;
            }
            projectile.transform.position = end;
            Destroy(projectile);
        }

        private void RenderState(GameState state)
        {
            foreach (var unit in state.Units)
            {
                var view = _unitViews[unit.Id];
                view.transform.position = new Vector3(unit.Position.X, 0.3f, unit.Position.Y);
                view.transform.rotation = Quaternion.Euler(0, FacingAngle(unit.Facing), 0);
                _displayHitPoints[unit.Id] = unit.HitPoints;
                _displayMaxHitPoints[unit.Id] = unit.MaxHitPoints;
                _displayActivityStates[unit.Id] = unit.ActivityState;
                ApplyActivityAppearance(unit.Id, unit.ActivityState);
            }
        }

        private void ApplyActivityAppearance(Guid unitId, UnitActivityState activityState)
        {
            var renderer = _unitViews[unitId].GetComponent<Renderer>();
            if (activityState == UnitActivityState.Incapacitated)
            {
                renderer.material.color = new Color(0.25f, 0.25f, 0.25f);
                return;
            }

            var unit = _scenario.InitialState.FindUnit(unitId)!;
            renderer.material.color = unit.FactionId == "blue"
                ? new Color(0.25f, 0.60f, 1.0f)
                : new Color(1.0f, 0.32f, 0.28f);
        }

        private static float FacingAngle(Facing facing) => facing switch
        {
            Facing.North => 0f,
            Facing.East => 90f,
            Facing.South => 180f,
            Facing.West => 270f,
            _ => 0f
        };

        private void EnsureCameraAndLight()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Sandbox Camera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.orthographic = true;
                camera.orthographicSize = 5.5f;
                cameraObject.transform.position = new Vector3(3.5f, 10f, 2.5f);
                cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                camera.backgroundColor = new Color(0.06f, 0.08f, 0.11f);
            }

            if (FindFirstObjectByType<Light>() == null)
            {
                var light = new GameObject("Sandbox Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.intensity = 1.1f;
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(14, 14, 850, 96), "Encounter Sandbox — deterministic movement, effects, and direct fire");
            var canResolve = !_isResolving && (_manualPlanningMode ? _draftedActions.Count > 0 && _encounter.Outcome?.IsComplete != true : _encounter.CompletedRounds < DemonstrationRoundCount);
            GUI.enabled = canResolve;
            var resolveLabel = _manualPlanningMode
                ? "Submit drafted order"
                : _encounter.CompletedRounds < DemonstrationRoundCount
                ? $"Resolve round {_encounter.CompletedRounds + 1}"
                : "Demo complete";
            if (GUI.Button(new Rect(24, 46, 140, 26), resolveLabel))
                StartRoundPlayback();
            GUI.enabled = true;
            if (GUI.Button(new Rect(174, 46, 90, 26), "Reset"))
                ResetSandbox();

            if (GUI.Button(new Rect(274, 46, 130, 26), _manualPlanningMode ? "Scripted demo mode" : "Manual order mode"))
            {
                _manualPlanningMode = !_manualPlanningMode;
                ResetSandbox();
            }

            if (_manualPlanningMode)
            {
                GUI.Label(new Rect(414, 46, 430, 20), _planningMessage);
                GUI.Label(new Rect(24, 76, 150, 20), "Selected unit: BLUE");
                if (GUI.Button(new Rect(174, 74, 70, 24), "North")) DraftMove(new GridPosition(0, 1));
                if (GUI.Button(new Rect(249, 74, 70, 24), "South")) DraftMove(new GridPosition(0, -1));
                if (GUI.Button(new Rect(324, 74, 70, 24), "East")) DraftMove(new GridPosition(1, 0));
                if (GUI.Button(new Rect(399, 74, 70, 24), "West")) DraftMove(new GridPosition(-1, 0));
                if (GUI.Button(new Rect(474, 74, 70, 24), "Heal")) DraftHeal();
                if (GUI.Button(new Rect(549, 74, 70, 24), "Attack red")) DraftAttack();
                if (GUI.Button(new Rect(624, 74, 90, 24), "Undo last"))
                    UndoLastDraft();
                if (GUI.Button(new Rect(719, 74, 110, 24), "Clear draft"))
                {
                    _draftedActions.Clear();
                    _planningMessage = "Draft cleared. Build a path or choose a new blue action.";
                }
            }

            var y = 110f;
            foreach (var line in _eventLines.Take(13))
            {
                GUI.Label(new Rect(20, y, 800, 20), line);
                y += 20f;
            }

            foreach (var unit in _scenario.InitialState.Units)
            {
                var screen = Camera.main!.WorldToScreenPoint(_unitViews[unit.Id].transform.position + Vector3.up * 0.65f);
                var label = $"{unit.FactionId.ToUpperInvariant()} {_displayHitPoints[unit.Id]}/{_displayMaxHitPoints[unit.Id]} {_displayActivityStates[unit.Id]}";
                GUI.Label(new Rect(screen.x - 70f, Screen.height - screen.y, 180f, 22f), label);
            }
        }
    }
}
