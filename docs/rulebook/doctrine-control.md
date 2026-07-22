# Doctrine Control

## DOC-CTL-001: Player-selected, inspectable auto-orders

Status: accepted first doctrine-control slice.

Player statement: Before a round, a player may choose a tactical doctrine for an individual Blue unit and ask the game to draft that unit’s next order. The player can inspect the generated action and reason, then submit it, undo it, clear it, or replace it with a manual order.

Resolution: A doctrine invokes the deterministic planner using the same current state, map, unit catalog, equipment, AP, visibility, and action rules as PvE. It produces at most one ordinary tactical action for the selected unit. The generated action appears in the normal Round plan with its rationale. It is not privileged automation: it validates, resolves, replays, and can fail exactly like a manual action.

| Doctrine | First-slice behavior |
| --- | --- |
| Aggressive | Use the conventional nearest-observable-target plan. |
| Hold objective | If already on an authored hold tile and no legal shot exists, wait rather than chase. |
| Keep range | Apply ranged minimum-range positioning; a crowded Marksman retreats to a legal tile that increases distance, preferring cover. |
| Support / follow | Heal a legal injured ally first; otherwise move toward the named friendly follow target, or the nearest active friendly unit when none is named. |

Manual override: issuing a manual movement, attack, heal, or overwatch order for a unit that has a doctrine-generated order clears that generated order and its rationale before drafting the manual order. `Undo` and `Clear` can also remove it. Selecting a doctrine alone does not spend AP or submit an order.

Knowledge boundary: doctrine selection cannot identify hidden enemies, grant extra actions, bypass action-point/inventory/skill limits, or change a resolved result. If the named follow target is inactive or unavailable, the support planner falls back to ordinary legal planning.

Tests/fixtures: `Pve_support_follow_doctrine_moves_toward_the_selected_friendly_unit`, `Pve_hold_doctrine_keeps_its_selected_unit_on_the_objective`, `Pve_marksman_repositions_when_a_target_is_inside_its_minimum_range`, and the existing planner determinism tests.
