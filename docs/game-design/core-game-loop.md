# Core Game Loop

1. Inspect the known tactical situation.
2. Submit one timed command bundle per faction.
3. Validate commands.
4. Resolve every valid action over the same discrete round timeline.
5. Play back the resulting ordered events.
6. Inspect outcomes and plan the next round.

Milestone 1 implements steps 3 and 4 plus replay data. It does not yet provide a player-facing planner or playback UI.
