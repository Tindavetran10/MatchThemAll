#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MatchThemAll.Scripts.UI;

namespace Match_Them_All.Scripts.Editor
{
    /// <summary>
    /// One-click builder for the scrolling level saga map. Menu:
    ///
    ///   Tools / Levels / Build Saga Map
    ///
    /// Creates in the OPEN scene:
    ///   • Level Saga Map Canvas — Screen Space Overlay + CanvasScaler + GraphicRaycaster
    ///   • a vertical ScrollRect + Content RectTransform (BOTTOM-anchored, AutoLayout off — nodes are absolutely positioned)
    ///   • a LevelMapPath (the connecting line, sitting behind nodes)
    ///   • a LevelMapNode prefab under UI/Prefabs/ (theme background Image + an instance of the existing
    ///     LevelButton prefab + the LevelMapNode component), all wired
    ///   • a LevelMapManager on the Canvas, wired to nodePrefab / contentRoot / scrollRect / pathRenderer / backButton
    ///   • a BackButton overlay (top-left corner) wired to LevelMapManager.OnBackClicked()
    ///
    /// Re-runnable: deletes any existing "Level Saga Map Canvas" first.
    /// </summary>
    public static class LevelMapBuilder
    {
        private const string NODE_PREFAB_PATH = "Assets/Match Them All/UI/Prefabs/LevelMapNode.prefab";
        private const string LEVEL_BUTTON_PREFAB = "Assets/Match Them All/UI/Prefabs/LevelButton.prefab";

        [MenuItem("Tools/Levels/Build Saga Map")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[LevelMapBuilder] Exit Play mode first.");
                return;
            }

            GameObject levelButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LEVEL_BUTTON_PREFAB);
            if (!levelButtonPrefab)
            {
                Debug.LogError($"[LevelMapBuilder] Existing LevelButton prefab not found at {LEVEL_BUTTON_PREFAB}.");
                return;
            }

            // Root Canvas.
            GameObject existing = GameObject.Find("Level Saga Map Canvas");
            if (existing) Object.DestroyImmediate(existing);

            var canvasGo = new GameObject("Level Saga Map Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // Match HEIGHT for a vertical scroll map: vertical canvas units stay constant across
            // portrait phones/tablets, so node spacing reads consistently. (Width varies, handled by
            // clamping zigzagWidth to a fraction of the viewport at runtime — see LevelMapManager.)
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
            // No EventSystem: taps go through the uGUI Button on each node, but the project uses the new
            // Input System. If an EventSystem already exists (for other UI), leave it; otherwise uGUI
            // buttons need one — add it lazily only if missing.
            EnsureEventSystemForUgui();

            // ScrollRect viewport + content.
            var viewport = MakeRect("Viewport", canvasGo.transform);
            StretchFill(viewport);
            var mask = viewport.gameObject.AddComponent<RectMask2D>(); // clip nodes outside the view

            var content = MakeRect("Content", viewport);
            // Bottom-anchored: level 1 sits near the bottom (small positive y), highest level near the top.
            // Scrolling UP shows higher (newer) levels — standard mobile saga map convention.
            content.anchorMin = new Vector2(0.5f, 0f);
            content.anchorMax = new Vector2(0.5f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.sizeDelta = new Vector2(1080f, 0f); // height set at runtime by LevelMapManager

            var scrollRect = canvasGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            // movementType/elasticity/scrollSensitivity are set at runtime by LevelMapManager.Start
            // (it forces Clamped); keeping them here too would be dead config that misleads.
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            // Path line behind the nodes (earlier sibling = rendered first = behind).
            var pathGo = new GameObject("Path");
            var pathRt = pathGo.AddComponent<RectTransform>();
            pathRt.SetParent(content, false);
            // Stretch to fill content so it can draw the full polyline.
            pathRt.anchorMin = Vector2.zero;
            pathRt.anchorMax = Vector2.one;
            pathRt.pivot = new Vector2(0.5f, 0.5f);
            pathRt.sizeDelta = new Vector2(0f, 0f);
            var pathRenderer = pathGo.AddComponent<LevelMapPath>();
            pathRenderer.color = new Color(1f, 1f, 1f, 0.6f);

            // Back button overlay — placed OUTSIDE the ScrollRect so it doesn't scroll.
            var backBtnGo = new GameObject("BackButton");
            backBtnGo.transform.SetParent(canvasGo.transform, false);
            var backBtnRt = backBtnGo.AddComponent<RectTransform>();
            backBtnRt.anchorMin = new Vector2(0f, 1f);
            backBtnRt.anchorMax = new Vector2(0f, 1f);
            backBtnRt.pivot = new Vector2(0f, 1f);
            backBtnRt.anchoredPosition = new Vector2(50f, -50f);
            backBtnRt.sizeDelta = new Vector2(130f, 130f);
            var backBtnImg = backBtnGo.AddComponent<Image>();
            backBtnImg.color = new Color(0.18f, 0.18f, 0.25f, 0.92f);
            var backBtnComp = backBtnGo.AddComponent<Button>();
            // Arrow icon inside the back button.
            var arrowGo = new GameObject("ArrowIcon");
            arrowGo.transform.SetParent(backBtnGo.transform, false);
            var arrowRt = arrowGo.AddComponent<RectTransform>();
            arrowRt.anchorMin = Vector2.zero;
            arrowRt.anchorMax = Vector2.one;
            arrowRt.offsetMin = new Vector2(20f, 20f);
            arrowRt.offsetMax = new Vector2(-20f, -20f);
            var arrowImg = arrowGo.AddComponent<Image>();
            arrowImg.raycastTarget = false;
            arrowImg.preserveAspect = true;
            arrowRt.localRotation = Quaternion.Euler(0f, 0f, -90f); // point left
            var upArrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Match Them All/Sprites/UI/Up Arrow.png");
            if (upArrowSprite) arrowImg.sprite = upArrowSprite;

            // Node prefab.
            LevelMapNode nodePrefab = BuildNodePrefab(levelButtonPrefab);

            // Manager on the canvas, wired.
            var manager = canvasGo.AddComponent<LevelMapManager>();
            var ser = new SerializedObject(manager);
            ser.FindProperty("nodePrefab").objectReferenceValue = nodePrefab;
            ser.FindProperty("contentRoot").objectReferenceValue = content;
            ser.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            ser.FindProperty("pathRenderer").objectReferenceValue = pathRenderer;
            ser.FindProperty("backButton").objectReferenceValue = backBtnComp;
            ser.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Selection.activeObject = canvasGo;
            Debug.Log("[LevelMapBuilder] Built Level Saga Map Canvas + node prefab. Save the scene. " +
                      "Nodes spawn at runtime from the LevelData SOs.");
        }

        private static LevelMapNode BuildNodePrefab(GameObject levelButtonPrefab)
        {
            // Root: theme background Image fills the node; the existing LevelButton sits on top as a child.
            var root = new GameObject("LevelMapNode");
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(220f, 220f);
            var themeBg = root.AddComponent<Image>();
            themeBg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f); // placeholder panel; LevelMapNode swaps the sprite in

            var button = (GameObject)PrefabUtility.InstantiatePrefab(levelButtonPrefab, root.transform);
            if (button)
            {
                var btnRt = button.transform as RectTransform;
                if (btnRt)
                {
                    btnRt.anchorMin = Vector2.zero;
                    btnRt.anchorMax = Vector2.one;
                    btnRt.offsetMin = Vector2.zero;
                    btnRt.offsetMax = Vector2.zero;
                }
            }

            var node = root.AddComponent<LevelMapNode>();
            var ser = new SerializedObject(node);
            ser.FindProperty("themeBackground").objectReferenceValue = themeBg;
            ser.FindProperty("button").objectReferenceValue = button ? button.GetComponent<LevelButtonUI>() : null;
            ser.ApplyModifiedPropertiesWithoutUndo();

            string dir = Path.GetDirectoryName(NODE_PREFAB_PATH);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            LevelMapNode prefab = PrefabUtility.SaveAsPrefabAsset(root, NODE_PREFAB_PATH).GetComponent<LevelMapNode>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystemForUgui()
        {
            // uGUI needs an EventSystem + an input module. The project uses the Input System only
            // (activeInputHandler: 1), so the legacy StandaloneInputModule does nothing — use the
            // Input System UI module. Direct reference resolves reliably (Unity.InputSystem is a dep).
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
#endif
