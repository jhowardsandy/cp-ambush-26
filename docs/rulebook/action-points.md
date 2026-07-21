# Action Points

## AP-BUD-001: Per-round action-point validation

Status: accepted.

Each unit has a non-negative per-round action-point budget. A submitted plan reserves the sum of its move terrain costs and its named attack/effect profile costs. If the total exceeds the unit budget, the plan is invalid and does not resolve. A later in-resolution failure does not refund its reserved AP in this first slice.

Evidence: `Planned_actions_cannot_exceed_the_unit_action_point_budget`.
