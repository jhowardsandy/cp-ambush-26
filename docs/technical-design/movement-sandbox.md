# Movement Sandbox

`Assets/Scenes/MovementSandbox.unity` is the first human-testable presentation slice. `MovementSandboxController` creates a setting-neutral eight-by-six scenario, submits two timed move paths through `ScenarioFactory` and `TimelineResolver`, and renders the final state with the emitted event log/checksum.

The sandbox is not alternate game logic: it invokes `Game.Core` directly. Use `Space` to resolve and `R` to reset. It intentionally uses primitive tiles and unit capsules; playback animation, order authoring, terrain, and combat remain future work.

`CP Ambush/Create or Open Movement Sandbox` regenerates/opens the scene from Unity's editor menu if needed.
