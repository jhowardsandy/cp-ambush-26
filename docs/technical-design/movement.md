# Movement

Milestone 2 implements deterministic grid movement. A `Move` contains a non-empty path of cardinal-adjacent grid positions. The action begins at `StartTick`; the unit enters path tile `n` at `StartTick + n + 1`. Movement consumes one tick per tile, so `DurationTicks` must equal path length. A legacy single `Destination` is treated as a one-tile path for replay compatibility.

## Accepted conflict policy

At each movement tick, the resolver snapshots occupancy before applying any movement. A movement step fails, and its remaining path is canceled, if its destination:

1. is requested by more than one unit on that tick; or
2. was occupied in the start-of-tick snapshot.

No priority is applied to a contested destination. Units may not enter a tile merely because its occupant is leaving that tick. Therefore swaps, crossing on the same edge, and same-tick movement chains are disallowed in this milestone. Facing does not change automatically while moving.

The resolver emits `UnitExitedTile` and `UnitEnteredTile` with source/destination positions for each successful step. A failed step emits `ActionFailed` with the reason; that action cannot complete.

## Deferred

Terrain costs, diagonal movement, doors, stance/suppression speed modifiers, movement interruption/resume, path replanning, reservation look-ahead, and richer conflict policies remain deferred.
