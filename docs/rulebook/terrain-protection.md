# Terrain Protection

## TRN-PRT-001: Portable cover and concealment data

Status: accepted.

Each terrain cell may declare non-negative `CoverValue` and `ConcealmentValue`. These are setting-neutral authored data: brush, ruins, trenches, walls, magical foliage, and equivalent terrain may use them without special-case engine code. `TerrainProtectionRules.At` returns the two values for a tile, and scenario JSON/replay inputs preserve them.

This foundation intentionally does not yet change damage, accuracy, targeting, or visibility. Those effects require separate accepted calculation rules that combine posture, terrain, facing, and attack/observation context.

Evidence: `Terrain_protection_values_are_portable_and_reject_negative_content`.
