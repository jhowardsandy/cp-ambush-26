# Graybox PvE 4v4 Scene

`CP Ambush/Create or Open Graybox PvE 4v4` creates `Assets/Scenes/GrayboxPve4v4.unity`. It is the first player-operated multi-unit presentation surface. The scene constructs an original 10×8 graybox map with four Blue and four Red units—two Riflemen and two Combat Medics per side—a blocking central structure, and two concealment tiles.

The player selects one of four Blue units, drafts one cardinal move or a named Red target attack for each selected unit, and submits the resulting Blue command bundle. A persistent four-row **Round plan** panel states the queued order for every Blue unit; units with no order are explicitly shown to wait, and replacing an existing unit order is explicit. Red receives a deterministic `PvePlanner` bundle. `EncounterResolver` is the sole authority for both sides; Unity renders its event log and final checksum.

Riflemen render as capsules and Medics as spheres; labels show role, vitality, and med-kit quantity. A Medic can draft a self-heal, which uses the same skill-gated, consumable core rule as all future healing. The scene intentionally remains narrow: one action per Blue unit, no tile clicking, no multi-step player paths, no player posture/overwatch buttons, no fog, no ally-targeted healing control, and primitive art. It is a graybox validation surface for multi-unit order submission and PvE playback, not the final game UI.

For recording and deterministic visual regression checks, **Auto-play demo** resets the scene then submits up to eight rounds of `PvePlanner` orders for both Blue and Red. It uses the ordinary `EncounterResolver` playback path and pauses early if an objective completes; it does not fabricate movement, combat, or outcomes in Unity.
