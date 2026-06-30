using System.Collections.Generic;
using MatchThemAll.Scripts;
using UnityEditor;
using UnityEngine;
using ZLinq;

namespace Match_Them_All.Scripts.Editor
{
    /// <summary>
    /// Shared, editor-only operations for finding and removing item-prefab references
    /// across LevelDataSO assets. Used by soft-delete, permanent (hard) delete, and the
    /// broken-reference validator so they share one source of truth. Also owns the
    /// canonical item/icon folder paths so callers don't duplicate them.
    /// </summary>
    internal static class ItemReferenceOps
    {
        // Single source of truth for the paths the editor relies on.
        internal const string ItemPrefabFolder = "Assets/Match Them All/_START_HERE/Items";
        internal const string ItemTrashFolder  = ItemPrefabFolder + "/Trash";
        internal const string IconsFolder      = "Assets/Match Them All/Sprites/Icons";
        internal const string IconsTrashFolder = IconsFolder + "/Trash";

        /// <summary>Load every LevelDataSO asset in the project.</summary>
        internal static List<LevelDataSO> FindAllLevels() => AssetDatabase.FindAssets("t:LevelDataSO")
            .AsValueEnumerable()
            .Select(g => AssetDatabase.LoadAssetAtPath<LevelDataSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(so => so != null).ToList();

        /// <summary>Every (level, index) where itemData[index].itemPrefab == item, within the given levels.</summary>
        internal static List<(LevelDataSO level, int index)> FindReferencingLevels(IList<LevelDataSO> levels, Item item)
        {
            var result = new List<(LevelDataSO, int)>();
            if (!item || levels == null) return result;
            foreach (var level in levels.AsValueEnumerable().Where(l => l != null && l.itemData != null))
            {
                for (int i = 0; i < level.itemData.Count; i++)
                    if (level.itemData[i].itemPrefab == item)
                        result.Add((level, i));
            }
            return result;
        }

        /// <summary>
        /// Remove every entry whose itemPrefab == item, from the given levels. If <paramref name="capture"/>
        /// is non-null, appends (level, originalIndex, entry) for each removal (soft-delete uses this to
        /// rebuild its undo record). Marks each affected level dirty; registers Undo when requested.
        /// Returns the number of entries removed. Pass a fresh FindAllLevels() to scrub every level.
        /// </summary>
        internal static int RemoveFromLevels(IList<LevelDataSO> levels, Item item, bool registerUndo,
            List<(LevelDataSO level, int index, ItemLevelData entry)> capture = null)
        {
            int removed = 0;
            if (!item || levels == null) return removed;
            foreach (var level in levels.AsValueEnumerable().Where(l => l != null && l.itemData != null))
            {
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
            foreach (var level in FindAllLevels().AsValueEnumerable().Where(l => l != null && l.itemData != null))
            {
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
            foreach (var level in FindAllLevels().AsValueEnumerable().Where(l => l != null && l.itemData != null))
            {
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
        /// True only if the item's icon is safe to delete alongside it: it lives under the generated-icons
        /// folder (NOT the Trash subfolder) AND no other item prefab — live or trashed — references the
        /// same icon sprite. Trashed prefabs are included so a shared icon isn't deleted while a sibling
        /// undo record still points at it.
        /// </summary>
        internal static bool IsIconSafeToDelete(Item item)
        {
            if (!item || !item.Icon) return false;
            string iconPath = AssetDatabase.GetAssetPath(item.Icon);
            if (string.IsNullOrEmpty(iconPath)) return false;
            // Bound the prefix so the Trash subfolder and sibling folders don't match.
            if (!iconPath.StartsWith(IconsFolder + "/")) return false;
            if (iconPath.StartsWith(IconsTrashFolder + "/")) return false;

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
