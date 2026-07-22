# Encounter Session

## Reinforcement schedules

`ReinforcementSchedule` is setting-neutral encounter content: a named unit definition spawns after a named completed round at the first free, passable tile in a named map area. A schedule may name a control area and opposing faction that disables its spawn when captured. Spawn and disable events are authoritative and replayable; occupied areas defer rather than stack units.

`EncounterState` is the core planning-boundary model. It contains a stable `EncounterDefinition` (ID, map, content version), the current authoritative `GameState`, and the completed-round count.

`EncounterResolver.ResolveRound` creates that round's scenario from the current state, delegates outcome calculation to `TimelineResolver`, and returns both the raw `SimulationResult` and the next `EncounterState`. Only a validation-valid simulation advances state and the round count. The API accepts the command bundles, round configuration, seed, optional simulation version, and effect catalog explicitly so the next round remains reproducible.

The session model is mode-neutral. A local player UI, a deterministic PvE planner, a hot-seat opponent, or a later networked PvP coordinator all produce command bundles at this boundary; none receives authority to alter the final state directly.
