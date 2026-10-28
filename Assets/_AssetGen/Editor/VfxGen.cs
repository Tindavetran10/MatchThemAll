using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dev-only (not exported): particle textures, URP particle materials, shield animation and the merge burst we ship.
// Run order: Generate VFX -> Apply Swap Map -> Fix VFX Prefabs.
static class VfxGen
{
    const string Dir = "Assets/MatchThemAllTemplate/Art/VFX/";
    const string VfxPrefabs = "Assets/MatchThemAllTemplate/Prefabs/VFX/";

    // ---------- textures (white + alpha, tinted by particle color) ----------
    static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);
    static float Smooth(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3 - 2 * t); }

    static readonly (string name, Func<float, float, float> alpha)[] Textures =
    {
        ("FX_SoftDot", (x, y) => Mathf.Pow(1 - Mathf.Clamp01(Len(x, y)), 2.2f)),
        ("FX_Ring", (x, y) => Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(Len(x, y) - .78f) / .1f), 1.5f)),
        ("FX_RingThick", (x, y) => Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(Len(x, y) - .7f) / .22f), 1.2f)),
        ("FX_Dash", (x, y) => Mathf.Pow(1 - Mathf.Clamp01(Mathf.Abs(x)), 1.5f) * Mathf.Pow(1 - Mathf.Clamp01(Mathf.Abs(y) / .16f), 2f)),
        ("FX_Star", (x, y) =>
        {
            float r = Len(x, y), arms = Mathf.Max(Mathf.Pow(1 - Mathf.Clamp01(Mathf.Abs(x) / .06f), 2) * (1 - Mathf.Abs(y)),
                Mathf.Pow(1 - Mathf.Clamp01(Mathf.Abs(y) / .06f), 2) * (1 - Mathf.Abs(x)));
            return Mathf.Clamp01(arms + Mathf.Pow(1 - Mathf.Clamp01(r / .45f), 2) * .8f);
        }),
        ("FX_Pixel", (x, y) => (1 - Smooth(.5f, .62f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(y))))),
        ("FX_Smoke", (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * 3 + 10, y * 3 + 10) * .6f + Mathf.PerlinNoise(x * 7 + 3, y * 7 + 3) * .4f;
            return Mathf.Clamp01((1 - Smooth(.35f, 1f, Len(x, y))) * (.55f + n * .7f));
        }),
        ("FX_Snow", (x, y) =>
        {
            float r = Len(x, y), a = Mathf.Atan2(y, x), best = 0;
            for (int k = 0; k < 6; k++)
            {
                float ang = k * Mathf.PI / 3, d = Mathf.Abs(Mathf.Sin(a - ang)) * r, along = Mathf.Cos(a - ang) * r;
                if (along < 0 || along > .92f) continue;
                float branch = Mathf.Abs(Mathf.Sin(a - ang - .7f)) * r; // small side twigs
                best = Mathf.Max(best, 1 - Smooth(.035f, .07f, d), along > .45f && along < .7f ? 1 - Smooth(.03f, .06f, branch) : 0);
            }
            return Mathf.Max(best, 1 - Smooth(.1f, .14f, r));
        }),
    };

    // name -> (texture or null, additive, tint)
    static readonly (string name, string tex, bool add, Color tint)[] Materials =
    {
        ("M_FX_Glow", "FX_SoftDot", true, Color.white), ("M_FX_Ring", "FX_Ring", true, Color.white), ("M_FX_RingThick", "FX_RingThick", true, Color.white),
        ("M_FX_Dash", "FX_Dash", true, Color.white), ("M_FX_Star", "FX_Star", true, Color.white), ("M_FX_Pixel", "FX_Pixel", true, Color.white),
        ("M_FX_Smoke", "FX_Smoke", false, Color.white), ("M_FX_Snow", "FX_Snow", false, Color.white),
        ("M_FX_Shield", null, true, new Color(.35f, .85f, 1f, .45f)),
    };

    [MenuItem("Tools/AssetGen/Generate VFX")]
    static void Generate()
    {
        Directory.CreateDirectory(Dir + "Textures");
        Directory.CreateDirectory(Dir + "Materials");
        Directory.CreateDirectory(Dir + "Animations");
        const int size = 256;
        foreach (var (name, alpha) in Textures)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(alpha((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1)));
            t.SetPixels(px);
            File.WriteAllBytes($"{Dir}Textures/{name}.png", t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
        }
        AssetDatabase.Refresh();
        foreach (var (name, _) in Textures)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath($"{Dir}Textures/{name}.png");
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        foreach (var (name, tex, add, tint) in Materials)
        {
            var path = $"{Dir}Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetTexture("_BaseMap", tex == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}Textures/{tex}.png"));
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Surface", 1);         // transparent
            m.SetFloat("_Blend", add ? 2 : 0); // additive / alpha
            BaseShaderGUI.SetupMaterialBlendMode(m);
            EditorUtility.SetDirty(m);
        }

        BuildShieldController();
        BuildMergeBurst();
        AssetDatabase.SaveAssets();
        Debug.Log("[VfxGen] Generated textures, materials, shield controller, merge burst.");
    }

    // Pop-in + soft pulse for the shield dome. Bound to the child named like the old shield object so the prefab keeps working.
    static void BuildShieldController()
    {
        const string path = Dir + "Animations/FX_Shield.controller";
        var clipPath = Dir + "Animations/FX_Shield_Pop.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
        clip.ClearCurves();
        const string target = "Mod_FX_Itme_Shield";
        var scale = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.18f, 1.15f), new Keyframe(.3f, 1f), new Keyframe(1.2f, 1.04f), new Keyframe(1.6f, 0));
        foreach (var axis in new[] { "x", "y", "z" }) clip.SetCurve(target, typeof(Transform), "m_LocalScale." + axis, scale);
        clip.SetCurve(target, typeof(MeshRenderer), "material._BaseColor.a",
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(.15f, .55f), new Keyframe(.7f, .35f), new Keyframe(1.6f, 0)));
        EditorUtility.SetDirty(clip);

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
        ctrl.layers[0].stateMachine.defaultState.motion = clip;
        EditorUtility.SetDirty(ctrl);
    }

    static void BuildMergeBurst()
    {
        var root = new GameObject("FX_MergeBurst");
        var ps = root.AddComponent<ParticleSystem>();
        Configure(ps, "M_FX_Star", 14, new Vector2(.35f, .6f), new Vector2(2f, 4f), new Vector2(.15f, .3f), new Color(1f, .9f, .45f));
        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        var g = glow.AddComponent<ParticleSystem>();
        Configure(g, "M_FX_Glow", 1, new Vector2(.25f, .25f), Vector2.zero, new Vector2(1.4f, 1.4f), new Color(1f, .95f, .7f));
        var ring = new GameObject("Ring");
        ring.transform.SetParent(root.transform, false);
        var r = ring.AddComponent<ParticleSystem>();
        Configure(r, "M_FX_Ring", 1, new Vector2(.3f, .3f), Vector2.zero, new Vector2(.4f, .4f), new Color(1f, 1f, 1f, .8f));
        var sol = r.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .3f), new Keyframe(1, 4f)));
        PrefabUtility.SaveAsPrefabAsset(root, VfxPrefabs + "FX_MergeBurst.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    static void Configure(ParticleSystem ps, string mat, int burst, Vector2 life, Vector2 speed, Vector2 size, Color color)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = .6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = color;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 0;
        em.SetBursts(new[] { new ParticleSystem.Burst(0, burst) });
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = .1f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .5f), new GradientAlphaKey(0, 1) });
        col.color = grad;
        if (burst > 1)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = .25f;
            limit.limit = .5f;
        }
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{Dir}Materials/{mat}.mat");
    }

    // After the swap: our textures are single images, not atlases, so drop atlas frame selection; point MergeManager at our burst.
    [MenuItem("Tools/AssetGen/Fix VFX Prefabs")]
    static void FixPrefabs()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { VfxPrefabs.TrimEnd('/') }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            int n = 0;
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var mat = ps.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                if (!mat || !AssetDatabase.GetAssetPath(mat).StartsWith(Dir)) continue;
                var tsa = ps.textureSheetAnimation;
                if (!tsa.enabled) continue;
                tsa.enabled = false;
                n++;
            }
            if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            if (n > 0) Debug.Log($"[VfxGen] {Path.GetFileName(path)}: disabled atlas animation on {n} systems");
        }

        var scene = EditorSceneManager.OpenScene("Assets/MatchThemAllTemplate/Scenes/MainScene.unity", OpenSceneMode.Additive);
        var burst = AssetDatabase.LoadAssetAtPath<ParticleSystem>(VfxPrefabs + "FX_MergeBurst.prefab");
        foreach (var go in scene.GetRootGameObjects())
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!mb || mb.GetType().Name != "MergeManager") continue;
            var so = new SerializedObject(mb);
            so.FindProperty("mergeParticle").objectReferenceValue = burst;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[VfxGen] MergeManager.mergeParticle -> FX_MergeBurst");
        }
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
    }
}
