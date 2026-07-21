# Graybox PvE Acceptance

## GBA-PVE-4V4-001: Four-round repeatable 4v4 encounter

The `iron-timeline-squad-skirmish-01` portable 16×12 fixture is executed as a four-round, four-versus-four graybox encounter. Both factions receive ordinary deterministic PvE command bundles each round, the encounter carries state and unit/faction catalogs forward, and every resolution must be valid.

The acceptance test runs the full encounter twice and requires identical per-round checksums, decision counts, four valid rounds, eight unit decisions per round, and observable event activity. This is an engine-level integration proof, not yet the player-operated Unity scene; player-side multi-unit authoring is the next presentation slice.

Evidence: `Graybox_four_v_four_pve_encounter_is_repeatable_across_multiple_rounds`.

## Manual mixed-roster scene check

In the player-operated scene, each side must visibly contain two capsule Riflemen and two sphere Combat Medics. A damaged Blue Medic can draft `Medic heal self`; successful resolution restores vitality and decrements the displayed med-kit count by one. This is presentation validation of core rules already covered by the deterministic inventory tests.

## GBA-SEQ-001: Player order sequence

The player can queue a cardinal move path then a named Red attack for the same Blue unit. The plan panel shows both actions and their AP total before submission; after resolution, the direct attack is evaluated from the moved position at its completion tick. Undo removes only the selected unit's latest action.

Evidence: `A_unit_can_move_then_resolve_a_named_attack_from_its_new_position`.
