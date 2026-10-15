using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.EditorTools
{
    /// <summary>Editor helper: renders a scene (3D + UI canvases) to a PNG for previewing layouts.</summary>
    public static class SceneCaptureUtil
    {
        /// <summary>Objects that are visible in the editor but hidden at runtime (skipped in previews).</summary>
        private static readonly string[] RuntimeHidden = { "TutorialCanvas", "TutorialTextCanvas" };

        public static string CaptureOpenScene(UnityEngine.SceneManagement.Scene scene, string outPath, int w = 1080, int h = 1920)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Camera cam = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var c in root.GetComponentsInChildren<Camera>(false))
                    if (cam == null || c.CompareTag("MainCamera")) cam = c;
            bool tempCam = false;
            if (cam == null)
            {
                cam = new GameObject("__CaptureCam").AddComponent<Camera>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam.gameObject, scene);
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
                tempCam = true;
            }
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;

            var restore = new List<System.Action>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(false))
                    if (System.Array.IndexOf(RuntimeHidden, t.name) >= 0)
                    {
                        var go = t.gameObject; go.SetActive(false);
                        restore.Add(() => go.SetActive(true));
                    }

            var canvases = new List<Canvas>();
            foreach (var root in scene.GetRootGameObjects())
                canvases.AddRange(root.GetComponentsInChildren<Canvas>(false));
            int order = 0;
            foreach (var cv in canvases)
            {
                if (!cv.isRootCanvas) continue;
                var c = cv;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    var mode = c.renderMode; var wc = c.worldCamera; var pd = c.planeDistance;
                    restore.Add(() => { c.renderMode = mode; c.worldCamera = wc; c.planeDistance = pd; });
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = cam.nearClipPlane + 0.05f + (0.001f * (100 - order++));
                }
                else if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera != null && c.worldCamera != cam)
                {
                    var wc = c.worldCamera;
                    restore.Add(() => { c.worldCamera = wc; });
                    c.worldCamera = cam;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                foreach (var cv in canvases) { var cs = cv.GetComponent<CanvasScaler>(); if (cs) { cs.enabled = false; cs.enabled = true; } }
                Canvas.ForceUpdateCanvases();
                cam.Render();
            }

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            cam.targetTexture = prevTarget;
            restore.Reverse();
            foreach (var r in restore) r();
            if (tempCam) Object.DestroyImmediate(cam.gameObject);
            rt.Release(); Object.DestroyImmediate(rt);
            return outPath;
        }

        /// <summary>Open scene additively (unless already open), capture, close without saving.</summary>
        public static string CaptureScenePath(string scenePath, string outPath, int w = 1080, int h = 1920, System.Action<UnityEngine.SceneManagement.Scene> beforeCapture = null)
        {
            var existing = EditorSceneManager.GetSceneByPath(scenePath);
            if (existing.IsValid() && existing.isLoaded && beforeCapture == null)
                return CaptureOpenScene(existing, outPath, w, h);

            var s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            var hidden = new List<GameObject>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var o = EditorSceneManager.GetSceneAt(i);
                if (o == s) continue;
                foreach (var g in o.GetRootGameObjects()) if (g.activeSelf) { g.SetActive(false); hidden.Add(g); }
            }
            try
            {
                beforeCapture?.Invoke(s);
                CaptureOpenScene(s, outPath, w, h);
            }
            finally
            {
                foreach (var g in hidden) g.SetActive(true);
                EditorSceneManager.CloseScene(s, true);
            }
            return outPath;
        }
    }
}
