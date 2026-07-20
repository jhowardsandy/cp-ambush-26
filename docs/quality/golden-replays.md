# Golden Replays

Golden replays lock accepted scenario behavior. A fixture names its scenario/content/simulation version, fixed commands and seed, ordered event contract, and final-state checksum. A failure is intentional review work: either the engine regressed or an accepted rule/schema changed and the fixture/version/docs must change together.

## GR-MOV-001: Terrain-delay path

- Scenario: `golden-terrain-delay`
- Map: four-by-four grid; tile `(1,0)` costs two movement ticks
- Input: blue unit moves `(0,0)` -> `(1,0)` -> `(2,0)` beginning at tick zero
- Expected entry ticks: `2`, then `3`
- Expected final checksum: `8A641DD03771B4B48B8223499FE694A48199F879F1C69B26F5F1B1167E98907E`
- Automated evidence: `Golden_replay_terrain_delay_has_stable_events_and_checksum`
