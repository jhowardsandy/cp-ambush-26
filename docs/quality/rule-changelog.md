# Rule Changelog

This log records accepted rule/schema changes that may affect replay, scenario, or presentation compatibility. The detailed evidence remains in the linked rulebook chapters, traceability matrix, tests, and golden replays.

## 2026-07-22 — Seeded weapon accuracy

- Added `AttackProfile.AccuracyPercent` (0–100 inclusive; default 100) and provisional starter-content values of 75% for service rifle and 85% for marksman rifle.
- Legal attacks and overwatch reactions now draw exactly one seeded 1–100 roll in timeline order. Hits use the existing cover-mitigated vitality calculation; legal misses complete with zero damage; illegal attacks draw no roll and remain failures.
- Added accuracy, roll, and hit/miss result to attack/reaction event details and Unity `MISS` feedback, plus zero-accuracy, replay-stability, and invalid-percentage tests.

Compatibility note: this changes event detail and combat outcome behavior for profiles that specify accuracy below 100. Replay fixtures should retain their original content version/seed; profile callers that omit accuracy preserve the prior guaranteed-hit behavior.

## 2026-07-22 — Ammunition accounting

- Added optional `AttackProfile` ammunition item and per-attempt quantity fields, preserving profiles that omit ammunition.
- Direct attacks and triggered overwatch reactions reserve and consume ammunition independently of weapon entitlement and hit/miss; untriggered overwatch and illegal completion-time attacks do not spend it.
- Added starter loadouts, event detail, Unity ammo labels, and legal-miss/over-reservation/overwatch-consumption regression coverage.

Compatibility note: starter military content now requires its declared ammunition. Scenario and replay content using those profiles must carry matching ammo inventory; standalone profiles that omit ammunition remain compatible.

## 2026-07-22 — Target armor mitigation

- Added non-negative authoritative `ArmorValue` to unit state and unit definitions; it is included in the deterministic state checksum and shown for armored graybox units.
- A hit now calculates `max(1, base damage − target-tile cover − target armor)` and logs the armor component. Misses record armor but apply no damage.
- Added armor-plus-cover regression coverage and updated golden checksum fixtures for the authoritative-state schema change. Starter Rifleman armor is provisionally 1.

Compatibility note: all replay/state checksums change because armor is part of canonical state. Existing scenario data without armor remains valid at armor 0.

## 2026-07-22 — Posture concealment

- Added deterministic posture concealment to observation: standing 0, crouched 1, prone 2; this stacks with target-tile concealment.
- Direct targeting and PvE target selection inherit the effect through the shared observation rule. The graybox can queue legal adjacent posture transitions and labels unit posture.
- Added posture-concealment calculation coverage and expanded posture event detail.

Compatibility note: no state-schema or checksum change; replay outcomes can change when a request resolves against a crouched/prone target because posture now affects visibility.

## 2026-07-20 — Initial deterministic engine foundation

- Added timeline ordering/validation, cardinal terrain-aware movement, and strict same-tick occupancy policy.
- Added portable scenario JSON, map bounds, terrain, named map areas, objectives, unit archetypes, and faction roster validation.
- Added objective terrain line of sight and present-time faction visibility.
- Added clamped vitality effects, incapacitation, structured outcome events, and direct guaranteed-hit attacks with range/LOS validation.
- Added multi-round encounter state; a valid round carries state forward, while an invalid plan does not advance it.
- Added eliminate-all-opponents objective completion and completed-encounter protection.
- Added golden replays for terrain delay, vitality restoration, and direct fire. Canonical checksums were revised when vitality entered the canonical state checksum.

Compatibility note: replay and scenario data are versioned by simulation/content version fields. No migration layer exists yet; changes that alter accepted event/state behavior require explicit fixture and content-version review.
