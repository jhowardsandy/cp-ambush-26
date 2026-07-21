# Terrain Protection

## TRN-PRT-001: Portable cover and concealment data

Status: accepted.

Each terrain cell may declare non-negative `CoverValue` and `ConcealmentValue`. These are setting-neutral authored data: brush, ruins, trenches, walls, magical foliage, and equivalent terrain may use them without special-case engine code. `TerrainProtectionRules.At` returns the two values for a tile, and scenario JSON/replay inputs preserve them.

For direct and overwatch attacks, target-tile `CoverValue` now reduces fixed profile damage by that value, with a minimum of one damage. `ConcealmentValue` remains authored data only until an observation/perception rule is accepted; it does not silently alter targeting or visibility.

Evidence: `Terrain_protection_values_are_portable_and_reject_negative_content`.
