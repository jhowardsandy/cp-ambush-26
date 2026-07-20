# Core Game Loop

1. Inspect the known tactical situation at the start of a round.
2. Select friendly units and submit one or more explicitly timed orders for each unit; examples will include move then heal or move then attack when those actions are legal.
3. Submit one timed command bundle per faction.
4. Validate commands.
5. Resolve every valid action over the same discrete round timeline.
6. Play back the resulting ordered events.
7. Carry the resolved battlefield state forward as the planning state for the next round, then inspect outcomes and issue new orders.

`EncounterState` now owns the authoritative state at the round boundary. A valid resolved round advances it; an invalid submission leaves it unchanged. The Unity sandbox still uses authored example orders, so a player-facing unit-selection/order-authoring interface remains future work.

For PvE, a deterministic enemy planner will submit the opposing faction's bundle at the same round boundary. For future PvP, each human player will submit one faction's bundle before the shared resolver runs. Both modes use this same encounter state and command contract.
