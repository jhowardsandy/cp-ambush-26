# Rule Changelog

This log records accepted rule/schema changes that may affect replay, scenario, or presentation compatibility. The detailed evidence remains in the linked rulebook chapters, traceability matrix, tests, and golden replays.

## 2026-07-20 — Initial deterministic engine foundation

- Added timeline ordering/validation, cardinal terrain-aware movement, and strict same-tick occupancy policy.
- Added portable scenario JSON, map bounds, terrain, named map areas, objectives, unit archetypes, and faction roster validation.
- Added objective terrain line of sight and present-time faction visibility.
- Added clamped vitality effects, incapacitation, structured outcome events, and direct guaranteed-hit attacks with range/LOS validation.
- Added multi-round encounter state; a valid round carries state forward, while an invalid plan does not advance it.
- Added eliminate-all-opponents objective completion and completed-encounter protection.
- Added golden replays for terrain delay, vitality restoration, and direct fire. Canonical checksums were revised when vitality entered the canonical state checksum.

Compatibility note: replay and scenario data are versioned by simulation/content version fields. No migration layer exists yet; changes that alter accepted event/state behavior require explicit fixture and content-version review.
