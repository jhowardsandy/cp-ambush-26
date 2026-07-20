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
        private const int DemonstrationRoundCount = 2;

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
                }));

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
            _encounter = new EncounterState(
                new EncounterDefinition(_scenario.Id, _scenario.Map, _scenario.ContentVersion),
                _scenario.InitialState);
            _eventLines.Clear();
            _eventLines.Add("Encounter reset. Round 1 is ready: movement orders.");
            RenderState(_scenario.InitialState);
        }

        private void StartRoundPlayback()
        {
            if (!_isResolving && _encounter.CompletedRounds < DemonstrationRoundCount)
                StartCoroutine(ResolveAndPlayback());
        }

        private IEnumerator ResolveAndPlayback()
        {
            _isResolving = true;
            var roundNumber = _encounter.CompletedRounds + 1;
            var stateBeforeRound = _encounter.CurrentState;
            var round = EncounterResolver.ResolveRound(
                _encounter,
                CommandsForRound(roundNumber),
                new RoundConfiguration(10),
                (uint)(20260720 + roundNumber),
                effects: new[] { new EffectDefinition("field-med-kit", 5) });

            _result = round.Resolution;
            _encounter = round.NextState;
            _eventLines.Clear();
            _eventLines.Add($"Resolving encounter round {roundNumber}.");
            RenderState(stateBeforeRound);

            foreach (var tickEvents in _result.Events.GroupBy(@event => @event.Tick).OrderBy(group => group.Key))
            {
                foreach (var @event in tickEvents)
                {
                    if (@event.Type == DomainEventType.UnitEnteredTile && @event.UnitId.HasValue && @event.ToPosition != null)
                        _unitViews[@event.UnitId.Value].transform.position = new Vector3(@event.ToPosition.X, 0.3f, @event.ToPosition.Y);

                    if (@event.Type == DomainEventType.EffectApplied && @event.UnitId.HasValue && @event.HitPointsAfter.HasValue && @event.ActivityStateAfter.HasValue)
                    {
                        _displayHitPoints[@event.UnitId.Value] = @event.HitPointsAfter.Value;
                        _displayActivityStates[@event.UnitId.Value] = @event.ActivityStateAfter.Value;
                    }

                    _eventLines.Add($"t{@event.Tick:00} {@event.Type} {@event.FactionId} {@event.Detail}");
                }

                yield return new WaitForSeconds(0.55f);
            }

            _eventLines.Add($"Checksum: {_result.FinalStateChecksum}");
            _eventLines.Add(_encounter.CompletedRounds < DemonstrationRoundCount
                ? $"Round {roundNumber} complete. Resolve round {_encounter.CompletedRounds + 1} for fresh orders."
                : "Two-round encounter demo complete. Reset to begin again.");
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

            var blueHeal = new TacticalAction(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), blueUnit, TacticalActionType.ApplyEffect, 0, 1,
                TargetUnitId: blueUnit, EffectId: "field-med-kit");
            var redWait = new TacticalAction(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), redUnit, TacticalActionType.Wait, 0, 1);
            return new[] { new CommandBundle("blue", new[] { blueHeal }), new CommandBundle("red", new[] { redWait }) };
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
            }
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
            GUI.Box(new Rect(14, 14, 590, 68), "Encounter Sandbox — deterministic multi-round playback");
            var canResolve = !_isResolving && _encounter.CompletedRounds < DemonstrationRoundCount;
            GUI.enabled = canResolve;
            var resolveLabel = _encounter.CompletedRounds < DemonstrationRoundCount
                ? $"Resolve round {_encounter.CompletedRounds + 1}"
                : "Demo complete";
            if (GUI.Button(new Rect(24, 46, 140, 26), resolveLabel))
                StartRoundPlayback();
            if (GUI.Button(new Rect(174, 46, 90, 26), "Reset"))
                ResetSandbox();
            GUI.enabled = true;

            var y = 90f;
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
