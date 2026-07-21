# Deterministic PvE Planner

`PvePlanner.Plan` is the first conventional enemy-planning foundation. It reads only the supplied authoritative state, map, faction ID, and named attack profile, then produces an ordinary `CommandBundle` plus one explanation per active unit. It never resolves simulation outcomes.

For each unit in stable ID order, it chooses the nearest active opposing unit by Manhattan distance, faction ID, then unit ID. It attacks if the named profile is currently legal and affordable; otherwise it reserves a legal, unoccupied, distance-reducing adjacent tile using the stable preference north, east, south, west; otherwise it waits. Planned destinations are reserved during planning to avoid same-destination AI commands.

This first slice does not use hidden knowledge, faction visibility snapshots, multi-action plans, posture, cover/concealment, overwatch, equipment loadouts, per-unit weapon selection, or randomness. Those are explicit later planner inputs and policies.

Evidence: `Pve_planner_is_deterministic_and_uses_the_same_attack_command_contract` and `Pve_planner_moves_toward_nearest_target_with_an_explanation`.
