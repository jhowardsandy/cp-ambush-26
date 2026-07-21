# Graybox PvE 4v4 Scene

`CP Ambush/Create or Open Graybox PvE 4v4` creates `Assets/Scenes/GrayboxPve4v4.unity`. It is the first player-operated multi-unit presentation surface. The scene constructs an original 10×8 graybox map with four Blue and four Red units, a blocking central structure, and two concealment tiles.

The player selects one of four Blue units, drafts one cardinal move or a named Red target attack for each selected unit, and submits the resulting Blue command bundle. Red receives a deterministic `PvePlanner` bundle. `EncounterResolver` is the sole authority for both sides; Unity renders its event log and final checksum.

The scene intentionally remains narrow: one action per Blue unit, no tile clicking, no multi-step player paths, no player posture/overwatch buttons, no fog, no inventory/capability gating, and primitive art. It is a graybox validation surface for multi-unit order submission and PvE playback, not the final game UI.
