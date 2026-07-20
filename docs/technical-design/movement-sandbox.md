# Movement Sandbox

`Assets/Scenes/MovementSandbox.unity` is the first human-testable presentation slice. `MovementSandboxController` creates a setting-neutral eight-by-six encounter, submits authored example orders through `EncounterResolver`, and renders the emitted event log/checksum.

The sandbox is not alternate game logic: it invokes `Game.Core` directly. Use the on-screen **Resolve round** and **Reset** buttons (or `Space` and `R`) to control deterministic tick-by-tick playback. Round 1 resolves the two factions' planned movement; the gold tile costs two movement ticks and the planned blue path crosses it. After that result becomes the next planning state, Round 2 resolves a blue `field-med-kit` order and a red wait order, visibly changing blue from 8/10 to 10/10 vitality from the resolver's structured `EffectApplied` event. Round 3 resolves a named direct-fire attack; Unity animates a yellow projectile only after receiving the core `AttackResolved` event, then renders red at 0/10 and incapacitated. It intentionally uses primitive tiles and unit capsules; player-authored order selection, accuracy, and richer combat remain future work.

`CP Ambush/Create or Open Movement Sandbox` regenerates/opens the scene from Unity's editor menu if needed.
