# Tactical Strategy Game

An original, deterministic squad-tactics simulation inspired by the design philosophy of classic simultaneous-resolution tactical games. It does not reproduce protected names, art, maps, text, or code.

## Current milestone

Milestone 1 implements a headless, deterministic round timeline. It supports configurable ticks, submitted faction command bundles, `Wait`, `Rotate`, `Move`, `Aim`, and `Attack` timing, ordered domain events, replay serialization, and final-state checksums.

No combat resolution, pathfinding, line of sight, reactions, presentation, or final art is implemented yet.

## Prerequisites

- Unity `6000.3.20f1` (Unity 6.3 LTS), Apple silicon
- A Unity Personal license activated in Unity Hub

## Test

Run Edit Mode tests from Unity's Test Runner, or headlessly:

```sh
/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "$(pwd)" \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode.xml
```

## Architecture

`Game.Core` is plain C# and has no Unity or presentation dependency. Unity-facing and editor assemblies depend inward on the core; tests reference the core directly. See [architecture.md](docs/technical-design/architecture.md) and [timeline-resolution.md](docs/technical-design/timeline-resolution.md).
