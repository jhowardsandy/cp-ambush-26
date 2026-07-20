# Architecture

Dependencies flow inward:

```text
Game.Presentation.Unity -> Game.Core
Game.Editor             -> Game.Core
Game.Tests              -> Game.Core
Game.Core               -X-> Unity / presentation
```

`Game.Core` is a no-engine-reference assembly. It must not reference scenes, GameObjects, MonoBehaviours, transforms, animation, Unity physics, device input, frame time, wall-clock time, rendering state, or network timing.
