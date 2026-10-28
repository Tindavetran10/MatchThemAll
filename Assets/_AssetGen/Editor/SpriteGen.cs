using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Dev-only (not exported): draws every UI sprite we ship, from signed-distance shapes + recolored Kenney (CC0) glyphs.
// Output mirrors the old GUIPackCartoon folder layout under Art/UI/Sprites so swap-map lines stay 1:1.
// Pixel space, origin bottom-left (Texture2D convention).
static class SpriteGen
{
    const string Out = "Assets/MatchThemAllTemplate/Art/UI/Sprites/";
    const string Glyphs = "Assets/_AssetGen/KenneyGlyphs/";

    delegate float Sdf(float x, float y);
    delegate float Cov(float x, float y);

    // ---------- palette ----------
    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
    static readonly Color Blue = Hex("2F92EA"), Teal = Hex("08BED8"), BlueTeal = Hex("17A8CB"), Green = Hex("6CCF12"),
        Orange = Hex("FE8A22"), Pink = Hex("F2709D"), Purple = Hex("CF66EA"), Red = Hex("F2405A"), Yellow = Hex("F9AA06"),
        Light = Hex("E7EAED"), Gray = Hex("ADB5BD"), Gold = Hex("FFC52E"), Brown = Hex("C07A3C"), Bone = Hex("F3EEDF"),
        Ink = Hex("2B2F45"), White = Color.white, Clear = new(1, 1, 1, 0);
    static Color Dark(Color c, float k = .42f) => A(Color.Lerp(c, Hex("1E1430"), k), c.a);
    static Color Lite(Color c, float k = .35f) => A(Color.Lerp(c, Color.white, k), c.a);
    static Color A(Color c, float a) { c.a = a; return c; }

    // ---------- canvas ----------
    class Canvas
    {
        public readonly int W, H;
        readonly Color[] px;
        public Canvas(int w, int h) { W = w; H = h; px = new Color[w * h]; }

        public void Fill(Cov cov, Func<float, float, Color> col)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float fx = x + .5f, fy = y + .5f, a = cov(fx, fy);
                if (a <= 0) continue;
                var s = col(fx, fy);
                a *= s.a;
                if (a <= 0) continue;
                ref var d = ref px[y * W + x];
                float outA = a + d.a * (1 - a);
                d = new Color((s.r * a + d.r * d.a * (1 - a)) / outA, (s.g * a + d.g * d.a * (1 - a)) / outA,
                    (s.b * a + d.b * d.a * (1 - a)) / outA, outA);
            }
        }

        public void Paint(Sdf s, Color c) => Fill(Edge(s), (_, _) => c);
        public void Paint(Sdf s, Func<float, float, Color> col) => Fill(Edge(s), col);

        public byte[] Png()
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.SetPixels(px);
            var png = t.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(t);
            return png;
        }
    }

    static Cov Edge(Sdf s) => (x, y) => Mathf.Clamp01(.5f - s(x, y));

    // ---------- shapes & ops ----------
    static Sdf Circle(float cx, float cy, float r) => (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

    static Sdf Box(float cx, float cy, float hw, float hh, float r) => (x, y) =>
    {
        float qx = Mathf.Abs(x - cx) - hw + r, qy = Mathf.Abs(y - cy) - hh + r;
        return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    };

    static Sdf Ellipse(float cx, float cy, float rx, float ry) => (x, y) => // cheap approximation, fine for decoration
    {
        float k = Mathf.Min(rx, ry);
        return (new Vector2((x - cx) / rx, (y - cy) / ry).magnitude - 1) * k;
    };

    static Sdf Capsule(float ax, float ay, float bx, float by, float r) => (x, y) =>
    {
        Vector2 p = new(x - ax, y - ay), e = new(bx - ax, by - ay);
        return (p - e * Mathf.Clamp01(Vector2.Dot(p, e) / Vector2.Dot(e, e))).magnitude - r;
    };

    static Sdf Poly(params float[] xy)
    {
        var v = new Vector2[xy.Length / 2];
        for (int i = 0; i < v.Length; i++) v[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
        return (x, y) =>
        {
            var p = new Vector2(x, y);
            float d = (p - v[0]).sqrMagnitude, s = 1;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i], w = p - v[i];
                d = Mathf.Min(d, (w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e))).sqrMagnitude);
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        };
    }

    static Sdf Star(float cx, float cy, float ro, float ri, int n = 5, float round = 0)
    {
        var pts = new float[n * 4];
        for (int i = 0; i < n * 2; i++)
        {
            float a = Mathf.PI / 2 + i * Mathf.PI / n, r = (i % 2 == 0 ? ro : ri) - round;
            pts[i * 2] = cx + Mathf.Cos(a) * r;
            pts[i * 2 + 1] = cy + Mathf.Sin(a) * r;
        }
        var p = Poly(pts);
        return (x, y) => p(x, y) - round;
    }

    static Sdf U(params Sdf[] s) => (x, y) => { float d = float.MaxValue; foreach (var f in s) d = Mathf.Min(d, f(x, y)); return d; };
    static Sdf I(Sdf a, Sdf b) => (x, y) => Mathf.Max(a(x, y), b(x, y));
    static Sdf Sub(Sdf a, Sdf b) => (x, y) => Mathf.Max(a(x, y), -b(x, y));
    static Sdf Grow(Sdf a, float k) => (x, y) => a(x, y) - k;
    static Sdf Move(Sdf a, float dx, float dy) => (x, y) => a(x - dx, y - dy);
    static Sdf Ring(float cx, float cy, float r, float t) { var c = Circle(cx, cy, r); return (x, y) => Mathf.Abs(c(x, y)) - t; }
    static Sdf Below(float yy) => (_, y) => y - yy;
    static Sdf Above(float yy) => (_, y) => yy - y;
    static Sdf Rot(Sdf a, float deg, float cx, float cy)
    {
        float r = -deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return (x, y) => a(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c);
    }

    static Func<float, float, Color> VGrad(Color top, Color bottom, float y0, float y1) =>
        (_, y) => Color.Lerp(bottom, top, Mathf.InverseLerp(y0, y1, y));

    // The house style: soft drop shadow, dark outline, vertical gradient, bottom inner shade, top gloss.
    static void Glossy(Canvas c, Sdf s, Color col, float ow, float y0, float y1, float gloss = -1, bool shadow = true)
    {
        float h = y1 - y0;
        if (gloss < 0) gloss = h * .22f;
        if (shadow) c.Paint(Move(Grow(s, ow), 0, -ow * .7f), A(Color.black, .18f));
        c.Paint(Grow(s, ow), Dark(col));
        c.Paint(s, VGrad(Lite(col, .28f), Color.Lerp(col, Dark(col), .25f), y0, y1));
        c.Paint(Sub(s, Move(s, 0, ow * 1.1f)), A(Dark(col), .3f));
        c.Paint(Sub(Grow(s, -ow * .55f), Move(s, 0, -gloss)), A(White, .38f));
    }

    // Flat tint mask: white, crisp, no decoration (Image.color tints these at runtime).
    static void Mask(Canvas c, Sdf s) => c.Paint(s, White);

    // ---------- Kenney glyphs (CC0) ----------
    static readonly Dictionary<string, Texture2D> GlyphCache = new();

    static Cov Glyph(string name, float gx, float gy, float size)
    {
        if (!GlyphCache.TryGetValue(name, out var t))
        {
            t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(Glyphs + name + ".png"));
            GlyphCache[name] = t;
        }
        float sharp = size / t.width * 1.4f;
        return (x, y) =>
        {
            float u = (x - gx) / size, v = (y - gy) / size;
            if (u < 0 || v < 0 || u > 1 || v > 1) return 0;
            return Mathf.Clamp01((t.GetPixelBilinear(u, v).a - .5f) * sharp + .5f);
        };
    }

    static Cov Dilate(Cov g, float r) => (x, y) =>
    {
        float m = g(x, y);
        for (int i = 0; i < 16 && m < 1; i++)
        {
            float a = i * Mathf.PI / 8, cx = Mathf.Cos(a), cy = Mathf.Sin(a);
            m = Mathf.Max(m, Mathf.Max(g(x + cx * r, y + cy * r), g(x + cx * r * .5f, y + cy * r * .5f)));
        }
        return m;
    };

    static Cov MoveCov(Cov g, float dx, float dy) => (x, y) => g(x - dx, y - dy);

    static void GlyphColored(Canvas c, string name, Color col, float pad = 14)
    {
        float size = c.W - pad * 2, ow = c.W * .045f;
        var g = Glyph(name, pad, pad, size);
        var outline = Dilate(g, ow);
        c.Fill(MoveCov(outline, 0, -ow * .7f), (_, _) => A(Color.black, .18f));
        c.Fill(outline, (_, _) => Dark(col));
        c.Fill(g, VGrad(Lite(col, .3f), col, pad, c.H - pad));
    }

    static void GlyphWhite(Canvas c, string name, float pad = 6)
    {
        var g = Glyph(name, pad, pad, c.W - pad * 2);
        c.Fill(MoveCov(g, 0, -c.W * .02f), (_, _) => A(Color.black, .2f));
        c.Fill(g, (_, _) => White);
    }

    // ---------- reusable icon parts ----------
    static void Coin(Canvas c, float cx, float cy, float r)
    {
        Glossy(c, Circle(cx, cy, r), Gold, r * .1f, cy - r, cy + r);
        c.Paint(Ring(cx, cy, r * .7f, r * .06f), A(Dark(Gold, .25f), .7f));
        c.Paint(Star(cx, cy, r * .42f, r * .19f, 5, r * .04f), Lite(Gold, .45f));
    }

    static void Clock(Canvas c, Color rim, bool stopwatch)
    {
        float cx = 128, cy = stopwatch ? 118 : 128, r = stopwatch ? 92 : 104;
        if (stopwatch)
        {
            Glossy(c, Box(128, 226, 18, 14, 5), Gray, 7, 212, 240, shadow: false);
            Glossy(c, Capsule(cx + 62, cy + 62, cx + 78, cy + 78, 11), Gray, 7, cy + 50, cy + 90, shadow: false);
        }
        Glossy(c, Circle(cx, cy, r), rim, 10, cy - r, cy + r);
        c.Paint(Circle(cx, cy, r * .74f), Lite(Light, .6f));
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            c.Paint(Capsule(cx + ca * r * .6f, cy + sa * r * .6f, cx + ca * r * .66f, cy + sa * r * .66f, i % 3 == 0 ? 4 : 2.5f), A(Ink, .6f));
        }
        if (stopwatch) c.Paint(Capsule(cx, cy, cx + r * .34f, cy + r * .4f, 6), Red);
        else
        {
            c.Paint(Capsule(cx, cy, cx, cy + r * .5f, 6), Ink);
            c.Paint(Capsule(cx, cy, cx + r * .36f, cy - r * .12f, 6), Ink);
        }
        c.Paint(Circle(cx, cy, 10), Ink);
    }

    static void Gift(Canvas c, Color box, Color ribbon)
    {
        Glossy(c, Box(128, 92, 88, 70, 12), box, 9, 22, 162);
        Glossy(c, Box(128, 172, 102, 26, 12), Lite(box, .1f), 9, 146, 198, shadow: false);
        c.Paint(Box(128, 92, 18, 70, 2), ribbon);
        c.Paint(Box(128, 172, 18, 26, 2), Lite(ribbon, .15f));
        Glossy(c, Rot(Ellipse(92, 216, 40, 22), -25, 92, 216), ribbon, 7, 196, 238, shadow: false);
        Glossy(c, Rot(Ellipse(164, 216, 40, 22), 25, 164, 216), ribbon, 7, 196, 238, shadow: false);
        Glossy(c, Circle(128, 204, 16), Lite(ribbon, .1f), 6, 188, 220, shadow: false);
    }

    static void Emoji(Canvas c, Action<Canvas> face)
    {
        Glossy(c, Circle(128, 128, 104), Hex("FFCF2E"), 10, 24, 232);
        face(c);
    }

    static void Rays(Canvas c, int count, float softness, float inner)
    {
        float cx = c.W / 2f, cy = c.H / 2f, R = c.W / 2f;
        c.Fill((x, y) =>
        {
            float dx = x - cx, dy = y - cy, r = Mathf.Sqrt(dx * dx + dy * dy) / R;
            if (r > 1) return 0;
            float a = Mathf.Atan2(dy, dx) / (2 * Mathf.PI) * count;
            float wedge = Mathf.Clamp01((Mathf.Abs(a - Mathf.Floor(a) - .5f) * 2 - .45f) / softness + .5f);
            return wedge * Mathf.Pow(1 - r, 1.6f) * Mathf.Clamp01(r / inner);
        }, (_, _) => White);
    }

    static Sdf CloudRow(float w, float baseY, int seed, float rMin, float rMax)
    {
        var rng = new System.Random(seed);
        var parts = new List<Sdf> { Below(baseY) };
        for (float x = -rMax * .5f; x < w + rMax; x += rMin * 1.1f)
        {
            float r = Mathf.Lerp(rMin, rMax, (float)rng.NextDouble());
            parts.Add(Circle(x, baseY - r * .25f + (float)rng.NextDouble() * r * .2f, r));
        }
        return U(parts.ToArray());
    }

    // Soft cyan rim inside a white shape (cloud look).
    static void Cloud(Canvas c, Sdf s, float rim)
    {
        c.Paint(s, White);
        c.Fill((x, y) => { float d = s(x, y); return d > .5f ? 0 : Mathf.Clamp01(.5f - d) * Mathf.Pow(1 - Mathf.Clamp01(-d / rim), 2); },
            (_, _) => A(Hex("8EE2F4"), .9f));
    }

    // ---------- the sprite list ----------
    static IEnumerable<(string path, int w, int h, Action<Canvas> draw)> Sprites()
    {
        // Popups & backgrounds
        yield return ("Backgrounds/Popup/Blue.png", 512, 512, c => Glossy(c, Circle(256, 256, 240), Hex("43B4D8"), 12, 16, 496, 90, false));
        yield return ("Backgrounds/Popup/White.png", 512, 512, c => Glossy(c, Circle(256, 256, 240), White, 10, 16, 496, 90, false));
        yield return ("Backgrounds/Popup/Clouds.png", 600, 323, c =>
            Cloud(c, U(Box(300, 128, 290, 122, 115), Circle(150, 226, 88), Circle(285, 236, 58), Circle(380, 232, 62)), 26));
        yield return ("Backgrounds/Popup/Headline - Flag.png", 620, 198, c =>
        {
            var tailL = Poly(6, 30, 118, 30, 118, 120, 6, 120, 44, 75);
            var tailR = Poly(614, 30, 502, 30, 502, 120, 614, 120, 576, 75);
            Glossy(c, U(tailL, tailR), Dark(Pink, .15f), 7, 30, 120, 20);
            c.Paint(U(Poly(96, 52, 118, 30, 118, 60), Poly(524, 52, 502, 30, 502, 60)), Dark(Pink, .5f));
            Glossy(c, Box(310, 110, 214, 62, 18), Pink, 8, 48, 172);
        });
        yield return ("Backgrounds/Level Scene/Clouds Bottom.png", 2280, 410, c => Cloud(c, CloudRow(2280, 250, 7, 70, 140), 34));
        yield return ("Backgrounds/Spin Wheel/Shine.png", 1024, 1024, c => Rays(c, 16, .12f, .08f));

        // Buttons
        var circles = new (string n, Color c)[] { ("Blue (Teal)", BlueTeal), ("Blue", Blue), ("Green", Green), ("Light", Light), ("Orange", Orange),
            ("Pink", Pink), ("Purple", Purple), ("Red", Red), ("Teal", Teal), ("Yellow", Yellow) };
        foreach (var (n, col) in circles)
            yield return ($"Buttons/Circles/{n}.png", 256, 256, c => Glossy(c, Circle(128, 130, 112), col, 10, 18, 242));
        foreach (var (n, col) in new[] { ("Blue (Teal)", BlueTeal), ("Green", Green), ("Orange", Orange), ("Pink", Pink), ("Yellow", Yellow) })
            yield return ($"Buttons/Rectangles/{n}.png", 260, 100, c => Glossy(c, Box(130, 52, 122, 42, 42), col, 6, 10, 94, 18));
        yield return ("Buttons/Rectangles/Shape.png", 260, 100, c => Mask(c, Box(130, 50, 128, 48, 48)));
        foreach (var (n, col) in new[] { ("Blue", Blue), ("Gray", Gray), ("Light", Light) })
            yield return ($"Buttons/Square/{n}.png", 256, 256, c => Glossy(c, Box(128, 130, 112, 112, 96), col, 10, 18, 242));

        // Shapes & effects (tint masks)
        yield return ("Shapes/Shapes/Circle/Circle - 256px.png", 256, 256, c => Mask(c, Circle(128, 128, 127.5f)));
        yield return ("Shapes/Shapes/Circle/Circle - Cartoon.png", 256, 256, c => Mask(c, Circle(128, 128, 120)));
        yield return ("Shapes/Shapes/Circle/Circle Cartoon - With Border.png", 256, 256, c => Mask(c, Ring(128, 128, 114, 8))); // used by the tray bake
        yield return ("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px.png", 256, 256, c => Mask(c, Circle(128, 128, 127.5f)));
        yield return ("Shapes/Shapes/Rectangle/Square - Cartoon.png", 256, 256, c => Mask(c, Box(128, 128, 126, 126, 99)));
        yield return ("Shapes/Effects/Inner Glow - Rounded Rectangle.png", 256, 256, c =>
        {
            var s = Circle(128, 128, 127.5f);
            c.Fill((x, y) => { float d = s(x, y); return d > .5f ? 0 : Mathf.Clamp01(.5f - d) * Mathf.Pow(1 - Mathf.Clamp01(-d / 46), 2.2f); }, (_, _) => White);
        });
        yield return ("Shapes/Effects/Radial Shine.png", 512, 512, c => Rays(c, 12, .18f, .1f));
        yield return ("Shapes/Effects/Shine.png", 256, 256, c =>
        {
            c.Fill((x, y) => Mathf.Pow(1 - Mathf.Clamp01(Mathf.Sqrt((x - 128) * (x - 128) + (y - 128) * (y - 128)) / 128), 2.5f) * .8f, (_, _) => White);
            c.Paint(Star(128, 128, 120, 14, 4, 2), White);
        });

        // Icons — Kenney glyphs, recolored
        yield return ("Icons/Icons Colored/Basic/Settings.png", 256, 256, c => GlyphColored(c, "gear", Hex("7C8BA6")));
        yield return ("Icons/Icons Colored/Commerce/Shop.png", 256, 256, c => GlyphColored(c, "shoppingCart", Teal));
        yield return ("Icons/Icons Colored/Lock - Key/Lock - Gold.png", 256, 256, c => GlyphColored(c, "locked", Gold));
        yield return ("Icons/Icons Colored/Media/Mobile.png", 256, 256, c => GlyphColored(c, "phone", Hex("4A63D8")));
        yield return ("Icons/Kawaii/Lock 512.png", 512, 512, c => GlyphColored(c, "locked", Gold, 28));
        yield return ("Icons/Kawaii/Lock 1536.png", 1536, 1536, c => GlyphColored(c, "locked", Gold, 84));
        foreach (var (file, glyph) in new[] { ("Arrows/Arrow - Left", "arrowLeft"), ("Basic/Checkmark Cartoon", "checkmark"), ("Basic/Settings", "gear"),
                     ("Basic/X Icon", "cross"), ("Media/Music", "musicOn"), ("Media/Sound On", "audioOn"), ("Navigation/Pause", "pause") })
            yield return ($"Icons/Icons White/{file}.png", 256, 256, c => GlyphWhite(c, glyph));

        // Icons — drawn
        yield return ("Icons/Icons Colored/Coins/Coin.png", 256, 256, c => Coin(c, 128, 128, 104));
        yield return ("Icons/Icons Colored/Coins/Coins x2.png", 256, 256, c => { Coin(c, 158, 150, 72); Coin(c, 98, 100, 72); });
        yield return ("Icons/Icons Colored/Coins/Coins x3.png", 256, 256, c => { Coin(c, 128, 168, 62); Coin(c, 76, 92, 62); Coin(c, 180, 92, 62); });
        yield return ("Icons/Icons Colored/Coins/Coins x4.png", 256, 256, c => { Coin(c, 84, 168, 56); Coin(c, 172, 168, 56); Coin(c, 84, 84, 56); Coin(c, 172, 84, 56); });
        yield return ("Icons/Icons Colored/Coins/Coins - Bag.png", 256, 256, c =>
        {
            Glossy(c, U(Circle(128, 98, 86), Box(128, 184, 34, 26, 12)), Hex("C99554"), 9, 12, 210);
            Glossy(c, U(Rot(Ellipse(100, 222, 30, 16), 20, 100, 222), Rot(Ellipse(156, 222, 30, 16), -20, 156, 222)), Hex("C99554"), 7, 206, 238, shadow: false);
            c.Paint(Box(128, 176, 40, 7, 4), Hex("8A5A2B"));
            Coin(c, 128, 92, 42);
        });
        yield return ("Icons/Icons Colored/Coins/Coins - Chest.png", 256, 256, c =>
        {
            Coin(c, 96, 176, 34); Coin(c, 150, 184, 34);
            Glossy(c, Box(128, 92, 106, 64, 14), Brown, 9, 28, 156);
            Glossy(c, Box(128, 160, 106, 30, 18), Lite(Brown, .1f), 9, 130, 190, shadow: false);
            c.Paint(U(Box(60, 110, 10, 82, 3), Box(196, 110, 10, 82, 3)), Gold);
            Glossy(c, Box(128, 132, 20, 24, 6), Gold, 6, 108, 156, shadow: false);
            c.Paint(Capsule(128, 122, 128, 134, 4), Ink);
        });
        yield return ("Icons/Icons Colored/Donuts/Donut - Blue.png", 256, 256, c =>
        {
            Glossy(c, Sub(Circle(128, 124, 104), Circle(128, 124, 36)), Hex("E8A65A"), 10, 20, 228);
            var icing = Sub(U(Circle(128, 132, 88), Circle(72, 92, 22), Circle(188, 96, 18), Circle(128, 64, 20)), Circle(128, 128, 46));
            Glossy(c, icing, Hex("4FB4FF"), 5, 40, 222, 30, false);
            var rng = new System.Random(3);
            var sprinkles = new[] { Red, Yellow, White, Pink, Green };
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2, r = 60 + (float)rng.NextDouble() * 18, x = 128 + Mathf.Cos(a) * r, y = 130 + Mathf.Sin(a) * r, t = (float)rng.NextDouble() * Mathf.PI;
                c.Paint(Capsule(x - Mathf.Cos(t) * 7, y - Mathf.Sin(t) * 7, x + Mathf.Cos(t) * 7, y + Mathf.Sin(t) * 7, 4), sprinkles[i % sprinkles.Length]);
            }
        });
        yield return ("Icons/Icons Colored/Candies/Candy - Multicolor Spiral.png", 256, 256, c =>
        {
            Glossy(c, Capsule(128, 18, 128, 110, 10), Light, 6, 8, 120);
            Glossy(c, Circle(128, 152, 88), Hex("FF5D8F"), 10, 64, 240, shadow: false);
            var cols = new[] { Yellow, Teal, White, Purple };
            c.Fill((x, y) =>
            {
                float dx = x - 128, dy = y - 152, r = Mathf.Sqrt(dx * dx + dy * dy);
                return r > 80 ? 0 : 1;
            }, (x, y) =>
            {
                float dx = x - 128, dy = y - 152, r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx) / (2 * Mathf.PI) + .5f;
                float band = a * 4 + r / 22f;
                int k = (int)Mathf.Floor(band) & 3;
                return band - Mathf.Floor(band) < .5f ? A(cols[k], .9f) : Clear;
            });
            c.Paint(Sub(Grow(Circle(128, 152, 88), -6), Move(Circle(128, 152, 88), 0, -22)), A(White, .35f));
        });
        yield return ("Icons/Icons Colored/Food/Apple.png", 256, 256, c =>
        {
            c.Paint(Capsule(128, 190, 138, 232, 8), Hex("7A4A22"));
            Glossy(c, Rot(Ellipse(166, 214, 34, 16), 30, 166, 214), Green, 6, 198, 232, shadow: false);
            Glossy(c, U(Circle(96, 118, 80), Circle(160, 118, 80), Circle(128, 70, 56)), Red, 10, 14, 200);
        });
        yield return ("Icons/Icons Colored/Food/Ice Cream.png", 256, 256, c =>
        {
            var cone = Poly(66, 140, 190, 140, 128, 14);
            Glossy(c, cone, Hex("E0A458"), 8, 14, 140);
            c.Fill(Edge(Grow(cone, -4)), (x, y) => ((int)((x + y) / 18) + (int)((x - y + 400) / 18)) % 2 == 0 ? A(Dark(Hex("E0A458"), .2f), .35f) : Clear);
            Glossy(c, U(Circle(128, 172, 64), Circle(82, 146, 26), Circle(174, 146, 26), Circle(128, 140, 30)), Pink, 9, 116, 236);
            Glossy(c, Circle(128, 238, 13), Red, 5, 225, 251, shadow: false);
        });
        yield return ("Icons/Icons Colored/Gems/Gem - Blue.png", 256, 256, c =>
        {
            Glossy(c, Poly(64, 196, 192, 196, 236, 150, 128, 20, 20, 150), Hex("3EA2FF"), 10, 20, 196, 0);
            c.Paint(Poly(64, 196, 104, 150, 20, 150), A(White, .35f));
            c.Paint(Poly(104, 150, 152, 150, 128, 20), A(White, .22f));
            c.Paint(Poly(192, 196, 236, 150, 152, 150), A(Ink, .18f));
            c.Paint(Poly(152, 150, 236, 150, 128, 20), A(Ink, .25f));
            c.Paint(Poly(64, 196, 192, 196, 152, 150, 104, 150), A(White, .15f));
        });
        yield return ("Icons/Icons Colored/Gifts/Gift.png", 256, 256, c => Gift(c, Red, Yellow));
        yield return ("Icons/Icons Colored/Gifts/Gift - Pink (Border).png", 256, 256, c => Gift(c, Pink, Hex("FFE36A")));
        yield return ("Icons/Icons Colored/Gifts/Gift - Yellow (Border).png", 256, 256, c => Gift(c, Yellow, Red));
        yield return ("Icons/Icons Colored/Loading Spinner/Loading Spinner.png", 256, 256, c =>
        {
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.PI / 2 - i * Mathf.PI / 4;
                Glossy(c, Circle(128 + Mathf.Cos(a) * 88, 128 + Mathf.Sin(a) * 88, 24 - i * 1.6f), A(Teal, 1 - i * .1f), 5, 0, 256, 8, false);
            }
        });
        yield return ("Icons/Icons Colored/Other/Rocket (Border).png", 256, 256, c =>
        {
            const float rot = -40;
            Sdf R(Sdf s) => Rot(s, rot, 128, 128);
            Glossy(c, R(Poly(128, 6, 108, 46, 148, 46)), Orange, 6, 6, 60, shadow: false);
            Glossy(c, R(U(Poly(86, 64, 112, 100, 112, 52, 86, 40), Poly(170, 64, 144, 100, 144, 52, 170, 40))), Red, 8, 40, 110);
            Glossy(c, R(U(Ellipse(128, 136, 40, 96), Box(128, 72, 30, 26, 8))), Light, 9, 40, 232);
            Glossy(c, R(Sub(Ellipse(128, 136, 40, 96), Below(190))), Red, 0, 190, 232, 10, false);
            Glossy(c, R(Circle(128, 140, 20)), Hex("47B6FF"), 7, 120, 160, 8, false);
        });
        yield return ("Icons/Icons Colored/Potion/Potion - Blue (Border).png", 256, 256, c =>
        {
            var glass = U(Circle(128, 92, 80), Box(128, 182, 26, 34, 6));
            Glossy(c, glass, Hex("CFE9F7"), 9, 12, 216);
            Glossy(c, I(Grow(Circle(128, 92, 80), -10), Below(112)), Hex("3D7CFF"), 0, 20, 112, 14, false);
            Glossy(c, Box(128, 224, 22, 16, 6), Brown, 7, 208, 240, shadow: false);
            c.Paint(Circle(104, 72, 9), A(White, .6f)); c.Paint(Circle(146, 54, 6), A(White, .5f));
        });
        yield return ("Icons/Icons Colored/Skull/Skull (Border).png", 256, 256, c =>
        {
            Glossy(c, U(Circle(128, 146, 92), Box(128, 62, 58, 40, 18)), Bone, 10, 22, 238);
            c.Paint(U(Circle(92, 132, 26), Circle(164, 132, 26)), Ink);
            c.Paint(Poly(128, 104, 114, 82, 142, 82), Ink);
            for (int i = -1; i <= 1; i++) c.Paint(Capsule(128 + i * 22, 30, 128 + i * 22, 56, 4), A(Ink, .8f));
        });
        yield return ("Icons/Icons Colored/Speech Bubble/Speech Bubble - Right.png", 256, 256, c =>
            Glossy(c, U(Box(128, 146, 112, 86, 44), Poly(170, 70, 226, 26, 204, 80)), White, 9, 26, 232));
        yield return ("Icons/Icons Colored/Stars/Star Yellow.png", 256, 256, c => Glossy(c, Star(128, 122, 114, 52, 5, 10), Hex("FFC92E"), 10, 20, 236));
        yield return ("Icons/Icons Colored/Stars/Star Gray.png", 256, 256, c => Glossy(c, Star(128, 122, 114, 52, 5, 10), Gray, 10, 20, 236));
        yield return ("Icons/Kawaii/Star Currency.png", 768, 768, c => Glossy(c, Star(384, 366, 342, 156, 5, 30), Hex("FFC92E"), 30, 60, 708));
        yield return ("Icons/Icons Colored/Storage/Box - Brown.png", 256, 256, c =>
        {
            Glossy(c, Box(128, 104, 100, 80, 10), Hex("C98C50"), 9, 24, 184);
            c.Paint(Poly(28, 184, 228, 184, 206, 224, 50, 224), Dark(Hex("C98C50"), .15f));
            c.Paint(Box(128, 150, 22, 44, 2), A(Hex("F4D9A6"), .9f));
        });
        yield return ("Icons/Icons Colored/Time/Calendar.png", 256, 256, c =>
        {
            Glossy(c, Box(128, 116, 104, 100, 20), White, 9, 16, 216);
            Glossy(c, I(Box(128, 116, 104, 100, 20), Above(170)), Red, 0, 170, 216, 14, false);
            c.Paint(U(Capsule(84, 192, 84, 236, 10), Capsule(172, 192, 172, 236, 10)), Gray);
            for (int r = 0; r < 3; r++)
            for (int k = 0; k < 4; k++)
                c.Paint(Box(62 + k * 44, 136 - r * 40, 15, 13, 4), r == 1 && k == 2 ? Red : A(Gray, .7f));
        });
        yield return ("Icons/Icons Colored/Time/Clock - Indigo.png", 256, 256, c => Clock(c, Hex("5B5BD6"), false));
        yield return ("Icons/Icons Colored/Time/Clock - Yellow.png", 256, 256, c => Clock(c, Yellow, false));
        yield return ("Icons/Icons Colored/Time/Timer.png", 256, 256, c => Clock(c, Orange, true));
        yield return ("Icons/Icons Colored/Tools/Magnet.png", 256, 256, c =>
        {
            var shoe = U(I(Sub(Circle(128, 140, 98), Circle(128, 140, 44)), Above(140)), Box(57, 96, 27, 52, 0), Box(199, 96, 27, 52, 0));
            Glossy(c, shoe, Red, 10, 30, 238);
            Glossy(c, U(Box(57, 52, 27, 22, 0), Box(199, 52, 27, 22, 0)), Light, 0, 30, 74, 8, false);
        });
        yield return ("Icons/Icons Colored/Badges/Crown (Border).png", 256, 256, c =>
        {
            Glossy(c, Poly(30, 60, 226, 60, 240, 190, 182, 120, 128, 210, 74, 120, 16, 190), Gold, 10, 60, 210);
            Glossy(c, Box(128, 56, 102, 20, 8), Dark(Gold, .1f), 8, 36, 76, shadow: false);
            foreach (var (x, y) in new[] { (16f, 196f), (128f, 218f), (240f, 196f) }) Glossy(c, Circle(x, y, 14), Gold, 5, y - 14, y + 14, shadow: false);
            Glossy(c, Circle(128, 110, 16), Red, 5, 94, 126, shadow: false);
            Glossy(c, U(Circle(76, 96, 11), Circle(180, 96, 11)), Hex("3EA2FF"), 4, 85, 107, shadow: false);
        });
        yield return ("Icons/Icons Colored/Bolt/Bolt - Blue (Border).png", 256, 256, c =>
            Glossy(c, Poly(150, 242, 54, 116, 118, 116, 92, 14, 202, 148, 136, 148), Hex("3EA2FF"), 10, 14, 242));
        yield return ("Icons/Icons Colored/Emojis/Cartoon/Big Smile - Cartoon.png", 256, 256, c => Emoji(c, e =>
        {
            e.Paint(U(Ellipse(92, 152, 14, 22), Ellipse(164, 152, 14, 22)), Ink);
            e.Paint(I(Circle(128, 112, 62), Below(112)), Ink);
            e.Paint(I(Circle(128, 70, 30), Circle(128, 112, 58)), Hex("FF6B7F"));
        }));
        yield return ("Icons/Icons Colored/Emojis/Cartoon/Sad - Cartoon.png", 256, 256, c => Emoji(c, e =>
        {
            e.Paint(U(Ellipse(92, 146, 13, 18), Ellipse(164, 146, 13, 18)), Ink);
            e.Paint(I(Ring(128, 40, 56, 7), Above(66)), Ink);
            Glossy(e, U(Circle(76, 106, 12), Poly(66, 110, 86, 110, 76, 132)), Hex("6CC6FF"), 3, 94, 132, 6, false);
        }));
        yield return ("Icons/Icons Colored/Emojis/Cartoon/Fear - Cartoon.png", 256, 256, c => Emoji(c, e =>
        {
            e.Paint(U(Circle(90, 148, 24), Circle(166, 148, 24)), White);
            e.Paint(U(Circle(90, 148, 10), Circle(166, 148, 10)), Ink);
            e.Paint(Ellipse(128, 78, 26, 32), Ink);
            Glossy(e, U(Circle(206, 182, 14), Poly(194, 186, 218, 186, 206, 214)), Hex("6CC6FF"), 3, 168, 214, 6, false);
        }));
    }

    // The tray texture is baked from UI sprites by UIOverhaulBuilder; rerun after regenerating sprites.
    [MenuItem("Tools/AssetGen/Rebake Powerup Tray")]
    static void RebakeTray() =>
        typeof(MatchThemAll.EditorTools.UIOverhaulBuilder)
            .GetMethod("BakePowerupTray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, null);

    [MenuItem("Tools/AssetGen/Generate UI Sprites")]
    static void Generate()
    {
        int n = 0;
        foreach (var (path, w, h, draw) in Sprites())
        {
            var c = new Canvas(w, h);
            draw(c);
            var full = Out + path;
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, c.Png());
            n++;
        }
        AssetDatabase.Refresh();
        foreach (var (path, _, _, _) in Sprites())
        {
            if (AssetImporter.GetAtPath(Out + path) is not TextureImporter t || t.textureType == TextureImporterType.Sprite) continue;
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.alphaIsTransparency = true;
            t.mipmapEnabled = false;
            t.SaveAndReimport();
        }
        GlyphCache.Clear();
        Debug.Log($"[SpriteGen] Wrote {n} sprites to {Out}");
    }
}
