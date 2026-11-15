#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// First-time setup for a project the template was imported into. A package can't change project settings,
    /// so this does it: TextMesh Pro essentials, render pipeline, input handling, level registration, build scenes.
    /// Every step only fills in what is missing, so it is safe to run twice.
    /// Open via: Match Them All → Set Up Project
    /// </summary>
    public static class ProjectSetup
    {
        private const string Root = "Assets/MatchThemAllTemplate";
        private const string LevelLabel = "LevelData";
        private static readonly string[] SceneNames = { "OpenScene", "Lobby", "LevelSelect", "LoadingScene", "MainScene" };

        [MenuItem("Match Them All/Set Up Project")]
        public static void Run()
        {
            bool restart = false;
            var lines = new List<string>
            {
                ImportTmpEssentials(),
                AssignRenderPipeline(),
                EnableInputSystem(ref restart),
                RegisterLevels(),
                AddBuildScenes(),
            };
            AssetDatabase.SaveAssets();

            string text = string.Join("\n", lines) + (restart ? "\n\nRestart Unity to finish the input change." : "");
            Debug.Log("[Set Up Project]\n" + text);
            EditorUtility.DisplayDialog("Set Up Project", text, "OK");
        }

        private static string ImportTmpEssentials()
        {
            if (AssetDatabase.FindAssets("TMP Settings t:ScriptableObject").Length > 0)
                return "• TextMesh Pro essentials: already imported";

            const string pkg = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
            if (!System.IO.File.Exists(pkg))
                return "✖ TextMesh Pro essentials: package not found. Install Unity UI in the Package Manager.";

            AssetDatabase.ImportPackage(pkg, false);
            return "✔ TextMesh Pro essentials: importing now";
        }

        private static string AssignRenderPipeline()
        {
            var rp = AssetDatabase.FindAssets("Mobile_RPAsset t:RenderPipelineAsset", new[] { Root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>)
                .FirstOrDefault(a => a != null);
            if (rp == null)
                return "✖ Render pipeline: Mobile_RPAsset not found. Install Universal RP in the Package Manager.";

            bool changed = false;
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                GraphicsSettings.defaultRenderPipeline = rp;
                changed = true;
            }

            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != null) continue;
                QualitySettings.renderPipeline = rp;
                changed = true;
            }
            QualitySettings.SetQualityLevel(current, false);

            return changed ? "✔ Render pipeline: Mobile_RPAsset assigned where none was set" : "• Render pipeline: already assigned";
        }

        private static string EnableInputSystem(ref bool restart)
        {
            // ProjectSettings → activeInputHandler: 0 = Input Manager (old), 1 = Input System (new), 2 = both.
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/ProjectSettings.asset"));
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null) return "✖ Input: couldn't read the setting. Set Active Input Handling to Both by hand.";
            if (prop.intValue != 0) return "• Input: already uses the Input System";

            prop.intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            restart = true;
            return "✔ Input: Active Input Handling set to Both";
        }

        private static string RegisterLevels()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) return "✖ Levels: Addressables is not installed.";

            settings.AddLabel(LevelLabel);
            int added = 0, total = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:LevelDataSO", new[] { Root }))
            {
                total++;
                var entry = settings.FindAssetEntry(guid);
                if (entry != null && entry.labels.Contains(LevelLabel)) continue;

                // Same registration the Template Editor's "+ New" does: file name as address, "LevelData" label.
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.address = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                entry.SetLabel(LevelLabel, true, true);
                added++;
            }

            if (total == 0) return "✖ Levels: no level files found under " + Root;
            return added > 0 ? $"✔ Levels: {added} of {total} registered with Addressables" : $"• Levels: all {total} already registered";
        }

        private static string AddBuildScenes()
        {
            var wanted = SceneNames.Select(n => $"{Root}/Scenes/{n}.unity").ToArray();
            var missing = wanted.Where(p => string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p))).ToArray();
            if (missing.Length > 0) return "✖ Build scenes: not found: " + string.Join(", ", missing);

            var current = EditorBuildSettings.scenes;
            if (current.Length >= wanted.Length && wanted.Select((p, i) => current[i].path == p && current[i].enabled).All(ok => ok))
                return "• Build scenes: already in place";

            // Template scenes first (OpenScene must be index 0), anything the project already had after them.
            EditorBuildSettings.scenes = wanted.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(current.Where(s => !wanted.Contains(s.path)))
                .ToArray();
            return "✔ Build scenes: the 5 template scenes added, OpenScene first";
        }
    }
}
#endif
