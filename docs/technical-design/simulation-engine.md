# Simulation Engine

`TimelineResolver` validates a `SimulationRequest`, resolves command bundles, emits domain events, and returns a final `GameState` checksum. `SeededRandom` is the only simulation-owned random provider; Milestone 1 does not consume random values yet.
