using Unity.AI.Assistant.Agent.Dynamic.Extension.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

internal class AdjustLobbyAnchors : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Open Lobby – ensures we work on the latest scene
        var scene = EditorSceneManager.OpenScene(
            "Assets/MatchThemAllTemplate/Scenes/Lobby.unity",
            OpenSceneMode.Single);

        // ----- Settings button (top‑right) -----
        var settingsGO = GameObject.Find("SettingsButton");
        if (settingsGO != null)
        {
            var rt = settingsGO.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Adjust SettingsButton anchors");
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f); // 20 px margin
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            result.Log("SettingsButton anchored to top‑right");
        }

        // ----- Shop button (top‑left) -----
        var shopGO = GameObject.Find("ShopOpenerButton");
        if (shopGO != null)
        {
            var rt = shopGO.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Adjust ShopOpenerButton anchors");
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -20f); // 20 px margin
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            result.Log("ShopOpenerButton anchored to top‑left");
        }

        // ----- Daily label (center) -----
        var dailyGO = GameObject.Find("Daily_Label");
        if (dailyGO != null)
        {
            var rt = dailyGO.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Adjust Daily_Label anchors");
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            result.Log("Daily_Label centered");
        }

        // Persist changes
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        result.Log("Lobby scene changes saved");

        // Re‑open so the scene stays active
        EditorSceneManager.OpenScene(
            "Assets/MatchThemAllTemplate/Scenes/Lobby.unity",
            OpenSceneMode.Single);
    }
}
