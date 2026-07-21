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
        private ScenarioDefinition _scenario = null!;
        private EncounterState _encounter = null!;
        private Guid _selectedBlue;
        private Guid _selectedRed;
        private Guid _selectedHealTarget;
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
            _selectedHealTarget = _selectedBlue;
            _message = "Draft one order for any Blue unit, then submit the round. Red uses deterministic PvE.";
            Render(_encounter.CurrentState);
        }

        private void DraftMove(GridPosition delta)
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            var priorActions = PlannedActions(unit.Id);
            var priorMove = priorActions.LastOrDefault(action => action.Type == TacticalActionType.Move);
            var origin = priorMove?.Path is { Count: > 0 } ? priorMove.Path[^1] : unit.Position;
            var destination = new GridPosition(origin.X + delta.X, origin.Y + delta.Y);
            if (!unit.ActivityState.Equals(UnitActivityState.Active) || !_scenario.Map.Contains(destination) || !_scenario.Map.CellAt(destination).IsPassable || _encounter.CurrentState.Units.Any(other => other.Position == destination))
            { _message = "That move is outside the map, blocked, or occupied."; return; }
            if (priorActions.LastOrDefault()?.Type == TacticalActionType.Move && priorMove is not null)
            {
                var path = priorMove.Path!.Append(destination).ToArray();
                priorActions[^1] = priorMove with { Path = path, DurationTicks = MovementRules.DurationFor(priorMove with { Path = path }, _scenario.Map) };
            }
            else
                QueueAction(unit, TacticalActionType.Move, _scenario.Map.CellAt(destination).MovementTicks, path: new[] { destination });
            _message = $"Queued move for Blue {UnitNumber(unit.Id)} to ({destination.X},{destination.Y}).";
        }

        private void DraftAttack()
        {
            var unit = _encounter.CurrentState.FindUnit(_selectedBlue)!;
            var target = _encounter.CurrentState.FindUnit(_selectedRed)!;
            if (unit.ActivityState != UnitActivityState.Active || target.ActivityState != UnitActivityState.Active) { _message = "Both units must be active."; return; }
            QueueAction(unit, TacticalActionType.Attack, 1, targetUnitId: target.Id, attackProfileId: Rifle.Id);
            _message = $"Queued speculative attack: Blue {UnitNumber(unit.Id)} targets Red {UnitNumber(target.Id)}.";
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
            QueueAction(unit, TacticalActionType.ApplyEffect, 1, targetUnitId: target.Id, effectId: FieldMedKit.Id);
            _message = $"Queued heal: Blue {UnitNumber(unit.Id)} targets Blue {UnitNumber(target.Id)}. Range and sight are checked at resolution.";
        }

        private List<TacticalAction> PlannedActions(Guid unitId)
        {
            if (_blueOrders.TryGetValue(unitId, out var actions)) return actions;
            actions = new List<TacticalAction>();
            _blueOrders.Add(unitId, actions);
            return actions;
        }

        private void QueueAction(UnitState unit, TacticalActionType type, int durationTicks, GridPosition? destination = null, IReadOnlyList<GridPosition>? path = null, Guid? targetUnitId = null, string? effectId = null, string? attackProfileId = null)
        {
            var actions = PlannedActions(unit.Id);
            var startTick = actions.Count == 0 ? 0 : actions[^1].StartTick + actions[^1].DurationTicks;
            if (startTick + durationTicks > 10)
            {
                _message = "That action would extend beyond this 10-tick round. Undo or clear an earlier order.";
                return;
            }
            actions.Add(new TacticalAction(PlannedActionId(unit.Id, actions.Count + 1), unit.Id, type, startTick, durationTicks, destination, Path: path, TargetUnitId: targetUnitId, EffectId: effectId, AttackProfileId: attackProfileId));
        }

        private static Guid PlannedActionId(Guid unitId, int sequence)
        {
            var canonical = unitId.ToString("N");
            return Guid.Parse(canonical[..28] + sequence.ToString("x1") + canonical[29..]);
        }

        private int PlannedActionCount => _blueOrders.Values.Sum(actions => actions.Count);

        private void UndoLastBlueAction()
        {
            if (!_blueOrders.TryGetValue(_selectedBlue, out var actions) || actions.Count == 0)
            {
                _message = $"Blue {UnitNumber(_selectedBlue)} has no queued action to undo.";
                return;
            }
            actions.RemoveAt(actions.Count - 1);
            if (actions.Count == 0) _blueOrders.Remove(_selectedBlue);
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
            yield return Resolve(new CommandBundle("blue", _blueOrders.Values.SelectMany(actions => actions).ToArray()));
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
            if (!_blueOrders.TryGetValue(unit.Id, out var actions) || actions.Count == 0) return "No order — waits";
            var descriptions = actions.Select(action => ActionDescription(action)).ToArray();
            var spent = actions.Sum(action => ActionPointRules.CostFor(action, _scenario.Map, new[] { FieldMedKit }, new[] { Rifle }));
            return $"{String.Join(" → ", descriptions)}  [{spent}/{unit.ActionPointBudget} AP]";
        }

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
            if (GUI.Button(new Rect(725, 76, 60, 24), "Undo")) UndoLastBlueAction();
            if (GUI.Button(new Rect(795, 76, 65, 24), "Clear")) _blueOrders.Remove(_selectedBlue);
            if (GUI.Button(new Rect(300, 104, 115, 24), "Next heal target")) _selectedHealTarget = NextBlueHealTarget();
            if (GUI.Button(new Rect(425, 104, 115, 24), "Medic heal target")) DraftHealTarget();
            GUI.Label(new Rect(550, 106, 300, 20), $"Heal target: Blue {UnitNumber(_selectedHealTarget)}");
            GUI.Box(new Rect(12, 138, 1000, 112), $"Round plan — {PlannedActionCount} actions across {_blueOrders.Count} Blue units. Orders resolve left-to-right; Undo removes the selected unit's last action.");
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

        private Guid NextBlueHealTarget()
        {
            var blue = _encounter.CurrentState.Units.Where(unit => unit.FactionId == "blue" && unit.ActivityState == UnitActivityState.Active).OrderBy(unit => unit.Id).ToArray();
            var index = Array.FindIndex(blue, unit => unit.Id == _selectedHealTarget); return blue.Length == 0 ? _selectedHealTarget : blue[(index + 1 + blue.Length) % blue.Length].Id;
        }
    }
}
