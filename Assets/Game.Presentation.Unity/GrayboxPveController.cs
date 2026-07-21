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
        private readonly Dictionary<Guid, TacticalAction> _blueOrders = new();
        private readonly List<string> _lines = new();
        private ScenarioDefinition _scenario = null!;
        private EncounterState _encounter = null!;
        private Guid _selectedBlue;
        private Guid _selectedRed;
        private bool _resolving;
        private string _message = string.Empty;
        private static readonly AttackProfile Rifle = new("graybox-rifle", 1, 3, 5);

        private void Start()
        {
            BuildScenario();
            BuildViews();
            ResetEncounter();
        }

        private void BuildScenario()
        {
            var units = new[]
            {
                Unit("blue", 1, 1, 1), Unit("blue", 2, 3, 1), Unit("blue", 3, 5, 1), Unit("blue", 4, 7, 1),
                Unit("red", 1, 1, 6), Unit("red", 2, 3, 6), Unit("red", 3, 5, 6), Unit("red", 4, 7, 6)
            };
            _scenario = new ScenarioDefinition("graybox-pve-4v4-01", new GridMapDefinition("graybox-pve-grid", 10, 8, new[]
            {
                new TerrainCellDefinition(new GridPosition(4, 3), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(5, 3), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(4, 4), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(5, 4), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(2, 3), MovementTicks: 2, ConcealmentValue: 2),
                new TerrainCellDefinition(new GridPosition(7, 4), MovementTicks: 2, ConcealmentValue: 2)
            }), new GameState(units), Objectives: new[] { new ObjectiveDefinition("eliminate-red", ObjectiveType.IncapacitateAllOpposingUnits, "blue") });
        }

        private static UnitState Unit(string faction, int number, int x, int y) =>
            new(Guid.Parse($"{(faction == "blue" ? "1" : "2")}0000000-0000-0000-0000-00000000000{number}"), faction, new GridPosition(x, y), faction == "blue" ? Facing.North : Facing.South, UnitActivityState.Active, ActionPointBudget: 6);

        private void BuildViews()
        {
            for (var x = 0; x < _scenario.Map.Width; x++)
            for (var y = 0; y < _scenario.Map.Height; y++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.transform.SetParent(transform, false); tile.transform.position = new Vector3(x, 0, y); tile.transform.localScale = new Vector3(.92f, .08f, .92f);
                var terrain = _scenario.Map.CellAt(new GridPosition(x, y));
                tile.GetComponent<Renderer>().material.color = !terrain.IsPassable ? new Color(.33f, .27f, .22f) : terrain.ConcealmentValue > 0 ? new Color(.22f, .38f, .22f) : new Color(.22f, .27f, .32f);
            }
            foreach (var unit in _scenario.InitialState.Units)
            {
                var view = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                view.transform.SetParent(transform, false); view.transform.localScale = Vector3.one * .42f;
                view.GetComponent<Renderer>().material.color = unit.FactionId == "blue" ? new Color(.2f, .62f, 1f) : new Color(1f, .3f, .25f);
                _views.Add(unit.Id, view);
            }
            var cameraObject = new GameObject("Graybox PvE Camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 5.6f;
            cameraObject.transform.position = new Vector3(4.5f, 10, 3.5f); cameraObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            new GameObject("Graybox Light").AddComponent<Light>().type = LightType.Directional;
        }

        private void ResetEncounter()
        {
            StopAllCoroutines(); _resolving = false; _blueOrders.Clear(); _lines.Clear();
            _encounter = new EncounterState(new EncounterDefinition(_scenario.Id, _scenario.Map, _scenario.ContentVersion, _scenario.Objectives), _scenario.InitialState);
            _selectedBlue = _scenario.InitialState.Units.First(unit => unit.FactionId == "blue").Id;
            _selectedRed = _scenario.InitialState.Units.First(unit => unit.FactionId == "red").Id;
            _message = "Draft one order for any Blue unit, then submit the round. Red uses deterministic PvE.";
            Render(_encounter.CurrentState);
        }

        private void DraftMove(GridPosition delta)
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            var destination = new GridPosition(unit.Position.X + delta.X, unit.Position.Y + delta.Y);
            if (!unit.ActivityState.Equals(UnitActivityState.Active) || !_scenario.Map.Contains(destination) || !_scenario.Map.CellAt(destination).IsPassable || _encounter.CurrentState.Units.Any(other => other.Position == destination))
            { _message = "That move is outside the map, blocked, or occupied."; return; }
            _blueOrders[unit.Id] = new TacticalAction(unit.Id, unit.Id, TacticalActionType.Move, 0, _scenario.Map.CellAt(destination).MovementTicks, Path: new[] { destination });
            _message = $"Drafted move for Blue {UnitNumber(unit.Id)} to ({destination.X},{destination.Y}).";
        }

        private void DraftAttack()
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            var target = _encounter.CurrentState.FindUnit(_selectedRed)!;
            if (unit.ActivityState != UnitActivityState.Active || target.ActivityState != UnitActivityState.Active) { _message = "Both units must be active."; return; }
            _blueOrders[unit.Id] = new TacticalAction(unit.Id, unit.Id, TacticalActionType.Attack, 0, 1, TargetUnitId: target.Id, AttackProfileId: Rifle.Id);
            _message = $"Drafted speculative attack: Blue {UnitNumber(unit.Id)} targets Red {UnitNumber(target.Id)}.";
        }

        private void Submit()
        {
            if (!_resolving && _blueOrders.Count > 0 && _encounter.Outcome?.IsComplete != true) StartCoroutine(Resolve());
        }

        private IEnumerator Resolve()
        {
            _resolving = true; var before = _encounter.CurrentState; var red = PvePlanner.Plan("red", before, _scenario.Map, Rifle);
            var result = EncounterResolver.ResolveRound(_encounter, new[] { new CommandBundle("blue", _blueOrders.Values.ToArray()), red.Commands }, new RoundConfiguration(10), (uint)(20260721 + _encounter.CompletedRounds), attackProfiles: new[] { Rifle });
            _blueOrders.Clear(); _lines.Clear(); Render(before);
            foreach (var group in result.Resolution.Events.GroupBy(@event => @event.Tick).OrderBy(group => group.Key))
            {
                foreach (var @event in group)
                {
                    if (@event.Type == DomainEventType.UnitEnteredTile && @event.UnitId.HasValue && @event.ToPosition is not null) _views[@event.UnitId.Value].transform.position = new Vector3(@event.ToPosition.X, .3f, @event.ToPosition.Y);
                    _lines.Add($"t{@event.Tick:00} {@event.Type} {@event.FactionId} {@event.Detail}");
                }
                yield return new WaitForSeconds(.35f);
            }
            _encounter = result.NextState; Render(_encounter.CurrentState);
            _lines.Add($"Checksum: {result.Resolution.FinalStateChecksum}");
            _message = _encounter.Outcome?.IsComplete == true ? _encounter.Outcome.Detail : $"Round {_encounter.CompletedRounds} complete. Draft next Blue orders.";
            _resolving = false;
        }

        private void Render(GameState state)
        {
            foreach (var unit in state.Units)
            {
                _views[unit.Id].transform.position = new Vector3(unit.Position.X, .3f, unit.Position.Y);
                _views[unit.Id].GetComponent<Renderer>().material.color = unit.ActivityState == UnitActivityState.Incapacitated ? Color.gray : unit.FactionId == "blue" ? new Color(.2f, .62f, 1f) : new Color(1f, .3f, .25f);
            }
        }

        private static int UnitNumber(Guid id) => id.ToString("N")[31] - '0';

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 1000, 118), "Graybox 4v4 PvE — player Blue vs deterministic Red");
            if (GUI.Button(new Rect(24, 44, 120, 26), "Submit round")) Submit();
            if (GUI.Button(new Rect(154, 44, 80, 26), "Reset")) ResetEncounter();
            GUI.Label(new Rect(250, 44, 740, 22), _message);
            var blue = _encounter.CurrentState.Units.Where(unit => unit.FactionId == "blue").OrderBy(unit => unit.Id).ToArray();
            for (var i = 0; i < blue.Length; i++) if (GUI.Button(new Rect(24 + i * 66, 76, 60, 24), $"Blue {i + 1}")) _selectedBlue = blue[i].Id;
            if (GUI.Button(new Rect(300, 76, 45, 24), "N")) DraftMove(new GridPosition(0, 1));
            if (GUI.Button(new Rect(350, 76, 45, 24), "S")) DraftMove(new GridPosition(0, -1));
            if (GUI.Button(new Rect(400, 76, 45, 24), "E")) DraftMove(new GridPosition(1, 0));
            if (GUI.Button(new Rect(450, 76, 45, 24), "W")) DraftMove(new GridPosition(-1, 0));
            if (GUI.Button(new Rect(505, 76, 100, 24), "Attack red")) DraftAttack();
            if (GUI.Button(new Rect(615, 76, 100, 24), "Next red")) _selectedRed = NextRed();
            if (GUI.Button(new Rect(725, 76, 100, 24), "Clear blue")) _blueOrders.Remove(_selectedBlue);
            var y = 138f; foreach (var line in _lines.Take(15)) { GUI.Label(new Rect(20, y, 990, 20), line); y += 19; }
            foreach (var unit in _encounter.CurrentState.Units)
            {
                var screen = Camera.main!.WorldToScreenPoint(_views[unit.Id].transform.position + Vector3.up * .65f);
                GUI.Label(new Rect(screen.x - 52, Screen.height - screen.y, 130, 20), $"{unit.FactionId.ToUpperInvariant()} {UnitNumber(unit.Id)} {unit.HitPoints}/10");
            }
        }

        private Guid NextRed()
        {
            var red = _encounter.CurrentState.Units.Where(unit => unit.FactionId == "red" && unit.ActivityState == UnitActivityState.Active).OrderBy(unit => unit.Id).ToArray();
            var index = Array.FindIndex(red, unit => unit.Id == _selectedRed); return red.Length == 0 ? _selectedRed : red[(index + 1 + red.Length) % red.Length].Id;
        }
    }
}
