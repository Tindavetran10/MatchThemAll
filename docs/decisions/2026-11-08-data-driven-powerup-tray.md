# Data-driven power-up tray (2026-11-08)

**Decision:** The power-up tray is spawned at runtime from `PowerupDatabase` under `Powerup Zone`. Each `PowerupDataSO.uiPrefab` is a slot prefab, and slots are sorted by `order`. MainScene no longer contains power-up spots.

**Why:** Template buyers create, reorder and unlock power-ups in `Match Them All ▸ Power-up Manager` without editing scenes. Row order and unlocks are pure data.

**Consequences:**
- **Built-in slots** are `Prefabs/PowerUps/Slots/Slot_{Freeze,Fan,Spring,Vacuum}.prefab`, saved from the old scene spots. Each carries its own prop, animator and points. The shared `Slot Background` / `Amount Container` stay nested prefabs.
- **Custom power-ups** use `PowerupSlot.prefab`. It shows `trayModel` under its `Model` anchor, or the `icon` when there is no model.
- **Scene VFX points:** `FanOrigin` and `FreezeMuzzle` are screen-centre points, so they stay under `Powerup Zone` as `PowerupManager` scene references. The vacuum's `SuckPoint` is found inside its slot.
- **Activation sounds** live on each `PowerupDataSO` (clip, volume, pitch). `SoundEvent` values 15–18 are retired and must not be reused.
- **Mechanics** are `PowerupEffect` subclasses. **New Mechanic…** writes one into `Scripts/Runtime/PowerUps/Custom`. An effect that must sync with its slot animation overrides `WaitsForSlotAnimation`.
- **Spec and plan:** `docs/superpowers/specs/2026-11-08-powerup-editor-design.md` and `docs/superpowers/plans/2026-11-08-powerup-editor.md`.
