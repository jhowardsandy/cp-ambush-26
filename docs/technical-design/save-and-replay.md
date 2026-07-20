# Save and Replay

`ReplayRecord` serializes the simulation inputs and output: initial state, command bundles, round configuration, seed, simulation/content versions, ordered events, diagnostics, and final checksum. The checksum is SHA-256 of a canonical unit-state ordering.
