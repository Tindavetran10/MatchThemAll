using System;
using System.IO;
using UnityEditor;
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
        // Beam / trail: soft across the width (v), constant along the length (u).
        ("FX_Beam", (x, y) => Mathf.Pow(1 - Mathf.Clamp01(Mathf.Abs(y)), 2.5f)),
        // Flame puff: soft blob with a noisy, licking edge.
        ("FX_Flame", (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * 4 + 20, y * 4 + 7);
            return Mathf.Clamp01((1 - Smooth(.25f, .95f, Len(x, y * 1.15f) + (n - .5f) * .35f)) * (.75f + n * .5f));
        }),
        // Ice shard: thin diamond, brighter core.
        ("FX_Shard", (x, y) => Mathf.Clamp01(1 - (Mathf.Abs(x) / .28f + Mathf.Abs(y))) * (.6f + .4f * (1 - Mathf.Abs(y)))),
    };

    // name -> (texture or null, additive, tint)
    static readonly (string name, string tex, bool add, Color tint)[] Materials =
    {
        ("M_FX_Glow", "FX_SoftDot", true, Color.white), ("M_FX_Ring", "FX_Ring", true, Color.white), ("M_FX_RingThick", "FX_RingThick", true, Color.white),
        ("M_FX_Dash", "FX_Dash", true, Color.white), ("M_FX_Star", "FX_Star", true, Color.white), ("M_FX_Pixel", "FX_Pixel", true, Color.white),
        ("M_FX_Smoke", "FX_Smoke", false, Color.white), ("M_FX_Snow", "FX_Snow", false, Color.white),
        ("M_FX_Beam", "FX_Beam", true, Color.white), ("M_FX_Flame", "FX_Flame", true, Color.white), ("M_FX_Shard", "FX_Shard", true, Color.white),
    };

    [MenuItem("Tools/AssetGen/Generate VFX")]
    static void Generate()
    {
        BuildTexturesAndMaterials();
        BuildMergeBurst();
        BuildPowerupFx();
        AssetDatabase.SaveAssets();
        Debug.Log("[VfxGen] Generated textures, materials, merge burst, power-up FX.");
    }

    // Rebuilds only the power-up FX (leaves FX_MergeBurst and its MergeManager reference alone).
    // Rebuilt prefabs can get new internal IDs: re-point the power-up assets / scene afterwards.
    [MenuItem("Tools/AssetGen/Generate Powerup FX")]
    static void GeneratePowerupFx()
    {
        BuildTexturesAndMaterials();
        BuildPowerupFx();
        AssetDatabase.SaveAssets();
        Debug.Log("[VfxGen] Generated power-up FX.");
    }

    static void BuildTexturesAndMaterials()
    {
        Directory.CreateDirectory(Dir + "Textures");
        Directory.CreateDirectory(Dir + "Materials");
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
    }

    // ---------- power-up FX (Prefabs/VFX/Powerups) ----------
    // The game camera looks straight down (screen up = world -Z), so "flat" systems emit in the XZ plane.
    const string PowerupFx = VfxPrefabs + "Powerups/";

    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Dir}Materials/{name}.mat");

    static ParticleSystem Sys(GameObject go, string mat, float duration, int burst, Vector2 life, Vector2 speed, Vector2 size, Color color)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = color;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        var em = ps.emission;
        em.rateOverTime = 0;
        if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0, burst) });
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle; // flat, radial
        sh.radius = .15f;
        sh.rotation = new Vector3(90, 0, 0);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Mat(mat);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        Fade(ps);
        return ps;
    }

    static ParticleSystem Child(GameObject root, string name, string mat, float duration, int burst, Vector2 life, Vector2 speed, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        return Sys(go, mat, duration, burst, life, speed, size, color);
    }

    // Alpha: in fast, hold, out. Optional colour ramp over life (multiplies startColor).
    static void Fade(ParticleSystem ps, params Color[] ramp)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        var keys = ramp.Length == 0 ? new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }
            : System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(ramp, (c, i) => new GradientColorKey(c, i / (float)(ramp.Length - 1))));
        g.SetKeys(keys, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .08f), new GradientAlphaKey(1, .45f), new GradientAlphaKey(0, 1) });
        col.color = g;
    }

    static void Grow(ParticleSystem ps, float from, float to)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, from), new Keyframe(1, to)));
    }

    static void Drag(ParticleSystem ps, float dampen)
    {
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.dampen = dampen;
        limit.limit = .3f;
    }

    static void Streak(ParticleSystem ps, float velocityScale)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = velocityScale;
        r.lengthScale = 1f;
    }

    static void Save(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, PowerupFx + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    static void BuildPowerupFx()
    {
        Color ice = new(.62f, .92f, 1f), iceSoft = new(.85f, .97f, 1f, .55f);
        Color fireYellow = new(1f, .88f, .35f), fireOrange = new(1f, .55f, .15f), fireRed = new(.85f, .18f, .05f);

        // Freeze: hit burst where the laser lands.
        var fb = new GameObject("FX_FreezeBurst");
        var flash = Sys(fb, "M_FX_Glow", .1f, 1, new(.3f, .3f), Vector2.zero, new(3f, 3f), new Color(.7f, .95f, 1f, .9f));
        Grow(flash, .6f, 1.4f);
        var fRing = Child(fb, "Ring", "M_FX_RingThick", .1f, 1, new(.5f, .5f), Vector2.zero, new(.5f, .5f), new Color(.6f, .92f, 1f, .8f));
        Grow(fRing, 1f, 9f);
        var shards = Child(fb, "Shards", "M_FX_Shard", .1f, 18, new(.5f, .8f), new(4f, 8f), new(.25f, .45f), ice);
        Streak(shards, .06f); Drag(shards, .15f);
        var snow = Child(fb, "Snow", "M_FX_Snow", .1f, 26, new(1f, 1.6f), new(1.5f, 4f), new(.15f, .35f), Color.white);
        Drag(snow, .12f);
        var mist = Child(fb, "Mist", "M_FX_Smoke", .1f, 8, new(1f, 1.4f), new(.5f, 1.2f), new(1.2f, 2f), iceSoft);
        Grow(mist, .6f, 1.3f);
        Save(fb, "FX_FreezeBurst");

        // Freeze: an ice sphere cracking when the freeze ends.
        var sh = new GameObject("FX_IceShatter");
        var sShards = Sys(sh, "M_FX_Shard", .1f, 10, new(.4f, .6f), new(2f, 4f), new(.15f, .3f), ice);
        Streak(sShards, .06f); Drag(sShards, .2f);
        Drag(Child(sh, "Snow", "M_FX_Snow", .1f, 12, new(.6f, 1f), new(1f, 2.5f), new(.1f, .22f), Color.white), .15f);
        Save(sh, "FX_IceShatter");

        // Freeze: laser beam (positions set by FreezeEffect).
        var laser = new GameObject("FX_FreezeLaser");
        var lr = laser.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.widthMultiplier = .45f;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.numCapVertices = 4;
        lr.sharedMaterial = Mat("M_FX_Beam");
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        var lg = new Gradient();
        lg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(ice, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        lr.colorGradient = lg;
        lr.enabled = false;
        Save(laser, "FX_FreezeLaser");

        // Freeze: ice sphere placed on the board's edges/corners (mesh from ModelGen, shared palette material).
        var sphere = new GameObject("IceSphere");
        sphere.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/MatchThemAllTemplate/Art/Models/Meshes/Prop_IceSphere.asset");
        var smr = sphere.AddComponent<MeshRenderer>();
        smr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MatchThemAllTemplate/Art/Models/Materials/M_Palette.mat");
        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smr.receiveShadows = false;
        sphere.transform.localScale = Vector3.one * .5f;
        var sparkle = Child(sphere, "Sparkle", "M_FX_Star", 1f, 0, new(.5f, .8f), Vector2.zero, new(.25f, .5f), Color.white);
        var spm = sparkle.main; spm.loop = true; spm.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var spe = sparkle.emission; spe.rateOverTime = 4;
        var sps = sparkle.shape; sps.shapeType = ParticleSystemShapeType.Sphere; sps.radius = 1f;
        Save(sphere, "IceSphere");

        // Bomb (Fan power-up): explosion at the board centre.
        var ex = new GameObject("FX_Explosion");
        var exFlash = Sys(ex, "M_FX_Glow", .1f, 1, new(.18f, .18f), Vector2.zero, new(4.5f, 4.5f), new Color(1f, .95f, .75f));
        Grow(exFlash, .7f, 1.3f);
        var fire = Child(ex, "Fireball", "M_FX_Flame", .1f, 16, new(.35f, .6f), new(1.5f, 4f), new(.9f, 1.6f), Color.white);
        Fade(fire, fireYellow, fireOrange, fireRed); Grow(fire, .7f, 1.3f); Drag(fire, .2f);
        var flames = Child(ex, "Flames", "M_FX_Flame", .1f, 10, new(.5f, .8f), new(.5f, 1.5f), new(1.2f, 2f), Color.white);
        Fade(flames, fireOrange, fireRed); Grow(flames, .8f, 1.2f);
        var smoke = Child(ex, "Smoke", "M_FX_Smoke", .1f, 12, new(.9f, 1.5f), new(.8f, 2.2f), new(1.4f, 2.4f), new Color(.2f, .18f, .17f, .8f));
        var smm = smoke.main; smm.startDelay = .1f;
        Grow(smoke, .6f, 1.5f); Drag(smoke, .1f);
        var embers = Child(ex, "Embers", "M_FX_Star", .1f, 28, new(.5f, .9f), new(4f, 9f), new(.08f, .2f), new Color(1f, .7f, .25f));
        Streak(embers, .05f); Drag(embers, .1f);
        var exRing = Child(ex, "Ring", "M_FX_RingThick", .1f, 1, new(.4f, .4f), Vector2.zero, new(.6f, .6f), new Color(1f, .7f, .3f, .8f));
        Grow(exRing, 1f, 10f);
        Save(ex, "FX_Explosion");

        // Spring: landing puff.
        var puff = new GameObject("FX_LandPuff");
        var pSmoke = Sys(puff, "M_FX_Smoke", .1f, 7, new(.45f, .6f), new(.8f, 1.8f), new(.35f, .6f), new Color(.97f, .97f, .97f, .75f));
        Grow(pSmoke, .7f, 1.4f); Drag(pSmoke, .2f);
        Grow(Child(puff, "Ring", "M_FX_Ring", .1f, 1, new(.3f, .3f), Vector2.zero, new(.3f, .3f), new Color(1f, 1f, 1f, .6f)), 1f, 4f);
        Save(puff, "FX_LandPuff");

        // Spring: flight trail (TrailRenderer, attached to the item by SpringEffect).
        var trail = new GameObject("FX_SpringTrail");
        var tr = trail.AddComponent<TrailRenderer>();
        tr.time = .35f;
        tr.minVertexDistance = .05f;
        tr.widthCurve = new AnimationCurve(new Keyframe(0, .7f), new Keyframe(1, 0));
        tr.sharedMaterial = Mat("M_FX_Beam");
        tr.alignment = LineAlignment.View;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        var tg = new Gradient();
        tg.SetKeys(new[] { new GradientColorKey(new Color(1f, .97f, .7f), 0), new GradientColorKey(fireOrange, 1) },
            new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) });
        tr.colorGradient = tg;
        tr.emitting = false;
        Save(trail, "FX_SpringTrail");

        // Vacuum: tornado funnel from the nozzle into the board (screen up = world -Z).
        var tor = new GameObject("FX_Tornado");
        var core = Sys(tor, "M_FX_Glow", 2.3f, 0, new(.4f, .6f), Vector2.zero, new(.7f, 1f), new Color(.8f, .9f, 1f, .5f));
        var ce = core.emission; ce.rateOverTime = 14;
        var csh = core.shape; csh.shapeType = ParticleSystemShapeType.Sphere; csh.radius = .05f;
        var funnelGo = new GameObject("Funnel");
        funnelGo.transform.SetParent(tor.transform, false);
        funnelGo.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var funnel = Sys(funnelGo, "M_FX_Dash", 2.3f, 0, new(.7f, 1f), new(-2.6f, -1.8f), new(.2f, .38f), new Color(.92f, .97f, 1f, 1f));
        var fm = funnel.main; fm.simulationSpace = ParticleSystemSimulationSpace.Local;
        var fe = funnel.emission; fe.rateOverTime = 170;
        var fsh = funnel.shape; fsh.shapeType = ParticleSystemShapeType.ConeVolume; fsh.angle = 24; fsh.radius = .12f; fsh.length = 3.4f; fsh.rotation = Vector3.zero;
        var vel = funnel.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.orbitalZ = 7f; vel.radial = -.4f;
        Streak(funnel, .18f);
        Grow(funnel, 1f, .4f);
        var dustGo = new GameObject("Dust");
        dustGo.transform.SetParent(tor.transform, false);
        dustGo.transform.localPosition = new Vector3(0, 0, -3.2f);
        dustGo.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var dust = Sys(dustGo, "M_FX_Smoke", 2.3f, 0, new(.8f, 1.2f), new(0f, .2f), new(.8f, 1.4f), new Color(.92f, .94f, 1f, .65f));
        var dm = dust.main; dm.simulationSpace = ParticleSystemSimulationSpace.Local;
        var de = dust.emission; de.rateOverTime = 32;
        var dsh = dust.shape; dsh.shapeType = ParticleSystemShapeType.Circle; dsh.radius = 1.3f; dsh.rotation = Vector3.zero;
        var dv = dust.velocityOverLifetime; dv.enabled = true; dv.space = ParticleSystemSimulationSpace.Local; dv.orbitalZ = 4f;
        Save(tor, "FX_Tornado");
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
