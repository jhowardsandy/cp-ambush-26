# Posture

## PST-TRN-001: Adjacent posture transitions

Status: accepted.

Each active unit has one posture: standing, crouched, or prone. `ChangePosture` costs one AP and completes after one tick. A change must move exactly one step in the sequence standing ↔ crouched ↔ prone; a standing unit cannot become prone in one action, and a prone unit cannot stand in one action.

The resolver emits `PostureChanged` before `ActionCompleted`, including the resulting posture. Posture is part of the final-state checksum and replay state.

## PST-CON-001: Posture concealment

Status: accepted.

Target posture contributes deterministic concealment to the existing observation calculation: standing adds 0, crouched adds 1, and prone adds 2. It stacks with authored target-tile concealment: `effective observation range = max(0, observer vision − terrain concealment − posture concealment)`. The target is observable only at or within that range. Because direct attacks and PvE targeting already use the shared observation contract, this one posture rule automatically affects them without a second combat or AI path.

Posture does not yet alter movement, armor, direct-hit accuracy, cover, overwatch eligibility, or reaction priority. Those remain separate rules. `PostureChanged` records the posture's concealment value so the player can inspect the new state.

Evidence: `Adjacent_posture_change_updates_state_and_emits_event`, `Posture_change_cannot_skip_an_intermediate_posture`, and `Crouched_and_prone_postures_add_to_target_tile_concealment_for_observation`.
