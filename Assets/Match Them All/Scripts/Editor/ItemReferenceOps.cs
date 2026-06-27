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
