using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Dev-only (not exported): builds the stylized low-poly item/prop meshes we ship, as Mesh assets.
// Each mesh is fitted to the bounds of the model it replaces, so transforms, colliders and tray layout stay identical.
// Flat-shaded on purpose (low-poly look); one submesh per material.
static class ModelGen
{
    const string MeshDir = "Assets/MatchThemAllTemplate/Art/Models/Meshes/";
    const string MatDir = "Assets/MatchThemAllTemplate/Art/Models/Materials/";

    // ---------- materials ----------
    // name -> (color, smoothness, metallic, emission)
    static readonly Dictionary<string, (string hex, float smooth, float metal, float emit)> Mats = new()
    {
        ["Gold"] = ("FFC23A", .5f, .5f, 0), ["Copper"] = ("E07A35", .6f, .4f, 0), ["Black"] = ("2A2D3A", .55f, 0, 0),
        ["Silver"] = ("D5DCE6", .8f, .7f, 0), ["Glass"] = ("E3F4FF", .95f, 0, 0), ["Liquid"] = ("3D6BFF", .85f, 0, .25f),
        ["Ruby"] = ("F0384F", .9f, .1f, .1f), ["Bone"] = ("F2EBD9", .35f, 0, 0), ["Ink"] = ("1E1A2B", .2f, 0, 0),
        ["Tan"] = ("C8955A", .3f, 0, 0), ["Spark"] = ("FFD84A", .5f, 0, 1.5f), ["Glow"] = ("3FE0FF", .7f, 0, 1.2f),
        ["Steel"] = ("566173", .6f, .5f, 0), ["Orange"] = ("FF8A2A", .5f, 0, 0), ["Teal"] = ("1FB5A8", .6f, .1f, 0),
        ["Blue"] = ("2F7BFF", .7f, .1f, .3f), ["Cork"] = ("A0683A", .25f, 0, 0), ["Pink"] = ("FF6F91", .55f, .05f, 0),
    };

    static Material Mat(string name)
    {
        var path = MatDir + "M_Item_" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        var (hex, smooth, metal, emit) = Mats[name];
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (emit > 0)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emit);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------- mesh builder ----------
    class MB
    {
        readonly List<Vector3> v = new(), n = new();
        readonly List<List<int>> subs = new();
        public readonly List<string> Materials = new();

        int Sub(string mat)
        {
            int i = Materials.IndexOf(mat);
            if (i >= 0) return i;
            Materials.Add(mat);
            subs.Add(new List<int>());
            return subs.Count - 1;
        }

        // Flat triangle; winding flipped if needed so the face points away from `inside`.
        public void Tri(string mat, Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
        {
            var nn = Vector3.Cross(b - a, c - a);
            if (nn.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(nn, (a + b + c) / 3 - inside) < 0) { (b, c) = (c, b); nn = -nn; }
            nn.Normalize();
            var s = subs[Sub(mat)];
            foreach (var p in new[] { a, b, c }) { s.Add(v.Count); v.Add(p); n.Add(nn); }
        }

        public void Quad(string mat, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
        {
            Tri(mat, a, b, c, inside);
            Tri(mat, a, c, d, inside);
        }

        // Revolve a (radius, height, material) profile around local Y, then place it with `m`.
        // Profiles run counter-clockwise in (r, y) (bottom axis -> out -> up -> top axis), so the outward
        // normal of each segment is exact even in concave spots (coin groove, potion neck).
        public void Lathe(Matrix4x4 m, int segs, params (float r, float y, string mat)[] prof)
        {
            for (int i = 0; i < prof.Length - 1; i++)
            {
                var n2 = new Vector2(prof[i + 1].y - prof[i].y, -(prof[i + 1].r - prof[i].r)).normalized;
                for (int k = 0; k < segs; k++)
                {
                    float a0 = k * Mathf.PI * 2 / segs, a1 = (k + 1) * Mathf.PI * 2 / segs, am = (a0 + a1) / 2;
                    Vector3 P(int j, float a) => m.MultiplyPoint3x4(new Vector3(prof[j].r * Mathf.Cos(a), prof[j].y, prof[j].r * Mathf.Sin(a)));
                    Vector3 a = P(i, a0), b = P(i, a1), c = P(i + 1, a1), d = P(i + 1, a0);
                    var outward = m.MultiplyVector(new Vector3(n2.x * Mathf.Cos(am), n2.y, n2.x * Mathf.Sin(am)));
                    // Faces get the material of the upper profile point.
                    Quad(prof[i + 1].mat, a, b, c, d, (a + b + c + d) / 4 - outward);
                }
            }
        }

        public void Sphere(Matrix4x4 m, string mat, int segs = 16, int rings = 10)
        {
            var prof = new (float, float, string)[rings + 1];
            for (int i = 0; i <= rings; i++)
            {
                float t = Mathf.PI * i / rings;
                prof[i] = (Mathf.Sin(t), -Mathf.Cos(t), mat);
            }
            Lathe(m, segs, prof);
        }

        public void Cylinder(Matrix4x4 m, string mat, float r, float h, int segs = 16, string capMat = null) =>
            Lathe(m, segs, (0, 0, capMat ?? mat), (r, 0, capMat ?? mat), (r, h, mat), (0, h, capMat ?? mat));

        public void Torus(Matrix4x4 m, string mat, float R, float r, int segs = 20, int sides = 8)
        {
            for (int i = 0; i < segs; i++)
            for (int j = 0; j < sides; j++)
            {
                Vector3 P(int a, int b)
                {
                    float u = a * Mathf.PI * 2 / segs, w = b * Mathf.PI * 2 / sides;
                    return new Vector3((R + r * Mathf.Cos(w)) * Mathf.Cos(u), r * Mathf.Sin(w), (R + r * Mathf.Cos(w)) * Mathf.Sin(u));
                }
                float uc = (i + .5f) * Mathf.PI * 2 / segs;
                var inside = m.MultiplyPoint3x4(new Vector3(R * Mathf.Cos(uc), 0, R * Mathf.Sin(uc)));
                Quad(mat, m.MultiplyPoint3x4(P(i, j)), m.MultiplyPoint3x4(P(i + 1, j)), m.MultiplyPoint3x4(P(i + 1, j + 1)), m.MultiplyPoint3x4(P(i, j + 1)), inside);
            }
        }

        public void Box(Matrix4x4 m, string mat)
        {
            var c = new Vector3[8];
            for (int i = 0; i < 8; i++) c[i] = m.MultiplyPoint3x4(new Vector3((i & 1) - .5f, ((i >> 1) & 1) - .5f, ((i >> 2) & 1) - .5f));
            var o = m.MultiplyPoint3x4(Vector3.zero);
            Quad(mat, c[0], c[1], c[3], c[2], o); Quad(mat, c[4], c[5], c[7], c[6], o);
            Quad(mat, c[0], c[1], c[5], c[4], o); Quad(mat, c[2], c[3], c[7], c[6], o);
            Quad(mat, c[0], c[2], c[6], c[4], o); Quad(mat, c[1], c[3], c[7], c[5], o);
        }

        // Faceted gem from a closed 2D outline (XY): outline ring at z=0, inset ring at ±z1, apex at ±z2.
        public void Gem2D(string mat, Vector2[] outline, float inset, float z1, float z2)
        {
            Vector2 c = Vector2.zero;
            float area = 0;
            for (int i = 0; i < outline.Length; i++)
            {
                c += outline[i];
                var q = outline[(i + 1) % outline.Length];
                area += outline[i].x * q.y - q.x * outline[i].y;
            }
            c /= outline.Length;
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 p0 = outline[i], p1 = outline[(i + 1) % outline.Length], e = p1 - p0;
                // Outward from the edge itself (not from the centre), so concave spots like the heart's notch stay correct.
                var n2 = (area > 0 ? new Vector2(e.y, -e.x) : new Vector2(-e.y, e.x)).normalized;
                Vector3 o0 = p0, o1 = p1;
                Vector3 i0 = (Vector3)Vector2.Lerp(c, p0, inset) + Vector3.forward * z1 * side;
                Vector3 i1 = (Vector3)Vector2.Lerp(c, p1, inset) + Vector3.forward * z1 * side;
                var outward = new Vector3(n2.x, n2.y, side * .5f);
                Quad(mat, o0, o1, i1, i0, (o0 + o1 + i0 + i1) / 4 - outward);
                var apex = new Vector3(c.x, c.y, z2 * side);
                Tri(mat, i0, i1, apex, (i0 + i1 + apex) / 3 - Vector3.forward * side);
            }
        }

        public Mesh Build(string name, Vector3 fitSize, Vector3 fitCenter)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            var b = new Bounds(v[0], Vector3.zero);
            foreach (var p in v) b.Encapsulate(p);
            var s = new Vector3(fitSize.x / b.size.x, fitSize.y / b.size.y, fitSize.z / b.size.z);
            if (fitSize == Vector3.zero) { s = Vector3.one; b.center = Vector3.zero; fitCenter = Vector3.zero; } // keep authored size + pivot
            var verts = new Vector3[v.Count];
            var norms = new Vector3[v.Count];
            for (int i = 0; i < v.Count; i++)
            {
                verts[i] = Vector3.Scale(v[i] - b.center, s) + fitCenter;
                norms[i] = new Vector3(n[i].x / s.x, n[i].y / s.y, n[i].z / s.z).normalized; // inverse-transpose of the fit scale
            }
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) mesh.SetTriangles(subs[i], i);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }

    static Matrix4x4 T(float x, float y, float z) => Matrix4x4.Translate(new Vector3(x, y, z));
    static Matrix4x4 TRS(Vector3 p, Vector3 euler, Vector3 s) => Matrix4x4.TRS(p, Quaternion.Euler(euler), s);
    static Matrix4x4 S(float x, float y, float z) => Matrix4x4.Scale(new Vector3(x, y, z));

    // ---------- models (fit size/center = the replaced mesh's bounds) ----------
    static IEnumerable<(string name, Vector3 size, Vector3 center, Action<MB> build)> Models()
    {
        yield return ("Item_Battery", new(.89f, 2.49f, .89f), new(0, .03f, 0), b =>
        {
            b.Lathe(Matrix4x4.identity, 16, (0, 0, "Silver"), (.42f, 0, "Silver"), (.445f, .06f, "Black"), (.445f, 1.6f, "Black"),
                (.445f, 1.62f, "Copper"), (.445f, 2.3f, "Copper"), (.4f, 2.36f, "Silver"), (0, 2.36f, "Silver"));
            b.Cylinder(T(0, 2.36f, 0), "Silver", .17f, .13f, 12);
        });
        yield return ("Item_BluePotion", new(1.68f, 2.66f, 1.68f), new(0, .5f, 0), b =>
        {
            var prof = new List<(float, float, string)>();
            for (int i = 0; i <= 9; i++)
            {
                float t = Mathf.PI * i / 9 * .86f; // stop short of the top to leave room for the neck
                float y = -Mathf.Cos(t) * .84f + .84f;
                prof.Add((Mathf.Sin(t) * .84f, y, y < .95f ? "Liquid" : "Glass"));
            }
            prof.Add((.26f, 1.68f, "Glass")); prof.Add((.26f, 2.2f, "Glass")); prof.Add((.33f, 2.26f, "Glass")); prof.Add((.33f, 2.32f, "Glass"));
            prof.Add((0, 2.32f, "Glass"));
            b.Lathe(Matrix4x4.identity, 16, prof.ToArray());
            b.Cylinder(T(0, 2.2f, 0), "Cork", .22f, .46f, 10);
        });
        yield return ("Item_Bomb", new(1.92f, 2.44f, 1.91f), new(0, .25f, 0), b =>
        {
            b.Sphere(S(.96f, .96f, .96f), "Black", 18, 12);
            b.Cylinder(T(0, .78f, 0), "Steel", .32f, .3f, 12);
            for (int i = 0; i < 5; i++) // fuse: a bent chain of short segments
            {
                float t = i / 5f;
                b.Box(TRS(new Vector3(t * .35f, 1.12f + t * .22f, 0), new Vector3(0, 0, -25 - t * 40), new Vector3(.12f, .2f, .12f)), "Tan");
            }
            b.Sphere(TRS(new Vector3(.38f, 1.43f, 0), Vector3.zero, Vector3.one * .14f), "Spark", 8, 6);
        });
        yield return ("Item_Coin", new(2.57f, 2.58f, .63f), Vector3.zero, b =>
        {
            var face = Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)); // lathe axis -> Z
            b.Lathe(face, 24, (0, -.24f, "Gold"), (.95f, -.24f, "Gold"), (1.05f, -.31f, "Gold"), (1.28f, -.25f, "Gold"), (1.28f, .25f, "Gold"),
                (1.05f, .31f, "Gold"), (.95f, .24f, "Gold"), (0, .24f, "Gold"));
            var star = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5, r = i % 2 == 0 ? .7f : .3f;
                star[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            b.Gem2D("Copper", star, .6f, .27f, .3f);
        });
        yield return ("Item_Diamond", new(2.76f, 2.42f, 2.63f), new(-.03f, -.22f, -.14f), b =>
            b.Lathe(Matrix4x4.identity, 5, (0, -1.6f, "Ruby"), (1.38f, 0, "Ruby"), (1.0f, .55f, "Ruby"), (0, .62f, "Ruby")));
        yield return ("Item_HeartGem", new(2.63f, 2.51f, .54f), new(0, -.06f, 0), b =>
        {
            var heart = new List<Vector2>();
            for (int i = 0; i < 28; i++)
            {
                float t = i * Mathf.PI * 2 / 28;
                heart.Add(new Vector2(16 * Mathf.Pow(Mathf.Sin(t), 3), 13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t)) / 16f);
            }
            b.Gem2D("Ruby", heart.ToArray(), .6f, .22f, .3f);
        });
        yield return ("Item_Skull", new(1.79f, 1.75f, 1.82f), new(0, -.01f, .04f), b =>
        {
            b.Sphere(TRS(new Vector3(0, .2f, 0), Vector3.zero, new Vector3(.9f, .82f, .92f)), "Bone", 16, 10);
            b.Box(TRS(new Vector3(0, -.5f, .3f), new Vector3(12, 0, 0), new Vector3(1.0f, .5f, .8f)), "Bone");
            foreach (var x in new[] { -.34f, .34f }) b.Sphere(TRS(new Vector3(x, .12f, .72f), Vector3.zero, new Vector3(.24f, .26f, .22f)), "Ink", 10, 6);
            b.Lathe(TRS(new Vector3(0, -.2f, .86f), new Vector3(90, 0, 0), new Vector3(1, 1, 1)), 3, (0, 0, "Ink"), (.12f, 0, "Ink"), (0, .1f, "Ink"));
            for (int i = -1; i <= 1; i++) b.Box(TRS(new Vector3(i * .2f, -.6f, .72f), Vector3.zero, new Vector3(.05f, .24f, .06f)), "Ink");
        });

        // Vacuum power-up (Prefabs/PowerUps/Vacuum.prefab): two rigid parts animated by Idle/Activate. Authored size, pivot at the base.
        yield return ("Prop_VacuumBody", Vector3.zero, Vector3.zero, b =>
        {
            b.Lathe(Matrix4x4.identity, 18, (0, 0, "Black"), (.62f, 0, "Black"), (.66f, .12f, "Black"), (.66f, .2f, "Pink"), (.62f, 1.55f, "Pink"),
                (.5f, 1.75f, "Pink"), (.3f, 1.85f, "Steel"), (0, 1.85f, "Steel"));
            b.Torus(T(0, .9f, 0), "Teal", .645f, .05f, 24, 6);
            b.Torus(TRS(new Vector3(0, 1.95f, -.3f), new Vector3(90, 0, 0), Vector3.one), "Steel", .3f, .06f, 18, 6);
        });
        yield return ("Prop_VacuumNozzle", Vector3.zero, Vector3.zero, b =>
        {
            for (int i = 0; i <= 10; i++) // corrugated hose curving up and forward
            {
                float t = i / 10f;
                var p = new Vector3(0, t * 1.2f, Mathf.Sin(t * Mathf.PI * .5f) * .35f);
                b.Torus(TRS(p, new Vector3(t * 25, 0, 0), Vector3.one), "Teal", .15f, .06f, 14, 6);
            }
            b.Lathe(TRS(new Vector3(0, 1.22f, .38f), new Vector3(25, 0, 0), Vector3.one), 16, (0, 0, "Steel"), (.17f, 0, "Steel"), (.19f, .22f, "Teal"),
                (.44f, .52f, "Teal"), (.44f, .6f, "Glow"), (0, .6f, "Glow"));
        });

        yield return ("FX_ShieldDome", Vector3.one, Vector3.zero, b => b.Sphere(Matrix4x4.identity, "Glow", 14, 9)); // replaces the 4Tudio shield mesh

        // Power-up props (MainScene)
        yield return ("Prop_SpringPad", new(28, 18.4f, 28), new(0, 9.2f, 0), b =>
        {
            b.Lathe(Matrix4x4.identity, 20, (0, 0, "Steel"), (1, 0, "Steel"), (1, .22f, "Steel"), (.9f, .3f, "Steel"), (0, .3f, "Blue"));
            b.Torus(T(0, .28f, 0), "Glow", .92f, .05f, 24, 6);
            for (int i = 0; i < 4; i++) b.Torus(T(0, .42f + i * .14f, 0), "Silver", .42f, .05f, 16, 6);
            b.Cylinder(T(0, .95f, 0), "Steel", .55f, .08f, 16, "Blue");
            b.Lathe(T(0, 1.05f, 0), 4, (0, 0, "Glow"), (.32f, 0, "Glow"), (0, .32f, "Glow"));
        });
        yield return ("Prop_FanCanister", new(.15f, .25f, .13f), new(.01f, .07f, 0), b =>
        {
            b.Lathe(Matrix4x4.identity, 16, (0, 0, "Steel"), (.5f, 0, "Steel"), (.55f, .1f, "Steel"), (.55f, .55f, "Steel"), (.555f, .6f, "Orange"),
                (.555f, .85f, "Orange"), (.55f, .9f, "Steel"), (.55f, 1.3f, "Steel"), (.42f, 1.42f, "Teal"), (0, 1.48f, "Teal"));
            b.Cylinder(T(0, 1.45f, 0), "Silver", .14f, .16f, 10);
            b.Torus(TRS(new Vector3(.32f, 1.62f, 0), new Vector3(90, 0, 0), Vector3.one), "Silver", .2f, .04f, 14, 6);
        });
        yield return ("Prop_FreezeBlaster", new(.13f, .27f, .42f), new(0, .14f, .07f), b =>
        {
            var alongZ = new Vector3(90, 0, 0);
            b.Lathe(TRS(new Vector3(0, .6f, -.9f), alongZ, Vector3.one), 14, (0, 0, "Teal"), (.3f, 0, "Teal"), (.34f, .5f, "Teal"), (.3f, 1.1f, "Teal"), (.18f, 1.2f, "Steel"), (0, 1.2f, "Steel"));
            for (int i = 0; i < 3; i++) b.Torus(TRS(new Vector3(0, .6f, .45f + i * .17f), alongZ, Vector3.one), "Orange", .2f - i * .02f, .06f, 14, 6);
            b.Sphere(TRS(new Vector3(0, .6f, 1.0f), Vector3.zero, Vector3.one * .16f), "Glow", 10, 6);
            b.Box(TRS(new Vector3(0, .1f, -.55f), new Vector3(-18, 0, 0), new Vector3(.22f, .75f, .3f)), "Black");
            b.Box(TRS(new Vector3(0, .98f, -.35f), new Vector3(0, 0, 0), new Vector3(.06f, .22f, .5f)), "Orange");
        });
    }

    [MenuItem("Tools/AssetGen/Generate Models")]
    static void Generate()
    {
        System.IO.Directory.CreateDirectory(MeshDir);
        System.IO.Directory.CreateDirectory(MatDir);
        foreach (var name in Mats.Keys) Mat(name);
        foreach (var (name, size, center, build) in Models())
        {
            var b = new MB();
            build(b);
            var mesh = b.Build(name, size, center);
            var path = MeshDir + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing)
            {
                // Rewrite in place via the Mesh API (keeps the GUID; CopySerialized left the live/GPU mesh stale).
                existing.Clear();
                existing.indexFormat = mesh.indexFormat;
                existing.vertices = mesh.vertices;
                existing.normals = mesh.normals;
                existing.subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++) existing.SetTriangles(mesh.GetTriangles(i), i);
                existing.RecalculateBounds();
                existing.RecalculateTangents();
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            else AssetDatabase.CreateAsset(mesh, path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[ModelGen] Generated meshes + materials.");
    }

    [MenuItem("Tools/AssetGen/Wire Models")]
    static void Wire()
    {
        foreach (var n in new[] { "Battery", "BluePotion", "Bomb", "Coin", "Diamond", "HeartGem", "Skull" })
        {
            var path = $"Assets/MatchThemAllTemplate/_START_HERE/Items/Item_{n}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var r = root.transform.Find("Renderer").gameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.UnpackPrefabInstance(r, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Apply(r, "Item_" + n);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        const string scenePath = "Assets/MatchThemAllTemplate/Scenes/MainScene.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        var props = new Dictionary<string, string>
        {
            ["Spring Spot/ShieldGenerator"] = "Prop_SpringPad",
            ["Fan Spot/Cosmic_Retro_Grenades_Pack_8"] = "Prop_FanCanister",
            ["Freeze Gun Spot/Cosmic_Retro_Blaster_11"] = "Prop_FreezeBlaster",
        };
        foreach (var go in scene.GetRootGameObjects())
        foreach (var kv in props)
        {
            var zone = go.transform.Find("Main Camera/Powerup Zone");
            var t = zone ? zone.Find(kv.Key) : null;
            if (!t) continue;
            if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Apply(t.gameObject, kv.Value);
            t.name = kv.Value.Replace("Prop_", "");
            // Leftover detail meshes from the old model: keep the objects (they may be referenced, e.g. a muzzle), drop the meshes.
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
                if (mf.transform != t) { UnityEngine.Object.DestroyImmediate(mf.GetComponent<MeshRenderer>()); UnityEngine.Object.DestroyImmediate(mf); }
            Debug.Log($"[ModelGen] wired {t.name}");
        }
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.SaveAssets();
    }

    // Swaps the course's skinned vacuum for our two rigid parts and rewrites its two clips in place
    // (same .anim GUIDs, so Vacuum.controller is untouched). Keeps the TriggerPowerupStart event timing.
    [MenuItem("Tools/AssetGen/Wire Vacuum")]
    static void WireVacuum()
    {
        const string prefab = "Assets/MatchThemAllTemplate/Prefabs/PowerUps/Vacuum.prefab";
        const string anims = "Assets/MatchThemAllTemplate/Art/Animations/Powerups/";
        var nozzlePivot = new Vector3(0, 1.8f, .25f);

        var root = PrefabUtility.LoadPrefabContents(prefab);
        foreach (var old in new[] { "Armature", "Vacuum Complete" })
            if (root.transform.Find(old)) UnityEngine.Object.DestroyImmediate(root.transform.Find(old).gameObject);
        GameObject Part(string name, string model, Vector3 pos)
        {
            var t = root.transform.Find(name);
            var go = t ? t.gameObject : new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.layer = root.layer;
            Apply(go, model);
            return go;
        }
        root.GetComponent<Animator>().avatar = null; // old avatar came from the course model; our clips are generic transform paths
        Part("Body", "Prop_VacuumBody", Vector3.zero);
        var nozzle = Part("Nozzle", "Prop_VacuumNozzle", nozzlePivot);

        // Suck point = centre of the nozzle mouth (its Glow cap), parented to the nozzle so it follows the animation
        // like the old rig's end bone did. Reused by name so its fileID (and the scene reference) survives reruns.
        var suck = nozzle.transform.Find("SuckPoint") ?? new GameObject("SuckPoint").transform;
        suck.SetParent(nozzle.transform, false);
        var nozzleMesh = MeshFor("Prop_VacuumNozzle");
        var mouthTris = nozzleMesh.GetTriangles(Array.FindIndex(MaterialsFor("Prop_VacuumNozzle"), m => m.name.EndsWith("Glow")));
        var mouth = Vector3.zero;
        foreach (var i in mouthTris) mouth += nozzleMesh.vertices[i];
        suck.localPosition = mouth / mouthTris.Length;
        PrefabUtility.SaveAsPrefabAsset(root, prefab);
        PrefabUtility.UnloadPrefabContents(root);

        void Key(AnimationClip clip, string path, string prop, params (float t, float v)[] keys)
        {
            var curve = new AnimationCurve();
            foreach (var (t, v) in keys) curve.AddKey(t, v);
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(anims + "Idle.anim");
        idle.ClearCurves();
        Key(idle, "Body", "m_LocalScale.y", (0, 1), (1, 1.025f), (2, 1));
        Key(idle, "Body", "m_LocalScale.x", (0, 1), (1, .99f), (2, 1));
        Key(idle, "Body", "m_LocalScale.z", (0, 1), (1, .99f), (2, 1));
        Key(idle, "Nozzle", "localEulerAnglesRaw.x", (0, 0), (1, -4), (2, 0));
        Key(idle, "Nozzle", "m_LocalPosition.y", (0, nozzlePivot.y), (1, nozzlePivot.y * 1.025f), (2, nozzlePivot.y)); // rides the body squash
        var idleSettings = AnimationUtility.GetAnimationClipSettings(idle);
        idleSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(idle, idleSettings);
        AnimationUtility.SetAnimationEvents(idle, new AnimationEvent[0]);

        var act = AssetDatabase.LoadAssetAtPath<AnimationClip>(anims + "Activate.anim");
        act.ClearCurves();
        // Wind-up squash, snap forward at the event, suction shake, settle.
        var bodyY = new List<(float, float)> { (0, 1), (.45f, .86f), (.62f, 1.12f), (.7f, 1) };
        var bodyXZ = new List<(float, float)> { (0, 1), (.45f, 1.09f), (.62f, .95f), (.7f, 1) };
        var noz = new List<(float, float)> { (0, 0), (.45f, -22), (.62f, 14), (.7f, 10) };
        for (float t = .8f; t < 1.8f; t += .1f)
        {
            bool odd = Mathf.RoundToInt(t * 10) % 2 == 1;
            bodyY.Add((t, odd ? 1.025f : .985f));
            noz.Add((t, odd ? 12 : 7));
        }
        bodyY.Add((1.95f, 1)); bodyY.Add((2.167f, 1));
        bodyXZ.Add((1.95f, 1)); bodyXZ.Add((2.167f, 1));
        noz.Add((1.95f, 0)); noz.Add((2.167f, 0));
        Key(act, "Body", "m_LocalScale.y", bodyY.ToArray());
        Key(act, "Body", "m_LocalScale.x", bodyXZ.ToArray());
        Key(act, "Body", "m_LocalScale.z", bodyXZ.ToArray());
        Key(act, "Nozzle", "localEulerAnglesRaw.x", noz.ToArray());
        Key(act, "Nozzle", "m_LocalPosition.y", bodyY.ConvertAll(k => (k.Item1, k.Item2 * nozzlePivot.y)).ToArray()); // rides the body squash
        AnimationUtility.SetAnimationEvents(act, new[] { new AnimationEvent { functionName = "TriggerPowerupStart", time = .667f } });
        EditorUtility.SetDirty(idle);
        EditorUtility.SetDirty(act);
        AssetDatabase.SaveAssets();
        // PowerupManager.vacuumSuckPosition used to point at the old rig's end bone; point it at our SuckPoint.
        const string scenePath = "Assets/MatchThemAllTemplate/Scenes/MainScene.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(scenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        Transform sceneSuck = null;
        MonoBehaviour manager = null;
        foreach (var go in scene.GetRootGameObjects())
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!mb) continue;
            if (mb.GetType().Name == "Vacuum") sceneSuck = mb.transform.Find("Nozzle/SuckPoint");
            if (mb.GetType().Name == "PowerupManager") manager = mb;
        }
        if (!sceneSuck || !manager) Debug.LogError($"[ModelGen] Couldn't wire vacuumSuckPosition (suck point {sceneSuck}, manager {manager}).");
        else
        {
            var so = new SerializedObject(manager);
            so.FindProperty("vacuumSuckPosition").objectReferenceValue = sceneSuck;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        if (openedHere) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[ModelGen] Vacuum rewired + clips rewritten.");
    }

    static void Apply(GameObject go, string model)
    {
        var mesh = MeshFor(model);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterials = MaterialsFor(model);
        if (go.TryGetComponent<MeshCollider>(out var mc)) mc.sharedMesh = mesh;
    }

    // Contact sheet of every model for a quick visual check (written next to the project, not into Assets).
    [MenuItem("Tools/AssetGen/Preview Models")]
    static void Preview()
    {
        var names = new List<string>();
        foreach (var m in Models()) names.Add(m.name);
        const int cell = 192;
        int cols = 5, rows = (names.Count + cols - 1) / cols;
        var sheet = new Texture2D(cell * cols, cell * rows, TextureFormat.RGBA32, false);
        var pru = new PreviewRenderUtility();
        pru.camera.fieldOfView = 30;
        pru.camera.clearFlags = CameraClearFlags.SolidColor;
        pru.camera.backgroundColor = new Color(.35f, .38f, .45f);
        pru.lights[0].intensity = 1.3f;
        pru.lights[0].transform.rotation = Quaternion.Euler(35, -30, 0);
        pru.lights[1].intensity = .6f;
        pru.ambientColor = new Color(.45f, .45f, .5f);
        for (int i = 0; i < names.Count; i++)
        {
            var mesh = UnityEngine.Object.Instantiate(MeshFor(names[i])); // fresh copy: never draw a stale GPU buffer
            var mats = MaterialsFor(names[i]);
            var b = mesh.bounds;
            float r = b.extents.magnitude;
            var rot = Quaternion.Euler(20, -35, 0);
            pru.BeginStaticPreview(new Rect(0, 0, cell, cell));
            pru.camera.transform.SetPositionAndRotation(rot * new Vector3(0, 0, -r * 4f), rot);
            pru.camera.nearClipPlane = r * .1f;
            pru.camera.farClipPlane = r * 10;
            for (int s = 0; s < mesh.subMeshCount; s++) pru.DrawMesh(mesh, Matrix4x4.Translate(-b.center), mats[s], s);
            pru.camera.Render();
            var tex = pru.EndStaticPreview();
            sheet.SetPixels((i % cols) * cell, (rows - 1 - i / cols) * cell, cell, cell, tex.GetPixels());
        }
        pru.Cleanup();
        var path = System.IO.Path.GetFullPath("Library/ModelGenPreview.png");
        System.IO.File.WriteAllBytes(path, sheet.EncodeToPNG());
        Debug.Log("[ModelGen] preview: " + path);
    }

    // Goal-card icons: rendered from our meshes into the existing icon files (same GUIDs, so every reference keeps working).
    [MenuItem("Tools/AssetGen/Render Item Icons")]
    static void RenderIcons()
    {
        const string icons = "Assets/MatchThemAllTemplate/Art/Sprites/Icons/";
        var jobs = new (string file, string model, int size)[]
        {
            ("icon_item_battery", "Item_Battery", 768), ("icon_item_bluepotion", "Item_BluePotion", 768), ("Trash/icon_item_bluepotion", "Item_BluePotion", 768),
            ("icon_potion", "Item_BluePotion", 256), ("icon_item_bomb", "Item_Bomb", 768), ("icon_item_coin", "Item_Coin", 768),
            ("icon_item_diamond", "Item_Diamond", 768), ("icon_item_heartgem", "Item_HeartGem", 768), ("icon_heartgem", "Item_HeartGem", 256),
            ("icon_item_skull", "Item_Skull", 768),
        };
        var pru = new PreviewRenderUtility();
        pru.camera.orthographic = true;
        pru.camera.clearFlags = CameraClearFlags.SolidColor;
        pru.lights[0].intensity = 1.25f;
        pru.lights[0].transform.rotation = Quaternion.Euler(40, -35, 0);
        pru.lights[1].intensity = .55f;
        pru.ambientColor = new Color(.5f, .5f, .55f);
        foreach (var (file, model, size) in jobs)
        {
            var mesh = UnityEngine.Object.Instantiate(MeshFor(model)); // fresh copy: never draw a stale GPU buffer
            var mats = MaterialsFor(model);
            // Yaw 180 so the model's +Z face (skull face, coin star) looks at the camera; small tilt for depth.
            var rot = Quaternion.Euler(12, 200, model == "Item_Battery" ? -28 : -8);
            var m = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one) * Matrix4x4.Translate(-mesh.bounds.center);
            var b = GeometryUtility.CalculateBounds(mesh.vertices, m);
            pru.camera.orthographicSize = Mathf.Max(b.extents.x, b.extents.y) * 1.12f;
            pru.camera.transform.SetPositionAndRotation(b.center + new Vector3(0, 0, -10), Quaternion.identity);
            pru.camera.nearClipPlane = .1f;
            pru.camera.farClipPlane = 30;
            Texture2D Shot(Color bg, Material only)
            {
                pru.camera.backgroundColor = bg;
                pru.BeginStaticPreview(new Rect(0, 0, size, size));
                for (int s = 0; s < mesh.subMeshCount; s++) pru.DrawMesh(mesh, m, only ? only : mats[s], s);
                pru.camera.Render();
                return pru.EndStaticPreview();
            }
            // Alpha from a flat silhouette (glossy materials reflect the background, so a black/white
            // difference would read reflections as transparency). Colour from a neutral-grey render, un-blended.
            var grey = new Color(.5f, .5f, .5f);
            var colour = Shot(grey, null).GetPixels();
            var mask = Shot(Color.black, Silhouette).GetPixels();
            var px = new Color[colour.Length];
            for (int i = 0; i < px.Length; i++)
            {
                float a = mask[i].r;
                if (a < .002f) { px[i] = Color.clear; continue; }
                var c = (colour[i] - grey * (1 - a)) / a;
                px[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), a);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            System.IO.File.WriteAllBytes(icons + file + ".png", tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        pru.Cleanup();
        UnityEngine.Object.DestroyImmediate(silhouette);
        silhouette = null;
        AssetDatabase.Refresh();
        Debug.Log($"[ModelGen] Rendered {jobs.Length} item icons.");
    }

    static Material silhouette;
    static Material Silhouette => silhouette ??= new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.white };

    public static Mesh MeshFor(string name) => AssetDatabase.LoadAssetAtPath<Mesh>(MeshDir + name + ".asset");

    // Renderer material array in submesh order (rebuilt from the model definition; cheap).
    public static Material[] MaterialsFor(string name)
    {
        foreach (var (n, _, _, build) in Models())
            if (n == name) { var b = new MB(); build(b); return b.Materials.ConvertAll(Mat).ToArray(); }
        throw new ArgumentException(name);
    }
}
