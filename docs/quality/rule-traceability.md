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
| SCN-MAP-001 | accepted | `technical-design/content-schema.md` | Scenario map dimension and starting-position validation test | Scenario definition survives replay serialization. |
| SCN-MAP-002 | accepted | `technical-design/content-schema.md` | Movement-out-of-bounds scenario test | Add map-boundary golden fixture with the fixture harness. |
| VIS-* | proposed | Future visibility chapter | Not implemented | Not implemented |
| ATK-* | proposed | Future combat chapter | Not implemented | Not implemented |
| UPG-* | deferred | Future progression chapter | Not implemented | Not implemented |
