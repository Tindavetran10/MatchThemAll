#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// One-shot: consolidates the scattered Prefabs folders (UI/Prefabs, Power Ups/Prefabs,
    /// Level System/Prefabs) into a single Prefabs/&lt;feature&gt;/ tree. Uses AssetDatabase.MoveAsset
    /// so GUIDs are preserved — scene/prefab references and Addressable entries survive untouched.
    ///
    /// Menu: Tools / Project / Consolidate Prefabs
    /// Idempotent: skips any prefab already at its destination.
    /// After running, the old feature Prefabs folders (and their parent if empty) are deleted.
    /// </summary>
    public static class PrefabConsolidator
    {
        private const string ROOT = "Assets/Match Them All";

        // source path (relative to ROOT) → destination subfolder under Prefabs/
        private static readonly (string src, string destFolder)[] Moves =
        {
            ("UI/Prefabs/DailyRewardPanel.prefab",         "UI"),
            ("UI/Prefabs/FloatingTextPrefab.prefab",       "UI"),
            ("UI/Prefabs/Goal Card.prefab",                "UI"),
            ("UI/Prefabs/Level/Level Label.prefab",        "UI/Level"),
            ("UI/Prefabs/Level/LevelButton.prefab",        "UI/Level"),
            ("UI/Prefabs/Level/LevelMapNode.prefab",       "UI/Level"),
            ("UI/Prefabs/Level/Lock Badge.prefab",         "UI/Level"),
            ("UI/Prefabs/Power Up/Amount Text.prefab",     "UI/Power Up"),
            ("UI/Prefabs/Power Up/Container.prefab",       "UI/Power Up"),
            ("UI/Prefabs/Power Up/Video Icon.prefab",      "UI/Power Up"),
            ("UI/Prefabs/Shop/ShopProductCard.prefab",     "UI/Shop"),
            ("UI/Prefabs/Shop/ShopTabButton.prefab",       "UI/Shop"),
            ("Power Ups/Prefabs/Vacuum.prefab",            "PowerUps"),
            ("Level System/Prefabs/LevelTemplate.prefab",  "Levels"),
        };

        // Folders to delete once their prefabs have been moved out (deepest first).
        private static readonly string[] FoldersToDelete =
        {
            "UI/Prefabs/Shop",
            "UI/Prefabs/Power Up",
            "UI/Prefabs/Level",
            "UI/Prefabs",
            "Power Ups/Prefabs",
            "Level System/Prefabs",
        };
        // Top-level feature folders that become empty after the move (deleted only if truly empty).
        private static readonly string[] TopLevelIfEmpty = { "UI", "Power Ups", "Level System" };

        [MenuItem("Tools/Project/Consolidate Prefabs")]
        public static void Consolidate()
        {
            int moved = 0, skipped = 0;
            var errors = new List<string>();

            foreach (var (src, destFolder) in Moves)
            {
                string srcPath = $"{ROOT}/{src}";
                string fileName = Path.GetFileName(srcPath);
                string destDir = $"{ROOT}/Prefabs/{destFolder}";
                string destPath = $"{destDir}/{fileName}";

                EnsureFolder(destDir);

                if (!AssetDatabase.LoadMainAssetAtPath(srcPath))
                {
                    if (AssetDatabase.LoadMainAssetAtPath(destPath)) { skipped++; continue; } // already moved
                    errors.Add($"Source not found: {srcPath}");
                    continue;
                }

                string err = AssetDatabase.MoveAsset(srcPath, destPath);
                if (string.IsNullOrEmpty(err)) moved++;
                else errors.Add($"{src} → {destPath}: {err}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Clean up emptied folders.
            int deleted = CleanupFolders();

            string report = $"[PrefabConsolidator] Moved {moved}, skipped {skipped}, deleted {deleted} folder(s).";
            if (errors.Count > 0) report += "\nErrors:\n  " + string.Join("\n  ", errors);
            Debug.Log(report);
        }

        private static int CleanupFolders()
        {
            int deleted = 0;
            foreach (var rel in FoldersToDelete)
            {
                string path = $"{ROOT}/{rel}";
                if (AssetDatabase.LoadMainAssetAtPath(path) != null && IsFolderEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    deleted++;
                }
            }
            foreach (var rel in TopLevelIfEmpty)
            {
                string path = $"{ROOT}/{rel}";
                if (AssetDatabase.LoadMainAssetAtPath(path) != null && IsFolderEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    deleted++;
                }
            }
            return deleted;
        }

        private static bool IsFolderEmpty(string assetPath)
        {
            string fs = AssetDatabase.AssetPathToGUID(assetPath);
            // Use AssetDatabase to check for any sub-assets; a folder is empty if it has no child assets.
            string[] subs = AssetDatabase.FindAssets("", new[] { assetPath });
            return subs == null || subs.Length == 0;
        }

        private static void EnsureFolder(string assetFolder)
        {
            // assetFolder is an asset path like "Assets/Match Them All/Prefabs/UI/Level"
            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (AssetDatabase.LoadMainAssetAtPath(next) == null)
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
