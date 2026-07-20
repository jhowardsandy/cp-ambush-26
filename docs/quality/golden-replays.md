# Golden Replays

Golden replays lock accepted scenario behavior. A fixture names its scenario/content/simulation version, fixed commands and seed, ordered event contract, and final-state checksum. A failure is intentional review work: either the engine regressed or an accepted rule/schema changed and the fixture/version/docs must change together.

## GR-MOV-001: Terrain-delay path

- Scenario: `golden-terrain-delay`
- Map: four-by-four grid; tile `(1,0)` costs two movement ticks
- Input: blue unit moves `(0,0)` -> `(1,0)` -> `(2,0)` beginning at tick zero
- Expected entry ticks: `2`, then `3`
- Expected final checksum: `250038CE4E0AAB526AA72283C5CB74F15B8BE237F667F384CDA8FFB4960632E6`
- Automated evidence: `Golden_replay_terrain_delay_has_stable_events_and_checksum`

The checksum was updated on 2026-07-20 when canonical unit vitality was added to simulation state. Movement events and final positions are unchanged.

## GR-EFF-001: Vitality restoration

- Scenario: direct two-unit state; blue begins at 4/10 vitality
- Input: blue applies `golden-aid` with a value of `+3`, beginning at tick one and completing at tick three
- Expected event calculation: `before=4; requested=3; applied=3; after=7`
- Expected final checksum: `E54B162B835B2819E70ECDBF2CD68C41039B759F16B1877504EA17123D5EC191`
- Automated evidence: `Golden_replay_vitality_effect_has_stable_event_sequence_and_checksum`
