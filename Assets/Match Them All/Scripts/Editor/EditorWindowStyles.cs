using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Shared IMGUI style factory for the Match Them All editor windows
    /// (LevelEditorWindow, ItemManagerWindow, ShopEditorWindow).
    ///
    /// Single source of truth for the custom button styles so the windows can't
    /// drift apart.
    ///
    /// Performance notes (editor IMGUI):
    ///  - Solid-color textures are cached statically (one per color, shared by every
    ///    window) instead of being recreated per window. They use
    ///    HideFlags.HideAndDontSave so Unity never tracks/leaks them.
    ///  - Rounded-corner textures (MakeRounded) are 18×18, Bilinear-filtered, and
    ///    9-sliced with a 4 px border so the corners never stretch regardless of
    ///    button size. Using a real 9-slice means Unity's geometry generator stays on
    ///    the fast path (no degenerate scaling branch).
    ///  - Both caches are cleared on every domain reload via [InitializeOnLoadMethod]
    ///    so stale destroyed textures never survive into the next session.
    /// </summary>
    public static class EditorWindowStyles
    {
        // ── Canonical button palette ─────────────────────────────────────────
        private static readonly Color ButtonBg   = new(0.25f, 0.25f, 0.28f);
        private static readonly Color AccentBlue = new(0.27f, 0.55f, 1.00f);

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

        // ── Texture caches ───────────────────────────────────────────────────
        private static readonly Dictionary<Color, Texture2D> SolidTextures   = new();
        private static readonly Dictionary<Color, Texture2D> RoundedTextures = new();

        /// <summary>
        /// Clears both texture caches on every domain reload (script compile, enter/exit
        /// play mode). Unity destroys HideAndDontSave objects during reload, so any
        /// Texture2D still in the dictionaries would become a fake-null white quad.
        /// Clearing forces fresh recreation after each reload.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearTextureCaches()
        {
            SolidTextures.Clear();
            RoundedTextures.Clear();
        }

        /// <summary>
        /// Returns a cached 2×2 solid-color texture (Point-filtered, border = 0 fast path).
        /// </summary>
        public static Texture2D MakeSolid(Color color)
        {
            if (!SolidTextures.TryGetValue(color, out var tex) || tex == null)
            {
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    hideFlags  = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode   = TextureWrapMode.Clamp
                };
                var pixels = new Color[4];
                for (var i = 0; i < pixels.Length; i++) pixels[i] = color;
                tex.SetPixels(pixels);
                tex.Apply(false, true); // upload, drop CPU copy
                SolidTextures[color] = tex;
            }
            return tex;
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

        private static readonly Color HoverBg    = new(0.30f, 0.30f, 0.34f);
        private static readonly Color SelectedBg = new(AccentBlue.r * 0.7f, AccentBlue.g * 0.7f, AccentBlue.b * 0.7f);

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

        /// <summary>Small square icon button (✕ / ⟲ / ⌫) placed on item &amp; trash cards.</summary>
        public static GUIStyle SmallIconButton()
        {
            var activeBg = MakeSolid(AccentBlue);
            var skinBg   = GUI.skin.button.normal.background;

            return new GUIStyle(GUI.skin.button)
            {
                padding  = new RectOffset(0, 0, 0, 0),
                fontSize = 10,
                hover    = { background = skinBg,    textColor = Color.white },
                active   = { background = activeBg,  textColor = Color.white },
                focused  = { background = skinBg,    textColor = Color.white }
            };
        }

        /// <summary>
        /// Standard action/toolbar button (Save, Reload, + New, Preview Layout, …) that keeps the
        /// default editor-skin look but with hover and focused identical to normal — so merely moving
        /// the mouse over it never shows a different "released" form. Pressing still turns blue
        /// (the skin's active state).
        /// </summary>
        public static GUIStyle ActionButton()
        {
            var style = new GUIStyle(GUI.skin.button);
            NeutralizeHover(style);
            return style;
        }

        /// <summary>Same as <see cref="ActionButton"/> for <c>EditorStyles.miniButton</c>-based buttons.</summary>
        public static GUIStyle MiniActionButton()
        {
            var style = new GUIStyle(EditorStyles.miniButton);
            NeutralizeHover(style);
            return style;
        }

        /// <summary>
        /// Style for a <c>GUILayout.Toolbar</c> tab switcher. Keeps the toolbarButton look but
        /// with hover/focused identical to normal on every tab, so tabs never light up from a
        /// plain mouse-over. The selected tab still renders via its on-state, and pressing
        /// still shows the skin's active state.
        /// </summary>
        public static GUIStyle ToolbarStyle()
        {
            var style = new GUIStyle(EditorStyles.toolbarButton);
            NeutralizeHover(style);
            // The selected tab is drawn with on-states; make its hover/focused match its on-normal too.
            style.onHover   = style.onNormal;
            style.onFocused = style.onNormal;
            return style;
        }

        /// <summary>
        /// Makes a style's hover and focused states render exactly like its normal state (no
        /// highlight when the mouse passes over, no stuck look after clicking).
        /// </summary>
        public static void NeutralizeHover(GUIStyle style)
        {
            style.hover   = style.normal;
            style.focused = style.normal;
        }
    }
}
