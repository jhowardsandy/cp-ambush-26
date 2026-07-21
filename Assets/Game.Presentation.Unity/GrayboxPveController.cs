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
        private bool _autoPlaying;
        private string _message = string.Empty;
        private static readonly AttackProfile Rifle = StarterMilitaryContent.ServiceRifle;
        private static readonly EffectDefinition FieldMedKit = StarterMilitaryContent.FieldMedKit;

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
                Unit("blue", 1, 1, 1, StarterMilitaryContent.Rifleman), Unit("blue", 2, 3, 1, StarterMilitaryContent.CombatMedic),
                Unit("blue", 3, 5, 1, StarterMilitaryContent.Rifleman), Unit("blue", 4, 7, 1, StarterMilitaryContent.CombatMedic),
                Unit("red", 1, 1, 6, StarterMilitaryContent.Rifleman), Unit("red", 2, 3, 6, StarterMilitaryContent.CombatMedic),
                Unit("red", 3, 5, 6, StarterMilitaryContent.Rifleman), Unit("red", 4, 7, 6, StarterMilitaryContent.CombatMedic)
            };
            _scenario = new ScenarioDefinition("graybox-pve-4v4-01", new GridMapDefinition("graybox-pve-grid", 10, 8, new[]
            {
                new TerrainCellDefinition(new GridPosition(4, 3), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(5, 3), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(4, 4), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(5, 4), IsPassable: false, BlocksLineOfSight: true, CoverValue: 3),
                new TerrainCellDefinition(new GridPosition(2, 3), MovementTicks: 2, ConcealmentValue: 2),
                new TerrainCellDefinition(new GridPosition(7, 4), MovementTicks: 2, ConcealmentValue: 2)
            }), new GameState(units), Objectives: new[] { new ObjectiveDefinition("eliminate-red", ObjectiveType.IncapacitateAllOpposingUnits, "blue") },
                UnitDefinitions: new[] { StarterMilitaryContent.Rifleman, StarterMilitaryContent.CombatMedic },
                FactionDefinitions: new[]
                {
                    new FactionDefinition("blue", new[] { StarterMilitaryContent.Rifleman.Id, StarterMilitaryContent.CombatMedic.Id }),
                    new FactionDefinition("red", new[] { StarterMilitaryContent.Rifleman.Id, StarterMilitaryContent.CombatMedic.Id })
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
                tile.GetComponent<Renderer>().material.color = !terrain.IsPassable ? new Color(.33f, .27f, .22f) : terrain.ConcealmentValue > 0 ? new Color(.22f, .38f, .22f) : new Color(.22f, .27f, .32f);
            }
            foreach (var unit in _scenario.InitialState.Units)
            {
                var view = GameObject.CreatePrimitive(unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                view.transform.SetParent(transform, false); view.transform.localScale = Vector3.one * .42f;
                view.GetComponent<Renderer>().material.color = UnitColor(unit);
                _views.Add(unit.Id, view);
            }
            var cameraObject = new GameObject("Graybox PvE Camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 5.6f;
            cameraObject.transform.position = new Vector3(4.5f, 10, 3.5f); cameraObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            new GameObject("Graybox Light").AddComponent<Light>().type = LightType.Directional;
        }

        private void ResetEncounter()
        {
            StopAllCoroutines(); _resolving = false; _autoPlaying = false; _blueOrders.Clear(); _lines.Clear();
            _encounter = new EncounterState(new EncounterDefinition(_scenario.Id, _scenario.Map, _scenario.ContentVersion, _scenario.Objectives, _scenario.UnitDefinitions, _scenario.FactionDefinitions), _scenario.InitialState);
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

        private void DraftSelfHeal()
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
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
            _blueOrders[unit.Id] = new TacticalAction(unit.Id, unit.Id, TacticalActionType.ApplyEffect, 0, 1, TargetUnitId: unit.Id, EffectId: FieldMedKit.Id);
            _message = $"Drafted self-heal for Blue {UnitNumber(unit.Id)}. One med kit will be spent on successful resolution.";
        }

        private void Submit()
        {
            if (_resolving || _encounter.Outcome?.IsComplete == true) return;
            if (_blueOrders.Count == 0)
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
            const int maximumDemoRounds = 8;
            for (var round = 0; round < maximumDemoRounds && _encounter.Outcome?.IsComplete != true; round++)
            {
                _message = $"Auto-play demo: planning round {_encounter.CompletedRounds + 1}.";
                var blue = PvePlanner.Plan("blue", _encounter.CurrentState, _scenario.Map, Rifle);
                yield return StartCoroutine(Resolve(blue.Commands));
                if (_encounter.Outcome?.IsComplete != true)
                    yield return new WaitForSeconds(.65f);
            }
            _autoPlaying = false;
            if (_encounter.Outcome?.IsComplete != true)
                _message = $"Auto-play demo paused after {maximumDemoRounds} rounds. Reset to replay.";
        }

        private IEnumerator Resolve()
        {
            yield return Resolve(new CommandBundle("blue", _blueOrders.Values.ToArray()));
        }

        private IEnumerator Resolve(CommandBundle blueCommands)
        {
            _resolving = true; var before = _encounter.CurrentState; var red = PvePlanner.Plan("red", before, _scenario.Map, Rifle);
            var result = EncounterResolver.ResolveRound(_encounter, new[] { blueCommands, red.Commands }, new RoundConfiguration(10), (uint)(20260721 + _encounter.CompletedRounds), effects: new[] { FieldMedKit }, attackProfiles: new[] { Rifle });
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
            _message = _encounter.Outcome?.IsComplete == true ? _encounter.Outcome.Detail : _autoPlaying ? $"Auto-play completed round {_encounter.CompletedRounds}." : $"Round {_encounter.CompletedRounds} complete. Draft next Blue orders.";
            _resolving = false;
        }

        private void Render(GameState state)
        {
            foreach (var unit in state.Units)
            {
                _views[unit.Id].transform.position = new Vector3(unit.Position.X, .3f, unit.Position.Y);
                _views[unit.Id].GetComponent<Renderer>().material.color = unit.ActivityState == UnitActivityState.Incapacitated ? Color.gray : UnitColor(unit);
            }
        }

        private static int UnitNumber(Guid id) => id.ToString("N")[31] - '0';
        private static string RoleName(UnitState unit) => unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? "MEDIC" : "RIFLE";
        private static Color UnitColor(UnitState unit) => unit.FactionId == "blue"
            ? unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? new Color(.25f, .9f, .65f) : new Color(.2f, .62f, 1f)
            : unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? new Color(1f, .55f, .25f) : new Color(1f, .3f, .25f);

        private string PlannedOrderDescription(UnitState unit)
        {
            if (!_blueOrders.TryGetValue(unit.Id, out var action)) return "No order — waits";
            if (action.Type == TacticalActionType.Move && action.Path is { Count: > 0 })
            {
                var destination = action.Path[^1];
                return $"Move to ({destination.X},{destination.Y})";
            }
            if (action.Type == TacticalActionType.Attack && action.TargetUnitId.HasValue)
                return $"Attack Red {UnitNumber(action.TargetUnitId.Value)} — checked at resolution";
            if (action.Type == TacticalActionType.ApplyEffect)
                return "Self-heal — med kit spent at resolution";
            return action.Type.ToString();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 1000, 118), "Graybox 4v4 PvE — player Blue vs deterministic Red");
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
            if (GUI.Button(new Rect(300, 76, 45, 24), "N")) DraftMove(new GridPosition(0, 1));
            if (GUI.Button(new Rect(350, 76, 45, 24), "S")) DraftMove(new GridPosition(0, -1));
            if (GUI.Button(new Rect(400, 76, 45, 24), "E")) DraftMove(new GridPosition(1, 0));
            if (GUI.Button(new Rect(450, 76, 45, 24), "W")) DraftMove(new GridPosition(-1, 0));
            if (GUI.Button(new Rect(505, 76, 100, 24), "Attack red")) DraftAttack();
            if (GUI.Button(new Rect(615, 76, 100, 24), "Next red")) _selectedRed = NextRed();
            if (GUI.Button(new Rect(725, 76, 100, 24), "Clear blue")) _blueOrders.Remove(_selectedBlue);
            if (GUI.Button(new Rect(835, 76, 100, 24), "Medic heal self")) DraftSelfHeal();
            GUI.Box(new Rect(12, 138, 1000, 112), $"Round plan — {_blueOrders.Count}/4 Blue orders queued. Choosing another order for a Blue unit replaces its current order.");
            for (var i = 0; i < blue.Length; i++)
            {
                var selected = blue[i].Id == _selectedBlue ? "> " : "  ";
                GUI.Label(new Rect(28, 164 + i * 20, 970, 20), $"{selected}Blue {i + 1}: {PlannedOrderDescription(blue[i])}");
            }
            var y = 258f; foreach (var line in _lines.Take(15)) { GUI.Label(new Rect(20, y, 990, 20), line); y += 19; }
            foreach (var unit in _encounter.CurrentState.Units)
            {
                var screen = Camera.main!.WorldToScreenPoint(_views[unit.Id].transform.position + Vector3.up * .65f);
                var medKits = InventoryRules.QuantityOf(unit, "med-kit");
                var inventory = medKits > 0 || unit.UnitDefinitionId == StarterMilitaryContent.CombatMedic.Id ? $" kit:{medKits}" : string.Empty;
                GUI.Label(new Rect(screen.x - 70, Screen.height - screen.y, 180, 20), $"{unit.FactionId.ToUpperInvariant()} {UnitNumber(unit.Id)} {RoleName(unit)} {unit.HitPoints}/{unit.MaxHitPoints}{inventory}");
            }
        }

        private Guid NextRed()
        {
            var red = _encounter.CurrentState.Units.Where(unit => unit.FactionId == "red" && unit.ActivityState == UnitActivityState.Active).OrderBy(unit => unit.Id).ToArray();
            var index = Array.FindIndex(red, unit => unit.Id == _selectedRed); return red.Length == 0 ? _selectedRed : red[(index + 1 + red.Length) % red.Length].Id;
        }
    }
}
