# Posture

## PST-TRN-001: Adjacent posture transitions

Status: accepted.

Each active unit has one posture: standing, crouched, or prone. `ChangePosture` costs one AP and completes after one tick. A change must move exactly one step in the sequence standing ↔ crouched ↔ prone; a standing unit cannot become prone in one action, and a prone unit cannot stand in one action.

The resolver emits `PostureChanged` before `ActionCompleted`, including the resulting posture. Posture is part of the final-state checksum and replay state. This first slice does not yet modify movement, attacks, visibility, cover, concealment, or reactions.

Evidence: `Adjacent_posture_change_updates_state_and_emits_event` and `Posture_change_cannot_skip_an_intermediate_posture`.
