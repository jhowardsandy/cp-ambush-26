# ADR 0005: Reusable Scenario Definitions

Status: Accepted.

## Decision

Maps and tactical encounters are represented as setting-neutral, serializable scenario data in `Game.Core`. A scenario owns stable IDs, map dimensions, initial state, and content version; the Unity scene is only one possible presentation of it.

## Consequences

- Replays can identify the scenario/content version that produced them.
- Any number of historical-inspired, fantasy, or future settings can supply maps and unit content without changing the resolver.
- Scenario-only rules such as terrain, doors, objectives, and deployment must remain data-driven extensions rather than Unity object behavior.
