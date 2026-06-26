# Item Deletion Lifecycle & Icon Propagation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a true permanent (hard) delete for item prefabs that leaves no dangling references, propagate the generated item icon to every editor surface, and add a validator that finds/repairs already-broken item references — all without disturbing the existing soft-delete (Trash) flow.

**Architecture:** Extract the shared "find/remove item references across `LevelDataSO`" logic into a new static editor helper `ItemReferenceOps`. Soft-delete, hard-delete, and the validator all call it (one source of truth). A small `GetItemIcon` helper centralizes icon rendering. Everything stays inside the existing `LevelEditorWindow.cs` assembly + one new file.

**Tech Stack:** Unity (URP) editor tooling, C# 9, `UnityEditor` APIs (`AssetDatabase`, `Undo`, `EditorUtility`, `AssetPreview`, `GenericMenu`). No new packages, no test assembly.

## Global Constraints

- **Editor-only code.** New file lives under `Assets/Match Them All/Scripts/Editor/` (an Editor folder) so it compiles into `Assembly-CSharp-Editor` only. Namespace: `Match_Them_All.Scripts.Editor` (matches `LevelEditorWindow.cs`).
- **No `EItemName` enum changes** on delete (orphan values are harmless; `Clock`/`Coin` already have no prefab).
- **No Addressables changes** (item prefabs are not addressable; only `LevelDataSO` is).
- **No `ItemLibrary`/registry ScriptableObject.** Item discovery stays folder-scan based.
- **No automated tests in this plan.** The project has no test assembly; verification is **manual** in the Unity Editor (perform action → check Console is error-free → confirm Play mode is clean). Each task's final step is the manual check + commit.
- **Verification console-check command** (run after each recompile, from the project root): open Unity → watch the Console; or, if MCP is connected, `Unity_GetConsoleLogs(logTypes="error")`. Expected: no new errors.
- **Commit target:** `main` (this solo project's established branch). One commit per task, message prefix `feat:`/`fix:`/`refactor:` as appropriate.

---

## File Structure

- **Create:** `Assets/Match Them All/Scripts/Editor/ItemReferenceOps.cs` — static, editor-only helper. Single responsibility: enumerate `LevelDataSO` assets and find/remove item-prefab references. Consumed by soft-delete, hard-delete, and the validator.
- **Modify:** `Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs` — refactor `SoftDeleteItem`/`RestoreFromTrash` to use the helper; add `GetItemIcon`, `HardDeleteItem`, `DrawItemReferenceValidator`; wire new UI (trash permanent-delete button, live-card right-click menu, validator section).

---

### Task 1: Create `ItemReferenceOps.cs`

**Files:**
- Create: `Assets/Match Them All/Scripts/Editor/ItemReferenceOps.cs`

**Interfaces:**
- Produces: `ItemReferenceOps.FindAllLevels()`, `FindReferencingLevels(Item)`, `RemoveFromLevels(Item, bool, capture)`, `FindBrokenReferences()`, `RemoveBrokenReferences(bool)`, `IsIconSafeToDelete(Item)` — signatures shown in Step 1. Tasks 3, 4, 6 consume these.

- [ ] **Step 1: Create the helper file with complete implementation**

Create `Assets/Match Them All/Scripts/Editor/ItemReferenceOps.cs`:

```csharp
using System.Collections.Generic;
using MatchThemAll.Scripts;
using UnityEditor;
using UnityEngine;

namespace Match_Them_All.Scripts.Editor
{
    /// <summary>
    /// Shared, editor-only operations for finding and removing item-prefab references
    /// across LevelDataSO assets. Used by soft-delete, permanent (hard) delete, and the
    /// broken-reference validator so they share one source of truth.
    /// </summary>
    internal static class ItemReferenceOps
    {
        private const string IconsFolder = "Assets/Match Them All/Sprites/Icons";
        private const string ItemPrefabFolder = "Assets/Match Them All/_START_HERE/Items";

        /// <summary>Load every LevelDataSO asset in the project.</summary>
        internal static List<LevelDataSO> FindAllLevels()
        {
            var levels = new List<LevelDataSO>();
            foreach (var g in AssetDatabase.FindAssets("t:LevelDataSO"))
            {
                var so = AssetDatabase.LoadAssetAtPath<LevelDataSO>(AssetDatabase.GUIDToAssetPath(g));
                if (so != null) levels.Add(so);
            }
            return levels;
        }

        /// <summary>Every (level, index) where itemData[index].itemPrefab == item.</summary>
        internal static List<(LevelDataSO level, int index)> FindReferencingLevels(Item item)
        {
            var result = new List<(LevelDataSO, int)>();
            if (item == null) return result;
            foreach (var level in FindAllLevels())
            {
                if (level.itemData == null) continue;
                for (int i = 0; i < level.itemData.Count; i++)
                    if (level.itemData[i].itemPrefab == item)
                        result.Add((level, i));
            }
            return result;
        }

        /// <summary>
        /// Remove every entry whose itemPrefab == item, from every level. If <paramref name="capture"/>
        /// is non-null, appends (level, originalIndex, entry) for each removal (soft-delete uses this to
        /// rebuild its undo record). Marks each affected level dirty; registers Undo when requested.
        /// Returns the number of entries removed.
        /// </summary>
        internal static int RemoveFromLevels(Item item, bool registerUndo,
            List<(LevelDataSO level, int index, ItemLevelData entry)> capture = null)
        {
            int removed = 0;
            if (item == null) return removed;
            foreach (var level in FindAllLevels())
            {
                if (level.itemData == null) continue;
                // high -> low so removals don't shift the indices we still need to read
                for (int i = level.itemData.Count - 1; i >= 0; i--)
                {
                    if (level.itemData[i].itemPrefab != item) continue;
                    if (registerUndo) Undo.RecordObject(level, "Remove Item Reference");
                    capture?.Add((level, i, level.itemData[i]));
                    level.itemData.RemoveAt(i);
                    EditorUtility.SetDirty(level);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>Every (level, index, levelName) where itemPrefab is null/missing — for the validator.</summary>
        internal static List<(LevelDataSO level, int index, string levelName)> FindBrokenReferences()
        {
            var result = new List<(LevelDataSO, int, string)>();
            foreach (var level in FindAllLevels())
            {
                if (level.itemData == null) continue;
                for (int i = 0; i < level.itemData.Count; i++)
                    if (level.itemData[i].itemPrefab == null)
                        result.Add((level, i, level.name));
            }
            return result;
        }

        /// <summary>Remove every entry whose itemPrefab is null/missing. Undoable. Returns count removed.</summary>
        internal static int RemoveBrokenReferences(bool registerUndo)
        {
            int removed = 0;
            foreach (var level in FindAllLevels())
            {
                if (level.itemData == null) continue;
                for (int i = level.itemData.Count - 1; i >= 0; i--)
                {
                    if (level.itemData[i].itemPrefab != null) continue;
                    if (registerUndo) Undo.RecordObject(level, "Remove Broken Item Reference");
                    level.itemData.RemoveAt(i);
                    EditorUtility.SetDirty(level);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// True only if the item's icon is safe to delete alongside it: it lives under the
        /// generated-icons folder AND no other item prefab references the same icon sprite.
        /// </summary>
        internal static bool IsIconSafeToDelete(Item item)
        {
            if (item == null || item.Icon == null) return false;
            string iconPath = AssetDatabase.GetAssetPath(item.Icon);
            if (string.IsNullOrEmpty(iconPath) || !iconPath.StartsWith(IconsFolder)) return false;

            int users = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { ItemPrefabFolder }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                var it = go != null ? go.GetComponent<Item>() : null;
                if (it != null && it.Icon == item.Icon) users++;
            }
            return users <= 1;
        }
    }
}
```

- [ ] **Step 2: Recompile and check for errors**

Save the file. Let Unity recompile (or trigger `AssetDatabase.Refresh()`). Check the Console.
Expected: no compile errors. (`ItemReferenceOps` is unused so far — that's fine.)

- [ ] **Step 3: Commit**

```bash
git add "Assets/Match Them All/Scripts/Editor/ItemReferenceOps.cs"
git commit -m "feat: add ItemReferenceOps helper for item-reference management"
```

---

### Task 2: Centralize icon rendering with `GetItemIcon` and apply everywhere

**Files:**
- Modify: `Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs`
  - Add method (place it just above `SoftDeleteItem`, ~L1300).
  - Replace icon block in `DrawItemsSection` (~L805-810).
  - Replace icon block in `DrawItemLibrarySection` (~L1175-1185).
  - Replace icon lines in `DrawTrashLibrarySection` (~L1265-1266).

**Interfaces:**
- Produces: `LevelEditorWindow.GetItemIcon(GameObject prefab) -> Texture`.

- [ ] **Step 1: Add the `GetItemIcon` helper**

Insert this method immediately before `private void SoftDeleteItem(GameObject prefab)` (~L1300):

```csharp
        /// <summary>Resolved icon texture for an item: its generated icon, else the prefab preview, else black.</summary>
        private Texture GetItemIcon(GameObject prefab)
        {
            var itemComp = prefab != null ? prefab.GetComponent<Item>() : null;
            if (itemComp != null && itemComp.Icon != null) return itemComp.Icon.texture;
            var preview = AssetPreview.GetAssetPreview(prefab);
            return preview != null ? preview : Texture2D.blackTexture;
        }
```

- [ ] **Step 2: Use it in the Configured Items table (`DrawItemsSection`)**

Replace the block currently at ~L805-810:

```csharp
                var preview = entry.itemPrefab != null
                    ? AssetPreview.GetAssetPreview(entry.itemPrefab.gameObject) : null;
                if (preview)
                    GUILayout.Label(preview, GUILayout.Width(iconW - 6), GUILayout.Height(30));
                else
                    GUILayout.Label("◉", GUILayout.Width(iconW - 6), GUILayout.Height(30));
```

with:

```csharp
                if (entry.itemPrefab != null)
                    GUILayout.Label(GetItemIcon(entry.itemPrefab.gameObject), GUILayout.Width(iconW - 6), GUILayout.Height(30));
                else
                    GUILayout.Label("◉", GUILayout.Width(iconW - 6), GUILayout.Height(30));
```

- [ ] **Step 3: Use it in the Prefab Library grid (`DrawItemLibrarySection`)**

Replace the block currently at ~L1175-1185:

```csharp
                Texture tex = Texture2D.blackTexture;
                var itemComp = prefab.GetComponent<Item>();
                if (itemComp != null && itemComp.Icon != null)
                {
                    tex = itemComp.Icon.texture;
                }
                else
                {
                    var preview = AssetPreview.GetAssetPreview(prefab);
                    if (preview != null) tex = preview;
                }
```

with:

```csharp
                var itemComp = prefab.GetComponent<Item>();
                Texture tex = GetItemIcon(prefab);
```

(Keep `var itemComp = prefab.GetComponent<Item>();` because it is reused by the new right-click menu added in Task 5.)

- [ ] **Step 4: Use it in the Trash grid (`DrawTrashLibrarySection`)**

Replace the two lines currently at ~L1265-1266:

```csharp
                var preview = AssetPreview.GetAssetPreview(prefab);
                Texture2D tex = preview != null ? preview : Texture2D.blackTexture;
```

with:

```csharp
                Texture tex = GetItemIcon(prefab);
```

- [ ] **Step 5: Recompile and verify manually**

Save, let Unity recompile, check Console (no errors). Open **Match Them All → Template Editor → Levels tab**: select a level — the Configured Items rows now show the generated icon (not the generic thumbnail). In the **Items tab**, the Prefab Library grid and the Trash grid (if any trashed items) show generated icons.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs"
git commit -m "feat: show generated item icon on all editor surfaces"
```

---

### Task 3: Refactor soft-delete/restore onto `ItemReferenceOps` (+ icon-restore bugfix)

**Files:**
- Modify: `Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs` — `SoftDeleteItem` (~L1300-1352) and `RestoreFromTrash` (~L1384-1397).

**Interfaces:**
- Consumes: `ItemReferenceOps.FindReferencingLevels(Item)`, `ItemReferenceOps.RemoveFromLevels(Item, bool, capture)`.

- [ ] **Step 1: Replace the two level loops in `SoftDeleteItem`**

In `SoftDeleteItem`, delete the affected-levels discovery loop (~L1303-1313):

```csharp
            int usedCount = 0;
            var affectedLevels = new List<LevelDataSO>();

            foreach (var level in _levels)
            {
                if (level.itemData != null && level.itemData.Any(i => i.itemPrefab == itemComp))
                {
                    usedCount++;
                    affectedLevels.Add(level);
                }
            }
```

and replace it with:

```csharp
            int usedCount = ItemReferenceOps.FindReferencingLevels(itemComp).Count;
```

Then, after the confirm dialog and the `var record = new DeletedItemRecord { ... }` block (~L1331-1335), replace the removal/capture loop (~L1337-1352):

```csharp
            // Remove from levels, capturing each entry's primitive state (not the Item ref) for restore
            foreach (var level in affectedLevels)
            {
                Undo.RecordObject(level, "Remove Deleted Item");
                var entry = level.itemData.First(i => i.itemPrefab == itemComp);
                record.removedFromLevels.Add(new RemovedLevelEntry
                {
                    levelGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(level)),
                    index = level.itemData.IndexOf(entry),
                    isGoal = entry.isGoal,
                    multiplier = entry.multiplier,
                    amount = entry.amount,
                });
                level.itemData.Remove(entry);
                EditorUtility.SetDirty(level);
            }
```

with:

```csharp
            // Remove from levels via the shared helper, capturing each removed entry for restore
            var capture = new List<(LevelDataSO level, int index, ItemLevelData entry)>();
            ItemReferenceOps.RemoveFromLevels(itemComp, registerUndo: true, capture: capture);
            foreach (var (level, index, entry) in capture)
            {
                record.removedFromLevels.Add(new RemovedLevelEntry
                {
                    levelGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(level)),
                    index = index,
                    isGoal = entry.isGoal,
                    multiplier = entry.multiplier,
                    amount = entry.amount,
                });
            }
```

Leave the rest of `SoftDeleteItem` (prefab/icon moves, `_deletedRecords.Add`, save, reload) unchanged.

- [ ] **Step 2: Fix icon-restore to use the record's captured paths**

In `RestoreFromTrash`, replace the name-derived icon-path lines and the icon-move block (~L1386-1397):

```csharp
            string originalIconPath = $"Assets/Match Them All/Sprites/Icons/icon_{trashPrefab.name.ToLowerInvariant()}.png";
            string trashIconPath = $"Assets/Match Them All/Sprites/Icons/Trash/icon_{trashPrefab.name.ToLowerInvariant()}.png";

            // Move Icon back (secondary: warn but continue on failure)
            if (AssetDatabase.LoadMainAssetAtPath(trashIconPath) != null)
            {
                string iconError = AssetDatabase.MoveAsset(trashIconPath, originalIconPath);
                if (!string.IsNullOrEmpty(iconError)) Debug.LogWarning($"[Template Editor] Could not restore icon: {iconError}");
            }
```

with (look up the record FIRST, then use its captured icon paths, falling back to the name convention only when no record exists):

```csharp
            var record = _deletedRecords.FirstOrDefault(r => r.trashPrefabPath == trashPrefabPath);

            // Use the record's captured icon paths (robust); fall back to the name convention if no record.
            string trashIconPath = record != null && !string.IsNullOrEmpty(record.trashIconPath)
                ? record.trashIconPath
                : $"Assets/Match Them All/Sprites/Icons/Trash/icon_{trashPrefab.name.ToLowerInvariant()}.png";
            string originalIconPath = record != null && !string.IsNullOrEmpty(record.originalIconPath)
                ? record.originalIconPath
                : $"Assets/Match Them All/Sprites/Icons/icon_{trashPrefab.name.ToLowerInvariant()}.png";

            if (AssetDatabase.LoadMainAssetAtPath(trashIconPath) != null)
            {
                string iconError = AssetDatabase.MoveAsset(trashIconPath, originalIconPath);
                if (!string.IsNullOrEmpty(iconError)) Debug.LogWarning($"[Template Editor] Could not restore icon: {iconError}");
            }
```

Then **remove the later duplicate `var record = ...` line** (~L1414, `var record = _deletedRecords.FirstOrDefault(r => r.trashPrefabPath == trashPrefabPath);`) since `record` is now declared above. The re-insert block that follows (`if (record != null) { ... }`) stays unchanged and now uses the already-declared `record`.

- [ ] **Step 3: Recompile and verify manually**

Save, recompile, check Console (no errors). In the Template Editor: soft-delete an item used in a level (✕) → confirm it moves to Trash and the level row disappears. Restore it (⟲) → confirm the item + its icon return and the level row reappears. Press Play → level spawns cleanly, no NRE.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs"
git commit -m "refactor: route soft-delete/restore through ItemReferenceOps; fix icon restore paths"
```

---

### Task 4: Add the permanent-delete method `HardDeleteItem`

**Files:**
- Modify: `Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs` — add method near `SoftDeleteItem`.

**Interfaces:**
- Consumes: `ItemReferenceOps.FindReferencingLevels`, `RemoveFromLevels`, `IsIconSafeToDelete`.
- Produces: `LevelEditorWindow.HardDeleteItem(Item item)` — wired to UI in Task 5.

- [ ] **Step 1: Add `HardDeleteItem`**

Insert this method immediately after `SoftDeleteItem` (after its closing brace, ~L1382):

```csharp
        /// <summary>
        /// Permanently delete an item: removes it from all levels, deletes its icon (only if it is the
        /// item's own generated icon and unused elsewhere), deletes the prefab, and drops any trash-undo
        /// record so it cannot be restored. Irreversible — gated by a confirm dialog. No Undo by design;
        /// use Move to Trash (SoftDeleteItem) for a reversible delete.
        /// </summary>
        private void HardDeleteItem(Item item)
        {
            if (item == null) { Debug.LogWarning("[Template Editor] HardDeleteItem: null item."); return; }

            string prefabPath = AssetDatabase.GetAssetPath(item.gameObject);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"[Template Editor] HardDeleteItem: could not resolve prefab path for '{item.name}'.");
                return;
            }

            var refs = ItemReferenceOps.FindReferencingLevels(item);
            bool iconSafe = ItemReferenceOps.IsIconSafeToDelete(item);
            string iconNote = iconSafe ? ", its icon" : "";
            string msg = $"Permanently delete '{item.name}'?\n\n" +
                         $"This removes the prefab{iconNote} and {refs.Count} level reference(s).\n" +
                         "This CANNOT be undone. (Use Move to Trash if you want it reversible.)";
            if (!EditorUtility.DisplayDialog("Delete Permanently", msg, "Delete permanently", "Cancel"))
                return;

            try
            {
                // 1. Scrub level references (no Undo — the whole operation is irreversible by design).
                ItemReferenceOps.RemoveFromLevels(item, registerUndo: false);

                // 2. Delete the icon only if it's this item's own generated icon and unused elsewhere.
                if (iconSafe && item.Icon != null)
                {
                    string iconPath = AssetDatabase.GetAssetPath(item.Icon);
                    if (!string.IsNullOrEmpty(iconPath))
                        AssetDatabase.DeleteAsset(iconPath);
                }

                // 3. Delete the prefab (works whether it is live or already in a Trash folder).
                AssetDatabase.DeleteAsset(prefabPath);

                // 4. Drop any trash-undo record so the item cannot be restored.
                _deletedRecords.RemoveAll(r => r.originalPrefabPath == prefabPath || r.trashPrefabPath == prefabPath);
                SaveTrashUndo();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Template Editor] HardDeleteItem failed for '{item.name}': {e}");
            }

            AssetDatabase.SaveAssets();
            LoadAll();
            Repaint();
        }
```

(`Exception` resolves via the existing `using System;` at the top of the file.)

- [ ] **Step 2: Recompile and check for errors**

Save, recompile, check Console. Expected: no errors. (`HardDeleteItem` is unused until Task 5.)

- [ ] **Step 3: Commit**

```bash
git add "Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs"
git commit -m "feat: add HardDeleteItem permanent-delete logic"
```

---

### Task 5: Wire hard-delete UI (Trash ⌫ button + live-card right-click menu)

**Files:**
- Modify: `Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs` — `DrawTrashLibrarySection` (~L1251-1296) and `DrawItemLibrarySection` (~L1186-1205).

**Interfaces:**
- Consumes: `LevelEditorWindow.HardDeleteItem(Item)` (Task 4), `SoftDeleteItem` (existing).

- [ ] **Step 1: Add a permanent-delete button to trash cards**

In `DrawTrashLibrarySection`, next to the existing `pendingRestorePrefab` declaration (~L1251):

```csharp
            var pendingRestorePrefab = (GameObject)null;
```

add:

```csharp
            var pendingPermanentDelete = (GameObject)null;
```

Then, inside the per-prefab loop, immediately after the restore button block (~L1281, after the `GUI.color = Color.white;` that follows the ⟲ button), add a permanent-delete button in the top-left corner of the card:

```csharp
                var permRect = new Rect(rect.xMin + 2, rect.yMin + 2, 16, 16);
                GUI.color = AccentRed;
                if (GUI.Button(permRect, "⌫", _iconCardSmallButtonStyle))
                {
                    pendingPermanentDelete = prefab;
                }
                GUI.color = Color.white;
```

Finally, after the loop's `if (pendingRestorePrefab != null) { ... }` block (~L1292-1296), add:

```csharp
            if (pendingPermanentDelete != null)
            {
                HardDeleteItem(pendingPermanentDelete.GetComponent<Item>());
                GUIUtility.ExitGUI();
            }
```

- [ ] **Step 2: Add a right-click context menu to live item cards**

In `DrawItemLibrarySection`, inside the per-prefab loop, immediately after the ✕ delete-button block (~L1205, after `GUI.color = Color.white;` that follows the ✕ button) and before `GUILayout.Label(prefab.name, ...)`, add right-click handling. (`e` is already declared at ~L1190 as `var e = Event.current;`.)

```csharp
                if (e.type == EventType.ContextClick && rect.Contains(e.mousePosition))
                {
                    var menu = new GenericMenu();
                    var capturedItem = itemComp; // Item on this prefab, for the permanent-delete closure
                    menu.AddItem(new GUIContent("Move to Trash"), false, () => SoftDeleteItem(prefab));
                    menu.AddItem(new GUIContent("Delete Permanently…"), false, () => HardDeleteItem(capturedItem));
                    menu.ShowAsContext();
                    e.Use();
                }
```

- [ ] **Step 3: Recompile and verify manually**

Save, recompile, check Console (no errors). In the Template Editor:

1. **Trash → permanent delete:** soft-delete an item (✕), then in the Trash grid click the red **⌫** → confirm. The prefab + its icon file disappear from disk, the card leaves the Trash grid, and the item is gone from `_START_HERE/Items/Trash` (verify in the Project window). The item is NOT restorable (no ⟲ record). Press Play on a level that used it → it was removed from that level, so no NRE.
2. **Live → permanent delete:** right-click a live item card → *Delete Permanently…* → confirm. Prefab + icon gone; any level rows referencing it are removed. Press Play → clean.
3. Confirm the Editor's Undo does **not** bring it back (permanent by design).

- [ ] **Step 4: Commit**

```bash
git add "Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs"
git commit -m "feat: wire permanent-delete UI (trash button + live-card context menu)"
```

---

### Task 6: Add the broken-reference validator

**Files:**
- Modify: `Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs` — add a state field; add `DrawItemReferenceValidator`; call it from the Items tab.

**Interfaces:**
- Consumes: `ItemReferenceOps.FindBrokenReferences()`, `RemoveBrokenReferences(bool)`.

- [ ] **Step 1: Add the validator state field**

In the State region (near the other `_trashScroll` / item state, ~L39-47), add:

```csharp
        private List<(LevelDataSO level, int index, string levelName)> _brokenRefs;
```

- [ ] **Step 2: Add the `DrawItemReferenceValidator` method**

Insert this method immediately after `DrawTrashLibrarySection` (after its closing brace, ~L1297):

```csharp
        private void DrawItemReferenceValidator()
        {
            GUILayout.Space(12);
            GUILayout.Label("Item Reference Health", _subHeaderStyle);
            GUILayout.Space(4);

            if (GUILayout.Button("Check item references", GUILayout.Width(180)))
                _brokenRefs = ItemReferenceOps.FindBrokenReferences();

            if (_brokenRefs == null) return;

            if (_brokenRefs.Count == 0)
            {
                GUI.color = AccentGreen;
                GUILayout.Label("✓ No broken item references.", EditorStyles.wordWrappedLabel);
                GUI.color = Color.white;
                return;
            }

            GUI.color = AccentOrange;
            GUILayout.Label($"⚠ {_brokenRefs.Count} broken item reference(s) found:", EditorStyles.wordWrappedLabel);
            GUI.color = Color.white;
            foreach (var (level, index, levelName) in _brokenRefs)
                GUILayout.Label($"  • {levelName} → slot #{index}: missing item", EditorStyles.miniLabel);

            GUILayout.Space(4);
            if (GUILayout.Button("Remove all broken entries", GUILayout.Width(200)))
            {
                int n = ItemReferenceOps.RemoveBrokenReferences(registerUndo: true);
                AssetDatabase.SaveAssets();
                LoadAll();
                _brokenRefs = ItemReferenceOps.FindBrokenReferences();
                Debug.Log($"[Template Editor] Removed {n} broken item reference(s).");
            }
        }
```

- [ ] **Step 3: Call it from the Items tab**

In the Items-tab method that ends with `DrawTrashLibrarySection(panelWidth);` (~L1222-1224):

```csharp
            GUILayout.Space(12);
            DrawTrashLibrarySection(panelWidth);
        }
```

add a call before the closing brace:

```csharp
            GUILayout.Space(12);
            DrawTrashLibrarySection(panelWidth);
            DrawItemReferenceValidator();
        }
```

- [ ] **Step 4: Recompile and verify manually**

Save, recompile, check Console (no errors). Seed a broken reference: in the Project window, manually delete (or move out of the project) one item prefab that a level references. Open the Template Editor → Items tab → **Check item references**: it lists the broken entry (`LevelData01 → slot #N: missing item`). Click **Remove all broken entries** → the broken entry is gone. Press Play → level spawns cleanly, no NRE. (Re-create/restore the prefab afterward to return to a clean state.)

- [ ] **Step 5: Commit**

```bash
git add "Assets/Match Them All/Scripts/Editor/LevelEditorWindow.cs"
git commit -m "feat: add broken-item-reference validator with one-click repair"
```

---

## Final verification (after Task 6)

End-to-end manual checklist (from the spec):

1. Generate an item → soft-delete (✕) → restore (⟲) → item + icon back, level row restored, Play clean.
2. Permanent-delete a live item (right-click → *Delete Permanently…*) → prefab + icon gone, level refs removed, Play clean, no NRE, not undoable.
3. Permanent-delete from Trash (⌫) → fully gone, not restorable.
4. Seed a broken ref → validator lists it → *Remove all broken entries* → Play clean.
5. Generated icons appear on the Configured Items table, Trash grid, and Prefab Library grid.

## Out of scope (per spec)

- No `EItemName` pruning, no `ItemLibrary` registry, no Addressables changes, no renaming legacy prefabs, no auto-test assembly (can be added later per the spec's optional item).
