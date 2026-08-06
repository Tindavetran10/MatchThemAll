using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Shared IMGUI style/texture factory for the Match Them All editor windows
    /// (LevelEditorWindow, ItemManagerWindow, ShopEditorWindow).
    ///
    /// Single source of truth for the rounded button style so the windows can't drift apart.
    ///
    /// Performance notes (editor IMGUI):
    ///  - Rounded textures are cached statically (one per color, shared by every window)
    ///    instead of being recreated per window, and use HideFlags.HideAndDontSave so
    ///    Unity never tracks/leaks them.
    ///  - They are 18×18, Bilinear-filtered, and 9-sliced with a 4 px border so the corners
    ///    never stretch regardless of button size — a real 9-slice keeps Unity's geometry
    ///    generator on the fast path (no degenerate scaling branch).
    ///  - The cache is cleared on every domain reload via [InitializeOnLoadMethod] so stale
    ///    destroyed textures never survive into the next session.
    /// </summary>
    public static class EditorWindowStyles
    {
        // ── Canonical button palette ─────────────────────────────────────────
        private static readonly Color ButtonBg   = new(0.25f, 0.25f, 0.28f);
        private static readonly Color HoverBg    = new(0.30f, 0.30f, 0.34f);
        private static readonly Color AccentBlue = new(0.27f, 0.55f, 1.00f);
        private static readonly Color SelectedBg = new(AccentBlue.r * 0.7f, AccentBlue.g * 0.7f, AccentBlue.b * 0.7f);

        // ── Rounded-texture constants ─────────────────────────────────────────
        // Texture is 18×18 px; 4 px corner radius; 9-slice border = 4 on each side.
        // The center 10×10 region scales freely; the 4 px corners are never stretched.
        private const int RndSize   = 18;
        private const int RndRadius = 4;

        /// <summary>
        /// The 9-slice border that matches MakeRounded's corner radius.
        /// Callers must set <c>border = new RectOffset(RndBorder, RndBorder, RndBorder, RndBorder)</c>
        /// so the corners are not stretched when the button is drawn at any size.
        /// </summary>
        public const int RndBorder = RndRadius;

        // ── Texture cache ────────────────────────────────────────────────────
        private static readonly Dictionary<Color, Texture2D> RoundedTextures = new();

        /// <summary>
        /// Clears the texture cache on every domain reload (script compile, enter/exit
        /// play mode). Unity destroys HideAndDontSave objects during reload, so any
        /// Texture2D still in the dictionary would become a fake-null white quad.
        /// Clearing forces fresh recreation after each reload.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearTextureCache()
        {
            RoundedTextures.Clear();
        }

        /// <summary>
        /// Returns a cached 18×18 rounded-corner solid-color texture (4 px radius,
        /// Bilinear-filtered). Use with <c>border = new RectOffset(RndBorder, RndBorder,
        /// RndBorder, RndBorder)</c> so Unity 9-slices the corners correctly.
        /// </summary>
        public static Texture2D MakeRounded(Color color)
        {
            if (!RoundedTextures.TryGetValue(color, out var tex) || tex == null)
            {
                tex = new Texture2D(RndSize, RndSize, TextureFormat.RGBA32, false)
                {
                    hideFlags  = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode   = TextureWrapMode.Clamp
                };
                var pixels = new Color[RndSize * RndSize];
                for (int y = 0; y < RndSize; y++)
                {
                    for (int x = 0; x < RndSize; x++)
                    {
                        // Distance (in pixels) from each pixel to the nearest horizontal/vertical edge.
                        // Pixels in the corner zone (cx < RndRadius && cy < RndRadius) get an SDF-based
                        // alpha so the corner is smooth; all other pixels are fully opaque.
                        float cx = Mathf.Min(x, RndSize - 1 - x);
                        float cy = Mathf.Min(y, RndSize - 1 - y);

                        if (cx < RndRadius && cy < RndRadius)
                        {
                            // SDF: distance from the corner arc's centre to this pixel.
                            // The arc centre sits RndRadius-0.5 px in from the corner of the texture.
                            float dx   = RndRadius - 0.5f - cx;
                            float dy   = RndRadius - 0.5f - cy;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            // Smoothstep over ±0.5 px for anti-aliased edge
                            float alpha = Mathf.Clamp01(RndRadius - 0.5f - dist + 0.5f);
                            pixels[y * RndSize + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                        }
                        else
                        {
                            pixels[y * RndSize + x] = color;
                        }
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply(false, true); // upload, drop CPU copy
                RoundedTextures[color] = tex;
            }
            return tex;
        }

        /// <summary>
        /// Standard dark list/card button used for level, item, tab and product rows.
        ///
        /// Normal state  : dark gray rounded pill (ButtonBg).
        /// Hover state   : lighter gray rounded pill (HoverBg).
        /// Selected state: muted blue rounded pill (SelectedBg) — stays after release.
        /// Active state  : full AccentBlue rounded pill — only while pressed, snaps back.
        ///
        /// All backgrounds use MakeRounded (18×18, 4 px radius) with a matching 4 px
        /// 9-slice border, so rounded corners never go square in any state.
        /// </summary>
        public static GUIStyle CardButton(bool selected = false)
        {
            var roundedBorder = new RectOffset(RndBorder, RndBorder, RndBorder, RndBorder);
            var normalTex     = MakeRounded(selected ? SelectedBg : ButtonBg);
            var hoverTex      = MakeRounded(selected ? SelectedBg : HoverBg);
            var activeTex     = MakeRounded(AccentBlue);

            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(12, 8, 8, 8),
                margin    = new RectOffset(4, 4, 2, 2),
                fontSize  = 12,
                fontStyle = selected ? FontStyle.Bold : FontStyle.Normal,
                border    = roundedBorder,
                normal    = { background = normalTex, textColor = Color.white },
                hover     = { background = hoverTex,  textColor = Color.white },
                active    = { background = activeTex, textColor = Color.white },
                focused   = { background = normalTex, textColor = Color.white },
            };
        }
    }
}
