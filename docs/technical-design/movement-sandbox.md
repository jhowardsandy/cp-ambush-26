# Movement Sandbox

`Assets/Scenes/MovementSandbox.unity` is the first human-testable presentation slice. `MovementSandboxController` creates a setting-neutral eight-by-six scenario, submits two timed move paths and one named vitality effect through `ScenarioFactory` and `TimelineResolver`, and renders the emitted event log/checksum.

The sandbox is not alternate game logic: it invokes `Game.Core` directly. Use the on-screen **Resolve round** and **Reset** buttons (or `Space` and `R`) to control deterministic tick-by-tick playback. A gold tile costs two movement ticks and a red tile is impassable; the planned blue path crosses the gold tile, making the data-driven delay visible. After completing that move, blue applies a named `field-med-kit` effect, visibly changing from 8/10 to 10/10 vitality from the resolver's structured `EffectApplied` event. It intentionally uses primitive tiles and unit capsules; order authoring and combat remain future work.

`CP Ambush/Create or Open Movement Sandbox` regenerates/opens the scene from Unity's editor menu if needed.
