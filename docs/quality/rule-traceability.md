# Rule Traceability Matrix

| Rule ID | Status | Specification | Automated evidence | Replay evidence |
| --- | --- | --- | --- | --- |
| TML-ORD-001 | accepted | `technical-design/timeline-resolution.md` | `Orders_same_tick_by_faction_then_unit_then_action_id` | Add first golden replay when fixture harness lands. |
| TML-PHS-001 | accepted | `technical-design/timeline-resolution.md` | `Starts_precede_completions_on_the_same_tick` | Add first golden replay when fixture harness lands. |
| TML-VAL-001 | accepted | `technical-design/timeline-resolution.md` | Invalid timing, round-overrun, and incapacitated-unit tests | Add invalid-command fixtures when validation report format stabilizes. |
| MOV-PTH-001 | accepted | `rulebook/movement-and-occupancy.md` | Cardinal path timing, diagonal rejection, and duration-mismatch tests | Add named golden movement fixtures with the fixture harness. |
| MOV-RES-001 | accepted | `rulebook/movement-and-occupancy.md` | Vacated-tile occupancy test | Add named golden movement fixtures with the fixture harness. |
| MOV-RES-002 | accepted | `rulebook/movement-and-occupancy.md` | Contested destination test | Add named golden movement fixtures with the fixture harness. |
| MOV-RES-003 | accepted | `rulebook/movement-and-occupancy.md` | Swap rejection test | Add named golden movement fixtures with the fixture harness. |
| MOV-TRN-001 | accepted | `rulebook/movement-and-occupancy.md` | Terrain timing and impassable-path tests | `GR-MOV-001` terrain-delay golden replay. |
| TRN-PRT-001 | accepted | `rulebook/terrain-protection.md` | Portable cover/concealment and invalid-content test | Scenario JSON round-trip coverage. |
| RXN-OW-001 | accepted | `rulebook/overwatch.md` | One-shot movement-triggered reaction test | Add an overwatch replay fixture after scenario fixture extraction. |
| SCN-MAP-001 | accepted | `technical-design/content-schema.md` | Scenario map dimension and starting-position validation test | Scenario definition survives replay serialization. |
| SCN-MAP-002 | accepted | `technical-design/content-schema.md` | Movement-out-of-bounds scenario test | Add map-boundary golden fixture with the fixture harness. |
| SCN-MAP-003 | accepted | `technical-design/content-schema.md` | Named map-area JSON round-trip and invalid-area validation test | Add an objective scenario replay when capture/search rules are accepted. |
| SCN-UNIT-001 | accepted | `technical-design/content-schema.md` | Unit-definition creation, JSON content round-trip, and invalid-reference validation tests | Add a unit-catalog scenario replay when weapon/effect capability binding is accepted. |
| SCN-FAC-001 | accepted | `technical-design/content-schema.md` | Faction roster legality and unknown-faction validation test | Add faction-content scenario coverage when AI planning uses faction rosters. |
| INV-SKL-001 | accepted | `rulebook/inventory-and-skills.md` | Gated medical action success; missing-skill and missing-item validation | Add a mixed-roster replay after presentation integration. |
| INV-SKL-002 | accepted | `rulebook/inventory-and-skills.md` | Deterministic med-kit consumption and over-subscription rejection | Add consumable action replay after presentation integration. |
| VIS-LOS-001 | accepted | `rulebook/visibility.md` | Blocking and symmetry line-of-sight tests | Add visibility golden fixtures after faction knowledge is introduced. |
| VIS-RNG-001 | accepted | `rulebook/visibility.md` | Range boundary, opaque blocker, active observer, and friendly-visibility tests | Add a visibility golden replay after snapshots are scheduled into round resolution. |
| EFF-VIT-001 | accepted | `rulebook/effects-and-vitality.md` | Vitality clamp, incapacity, calculation-event, invalid-content, and replay-serialization tests | `GR-EFF-001` vitality-restoration golden replay. |
| ENC-LOOP-001 | accepted | `rulebook/encounter-rounds.md` | Valid carry-forward and invalid-plan no-advance tests | Add a two-round golden replay after fixture extraction supports encounter records. |
| ATK-DIR-001 | accepted | `rulebook/direct-attacks.md` | Legal direct-fire, moving-target into/out-of-range completion, blocked-resolution, invalid-profile, and friendly-target tests | `GR-ATK-001` guaranteed direct-fire golden replay. |
| OBJ-ELM-001 | accepted | `rulebook/objectives.md` | Objective completion, incomplete state, completed-encounter protection, and scenario JSON tests | Add a completed-encounter golden replay after encounter fixtures are extracted. |
| VIS-* | proposed | Future visibility chapter | Not implemented | Not implemented |
| ATK-* | proposed | Future combat chapter | Not implemented | Not implemented |
| UPG-* | deferred | Future progression chapter | Not implemented | Not implemented |
