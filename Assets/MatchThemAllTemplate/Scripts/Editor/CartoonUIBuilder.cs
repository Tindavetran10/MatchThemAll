#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MatchThemAll.Scripts.Shop;
using MatchThemAll.Scripts.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Builds the cartoon-styled panels (popup frame, headline flag, X close button, colored
    /// rectangle buttons, rounded cards) for the Lobby and Main scenes. Existing root components and
    /// script references are kept; only the visual children are rebuilt. Re-runnable.
    ///
    ///   Tools / Cartoon UI / Build Lobby UI   (open Lobby.unity first)
    ///   Tools / Cartoon UI / Build Main UI    (open MainScene.unity first)
    /// </summary>
    public static class CartoonUIBuilder
    {
        // ── Paths ────────────────────────────────────────────────────────────
        private const string PACK_SPRITES = "Assets/MatchThemAllTemplate/Art/UI/Sprites";
        private const string FONT_PATH = "Assets/MatchThemAllTemplate/Art/UI/Fonts/LilitaOne-Regular SDF.asset";
        private const string FONT_MAT_DIR = "Assets/MatchThemAllTemplate/Art/UI/Fonts";
        private const string PREFAB_DIR = "Assets/MatchThemAllTemplate/Prefabs/UI";
        private const string DAILY_PREFAB = PREFAB_DIR + "/DailyRewardPanel.prefab";
        private const string SETTINGS_PREFAB = PREFAB_DIR + "/SettingsPanel.prefab";
        private const string SHOP_PREFAB = PREFAB_DIR + "/Shop/ShopPanel.prefab";
        private const string CARD_PREFAB = PREFAB_DIR + "/Shop/ShopProductCard.prefab";
        private const string TAB_PREFAB = PREFAB_DIR + "/Shop/ShopTabButton.prefab";

        // ── Palette (taken from the pack's demo popups) ──────────────────────
        private static readonly Color Ink = new Color32(0x2F, 0x76, 0xAC, 255);       // blue body text
        private static readonly Color Pink = new Color32(0xFF, 0x69, 0x93, 255);      // item titles
        private static readonly Color AmountRed = new Color32(0xFF, 0x5B, 0x89, 255); // reward amounts
        private static readonly Color CardBg = new Color32(0xE3, 0xFA, 0xFC, 255);    // light cyan card
        private static readonly Color CardGlow = new Color32(0xC5, 0xF6, 0xFA, 255);
        private static readonly Color Track = new Color32(0xCE, 0xD4, 0xDA, 255);
        private static readonly Color Lime = new Color32(0x94, 0xDD, 0x26, 255);
        private static readonly Color Gold = new Color32(0xFF, 0xD4, 0x3B, 255);
        private static readonly Color Shade = new Color(0f, 0f, 0f, 0.10f);           // soft icon shadow
        private static readonly Color ShadeBtn = new Color(0f, 0f, 0f, 0.13f);        // button shadow
        private static readonly Color Flag = new Color32(0x00, 0x64, 0xAB, 0x14);
        private static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopMid = new Vector2(0.5f, 1f);

        // ═════════════════════════════════════════════════════════════════════
        //  Low-level helpers
        // ═════════════════════════════════════════════════════════════════════

        private static string[] _spritePaths;
        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        /// <summary>Loads a UI sprite by file name; <paramref name="hint"/> disambiguates same-named files by folder.</summary>
        private static Sprite Sp(string name, string hint = null)
        {
            string key = name + "|" + hint;
            if (SpriteCache.TryGetValue(key, out var cached) && cached) return cached;

            _spritePaths ??= AssetDatabase.FindAssets("t:Sprite", new[] { PACK_SPRITES })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();

            foreach (var p in _spritePaths)
            {
                if (Path.GetFileNameWithoutExtension(p) != name) continue;
                if (hint != null && !p.Contains(hint)) continue;
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (!s) continue;
                SpriteCache[key] = s;
                return s;
            }
            throw new Exception($"[CartoonUI] UI sprite '{name}' (hint '{hint}') not found.");
        }

        private static RectTransform Rt(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Fixed-size rect with a single anchor point (pos is relative to that anchor).</summary>
        private static RectTransform At(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var r = Rt(parent, name);
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = Mid;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        /// <summary>Rect centered on its parent. <paramref name="y"/> is offset from the parent's centre (up positive).</summary>
        private static RectTransform Ctr(Transform parent, string name, float x, float y, float w, float h) =>
            At(parent, name, Mid, new Vector2(x, y), new Vector2(w, h));

        /// <summary>Rect hanging from the parent's top-centre. <paramref name="y"/> is the distance DOWN to the rect's centre.</summary>
        private static RectTransform Top(Transform parent, string name, float x, float y, float w, float h) =>
            At(parent, name, TopMid, new Vector2(x, -y), new Vector2(w, h));

        private static void Stretch(RectTransform r, float l = 0, float b = 0, float rt = 0, float t = 0)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = Mid;
            r.offsetMin = new Vector2(l, b);
            r.offsetMax = new Vector2(-rt, -t);
        }

        private static RectTransform Fill(Transform parent, string name, float l = 0, float b = 0, float rt = 0, float t = 0)
        {
            var r = Rt(parent, name);
            Stretch(r, l, b, rt, t);
            return r;
        }

        private static Image Im(RectTransform r, Sprite s, Color c, Image.Type type = Image.Type.Simple,
            float ppum = 1f, bool preserve = false, bool raycast = false)
        {
            var i = r.gameObject.AddComponent<Image>();
            i.sprite = s;
            i.color = c;
            i.type = type;
            if (type == Image.Type.Sliced) i.pixelsPerUnitMultiplier = ppum;
            i.preserveAspect = preserve;
            i.raycastTarget = raycast;
            return i;
        }

        private static void Clear(Transform t)
        {
            while (t.childCount > 0) UnityEngine.Object.DestroyImmediate(t.GetChild(0).gameObject);
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new Exception($"[CartoonUI] {target.GetType().Name}.{field} not found");
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(UnityEngine.Object target, string field, IList<UnityEngine.Object> values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new Exception($"[CartoonUI] {target.GetType().Name}.{field} not found");
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string field, float v)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ClearClicks(Button b)
        {
            for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(b.onClick, i);
        }

        private static T Ensure<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

        private static void OnClick(Button b, UnityAction a) => UnityEventTools.AddPersistentListener(b.onClick, a);

        // ── Text ─────────────────────────────────────────────────────────────

        private static TMP_FontAsset _font;
        private static TMP_FontAsset Font => _font ? _font : _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);

        /// <summary>mat = Green / Orange / Pink / Navy / Title (white text with a coloured outline), or null for the plain font material.</summary>
        private static TextMeshProUGUI Tx(Transform parent, string name, string text, float size, Color color,
            string mat = null, TextAlignmentOptions align = TextAlignmentOptions.Center, bool auto = false)
        {
            var r = Rt(parent, name);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            if (mat != null)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{FONT_MAT_DIR}/LilitaOne SDF - {mat}.mat");
                if (m) t.fontSharedMaterial = m;
            }
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            if (auto)
            {
                t.enableAutoSizing = true;
                t.fontSizeMax = size;
                t.fontSizeMin = size * 0.5f;
            }
            return t;
        }

        private static TextMeshProUGUI TxFill(Transform parent, string name, string text, float size, Color color,
            string mat = null, TextAlignmentOptions align = TextAlignmentOptions.Center, float inset = 0, bool auto = true)
        {
            var t = Tx(parent, name, text, size, color, mat, align, auto);
            Stretch(t.rectTransform, inset, 0, inset, 0);
            return t;
        }

        // ── Icons / shadows ──────────────────────────────────────────────────

        /// <summary>Sprite with the pack's soft drop shadow underneath (Shadow + Image children).</summary>
        private static RectTransform Icon(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var holder = At(parent, name, anchor, pos, size);
            var sh = Fill(holder, "Shadow");
            sh.offsetMin = new Vector2(0, -10);
            sh.offsetMax = new Vector2(0, -10);
            Im(sh, sprite, Shade, preserve: true);
            Im(Fill(holder, "Image"), sprite, Color.white, preserve: true);
            return holder;
        }

        // ── Buttons ──────────────────────────────────────────────────────────

        private static void StyleButton(Button b, Graphic target)
        {
            b.targetGraphic = target;
            b.transition = Selectable.Transition.ColorTint;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var c = b.colors;
            c.normalColor = Color.white;
            c.highlightedColor = Color.white;
            c.selectedColor = Color.white;
            c.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            c.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            c.fadeDuration = 0.08f;
            b.colors = c;
        }

        /// <summary>Adds the pack's shadow + face to an existing rect and turns it into a Button (reusing one if present).</summary>
        private static Button MakeButton(RectTransform root, Sprite face, bool circle)
        {
            var sh = Fill(root, "Shadow");
            sh.offsetMin = new Vector2(0, -10);
            sh.offsetMax = new Vector2(0, -10);
            Im(sh, circle ? Sp("Circle - Cartoon") : Sp("Shape"), ShadeBtn, circle ? Image.Type.Simple : Image.Type.Sliced);

            var f = Fill(root, "Face");
            var img = Im(f, face, Color.white, circle ? Image.Type.Simple : Image.Type.Sliced, raycast: true);

            var b = Ensure<Button>(root.gameObject);
            StyleButton(b, img);
            if (!root.GetComponent<UIButtonPunch>()) root.gameObject.AddComponent<UIButtonPunch>();
            return b;
        }

        /// <summary>Rectangle button in one of the pack's colours (Green, Orange, Pink, Yellow, Blue (Teal)...) with a label.</summary>
        private static Button RectButton(Transform parent, string name, string color, Vector2 size, string label,
            string mat, float fontSize, Vector2? pos = null, Vector2? anchor = null)
        {
            var root = At(parent, name, anchor ?? Mid, pos ?? Vector2.zero, size);
            var b = MakeButton(root, Sp(color, "Rectangles"), false);
            TxFill(root, "Text", label, fontSize, Color.white, mat, inset: 24);
            return b;
        }

        /// <summary>The pack's round X close button (top-right of a popup frame).</summary>
        private static Button CloseButton(Transform frame)
        {
            var root = At(frame, "Close Button", Vector2.one, new Vector2(-50, -50), new Vector2(100, 100));
            var sh = Fill(root, "Shadow");
            sh.offsetMin = new Vector2(0, -10);
            sh.offsetMax = new Vector2(0, -10);
            Im(sh, Sp("Teal", "Circles"), new Color(0, 0, 0, 0.133f));
            var face = Fill(root, "Button");
            var img = Im(face, Sp("Blue (Teal)", "Circles"), Color.white, raycast: true);
            Icon(root, "Icon", Sp("X Icon", "Icons White/Basic"), Mid, new Vector2(0, 4), new Vector2(50, 50));
            var b = root.gameObject.AddComponent<Button>();
            StyleButton(b, img);
            var punch = root.gameObject.AddComponent<UIButtonPunch>();
            var punchSo = new SerializedObject(punch);
            punchSo.FindProperty("clickSound").intValue = (int)SoundEvent.ButtonClose;
            punchSo.ApplyModifiedPropertiesWithoutUndo();
            return b;
        }

        // ── Popup frame ──────────────────────────────────────────────────────

        private class Popup
        {
            public RectTransform Card;
            public Button Close;
        }

        /// <summary>
        /// The pack's popup: blue frame, light inner panel, headline flag, optional hanging deco icon, X button.
        /// Children are positioned from the card's top edge (see <see cref="Top"/>).
        /// </summary>
        private static Popup MakePopup(Transform parent, string title, Vector2 size, Sprite deco, bool clouds,
            float frameTop = 40f, float whiteTop = 40f, float whiteBottom = 15f, float headlineY = 85f, float titleSize = 80f)
        {
            var card = Ctr(parent, "Card", 0, 0, size.x, size.y);

            var frame = Fill(card, "Popup", 0, 50, 0, frameTop);
            var bg = Fill(frame, "Background");
            var bgShadow = Fill(bg, "Shadow");
            bgShadow.offsetMin = new Vector2(0, -20);
            bgShadow.offsetMax = new Vector2(0, -20);
            Im(bgShadow, Sp("Blue", "Popup"), new Color(0, 0, 0, 0.10f), Image.Type.Sliced);
            Im(Fill(bg, "Image"), Sp("Blue", "Popup"), Color.white, Image.Type.Sliced);

            if (deco) Icon(frame, "Image (Deco)", deco, TopMid, new Vector2(-size.x * 0.32f, -38), new Vector2(256, 256));

            var whiteSprite = clouds ? Sp("Clouds", "Popup") : Sp("White", "Popup");
            var whiteShadow = Fill(frame, "White Shadow", 20, whiteBottom, 20, whiteTop);
            Im(whiteShadow, whiteSprite, new Color(0, 0, 0, 0.10f), Image.Type.Sliced);
            var white = Fill(whiteShadow, "White");
            white.offsetMin = new Vector2(0, 5);
            white.offsetMax = Vector2.zero;
            Im(white, whiteSprite, Color.white, Image.Type.Sliced);

            var close = CloseButton(frame);

            // Headline flag
            float hw = 450f;
            var head = Top(card, "Headline", 0, headlineY, hw, 162);
            var hs = Fill(head, "Shadow");
            hs.offsetMin = new Vector2(-54, -15 - 18);
            hs.offsetMax = new Vector2(54, -15 + 18);
            Im(hs, Sp("Headline - Flag"), Flag, Image.Type.Sliced);
            var hi = Fill(head, "Image");
            hi.offsetMin = new Vector2(-54, -18);
            hi.offsetMax = new Vector2(54, 18);
            Im(hi, Sp("Headline - Flag"), Color.white, Image.Type.Sliced);
            TxFill(head, "Title Text", title, titleSize, Color.white, "Title", inset: 20);

            return new Popup { Card = card, Close = close };
        }

        /// <summary>Full-screen dimmer root with Canvas + raycaster + UIAnimator (pops its "Card" child).</summary>
        private static GameObject MakePanelRoot(string name, Transform parent, float dim = 0.78f, int sorting = 20)
        {
            var r = parent ? Fill(parent, name) : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.gameObject.layer = 5;
            Stretch(r);
            Im(r, null, new Color(0, 0, 0, dim), raycast: true);
            var cv = r.gameObject.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = sorting;
            r.gameObject.AddComponent<GraphicRaycaster>();
            r.gameObject.AddComponent<UIAnimator>();
            return r.gameObject;
        }

        // ── Sliders / toggles / settings rows ────────────────────────────────

        private static Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var root = Ctr(parent, name, pos.x, pos.y, size.x, size.y);

            var bg = At(root, "Background", new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 36));
            bg.anchorMax = new Vector2(1, 0.5f);
            Im(bg, Sp("Rounded Rectangle - 256px"), Track, Image.Type.Sliced, 8f);

            var fillArea = At(root, "Fill Area", new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 36));
            fillArea.anchorMax = new Vector2(1, 0.5f);
            var fill = Fill(fillArea, "Fill");
            Im(fill, Sp("Rounded Rectangle - 256px"), Lime, Image.Type.Sliced, 8f);

            var slide = Fill(root, "Handle Slide Area", size.y / 2f, 0, size.y / 2f, 0);
            var handle = Rt(slide, "Handle");
            handle.anchorMin = new Vector2(0, 0);
            handle.anchorMax = new Vector2(0, 1);
            handle.sizeDelta = new Vector2(size.y, 0);
            var handleImg = Im(handle, Sp("Green", "Circles"), Color.white, raycast: true);
            var shadow = handle.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.2f);
            shadow.effectDistance = new Vector2(0, -8);

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = handleImg;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0;
            s.maxValue = 1;
            s.value = 0.8f;
            s.navigation = new Navigation { mode = Navigation.Mode.None };
            return s;
        }

        private static Toggle MakeToggle(Transform parent, string name, Vector2 pos, float size)
        {
            var root = Ctr(parent, name, pos.x, pos.y, size, size);
            var sh = Fill(root, "Shadow");
            sh.offsetMin = new Vector2(0, -10);
            sh.offsetMax = new Vector2(0, -10);
            Im(sh, Sp("Square - Cartoon"), Shade, Image.Type.Sliced, 3f);
            var off = Fill(root, "Background");
            var offImg = Im(off, Sp("Gray", "Square"), Color.white, raycast: true);
            var on = Fill(root, "On");
            Im(on, Sp("Blue", "Square"), Color.white);
            var check = Ctr(on, "Checkmark", 0, 0, size * 0.55f, size * 0.55f);
            Im(check, Sp("Checkmark Cartoon"), Color.white, preserve: true);

            var t = root.gameObject.AddComponent<Toggle>();
            t.targetGraphic = offImg;
            t.graphic = on.GetComponent<Image>();
            t.transition = Selectable.Transition.None;
            t.navigation = new Navigation { mode = Navigation.Mode.None };
            t.isOn = true;
            return t;
        }

        /// <summary>A round coloured icon (circle button art + white glyph) used as the row icon.</summary>
        private static void RoundIcon(Transform parent, string circleColor, Sprite glyph, Vector2 pos, float size)
        {
            var root = Ctr(parent, "Icon", pos.x, pos.y, size, size);
            var sh = Fill(root, "Shadow");
            sh.offsetMin = new Vector2(0, -8);
            sh.offsetMax = new Vector2(0, -8);
            Im(sh, Sp("Circle - Cartoon"), ShadeBtn);
            Im(Fill(root, "Circle"), Sp(circleColor, "Circles"), Color.white);
            Icon(root, "Glyph", glyph, Mid, new Vector2(0, 2), new Vector2(size * 0.52f, size * 0.52f));
        }

        private static RectTransform SettingsRow(Transform parent, string name, float yFromTop, float width)
        {
            var row = Top(parent, name, 0, yFromTop, width, 140);
            Im(Fill(row, "Background"), Sp("Rounded Rectangle - 256px"), CardBg, Image.Type.Sliced, 2f);
            Im(Fill(row, "Glow"), Sp("Inner Glow - Rounded Rectangle"), CardGlow, Image.Type.Sliced, 2f);
            return row;
        }

        private class SettingsControls
        {
            public Slider Music, Sfx;
            public Toggle Haptics;
        }

        /// <summary>Music / Sound FX sliders and Haptics toggle, shaped to match SettingsManager's fields.</summary>
        private static SettingsControls MakeSettingsRows(Transform card, float firstRowY, float width)
        {
            var c = new SettingsControls();
            float step = 156f;
            float iconX = -width / 2f + 80f;
            float bodyX = -width / 2f + 160f;                 // left edge of label / slider
            float bodyW = width - 160f - 36f;
            float bodyC = bodyX + bodyW / 2f;

            var r1 = SettingsRow(card, "MusicRow", firstRowY, width);
            RoundIcon(r1, "Green", Sp("Music", "Icons White/Media"), new Vector2(iconX, 0), 100);
            Tx(r1, "MusicLabel", "Music", 40, Ink, null, TextAlignmentOptions.Left).rectTransform.Setup(bodyC, 36, bodyW, 50);
            c.Music = MakeSlider(r1, "MusicSlider", new Vector2(bodyC, -26), new Vector2(bodyW, 72));

            var r2 = SettingsRow(card, "SfxRow", firstRowY + step, width);
            RoundIcon(r2, "Orange", Sp("Sound On", "Icons White/Media"), new Vector2(iconX, 0), 100);
            Tx(r2, "SfxLabel", "Sound FX", 40, Ink, null, TextAlignmentOptions.Left).rectTransform.Setup(bodyC, 36, bodyW, 50);
            c.Sfx = MakeSlider(r2, "SFXSlider", new Vector2(bodyC, -26), new Vector2(bodyW, 72));

            var r3 = SettingsRow(card, "HapticsRow", firstRowY + step * 2, width);
            RoundIcon(r3, "Purple", Sp("Mobile", "Icons Colored/Media"), new Vector2(iconX, 0), 100);
            Tx(r3, "HapticsLabel", "Haptics", 40, Ink, null, TextAlignmentOptions.Left).rectTransform.Setup(bodyX + 130f, 0, 260, 60);
            c.Haptics = MakeToggle(r3, "HapticsToggle", new Vector2(width / 2f - 90f, 0), 90);
            return c;
        }

        private static void Setup(this RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = Mid;
            r.pivot = Mid;
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        // ── Prefab IO ────────────────────────────────────────────────────────

        private static GameObject SavePrefab(GameObject root, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  LOBBY
        // ═════════════════════════════════════════════════════════════════════

        public static void BuildLobbyUI()
        {
            BuildShopCardPrefab();
            BuildShopTabPrefab();
            BuildDailyRewardPrefab();
            BuildSettingsPrefab();
            BuildShopPanelPrefab();
            AssignShopProductIcons();
            AssetDatabase.SaveAssets();
        }

        // ── Daily Reward ─────────────────────────────────────────────────────

        private static void BuildDailyRewardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(DAILY_PREFAB);
            try
            {
                Clear(root.transform);

                // Keep the root canvas in World Space (its original setting): once the panel is parented under the
                // scene canvas it's a nested canvas, and an Overlay root would make Unity drive/zero its RectTransform.
                var rootCanvas = root.GetComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.WorldSpace;
                var rootRect = (RectTransform)root.transform;
                Stretch(rootRect);
                rootRect.localScale = Vector3.one;
                rootCanvas.overrideSorting = true;
                rootCanvas.sortingOrder = 20;

                var popup = MakePopup(root.transform, "Daily Reward", new Vector2(800, 1400),
                    Sp("Calendar", "Icons Colored"), clouds: true, titleSize: 64f);
                var card = popup.Card;

                var dayText = Tx(card, "DayText", "Day 1", 64, Ink, null);
                dayText.rectTransform.Setup(0, 0, 500, 80);
                dayText.rectTransform.anchorMin = dayText.rectTransform.anchorMax = TopMid;
                dayText.rectTransform.anchoredPosition = new Vector2(0, -236);

                var coin1 = Sp("Coin", "Icons Colored/Coins");
                var days = new (string amount, string sub, Sprite icon)[]
                {
                    ("10", null, coin1),
                    ("25", null, Sp("Coins x2")),
                    ("50", "+ Vacuum", Sp("Gift", "Icons Colored/Gifts")),
                    ("75", null, Sp("Coins x3")),
                    ("100", "+ Spring", Sp("Gift - Pink (Border)")),
                };
                var more = new (string amount, string sub, Sprite icon)[] { ("150", null, Sp("Coins x4")) };

                var highlights = new List<UnityEngine.Object>();
                var all = days.Concat(more).ToArray();
                float[] xs = { -240f, 0f, 240f };
                for (int i = 0; i < 6; i++)
                {
                    float y = i < 3 ? 440f : 730f;
                    var cell = DayCell(card, i + 1, new Vector2(xs[i % 3], y), new Vector2(220, 270), false,
                        all[i].icon, all[i].amount, all[i].sub);
                    highlights.Add(cell.Find("Highlight").gameObject);
                }
                var d7 = DayCell(card, 7, new Vector2(0, 1020), new Vector2(690, 250), true,
                    Sp("Coins - Chest"), "300", "+ ALL BOOSTERS");
                highlights.Add(d7.Find("Highlight").gameObject);

                var claim = RectButton(card, "ClaimButton", "Green", new Vector2(560, 140), "CLAIM!", "Green", 68,
                    new Vector2(0, -1255), TopMid);

                var panel = root.GetComponent<DailyRewardPanel>();
                Set(panel, "claimButton", claim);
                Set(panel, "closeButton", popup.Close);
                Set(panel, "dayText", dayText);
                SetArray(panel, "dayHighlights", highlights);
                Set(panel, "modalTransform", card);
                Set(panel, "canvasGroup", root.GetComponent<CanvasGroup>());
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(root, DAILY_PREFAB);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static RectTransform DayCell(Transform card, int day, Vector2 pos, Vector2 size, bool wide,
            Sprite icon, string amount, string sub)
        {
            var cell = Top(card, "DayCell_" + day, pos.x, pos.y, size.x, size.y);

            var hl = Fill(cell, "Highlight", -8, -8, -8, -8);
            hl.gameObject.SetActive(day == 1);   // DailyRewardPanel.Initialize switches the current day's highlight on
            Im(hl, Sp("Rounded Rectangle - 256px"), Gold, Image.Type.Sliced, 2f);
            Im(Fill(cell, "Bg"), Sp("Rounded Rectangle - 256px"), CardBg, Image.Type.Sliced, 2f);
            Im(Fill(cell, "Glow"), Sp("Inner Glow - Rounded Rectangle"), CardGlow, Image.Type.Sliced, 2f);

            if (!wide)
            {
                var label = Tx(cell, "Text", $"Day {day}", 36, Ink);
                label.rectTransform.Setup(0, size.y / 2f - 34, size.x, 44);
                Icon(cell, "Icon", icon, Mid, new Vector2(0, 14), new Vector2(120, 120));
                var amt = Tx(cell, "Amount", amount, 50, AmountRed);
                amt.rectTransform.Setup(0, -size.y / 2f + (sub != null ? 62 : 42), size.x, 56);
                if (sub != null)
                {
                    var s = Tx(cell, "Sub", sub, 28, Ink);
                    s.rectTransform.Setup(0, -size.y / 2f + 26, size.x, 34);
                }
            }
            else
            {
                Icon(cell, "Icon", icon, Mid, new Vector2(-size.x / 2f + 140, 0), new Vector2(180, 180));
                var label = Tx(cell, "Text", $"Day {day}", 40, Ink, null, TextAlignmentOptions.Left);
                label.rectTransform.Setup(70, 72, 380, 50);
                var amt = Tx(cell, "Amount", amount, 76, AmountRed, null, TextAlignmentOptions.Left);
                amt.rectTransform.Setup(70, 8, 380, 90);
                var s = Tx(cell, "Sub", sub, 34, Ink, null, TextAlignmentOptions.Left);
                s.rectTransform.Setup(70, -58, 380, 44);
            }
            return cell;
        }

        // ── Settings ─────────────────────────────────────────────────────────

        private static void BuildSettingsPrefab()
        {
            var root = MakePanelRoot("SettingsPanel", null);
            var popup = MakePopup(root.transform, "Settings", new Vector2(800, 760),
                Sp("Settings", "Icons Colored"), clouds: true);
            var c = MakeSettingsRows(popup.Card, 270, 680);

            var mgr = root.AddComponent<SettingsManager>();
            Set(mgr, "musicSlider", c.Music);
            Set(mgr, "sfxSlider", c.Sfx);
            Set(mgr, "hapticsToggle", c.Haptics);

            OnClick(popup.Close, root.GetComponent<UIAnimator>().ClosePanel);
            SavePrefab(root, SETTINGS_PREFAB);
        }

        // ── Shop ─────────────────────────────────────────────────────────────

        private static void BuildShopTabPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(TAB_PREFAB);
            try
            {
                Clear(root.transform);
                var oldImg = root.GetComponent<Image>();
                if (oldImg) UnityEngine.Object.DestroyImmediate(oldImg);
                var rt = (RectTransform)root.transform;
                rt.sizeDelta = new Vector2(190, 100);

                MakeButton(rt, Sp("Blue (Teal)", "Rectangles"), false);
                TxFill(rt, "Tab_Label", "Tab", 40, Color.white, "Navy", inset: 16);

                var le = Ensure<LayoutElement>(root);
                le.minHeight = 100;
                le.preferredHeight = 100;
                le.flexibleWidth = 1;
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(root, TAB_PREFAB);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildShopCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CARD_PREFAB);
            try
            {
                Clear(root.transform);
                var rt = (RectTransform)root.transform;
                rt.sizeDelta = new Vector2(780, 200);

                var bgImg = root.GetComponent<Image>();
                bgImg.sprite = Sp("Rounded Rectangle - 256px");
                bgImg.type = Image.Type.Sliced;
                bgImg.pixelsPerUnitMultiplier = 2f;
                bgImg.color = CardBg;
                bgImg.raycastTarget = true;
                Im(Fill(rt, "Glow"), Sp("Inner Glow - Rounded Rectangle"), CardGlow, Image.Type.Sliced, 2f);

                // Item icon: white ring + pastel disc + the product icon.
                var iconRoot = At(rt, "IconFrame", new Vector2(0, 0.5f), new Vector2(105, 0), new Vector2(150, 150));
                var ish = Fill(iconRoot, "Shadow");
                ish.offsetMin = new Vector2(0, -10);
                ish.offsetMax = new Vector2(0, -10);
                Im(ish, Sp("Circle - Cartoon"), Shade);
                Im(Fill(iconRoot, "Border"), Sp("Circle - Cartoon"), Color.white);
                Im(Fill(iconRoot, "Background (Color)", 10, 10, 10, 10), Sp("Circle - Cartoon"), new Color32(0xA5, 0xD8, 0xFF, 255));
                var icon = Ctr(iconRoot, "Icon", 0, 0, 100, 100);
                var iconImg = Im(icon, null, Color.white, preserve: true);
                iconImg.enabled = false;

                var name = Tx(rt, "Name_Label", "Name", 44, Pink, null, TextAlignmentOptions.Left, auto: true);
                name.rectTransform.Setup(-30, 28, 330, 56);
                name.rectTransform.anchorMin = name.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

                // Badges
                var best = Badge(rt, "BestValueBadge", "BEST VALUE", Gold, "Orange", new Vector2(-30, -26));
                var popular = Badge(rt, "MostPopularBadge", "POPULAR", new Color32(0x4D, 0xC9, 0xF6, 255), "Navy", new Vector2(-30, -26));
                best.SetActive(false);
                popular.SetActive(false);

                // Buy button (price) with an owned overlay
                var btnRoot = At(rt, "PriceButton", new Vector2(1, 0.5f), new Vector2(-135, 0), new Vector2(230, 100));
                var buy = MakeButton(btnRoot, Sp("Green", "Rectangles"), false);
                var price = TxFill(btnRoot, "Price_Label", "Price", 40, Color.white, "Green", inset: 16);
                var owned = TxFill(btnRoot, "Owned_Label", "OWNED", 38, Color.white, "Navy", inset: 12);
                owned.gameObject.SetActive(false);

                var le = Ensure<LayoutElement>(root);
                le.minHeight = 200;
                le.preferredHeight = 200;

                var card = root.GetComponent<ShopProductCard>();
                Set(card, "iconImage", iconImg);
                Set(card, "nameText", name);
                Set(card, "priceText", price);
                Set(card, "buyButton", buy);
                Set(card, "bestValueBadge", best);
                Set(card, "mostPopularBadge", popular);
                Set(card, "ownedLabel", owned.gameObject);
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(root, CARD_PREFAB);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject Badge(Transform parent, string name, string text, Color fill, string mat, Vector2 pos)
        {
            var b = Ctr(parent, name, pos.x, pos.y, 250, 46);
            Im(b, Sp("Rounded Rectangle - 256px"), fill, Image.Type.Sliced, 4f);
            TxFill(b, "Text", text, 28, Color.white, mat, inset: 10);
            return b.gameObject;
        }

        private static void BuildShopPanelPrefab()
        {
            var root = MakePanelRoot("ShopPanel", null);
            var popup = MakePopup(root.transform, "Shop", new Vector2(860, 1500),
                Sp("Shop", "Icons Colored"), clouds: true);
            var card = popup.Card;

            // Tabs
            var tabs = Top(card, "TabContainer", 0, 250, 780, 100);
            var hl = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;

            // Scroll area
            var scroll = At(card, "ScrollView", Mid, new Vector2(0, -130), new Vector2(780, 1000));
            scroll.anchorMin = new Vector2(0.5f, 0);
            scroll.anchorMax = new Vector2(0.5f, 1);
            scroll.offsetMin = new Vector2(-390, 90);
            scroll.offsetMax = new Vector2(390, -330);
            Im(scroll, Sp("Rounded Rectangle - 256px"), new Color(1, 1, 1, 0.2f), Image.Type.Sliced, 2f, raycast: true);

            var viewport = Fill(scroll, "Viewport", 0, 8, 0, 8);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rt(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 16;
            vl.padding = new RectOffset(0, 0, 8, 8);
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40f;

            // Hook up the ShopPanel
            var panel = root.AddComponent<ShopPanel>();
            var dbPath = "Assets/MatchThemAllTemplate/Resources/Shop/ShopDatabase.asset";
            Set(panel, "database", AssetDatabase.LoadAssetAtPath<ShopDatabaseSO>(dbPath));
            Set(panel, "cardPrefab", AssetDatabase.LoadAssetAtPath<ShopProductCard>(CARD_PREFAB));
            Set(panel, "cardContainer", content);
            Set(panel, "tabButtonPrefab", AssetDatabase.LoadAssetAtPath<Button>(TAB_PREFAB));
            Set(panel, "tabContainer", tabs);
            Set(panel, "closeButton", popup.Close);
            Set(panel, "root", root);

            SavePrefab(root, SHOP_PREFAB);
        }

        /// <summary>The shop products ship without icons; fill empty ones with matching art so cards aren't blank.</summary>
        private static void AssignShopProductIcons()
        {
            var map = new Dictionary<string, Sprite>
            {
                ["bundle_starter"] = Sp("Gift - Yellow (Border)"),
                ["coins_500"] = Sp("Coins - Bag"),
                ["gems_2x_starter"] = Sp("Gem - Blue"),
                ["powerup_fan_pack"] = Sp("Bolt - Blue (Border)"),
                ["powerup_freeze_pack"] = Sp("Potion - Blue (Border)"),
                ["powerup_spring_pack"] = Sp("Rocket (Border)"),
                ["powerup_vacuum_pack"] = Sp("Magnet"),
                ["remove_ads"] = Sp("Crown (Border)"),
            };
            foreach (var guid in AssetDatabase.FindAssets("t:ShopProductSO"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ShopProductSO>(p);
                if (!so || so.Icon || !map.TryGetValue(so.id, out var sprite)) continue;
                Set(so, "icon", sprite);
                EditorUtility.SetDirty(so);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  MAIN SCENE
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Rebuilds a scene panel's children in place so its root components and script references stay intact.</summary>
        private static Transform ResetPanel(Transform canvas, string name)
        {
            var p = canvas.Find(name) ?? throw new Exception($"[CartoonUI] Panel '{name}' not found under {canvas.name}");
            Clear(p);
            return p;
        }

        public static void BuildMainUI(Transform mainCanvas)
        {
            BuildPausePanel(ResetPanel(mainCanvas, "Pause"));
            BuildWinPanel(ResetPanel(mainCanvas, "Level Complete"));
            BuildLosePanel(ResetPanel(mainCanvas, "Game Over"));
            BuildContinuePanel(ResetPanel(mainCanvas, "ContinuePanel"));
            BuildPowerupBadges();
        }

        // ── Power-up counters (world-space badges above each power-up) ──────

        private const string POWERUP_PREFAB_DIR = PREFAB_DIR + "/Power Up";

        private static SpriteRenderer WorldSprite(Transform parent, string name, Sprite s, Color c, int order, Vector3 pos, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Restyles the three prefabs behind each power-up's counter: the badge disc (Container), the number
        /// (Amount Text) and the "watch a video" badge (Video Icon). Positions/scales on the scene instances are untouched.
        /// </summary>
        public static void BuildPowerupBadges()
        {
            var ring = Sp("Circle - Cartoon");

            // Number badge: white ring + red disc + soft shadow (the pack's "Amount Circle - Red").
            var container = PrefabUtility.LoadPrefabContents(POWERUP_PREFAB_DIR + "/Container.prefab");
            try
            {
                Clear(container.transform);
                var sr = container.GetComponent<SpriteRenderer>();
                sr.sprite = ring;
                sr.color = Color.white;
                sr.sortingOrder = 0;
                WorldSprite(container.transform, "Shadow", ring, new Color(0, 0, 0, 0.2f), -1, new Vector3(0, -0.14f, 0), 1f);
                WorldSprite(container.transform, "Fill", ring, new Color32(0xFF, 0x51, 0x69, 255), 1, Vector3.zero, 0.82f);
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(container, POWERUP_PREFAB_DIR + "/Container.prefab");
                PrefabUtility.UnloadPrefabContents(container);
            }

            // The number itself: LilitaOne with the project's outlined material, drawn above the disc.
            var amount = PrefabUtility.LoadPrefabContents(POWERUP_PREFAB_DIR + "/Amount Text.prefab");
            try
            {
                var tm = amount.GetComponent<TextMeshPro>();
                tm.font = Font;
                tm.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{FONT_MAT_DIR}/LilitaOne SDF - Pink.mat");
                tm.color = Color.white;
                tm.sortingOrder = 2;
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(amount, POWERUP_PREFAB_DIR + "/Amount Text.prefab");
                PrefabUtility.UnloadPrefabContents(amount);
            }

            // Shown instead of the number when the player has none: the project's video icon on a light badge.
            var video = PrefabUtility.LoadPrefabContents(POWERUP_PREFAB_DIR + "/Video Icon.prefab");
            try
            {
                Clear(video.transform);
                // Load by name: reading it back off the root broke re-runs (the root already holds the ring after the first run).
                var clapper = Sp("Video", "Icons Colored/Media");
                var sr = video.GetComponent<SpriteRenderer>();
                sr.sprite = ring;
                sr.color = Color.white;
                sr.sortingOrder = 0;
                video.transform.localScale = Vector3.one * 0.18f;
                WorldSprite(video.transform, "Shadow", ring, new Color(0, 0, 0, 0.2f), -1, new Vector3(0, -0.14f, 0), 1f);
                WorldSprite(video.transform, "Fill", ring, CardBg, 1, Vector3.zero, 0.82f);
                WorldSprite(video.transform, "Clapper", clapper, Color.white, 2, Vector3.zero, 1.1f);
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(video, POWERUP_PREFAB_DIR + "/Video Icon.prefab");
                PrefabUtility.UnloadPrefabContents(video);
            }
        }

        // ── Pause ────────────────────────────────────────────────────────────

        private static void BuildPausePanel(Transform root)
        {
            var popup = MakePopup(root, "Paused", new Vector2(800, 1060), Sp("Clock - Indigo"), clouds: true);
            var c = MakeSettingsRows(popup.Card, 270, 680);

            var resume = RectButton(popup.Card, "Resume Button", "Green", new Vector2(560, 130), "Resume", "Green", 56,
                new Vector2(0, -775), TopMid);
            var quit = RectButton(popup.Card, "Quit Button", "Pink", new Vector2(560, 130), "Quit", "Pink", 56,
                new Vector2(0, -920), TopMid);

            var gm = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            foreach (var b in new[] { resume, popup.Close }) { ClearClicks(b); OnClick(b, gm.ResumeGame); }
            ClearClicks(quit);
            OnClick(quit, gm.QuitToMenuCallback);

            // The scene's SettingsManager (on UIManager) drives these controls.
            foreach (var sm in UnityEngine.Object.FindObjectsByType<SettingsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sm.gameObject.scene != root.gameObject.scene) continue;
                Set(sm, "musicSlider", c.Music);
                Set(sm, "sfxSlider", c.Sfx);
                Set(sm, "hapticsToggle", c.Haptics);
            }
        }

        // ── Level Complete ───────────────────────────────────────────────────

        private static void BuildWinPanel(Transform root)
        {
            var popup = MakePopup(root, "Level Complete", new Vector2(800, 1160), null, clouds: false,
                frameTop: 140f, whiteTop: 180f, whiteBottom: 25f, headlineY: 310f, titleSize: 58f);
            var card = popup.Card;
            Destroy(popup.Close.gameObject);

            // Rotating shine behind the whole popup (first child so it renders underneath).
            var shine = Ctr(card, "Radial Shine", 0, 200, 1000, 1000);
            Im(shine, Sp("Radial Shine"), new Color32(0x67, 0xF4, 0xFF, 255));
            shine.SetAsFirstSibling();
            shine.gameObject.AddComponent<UISimpleMotion>().Setup(UISimpleMotion.Mode.Rotate, 12f, 0f);

            // Stars (WinPanelManager scales these in one by one).
            var stars = new List<UnityEngine.Object>();
            var star = Sp("Star Yellow");
            stars.Add(MakeStar(card, "Star 1", star, new Vector2(-215, 100), 200));
            stars.Add(MakeStar(card, "Star 2", star, new Vector2(0, 56), 240));
            stars.Add(MakeStar(card, "Star 3", star, new Vector2(215, 100), 200));

            // Cheerful emoji + speech bubble
            Icon(card, "Emoji", Sp("Big Smile - Cartoon"), TopMid, new Vector2(-70, -590), new Vector2(210, 210));
            MakeBubble(card, "Yay!", new Vector2(120, -490), Ink);

            var next = RectButton(card, "Next Button", "Green", new Vector2(560, 140), "NEXT LEVEL", "Green", 54,
                new Vector2(0, -820), TopMid);
            var select = RectButton(card, "Level Select", "Blue (Teal)", new Vector2(560, 120), "LEVEL SELECT", "Navy", 46,
                new Vector2(0, -975), TopMid);

            var win = root.GetComponent<WinPanelManager>();
            SetArray(win, "starObjects", stars);
            Set(win, "unearnedStarSprite", Sp("Star Gray"));
            ClearClicks(next);
            ClearClicks(select);
            OnClick(next, win.OnNextLevelClicked);
            OnClick(select, win.OnLevelSelectClicked);
        }

        private static GameObject MakeStar(Transform parent, string name, Sprite sprite, Vector2 posFromTop, float size)
        {
            var r = Top(parent, name, posFromTop.x, posFromTop.y, size, size);
            Im(r, sprite, Color.white, preserve: true);
            var sh = r.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.1f);
            sh.effectDistance = new Vector2(0, -10);
            return r.gameObject;
        }

        private static void MakeBubble(Transform parent, string text, Vector2 anchoredFromTop, Color textColor)
        {
            var b = Icon(parent, "Speech Bubble", Sp("Speech Bubble - Right"), TopMid, anchoredFromTop, new Vector2(170, 170));
            var t = Tx(b, "Text", text, 40, textColor);
            Stretch(t.rectTransform, 0, 0, 0, 36);
        }

        // ── Game Over ────────────────────────────────────────────────────────

        private static void BuildLosePanel(Transform root)
        {
            var popup = MakePopup(root, "Game Over", new Vector2(800, 1160), null, clouds: false,
                frameTop: 140f, whiteTop: 180f, whiteBottom: 25f, headlineY: 310f, titleSize: 64f);
            var card = popup.Card;
            Destroy(popup.Close.gameObject);

            var skull = Sp("Skull (Border)");
            MakeDecor(card, "Skull 1", skull, new Vector2(-215, 100), 160);
            var big = MakeDecor(card, "Skull 2", skull, new Vector2(0, 56), 250);
            MakeDecor(card, "Skull 3", skull, new Vector2(215, 100), 160);
            foreach (float x in new[] { -45f, 45f })
            {
                var glow = Ctr(big, "Eye Glow", x, 0, 30, 30);
                Im(glow, Sp("Shine"), new Color32(0xC0, 0x30, 0x4A, 255));
            }

            Icon(card, "Emoji", Sp("Sad - Cartoon"), TopMid, new Vector2(-70, -590), new Vector2(210, 210));
            MakeBubble(card, "Oh no!", new Vector2(120, -490), new Color32(0x49, 0x50, 0x57, 255));

            var retry = RectButton(card, "Retry Button", "Green", new Vector2(560, 140), "RETRY", "Green", 60,
                new Vector2(0, -820), TopMid);
            var select = RectButton(card, "Level Select", "Blue (Teal)", new Vector2(560, 120), "LEVEL SELECT", "Navy", 46,
                new Vector2(0, -975), TopMid);

            var lose = root.GetComponent<LosePanelManager>();
            ClearClicks(retry);
            ClearClicks(select);
            OnClick(retry, lose.OnRetryClicked);
            OnClick(select, lose.OnLevelSelectClicked);
        }

        private static RectTransform MakeDecor(Transform parent, string name, Sprite sprite, Vector2 posFromTop, float size)
        {
            var r = Icon(parent, name, sprite, TopMid, new Vector2(posFromTop.x, -posFromTop.y), new Vector2(size, size));
            return r;
        }

        // ── Continue (out of time) ───────────────────────────────────────────

        private static void BuildContinuePanel(Transform root)
        {
            var popup = MakePopup(root, "Out of Time!", new Vector2(800, 1120), Sp("Clock - Yellow"), clouds: true,
                titleSize: 64f);
            var card = popup.Card;
            Destroy(popup.Close.gameObject);   // the player must pick Continue or Give Up

            var caption = Tx(card, "Caption", "Keep playing?", 64, Ink);
            caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = TopMid;
            caption.rectTransform.sizeDelta = new Vector2(640, 80);
            caption.rectTransform.anchoredPosition = new Vector2(0, -280);

            Icon(card, "Emoji", Sp("Fear - Cartoon"), TopMid, new Vector2(0, -470), new Vector2(200, 200));

            var buttons = Top(card, "Buttons", 0, 790, 620, 440);
            buttons.pivot = TopMid;
            buttons.anchoredPosition = new Vector2(0, -600);
            var vl = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 24;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = false;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = false;
            vl.childForceExpandHeight = false;

            // Pay with coins: [coin] cost
            var pay = RectButton(buttons, "PayCoinsButton", "Yellow", new Vector2(600, 130), "", "Orange", 56);
            Destroy(pay.transform.Find("Text").gameObject);
            Icon(pay.transform, "CoinIcon", Sp("Coin", "Icons Colored/Coins"), Mid, new Vector2(-140, 4), new Vector2(86, 86));
            var cost = Tx(pay.transform, "CoinsCostText", "900", 64, Color.white, "Orange");
            cost.rectTransform.Setup(40, 4, 300, 90);

            var ad = RectButton(buttons, "WatchAdButton", "Blue (Teal)", new Vector2(600, 130), "Watch Ad   +30s", "Navy", 46);
            var give = RectButton(buttons, "GiveUpButton", "Pink", new Vector2(600, 110), "Give Up", "Pink", 48);

            var mgr = UnityEngine.Object.FindObjectsByType<ContinuePanelManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(m => m.gameObject.scene == root.gameObject.scene);
            Set(mgr, "continuePanel", root.gameObject);
            Set(mgr, "watchAdButton", ad);
            Set(mgr, "payCoinsButton", pay);
            Set(mgr, "giveUpButton", give);
            Set(mgr, "coinsCostText", cost);
        }

        private static void Destroy(GameObject go) => UnityEngine.Object.DestroyImmediate(go);

        // ── Scene wiring ─────────────────────────────────────────────────────

        /// <summary>Swaps the Lobby's placeholder Settings/Shop panels for the new prefab instances and re-points their references.</summary>
        public static void WireLobbyScene(Transform canvas)
        {
            var panels = (RectTransform)canvas.Find("Panels");
            Stretch(panels);   // the old container carried a stray size offset; the new panels are plain full-screen overlays

            var settings = ReplacePanel(panels, "SettingsPanel", SETTINGS_PREFAB);
            var shop = ReplacePanel(panels, "ShopPanel", SHOP_PREFAB);

            var scene = canvas.gameObject.scene;
            foreach (var mm in UnityEngine.Object.FindObjectsByType<MainMenuManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mm.gameObject.scene == scene) Set(mm, "settingsPanel", settings);
            foreach (var op in UnityEngine.Object.FindObjectsByType<ShopOpener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (op.gameObject.scene == scene) Set(op, "shopPanel", shop.GetComponent<ShopPanel>());

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        private static GameObject ReplacePanel(Transform parent, string name, string prefabPath)
        {
            var old = parent.Find(name);
            int index = old ? old.GetSiblingIndex() : parent.childCount;
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), parent);
            inst.name = name;
            inst.transform.SetSiblingIndex(index);
            inst.SetActive(false);
            return inst;
        }

        // ── Menu entries ─────────────────────────────────────────────────────

        [MenuItem("Tools/Cartoon UI/Build Lobby UI")]
        public static void BuildLobbyAll()
        {
            BuildLobbyUI();
            WireLobbyScene(GameObject.Find("Canvas").transform);
        }

        [MenuItem("Tools/Cartoon UI/Build Main UI")]
        public static void BuildMainAll()
        {
            var canvas = GameObject.Find("Main Canvas") ?? throw new Exception("[CartoonUI] Open MainScene first.");
            BuildMainUI(canvas.transform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
        }
    }
}
#endif
