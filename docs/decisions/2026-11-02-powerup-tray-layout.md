# Power-up tray: per-slot cards + automatic layout

**Date:** 2026-11-02

## Context
The tray under the power-ups was one baked texture (`PowerupTray.png`) with exactly four slots, and each
spot had a hand-placed X position. Adding a fifth power-up meant re-baking the art and moving every spot.

## Decision
- Each power-up spot under `Main Camera/Powerup Zone` carries its own `Slot Background` card: an instance of
  `Prefabs/UI/Power Up/Slot Background.prefab` (sprite `Art/UI/Sprites/Power Up/Powerup Slot.png`, 200 PPU,
  sorting order -10). Restyle all cards by editing the prefab.
- `PowerupTrayLayout` (on `Powerup Zone`, runs in edit mode and at runtime) treats every active child that
  contains a `Powerup` as a slot, in hierarchy order (left → right). Slots keep 1.15 spacing while they fit the
  camera width; beyond that, every slot shrinks evenly around the row centre (5 → 80 %, 6 → 67 %).
- Children without a `Powerup` are not touched — `FanOrigin` / `FreezeMuzzle` moved up to `Powerup Zone` so
  they stay put when slots scale.
- The baked tray quad, `M_PowerupTray`, the bake code and the old `ItemSpotLayout` on the zone were removed.

## Adding a power-up
Duplicate an existing spot under `Powerup Zone`, swap its model, set the `Powerup` id. The row re-flows itself.
