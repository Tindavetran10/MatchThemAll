# Power-up Editor — Design

**Date:** 2026-11-08 · **Status:** approved in chat, awaiting spec review

## Goal

A Power-up Manager editor window in which the developer, and buyers of the template, can create, edit, reorder and delete power-ups: icon, tray model, VFX, sound, position in the tray row, unlock level, starting charges, price, and the mechanic. It must look and behave like the other Match Them All managers (Item, Shop, Sound).

## Decisions (from the brainstorm)

| Question | Decision |
|---|---|
| How a user gets a mechanic | Pick a built-in mechanic and tune it, **or** generate a C# effect script template (**New Mechanic…**) that then appears in the picker. |
| What a custom slot shows | Optional 3D **tray model**; the power-up's **icon** is the fallback when none is set. |
| How power-ups reach the tray | **Built from data at runtime.** The editor never edits MainScene. |
| Activation sound | An `AudioClip` per power-up, played on the SFX mixer group, replacing the hard-coded id switch. |
| Slot preview | A live 3D preview in the editor, same setup as the Item Manager dock preview. |

Out of scope: an "Unlocked!" popup or tutorial, and no-code mechanic building blocks.

## 1. Runtime

### Data — `PowerupDataSO` (no field renames)
- Existing fields stay: `id`, `displayName`, `icon`, `order`, `unlockLevel` (0-based), `defaultAmount`, `buyCurrency`, `buyCost`, `effect` (`[SerializeReference]`), `activateVfx`, `endVfx`.
- `uiPrefab` (currently unused) becomes the **slot prefab**. Its serialized name stays `uiPrefab`, and the Inspector label/tooltip reads "Slot Prefab".
- New: `trayModel` (GameObject, optional), `activateSound` (AudioClip, optional), `activateVolume` (0–1, default 1) and `activatePitch` (default 1). The built-in sounds use non-default volume and pitch (e.g. Fan pitch 0.7), so these carry over exactly.

### Slot prefabs
- **`Prefabs/PowerUps/PowerupSlot.prefab`** (shared base): the `Powerup` component, collider, `Slot Background`, `Amount Container` (counter, video icon, lock badge, level label) and a `Model` anchor.
  - With `trayModel` set, the model is instantiated under `Model`.
  - Without it, a SpriteRenderer under `Model` shows `icon`.
- **Built-ins** become their own prefabs, saved from the existing scene spots: `Slot_Vacuum`, `Slot_Fan`, `Slot_Freeze`, `Slot_Spring`.
  - *Planning change:* not variants. Each spot's `Slot Background` and `Amount Container` are already nested prefabs, so the shared parts are already shared.
  - Each carries its own prop and animator (the vacuum includes `SuckPoint`).
  - Their `trayModel` stays empty, because the prop is part of the prefab.
- Every `Powerup` must reference the badge, label and counter under **its own** slot.

### Spawning — `PowerupManager`
- In `Awake`, spawn `database.Ordered` slot prefabs under the `Powerup Zone` (the object carrying `PowerupTrayLayout`), and bind each spawned `Powerup` to its `PowerupDataSO`.
- `PowerupTrayLayout` already lays out every child that has a `Powerup`, in hierarchy order, so spawning in `order` gives the row order.
- The four hand-made spots are removed from MainScene after migration (see §3).

### Effects and context
- `PowerupContext.Source` is the tapped slot's prop.
- `ctx.VacuumSuckPosition` is filled from the tapped slot (its `SuckPoint`), so `PowerupManager` drops its `vacuum` and `vacuumSuckPosition` fields.
- *Planning change:* `FanOrigin` and `FreezeMuzzle` are screen-centre VFX points directly under `Powerup Zone`, not inside the slots, so they stay scene objects referenced by `PowerupManager`. Moving them into slots would shift the VFX whenever the tray rescales.
- `PowerupContext` keeps its fields, so effect code is unchanged.
- The vacuum's wait for its suck animation is driven by the vacuum's slot/effect, not by `so.id == "vacuum"` in `PowerupManager`.
- Sound: `PowerupManager` plays `so.activateSound` at `activateVolume`/`activatePitch` through a new `SoundManager` method that plays a clip on the SFX group.
  - The four built-ins get their current library clip, volume and pitch.
  - `SoundEvent.PowerupVacuum`, `PowerupSpring`, `PowerupFan` and `PowerupFreeze` (explicit values 15–18) are removed from the enum, from the Sound Manager descriptions and from its default-clip table (`SoundManagerWindow.cs`), and their entries are removed from `SoundLibrary`.

### Unchanged
Tap input (`InputManager` → `PowerupClickedEvent`), charges (`SaveManager.UsePowerupCharge` and the rest), the 0-charge `PowerupOfferPanel`, locks (`IsLockedAt`, badge shows `unlockLevel + 1`), and save data keyed by `id`.

## 2. Editor window — `PowerupManagerWindow`

Menu: `Match Them All ▸ Power-up Manager`. It replaces the minimal version added on 2026-11-08 (same file and class).

**Look.** Same palette, toolbar, card styles and resizable list/detail divider as `SoundManagerWindow` and `ShopEditorWindow`, through `EditorWindowStyles`.

**Toolbar.** **+ New Power-up** · **Duplicate** · **Delete** · **New Mechanic…**

**Left list.**
- Cards in tray order: icon, name, "from start" / "level N".
- ▲▼ buttons rewrite `order` as 0..n-1.
- Warning marker for: empty or duplicate `id`, no mechanic, no icon.

**Detail cards (right).**
1. **Identity:** display name, id (unique, lowercase, no spaces; changing it warns that existing players lose that power-up's saved charges), icon.
2. **Tray:**
   - Tray model, and the slot prefab (defaults to `PowerupSlot`).
   - A **live 3D slot preview** in a `PreviewRenderUtility` with orbit, pan, zoom and Reset Camera, matching `ItemManagerWindow`.
3. **Progression:** "Unlocks at level", shown 1-based and stored as `unlockLevel = shown - 1`; starting charges.
4. **Economy:** buy currency and price.
5. **Mechanic:**
   - A dropdown of every concrete `PowerupEffect` subclass (`TypeCache.GetTypesDerivedFrom<PowerupEffect>()`).
   - Changing the mechanic confirms first, then replaces `effect` with a new instance.
   - The effect's fields are drawn below.
6. **Effects:** activate VFX, end VFX, and activate sound with a ▶ preview button.

**Actions.**
- **New Power-up:**
  - Asks for a name and derives the id from it.
  - Creates `Resources/Powerups/Powerup_<id>.asset` and adds it to `PowerupDatabase`.
  - Defaults: `order` last, `uiPrefab = PowerupSlot`, `defaultAmount = 3`, `unlockLevel = 0`, no mechanic (flagged).
- **Duplicate:** copies the asset with a new unique id and appends it to the end of the row.
- **Delete:** confirms, removes the asset from the database and moves it to the trash (`AssetDatabase.MoveAssetToTrash`). Saved charges for that id are left in the save and are harmless.
- **New Mechanic…:**
  - Asks for a name and writes `Scripts/Runtime/PowerUps/Custom/<Name>Effect.cs`.
  - The file is a `[Serializable]` `PowerupEffect` subclass with `CanActivate`/`Activate`, one example tunable field, and commented examples using `ctx` (items, goals, tray, timer, `Source`).
  - It appears in the Mechanic dropdown after compiling.

All edits go through `SerializedObject` with Undo. Asset creation and deletion use `AssetDatabase`.

## 3. Migration and testing

**Migration** (in order, each step checked before the next):
1. Build `PowerupSlot` from the shared parts (`Slot Background` and `Power Up/Amount Container` are already prefabs).
2. Make the four variants from the existing scene spots, moving the props, animators and points in. Then audit that each `Powerup`'s badge, label, counter and video-icon references sit under its own slot.
3. Snapshot every slot's world position, scale and badge/counter state in MainScene. Remove the scene spots and enable spawning. Compare the spawned tray against the snapshot (numeric diff, not by eye).
4. Copy the four power-up clips, with their volume and pitch, from the Sound Library onto the data assets. Remove the four `SoundEvent`s.

**Play-mode checks** (real tap path through `PowerupClickedEvent`, with the level not OUTOFTIME):
- Each built-in with charges: the effect happens (vacuum collects goal items, fan pushes items, freeze stops the timer, spring launches items), its sound plays, and one charge is used.
- At 0 charges: `PowerupOfferPanel` opens and nothing is spent silently.
- Lock: with a high unlock level, the slot shows the badge and "Lv N" and can't be tapped. Then restore the level.
- End to end:
  - Create a test power-up in the window.
  - Generate a test mechanic (adds 10 s to the timer).
  - Check it spawns at the chosen position in the row and that tapping it adds the time.
  - Delete the power-up and the script.
- Save data recorded before and restored after, then `SaveManager.Flush()`.
- `PlayerSettings.runInBackground` restored if a test sets it.

**Finish:**
- Clean console.
- Player-scripts compile (`PlayerBuildInterface.CompilePlayerScripts`).
- `graphify update .`
- `docs/decisions/` note: the tray is data-driven.
- CLAUDE.md pitfalls: how to add a power-up, slot references must stay inside their own slot, and power-up sounds live on the data asset.

## Risks

- **Built-in refactor.** The four effects are working code. Mitigation: migrate one power-up at a time, and test each one in Play mode before the next.
- **Hand-edited scene.** The tray spots are removed from MainScene. Mitigation: the snapshot/diff in step 3, and no scene builders.
- **Removed serialized fields and enum values** (`PowerupManager` scene points, four `SoundEvent`s). Mitigation: grep every use first. `SoundEvent` values are explicit (15–18 here), so removing them doesn't renumber the rest; `SoundLibrary` entries keyed 15–18 are removed with them.
