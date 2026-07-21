# Graybox PvE 4v4 Scene

`CP Ambush/Create or Open Graybox PvE 4v4` creates `Assets/Scenes/GrayboxPve4v4.unity`. It is the first player-operated multi-unit presentation surface. The scene constructs an original 10×8 graybox map with four Blue and four Red units—two Riflemen and two Combat Medics per side—a blocking central structure, and two concealment tiles.

The player selects one of four Blue units and queues a short left-to-right action sequence: cardinal movement, named Red attack, or Medic self-heal. Repeated movement inputs extend one explicit path; later actions start after that path completes. A persistent four-row **Round plan** panel shows every sequence, AP spent versus budget, and an Undo control for the selected unit's last action. Units with no order are explicitly shown to wait. Red receives a deterministic `PvePlanner` bundle. `EncounterResolver` is the sole authority for both sides; Unity renders its event log and final checksum.

Riflemen render as capsules and Medics as spheres; labels show role, vitality, and med-kit quantity. A Medic can draft a self-heal, which uses the same skill-gated, consumable core rule as all future healing. The scene intentionally remains narrow: no tile clicking, no player posture/overwatch buttons, no fog, no ally-targeted healing control, no planned-path collision forecasting, and primitive art. It is a graybox validation surface for multi-unit order submission and PvE playback, not the final game UI.

The healing target selector cycles active Blue units independently from the acting Blue selector. Field med kits may target an active friendly unit at Manhattan range 0–1 with direct line of sight; legality is evaluated when the queued heal completes, and an out-of-range/blocked attempt does not consume a kit.

For recording and deterministic visual regression checks, **Auto-play demo** resets the scene then submits up to eight rounds of `PvePlanner` orders for both Blue and Red. It uses the ordinary `EncounterResolver` playback path and pauses early if an objective completes; it does not fabricate movement, combat, or outcomes in Unity.
