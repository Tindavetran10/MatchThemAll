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
        private const string MobileRpGuid = "5e6cbd92db86f4b18aec3ed561671858"; // Settings/Mobile_RPAsset.asset.meta
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
                EnsureLayers(),
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
            // Look it up by GUID, not by folder: this is the same GUID as Unity's Universal 3D template's Mobile_RPAsset,
            // so in such a project Unity imports the template's copy over Assets/Settings/Mobile_RPAsset.asset
            // and nothing exists at MatchThemAllTemplate/Settings.
            var rp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(MobileRpGuid));
            if (rp == null)
                return "✖ Render pipeline: Mobile_RPAsset not found or not readable. Check that Universal RP is installed and the template imported completely.";

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

            if (changed) return "✔ Render pipeline: Mobile_RPAsset assigned where none was set";
            var assigned = GraphicsSettings.defaultRenderPipeline;
            return "• Render pipeline: already assigned" + (assigned != null ? " (" + assigned.name + ")" : "");
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

        // Scenes and prefabs store layers by number (items on 6, power-up props on 7, masks in MainScene), and the code
        // looks "Tutorial" up by name, so the names must sit at exactly these numbers.
        private static readonly (int index, string name)[] Layers = { (6, "Match Stuff"), (7, "Power Ups"), (8, "Tutorial") };

        private static string EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var layers = tagManager.FindProperty("layers");
            if (layers == null || layers.arraySize < 32) return "✖ Layers: couldn't read the layer list. Add Match Stuff (6), Power Ups (7) and Tutorial (8) by hand.";

            var added = new List<string>();
            var problems = new List<string>();
            foreach (var (index, name) in Layers)
            {
                string at = layers.GetArrayElementAtIndex(index).stringValue;
                if (at == name) continue;
                if (!string.IsNullOrEmpty(at)) { problems.Add($"layer {index} is \"{at}\", the template needs \"{name}\" there"); continue; }

                int elsewhere = -1;
                for (int i = 0; i < layers.arraySize && elsewhere < 0; i++)
                    if (layers.GetArrayElementAtIndex(i).stringValue == name) elsewhere = i;
                if (elsewhere >= 0) { problems.Add($"\"{name}\" is on layer {elsewhere}, the template needs it on {index}"); continue; }

                layers.GetArrayElementAtIndex(index).stringValue = name;
                added.Add(name);
            }
            if (added.Count > 0) tagManager.ApplyModifiedProperties();

            if (problems.Count > 0)
                return "✖ Layers: " + string.Join("; ", problems) + ". Change it in Project Settings ▸ Tags and Layers." + (added.Count > 0 ? " Added: " + string.Join(", ", added) + "." : "");
            return added.Count > 0 ? "✔ Layers: added " + string.Join(", ", added) : "• Layers: already in place";
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
