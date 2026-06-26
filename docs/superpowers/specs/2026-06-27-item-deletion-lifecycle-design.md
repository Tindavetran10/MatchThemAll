# Item Deletion Lifecycle & Icon Propagation — Design

- **Date:** 2026-06-27
- **Status:** Approved (brainstorming) — pending user spec review
- **Owner:** Tindavetran10
- **Scope:** `Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs` + one new editor file

## Background & problem

All item tooling lives in `LevelEditorWindow.cs` (the **Template Editor** window). Today the only way to remove an item is **soft-delete** (`SoftDeleteItem`, ~L1300), which:

- moves the prefab → `Assets/Match Them All/_START_HERE/Items/Trash/`,
- moves its icon → `Assets/Match Them All/Sprites/Icons/Trash/` (only if a file exists at the name-convention path),
- removes the item's entries from every `LevelDataSO.itemData` list (recording them for undo),
- records a `DeletedItemRecord` in `SessionState` so `RestoreFromTrash` can reverse it.

`RestoreFromTrash` (~L1384) reverses both moves and re-inserts the level entries.

**The problem:** when an item prefab is deleted *manually* (outside the editor), nothing scrubs its references. The only thing that points at an item prefab is `LevelDataSO.itemData[].itemPrefab` (a direct `Item` reference, `ItemLevelData.cs:9`). A manual delete turns those into "Missing" refs, and at runtime `ItemPlacer` (`SpawnItem(entry.itemPrefab)`, no null guard) and `GoalManager` (dereferences `itemPrefab.Icon`/`.ItemNameKey`, no guard) throw `NullReferenceException` on level start — so the level won't spawn and goal UI breaks.

Secondary issues found during exploration:
- **Icon not propagated:** the generated icon (`Item.icon`) is shown in the Prefab Library grid but the **Configured Items** table (`DrawItemsSection`, ~L805) and **Trash** grid (`DrawTrashLibrarySection`, ~L1265) use `AssetPreview.GetAssetPreview(...)` (the generic prefab thumbnail) instead of `Item.Icon`.
- **Icon restore is fragile:** `RestoreFromTrash` re-derives the icon path by name (`icon_{prefab.name}.png`) and ignores the `DeletedItemRecord`'s captured `originalIconPath`/`trashIconPath`, so icons for legacy/spaced-name items (e.g. "Blue Potion") aren't restored correctly.

## Goals

1. **Keep soft-delete** (Trash move + restore) exactly as it behaves today.
2. **Add a true permanent (hard) delete** that removes the prefab, its icon (when safe), and **all** `LevelDataSO` references — leaving no dangling refs and no runtime errors.
3. **Use the generated item icon on every item surface** in the editor.
4. **Add a validator** that finds and repairs item references that are *already* broken (from past manual deletes).

## Non-goals

- No `EItemName` enum pruning (orphan values are harmless — `Clock`/`Coin` already have no prefab).
- No `ItemLibrary`/registry ScriptableObject.
- No Addressables changes (item prefabs are not addressable; only `LevelDataSO` is).
- No renaming legacy prefabs / no mass icon re-generation.

## Decisions (from brainstorming)

| Decision | Choice |
|---|---|
| Hard-delete UX | **Both** — soft-delete ✕ on live cards + permanent-delete in Trash + optional permanent-delete on live cards (right-click menu) |
| Icon scope | **All surfaces** — Configured Items table, Trash grid, and any other item card |
| Enum on delete | **Leave it** (orphaned, safe) |
| Repair existing broken refs | **Yes** — add a validator/sweep |
| Architecture | **A — shared `ItemReferenceOps` helper** |

## Architecture

### New file: `Scripts/Editor/ItemReferenceOps.cs`

A static, editor-only utility — the single source of truth for finding/removing item references. Soft-delete, hard-delete, and the validator all call it.

```csharp
namespace MatchThemAll.EditorTools
{
    internal static class ItemReferenceOps
    {
        // Load every LevelDataSO asset in the project.
        static List<LevelDataSO> FindAllLevels();

        // Every (level, index) where itemData[index].itemPrefab == item.
        static List<(LevelDataSO level, int index)> FindReferencingLevels(Item item);

        // Remove those entries; mark each level dirty; register Undo when requested.
        // If `capture` != null, appends (level, index, entry) for each removal (soft-delete uses this for restore). Returns count removed.
        static int RemoveFromLevels(Item item, bool registerUndo, List<(LevelDataSO level, int index, ItemLevelData entry)> capture = null);

        // Every (level, index) where itemPrefab is null / "Missing" — for the validator.
        static List<(LevelDataSO level, int index, string label)> FindBrokenReferences();

        // Remove broken entries; mark dirty; register Undo. Returns count removed.
        static int RemoveBrokenReferences(bool registerUndo);

        // True only if the item's icon asset lives under Sprites/Icons/ AND no other item prefab references it.
        static bool IsIconSafeToDelete(Item item);
    }
}
```

Level enumeration reuses the existing approach (`AssetDatabase.FindAssets("t:LevelDataSO")`, as in `LevelEditorWindow.cs:147`). Reference matching reuses the existing predicate (`i.itemPrefab == item`, as in `LevelEditorWindow.cs:1308/1341`).

### Changes to `LevelEditorWindow.cs`

1. **Refactor** `SoftDeleteItem` and `RestoreFromTrash` to route level work through `ItemReferenceOps` (behavior-preserving — just moves the loop out).
2. **Bugfix (bundled):** `RestoreFromTrash` uses the `DeletedItemRecord`'s captured `originalIconPath`/`trashIconPath` instead of re-deriving by name — restores icons correctly for legacy/spaced-name items. (No change to delete's intent; delete already captures these paths.)
3. **New `HardDeleteItem(Item item, bool isInTrash)`** — the permanent path.
4. **New `DrawItemIcon(Item item, float size)`** — draws `Item.Icon.texture`, falling back to `AssetPreview`, then a glyph. Replaces every `AssetPreview.GetAssetPreview(...)` used for item display.
5. **New validator section** — "Check item references" button → results list → "Remove broken entries".

### UI placement

- **Live item cards:** keep the ✕ (soft-delete → Trash). Add a **right-click context menu**: *Move to Trash* / *Delete permanently…*. The permanent-from-library path stays optional and out of the way of routine edits.
- **Trash cards:** keep the ⟲ (restore). Add a red **⌫ permanent-delete** button with a strong confirm.

## Detailed behavior

### Permanent delete — `HardDeleteItem(Item item, bool isInTrash)`

1. **Confirm dialog** (irreversible): *"Permanently delete '<name>'? This removes the prefab, its icon (if unused), and <N> level reference(s). This cannot be undone."* Cancel → abort.
2. Resolve the prefab's **actual current** asset path via `AssetDatabase` (works whether the item is live or already in a Trash folder).
3. `ItemReferenceOps.RemoveFromLevels(item, registerUndo: false)` — scrubs every level reference (the thing manual delete misses). No Undo on purpose: the whole operation is irreversible, matching the confirm. (Soft-delete remains the reversible path.)
4. If `ItemReferenceOps.IsIconSafeToDelete(item)` → resolve icon path and `AssetDatabase.DeleteAsset(iconPath)`. Otherwise leave the icon and log that it was skipped.
5. `AssetDatabase.DeleteAsset(prefabPath)`.
6. Remove any `DeletedItemRecord` for this item from `SessionState` (so it can't be restored).
7. `AssetDatabase.SaveAssets()` → `LoadAll()` → `Repaint()` → log a summary (`Deleted '<name>' (prefab + icon), removed N level refs`).

`AssetDatabase.DeleteAsset` is intentionally **not** wrapped in an Undo (it's permanent by design). The strong confirm is the safeguard; soft-delete remains the reversible path.

### Soft-delete (refactor + bugfix)

- `SoftDeleteItem` calls `ItemReferenceOps.RemoveFromLevels(item, registerUndo: true, capture: <list>)` to remove the entries **and** capture them (index / isGoal / multiplier / amount + level GUID) into its `DeletedItemRecord` for restore; asset moves and SessionState unchanged.
- `RestoreFromTrash` reads icon paths from the `DeletedItemRecord` (the bugfix) and re-inserts the captured entries using its existing re-insert logic (restore is not a helper method).

### Icon propagation — `DrawItemIcon(Item item, float size)`

```csharp
Texture2D tex = item.Icon != null ? item.Icon.texture : AssetPreview.GetAssetPreview(item.gameObject);
// draw tex at size; if null, draw a fallback glyph (e.g. "◉")
```

Applied to: `DrawItemsSection` (Configured Items table), `DrawTrashLibrarySection` (Trash grid), `DrawItemLibrarySection` (refactor existing inline version to call the helper), and any other item card.

### Validator

- A **"Check item references"** button (Levels or Items tab) runs `ItemReferenceOps.FindBrokenReferences()`.
- Empty result → "✓ No broken item references."
- Otherwise a list: `<LevelDataSO name> → slot #<index>: missing item` and a **Remove all broken entries** button → `ItemReferenceOps.RemoveBrokenReferences(registerUndo: true)` → `SaveAssets()` → re-scan.

## Safety & error handling

- **Permanent delete** is irreversible → gated by a confirm naming the item and the reference count; never deletes icons that live outside `Sprites/Icons/` or that are referenced by another item prefab; wrapped in try/catch with `Debug.LogError` on any step failure (abort cleanly, leave levels consistent).
- **Soft-delete & validator-removal** stay **undoable** (Undo registered on the modified `LevelDataSO` assets).
- All asset-path resolution uses `AssetDatabase.GetAssetPath` / the actual prefab location rather than name guessing, so live-vs-Trash and legacy-naming cases are handled.

## Testing

- **Manual checklist (primary):**
  1. Generate an item → soft-delete → restore → item + icon back, levels re-populated, Play is clean.
  2. Permanent-delete a live item (right-click) → prefab + icon gone, level entries removed, Play is clean, no NRE.
  3. Permanent-delete from Trash → prefab + icon fully gone, no `DeletedItemRecord`, no restore possible.
  4. Seed a broken ref (manually delete a prefab's underlying asset) → validator lists it → Remove → Play is clean.
  5. Icon appears on Configured Items table, Trash grid, and Prefab Library grid.
- **Optional EditMode test** for `ItemReferenceOps` (`FindReferencingLevels` / `RemoveFromLevels` / `FindBrokenReferences`) — requires adding a small test assembly definition; add on request.

## Open items / out of scope

- Whether to also surface the validator result inline (badge on affected levels) — deferred; button + list is sufficient for v1.
- Auto-cleaning the two pre-convention icons (`icon_potion.png`, `icon_heartgem.png`) — out of scope; left as-is.
