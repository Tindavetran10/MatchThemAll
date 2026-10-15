using System.Collections.Generic;
using System.IO;
using MatchThemAll.Scripts.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.EditorTools
{
    /// <summary>
    /// Re-skins the five game scenes with the GUIPackCartoon art set.
    /// Every step is idempotent (find-or-create by name) and restyles the EXISTING objects in place,
    /// so all serialized references and Button.onClick bindings keep working.
    /// Menu: Tools ▸ Match Them All ▸ UI Overhaul.
    /// </summary>
    public static class UIOverhaulBuilder
    {
        // ───────────────────────────────── Paths ─────────────────────────────────
        private const string Gui = "Assets/GUIPackCartoon/Demo/Sprites/";
        private const string Art = "Assets/MatchThemAllTemplate/Art/UI/";
        private const string Scenes = "Assets/MatchThemAllTemplate/Scenes/";
        private const string NodePrefabPath = "Assets/MatchThemAllTemplate/Prefabs/UI/Level/LevelMapNode.prefab";
        private const string FontPath = "Assets/GUIPackCartoon/Demo/Fonts/LilitaOne - Regular SDF.asset";

        // ──────────────────────────────── Palette ────────────────────────────────
        private static readonly Color Navy       = Hex("1B2A6B");
        private static readonly Color NavyDeep   = Hex("16205A");
        private static readonly Color GreenDark  = Hex("2D7A0E");
        private static readonly Color OrangeDark = Hex("A84300");
        private static readonly Color PinkDark   = Hex("9A1B5A");
        private static readonly Color GoldTop    = Hex("FFF27A");
        private static readonly Color GoldBottom = Hex("FFB61E");

        // ───────────────────────────────── Menu ──────────────────────────────────
        [MenuItem("Tools/Match Them All/UI Overhaul/Build All Scenes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildScene("OpenScene", BuildOpen);
            BuildScene("Lobby", BuildLobby);
            BuildScene("LoadingScene", BuildLoading);
            BuildNodePrefab();
            BuildScene("LevelSelect", BuildLevelSelect);
            BuildScene("MainScene", BuildMain);
            AssetDatabase.SaveAssets();
            Debug.Log("[UIOverhaul] All scenes rebuilt.");
        }

        [MenuItem("Tools/Match Them All/UI Overhaul/Open Scene")]   public static void MenuOpen()    => BuildScene("OpenScene", BuildOpen);
        [MenuItem("Tools/Match Them All/UI Overhaul/Lobby")]        public static void MenuLobby()   => BuildScene("Lobby", BuildLobby);
        [MenuItem("Tools/Match Them All/UI Overhaul/Loading")]      public static void MenuLoading() => BuildScene("LoadingScene", BuildLoading);
        [MenuItem("Tools/Match Them All/UI Overhaul/Level Select")] public static void MenuLevels()  { BuildNodePrefab(); BuildScene("LevelSelect", BuildLevelSelect); }
        [MenuItem("Tools/Match Them All/UI Overhaul/Main Scene")]   public static void MenuMain()    => BuildScene("MainScene", BuildMain);

        /// <summary>Opens (or reuses) a scene, applies the builder, saves. Leaves the user's active scene as it was.</summary>
        public static void BuildScene(string sceneName, System.Action<UnityEngine.SceneManagement.Scene> build)
        {
            string path = Scenes + sceneName + ".unity";
            var scene = EditorSceneManager.GetSceneByPath(path);
            bool openedHere = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                openedHere = true;
            }
            build(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }

        // ═════════════════════════════ OPEN SCENE ════════════════════════════════
        public static void BuildOpen(UnityEngine.SceneManagement.Scene scene)
        {
            var canvas = FindRoot<Canvas>(scene, "Canvas");
            SetScaler(canvas, 0.5f);
            var root = (RectTransform)canvas.transform;

            // Gradient sky → violet
            var bg = Node(root, "Background"); Stretch(bg); bg.SetSiblingIndex(0);
            Img(bg, Gradient("BG_Open", Hex("2EC5FF"), Hex("3B7DF4"), Hex("7A42E8")), Color.white);

            var decor = Node(root, "Decor"); Stretch(decor); decor.SetSiblingIndex(1);
            Bubbles(decor, 7, 14, 0.9f);
            Clouds(decor);

            var group = Node(root, "TitleGroup");
            Place(group, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(1000, 760));
            group.SetSiblingIndex(2);

            var shine = Node(group, "Shine"); Place(shine, Center, Center, new Vector2(0, 20), new Vector2(1500, 1500));
            Img(shine, Spr("Shapes/Effects/Radial Shine"), new Color(1, 1, 1, 0.28f));
            Motion(shine, UISimpleMotion.Mode.Rotate, 8f, 0f);
            shine.SetSiblingIndex(0);

            var glow = Node(group, "Glow"); Place(glow, Center, Center, new Vector2(0, 20), new Vector2(1000, 800));
            Img(glow, Spr("Shapes/Effects/Shine"), new Color(1, 1, 1, 0.45f));
            glow.SetSiblingIndex(1);

            // Existing TitleText (child of Banner) → becomes the main title
            var title = FindDeep(root, "TitleText") as RectTransform;
            var banner = FindDeep(root, "Banner") as RectTransform;
            if (title)
            {
                title.SetParent(group, false);
                Place(title, Center, Center, new Vector2(0, 90), new Vector2(1000, 460));
                TitleStyle(title.GetComponent<TextMeshProUGUI>(), 200f);
                Motion(title, UISimpleMotion.Mode.Float, 10f, 0.35f);
            }
            if (banner)
            {
                banner.SetParent(group, false);
                Place(banner, Center, Center, new Vector2(0, -230), new Vector2(760, 170));
                Img(banner, Spr("Backgrounds/Popup/Headline - Flag"), Color.white, type: Image.Type.Sliced, ppum: 1f);
                var sub = Node(banner, "Subtitle"); Stretch(sub, 80, 80, 20, 45);
                Txt(sub, "A Match 3D Game", 62, Color.white, OutlineMat("Pink", PinkDark));
            }

            var footer = Node(root, "Footer");
            Place(footer, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 340), new Vector2(800, 80));
            Txt(footer, "Tap. Match. Clear!", 44, Color.white, OutlineMat("Navy", Navy));
            footer.SetAsLastSibling();
        }

        // ═════════════════════════════ LOBBY ═════════════════════════════════════
        public static void BuildLobby(UnityEngine.SceneManagement.Scene scene)
        {
            var canvas = FindRoot<Canvas>(scene, "Canvas");
            SetScaler(canvas, 0.5f);
            var root = (RectTransform)canvas.transform;

            var bg = Node(root, "Background"); Stretch(bg); bg.SetSiblingIndex(0);
            Img(bg, Gradient("BG_Lobby", Hex("62DCFF"), Hex("3FA2F7"), Hex("3466E6")), Color.white);

            var decor = Node(root, "Decor"); Stretch(decor); decor.SetSiblingIndex(1);
            var shine = Node(decor, "Shine"); Place(shine, Center, Center, new Vector2(0, 120), new Vector2(1600, 1600));
            Img(shine, Spr("Shapes/Effects/Radial Shine"), new Color(1, 1, 1, 0.22f));
            Motion(shine, UISimpleMotion.Mode.Rotate, 6f, 0f);
            Bubbles(decor, 21, 8, 0.75f);
            Clouds(decor);

            // Logo
            var logo = Node(root, "Logo"); logo.SetSiblingIndex(2);
            Place(logo, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1000, 420));
            var lt = Node(logo, "Title"); Place(lt, Center, Center, new Vector2(0, 30), new Vector2(1000, 340));
            TitleStyle(Txt(lt, "MATCH\nTHEM ALL", 150, Color.white, null), 150f);
            Motion(lt, UISimpleMotion.Mode.Float, 8f, 0.35f);

            // Hero: a pile of colourful items (what you match in-game)
            var hero = Node(root, "Hero"); hero.SetSiblingIndex(3);
            Place(hero, Center, Center, new Vector2(0, 40), new Vector2(700, 600));
            var heroGlow = Node(hero, "Glow"); Place(heroGlow, Center, Center, Vector2.zero, new Vector2(900, 800));
            Img(heroGlow, Spr("Shapes/Effects/Shine"), new Color(1, 1, 1, 0.55f)); heroGlow.SetSiblingIndex(0);
            HeroItem(hero, "Box", "Icons/Icons Colored/Storage/Box - Brown", new Vector2(0, -120), 360, 0, 0f);
            HeroItem(hero, "Gift", "Icons/Icons Colored/Gifts/Gift", new Vector2(-150, 70), 230, 8, 0.1f);
            HeroItem(hero, "Donut", "Icons/Icons Colored/Donuts/Donut - Blue", new Vector2(150, 90), 200, -12, 0.35f);
            HeroItem(hero, "IceCream", "Icons/Icons Colored/Food/Ice Cream", new Vector2(-240, -150), 190, 14, 0.6f);
            HeroItem(hero, "Apple", "Icons/Icons Colored/Food/Apple", new Vector2(250, -130), 170, -8, 0.8f);
            HeroItem(hero, "Candy", "Icons/Icons Colored/Candies/Candy - Multicolor Spiral", new Vector2(10, 170), 170, 0, 0.5f);

            // Buttons container must fill the screen so edge anchors work
            var buttons = Node(root, "Buttons"); Stretch(buttons); buttons.SetSiblingIndex(4);

            // Coin pill (top-left) – reuses CoinDisplay
            var coin = Node(buttons, "CoinPill");
            Place(coin, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -70), new Vector2(300, 96));
            Pill(coin);
            var coinIcon = Node(coin, "Icon"); Place(coinIcon, new Vector2(0, 0.5f), Center, new Vector2(4, 0), new Vector2(124, 124));
            Img(coinIcon, Spr("Icons/Icons Colored/Coins/Coin"), Color.white, preserve: true);
            var coinTxt = Node(coin, "CoinText"); Stretch(coinTxt, 70, 20, 0, 4);
            Txt(coinTxt, "0", 54, Color.white, OutlineMat("Navy", Navy));
            if (!coinTxt.GetComponent<CoinDisplay>()) coinTxt.gameObject.AddComponent<CoinDisplay>();

            // Settings (top-right)
            var settings = FindDeep(root, "SettingsButton") as RectTransform;
            if (settings)
            {
                settings.SetParent(buttons, false);
                Place(settings, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -50), new Vector2(140, 140));
                Img(settings, Spr("Buttons/Square/Blue"), Color.white, raycast: true, preserve: true);
                var ic = Node(settings, "Icon"); Stretch(ic, 32, 32, 30, 34);
                Img(ic, Spr("Icons/Icons White/Basic/Settings"), Color.white, preserve: true);
                Punch(settings);
            }

            // Daily Rewards (left) and Shop (right) tiles
            Tile(FindDeep(root, "OpenDailyRewardButton") as RectTransform, buttons, "Daily_Label", "Daily Rewards",
                 "Icons/Icons Colored/Time/Calendar", "Buttons/Rectangles/Green", GreenDark, left: true);
            Tile(FindDeep(root, "ShopOpenerButton") as RectTransform, buttons, "Shop_Label", "Shop",
                 "Icons/Icons Colored/Commerce/Shop", "Buttons/Rectangles/Orange", OrangeDark, left: false);

            // Play (bottom, big green)
            var play = FindDeep(root, "PlayButton") as RectTransform;
            if (play)
            {
                play.SetParent(buttons, false);
                Place(play, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(640, 210));
                Img(play, Spr("Buttons/Rectangles/Green"), Color.white, raycast: true, type: Image.Type.Sliced, ppum: 0.48f);
                var t = play.Find("Text (TMP)") as RectTransform;
                if (t)
                {
                    Stretch(t, 0, 0, 0, 14);
                    Txt(t, "PLAY", 120, Color.white, OutlineMat("Green", GreenDark));
                }
                Punch(play);
            }

            var panels = root.Find("Panels") as RectTransform;
            if (panels) panels.SetAsLastSibling();
        }

        // ═════════════════════════════ LOADING ═══════════════════════════════════
        public static void BuildLoading(UnityEngine.SceneManagement.Scene scene)
        {
            var canvas = FindRoot<Canvas>(scene, "Canvas");
            SetScaler(canvas, 0.5f);
            var root = (RectTransform)canvas.transform;

            var bg = root.Find("Background") as RectTransform; Stretch(bg); bg.SetSiblingIndex(0);
            Img(bg, Gradient("BG_Loading", Hex("7A45E6"), Hex("4B6BF2"), Hex("2E9EFF")), Color.white);

            var decor = Node(root, "Decor"); Stretch(decor); decor.SetSiblingIndex(1);
            var shine = Node(decor, "Shine"); Place(shine, Center, Center, new Vector2(0, 220), new Vector2(1500, 1500));
            Img(shine, Spr("Shapes/Effects/Radial Shine"), new Color(1, 1, 1, 0.2f));
            Motion(shine, UISimpleMotion.Mode.Rotate, 10f, 0f);
            Bubbles(decor, 33, 10, 0.8f);
            Clouds(decor);

            // Logo → title
            var logo = root.Find("Logo") as RectTransform;
            if (logo)
            {
                Place(logo, Center, Center, new Vector2(0, 220), new Vector2(1000, 520));
                var li = logo.GetComponent<Image>(); if (li) { li.sprite = null; li.color = Color.clear; li.raycastTarget = false; }
                var glow = Node(logo, "Glow"); Place(glow, Center, Center, Vector2.zero, new Vector2(1000, 800));
                Img(glow, Spr("Shapes/Effects/Shine"), new Color(1, 1, 1, 0.4f)); glow.SetSiblingIndex(0);
                var t = Node(logo, "Title"); Place(t, Center, Center, new Vector2(0, 40), new Vector2(1000, 420));
                TitleStyle(Txt(t, "MATCH\nTHEM ALL", 180, Color.white, null), 180f);
                Motion(t, UISimpleMotion.Mode.Float, 10f, 0.4f);
            }

            // Spinner + "Loading" label above the bar
            var spinner = Node(root, "Spinner");
            Place(spinner, new Vector2(0.5f, 0), Center, new Vector2(-205, 590), new Vector2(110, 110));
            Img(spinner, Spr("Icons/Icons Colored/Loading Spinner/Loading Spinner"), Color.white, preserve: true);
            Motion(spinner, UISimpleMotion.Mode.Rotate, 220f, 0f);
            var label = Node(root, "LoadingLabel");
            Place(label, new Vector2(0.5f, 0), Center, new Vector2(40, 590), new Vector2(360, 100));
            Txt(label, "Loading...", 64, Color.white, OutlineMat("Navy", Navy), TextAlignmentOptions.Left);

            // Progress bar (Slider)
            var bar = root.Find("ProgressBar") as RectTransform;
            if (bar)
            {
                Place(bar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 430), new Vector2(820, 96));
                var slider = bar.GetComponent<Slider>();
                slider.interactable = false; slider.transition = Selectable.Transition.None;
                slider.direction = Slider.Direction.LeftToRight; slider.handleRect = null;

                var shadow = Node(bar, "Shadow"); Stretch(shadow); shadow.anchoredPosition = new Vector2(0, -8); shadow.SetSiblingIndex(0);
                Img(shadow, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), new Color(0, 0, 0, 0.18f), type: Image.Type.Sliced, ppum: 2.7f);
                var track = bar.Find("Background") as RectTransform; Stretch(track); track.SetSiblingIndex(1);
                Img(track, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), Color.white, type: Image.Type.Sliced, ppum: 2.7f);
                var inner = Node(track, "Inner"); Stretch(inner, 9, 9, 9, 9);
                Img(inner, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), Hex("D9E2F2"), type: Image.Type.Sliced, ppum: 3.3f);

                var area = bar.Find("Fill Area") as RectTransform; Stretch(area, 9, 9, 9, 9); area.SetSiblingIndex(2);
                var fill = area.Find("Fill") as RectTransform;
                fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.sizeDelta = Vector2.zero; fill.anchoredPosition = Vector2.zero;
                Img(fill, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), Hex("5EC72A"), type: Image.Type.Sliced, ppum: 3.3f);
                var hl = Node(fill, "Highlight"); Stretch(hl, 14, 14, 8, 40);
                Img(hl, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), new Color(1, 1, 1, 0.35f), type: Image.Type.Sliced, ppum: 8f);
                slider.fillRect = fill;
                slider.value = 0.35f; // preview value in the editor; runtime overrides it
            }

            var pt = root.Find("ProgressText") as RectTransform;
            if (pt)
            {
                Place(pt, new Vector2(0.5f, 0), Center, new Vector2(0, 478), new Vector2(300, 96));
                Txt(pt, "35%", 50, Color.white, OutlineMat("Green", GreenDark));
                pt.SetAsLastSibling();
            }
            var tip = root.Find("TipText") as RectTransform;
            if (tip)
            {
                Place(tip, new Vector2(0.5f, 0), Center, new Vector2(0, 290), new Vector2(800, 110));
                var tt = Txt(tip, "Tip: Use the Vacuum powerup on your biggest goal!", 38, Color.white, OutlineMat("Navy", Navy));
                tt.textWrappingMode = TextWrappingModes.Normal;
                // Readable backdrop over the clouds
                var tipBg = Node(root, "TipBackdrop");
                Place(tipBg, new Vector2(0.5f, 0), Center, new Vector2(0, 290), new Vector2(880, 140));
                Img(tipBg, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), new Color(0.1f, 0.14f, 0.4f, 0.55f), type: Image.Type.Sliced, ppum: 1.9f);
                tipBg.SetSiblingIndex(tip.GetSiblingIndex());
            }
        }

        // ═════════════════════════════ LEVEL SELECT ══════════════════════════════
        public static void BuildLevelSelect(UnityEngine.SceneManagement.Scene scene)
        {
            var canvas = FindRoot<Canvas>(scene, "Level Saga Map Canvas");
            var root = (RectTransform)canvas.transform;

            var bg = Node(root, "Background"); Stretch(bg); bg.SetSiblingIndex(0);
            Img(bg, Gradient("BG_Levels", Hex("3AB0F7"), Hex("4F7CEE"), Hex("6C4FE0")), Color.white);

            var decor = Node(root, "Decor"); Stretch(decor); decor.SetSiblingIndex(1);
            Bokeh(decor, 5);
            Clouds(decor);

            var path = root.GetComponentInChildren<LevelMapPath>(true);
            if (path)
            {
                path.color = new Color(1, 1, 1, 0.55f);
                var so = new SerializedObject(path);
                var th = so.FindProperty("thickness"); if (th != null) { th.floatValue = 28f; so.ApplyModifiedPropertiesWithoutUndo(); }
            }

            // Header ribbon
            var header = Node(root, "Header");
            Place(header, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(40, -60), new Vector2(480, 150));
            Img(header, Spr("Backgrounds/Popup/Headline - Flag"), Color.white, type: Image.Type.Sliced);
            var ht = Node(header, "Text"); Stretch(ht, 60, 60, 16, 44);
            Txt(ht, "LEVELS", 66, Color.white, OutlineMat("Pink", PinkDark));
            var viewport = root.Find("Viewport"); if (viewport) header.SetSiblingIndex(viewport.GetSiblingIndex() + 1);

            // Back arrow (top-left)
            var back = root.Find("BackButton") as RectTransform;
            if (back)
            {
                Place(back, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -55), new Vector2(140, 140));
                back.pivot = new Vector2(0, 1); back.anchoredPosition = new Vector2(40, -55);
                Img(back, Spr("Buttons/Circles/Orange"), Color.white, raycast: true, preserve: true);
                var arrow = back.Find("ArrowIcon") as RectTransform;
                if (arrow)
                {
                    Stretch(arrow, 34, 38, 36, 38);
                    arrow.localRotation = Quaternion.identity;
                    Img(arrow, Spr("Icons/Icons White/Arrows/Arrow - Left"), Color.white, preserve: true);
                }
                Punch(back);
                back.SetAsLastSibling();
            }
        }

        /// <summary>Re-skins the level node prefab as glossy GUIPack circles with state art.</summary>
        public static void BuildNodePrefab()
        {
            var rootGo = PrefabUtility.LoadPrefabContents(NodePrefabPath);
            try
            {
                var root = (RectTransform)rootGo.transform;
                var btnRt = root.Find("LevelButton") as RectTransform;
                var lbui = btnRt.GetComponent<LevelButtonUI>();

                // Current-level shine (behind the button)
                var hl = Node(root, "CurrentHighlight");
                hl.anchorMin = new Vector2(-0.45f, -0.45f); hl.anchorMax = new Vector2(1.45f, 1.45f); hl.sizeDelta = Vector2.zero; hl.anchoredPosition = Vector2.zero;
                Img(hl, Spr("Shapes/Effects/Radial Shine"), new Color(1, 0.95f, 0.6f, 0.75f), preserve: true);
                Motion(hl, UISimpleMotion.Mode.Rotate, 25f, 0f);
                hl.SetSiblingIndex(0);
                hl.gameObject.SetActive(false);

                var shadow = Node(root, "Shadow"); Stretch(shadow); shadow.anchoredPosition = new Vector2(0, -12);
                Img(shadow, Spr("Shapes/Shapes/Circle/Circle - 256px"), new Color(0, 0, 0, 0.16f), preserve: true);
                shadow.SetSiblingIndex(1);

                var img = btnRt.GetComponent<Image>();
                Img(btnRt, Spr("Buttons/Circles/Green"), Color.white, raycast: true, preserve: true);
                var btn = btnRt.GetComponent<Button>();
                var cb = btn.colors; cb.disabledColor = Color.white; cb.pressedColor = new Color(0.85f, 0.85f, 0.85f); cb.highlightedColor = Color.white; btn.colors = cb;
                Punch(btnRt);

                var num = btnRt.Find("LevelNumber") as RectTransform;
                if (num)
                {
                    Stretch(num, 10, 10, 6, 18);
                    var t = Txt(num, "1", 96, Color.white, OutlineMat("Navy", Navy));
                    t.enableAutoSizing = true; t.fontSizeMin = 36; t.fontSizeMax = 96;
                }
                var lockRt = btnRt.Find("LockIcon") as RectTransform;
                if (lockRt)
                {
                    lockRt.anchorMin = new Vector2(0.58f, -0.04f); lockRt.anchorMax = new Vector2(1.04f, 0.42f);
                    lockRt.sizeDelta = Vector2.zero; lockRt.anchoredPosition = Vector2.zero;
                    Img(lockRt, Spr("Icons/Icons Colored/Lock - Key/Lock - Gold"), Color.white, preserve: true);
                    lockRt.SetAsLastSibling();
                }
                var stars = btnRt.Find("StarsContainer") as RectTransform;
                if (stars)
                {
                    stars.anchorMin = new Vector2(0, 0); stars.anchorMax = new Vector2(1, 0);
                    stars.pivot = new Vector2(0.5f, 0.5f); stars.anchoredPosition = new Vector2(0, -4); stars.sizeDelta = new Vector2(20, 64);
                    var hlg = stars.GetComponent<HorizontalLayoutGroup>();
                    hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = -4;
                    hlg.childControlWidth = true; hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
                    foreach (Transform s in stars)
                    {
                        Img(s, Spr("Icons/Icons Colored/Stars/Star Yellow"), Color.white, preserve: true);
                        if (!s.TryGetComponent(out LayoutElement le)) le = s.gameObject.AddComponent<LayoutElement>();
                        le.preferredWidth = 60; le.preferredHeight = 60;
                    }
                }

                var so = new SerializedObject(lbui);
                so.FindProperty("stateImage").objectReferenceValue = img;
                so.FindProperty("completedSprite").objectReferenceValue = Spr("Buttons/Circles/Green");
                so.FindProperty("currentSprite").objectReferenceValue = Spr("Buttons/Circles/Yellow");
                so.FindProperty("lockedSprite").objectReferenceValue = Spr("Buttons/Circles/Light");
                so.FindProperty("currentHighlight").objectReferenceValue = hl.gameObject;
                so.FindProperty("unlockedTextColor").colorValue = Color.white;
                so.FindProperty("lockedTextColor").colorValue = Hex("9AA6BF");
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(rootGo, NodePrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(rootGo); }
        }

        // ═════════════════════════════ MAIN SCENE ════════════════════════════════
        public static void BuildMain(UnityEngine.SceneManagement.Scene scene)
        {
            // ── 3D gradient floor (the board background) ──
            var env = FindRootGo(scene, "//ENVIRONMENT");
            var ground = FindDeep(env.transform, "Ground");
            if (ground && ground.TryGetComponent(out MeshRenderer gmr)) gmr.enabled = false;

            var floor = env.transform.Find("GradientFloor");
            if (!floor)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "GradientFloor";
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(env.transform, false);
                floor = q.transform;
            }
            floor.position = new Vector3(0, 0.01f, 0);
            floor.rotation = Quaternion.Euler(90, 0, 0);
            floor.localScale = new Vector3(16f, 12f, 1f);
            // Quad v=1 lands at world +Z, which is the BOTTOM of the screen for this camera.
            var floorTex = GradientTexture("BG_Game", Hex("5E7FF5"), Hex("5BB0FA"), Hex("8BE2FF"));
            var fmr = floor.GetComponent<MeshRenderer>();
            fmr.sharedMaterial = LitMaterial("M_GradientFloor", floorTex, 0f, 0.62f);
            fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fmr.receiveShadows = true;

            // ── Power-up tray background ──
            var cam = FindDeep(env.transform, "Main Camera");
            var zone = cam ? cam.Find("Powerup Zone") : null;
            if (zone)
            {
                var old = zone.Find("Renderer");
                if (old && old.TryGetComponent(out MeshRenderer omr)) omr.enabled = false;

                // NOTE: Powerup Zone runs ItemSpotLayout over its children, so the tray lives on the camera instead.
                var stray = zone.Find("PowerupBackground");
                if (stray) stray.SetParent(cam, true);
                var tray = cam.Find("PowerupBackground");
                if (!tray)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    q.name = "PowerupBackground";
                    Object.DestroyImmediate(q.GetComponent<Collider>());
                    q.transform.SetParent(cam, false);
                    tray = q.transform;
                }
                tray.SetSiblingIndex(zone.GetSiblingIndex() + 1);
                // Old container top face: centre (0, 8.95, 3.65), 4.65 x 1.0 on XZ.
                tray.position = new Vector3(0f, 8.6f, 3.93f);
                tray.rotation = Quaternion.Euler(90, 180, 0);
                tray.localScale = new Vector3(5.3f, 1.15f, 1f);
                var trayTex = BakePowerupTray();
                var tmr = tray.GetComponent<MeshRenderer>();
                tmr.sharedMaterial = UnlitTransparent("M_PowerupTray", trayTex);
                tmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tmr.receiveShadows = false;
            }

            // ── HUD ──
            var canvas = FindDeep(FindRootGo(scene, "//UI").transform, "Main Canvas").GetComponent<Canvas>();
            var root = (RectTransform)canvas.transform;
            var bar = FindDeep(root, "First Horizontal Layout ") as RectTransform ?? FindDeep(root, "First Horizontal Layout") as RectTransform;
            if (bar)
            {
                var bi = bar.GetComponent<Image>(); if (bi) { bi.sprite = null; bi.color = Color.clear; bi.raycastTarget = false; }
                bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1);
                bar.anchoredPosition = Vector2.zero; bar.sizeDelta = new Vector2(0, 165);
            }

            var coinPanel = FindDeep(root, "CoinPanel") as RectTransform;
            if (coinPanel)
            {
                Place(coinPanel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -34), new Vector2(250, 96));
                coinPanel.pivot = new Vector2(0, 1); coinPanel.anchoredPosition = new Vector2(36, -34);
                var p = Pill(coinPanel); p.SetSiblingIndex(0);
                var icon = coinPanel.Find("CoinIcon") as RectTransform;
                Place(icon, new Vector2(0, 0.5f), Center, new Vector2(30, 0), new Vector2(118, 118));
                Img(icon, Spr("Icons/Icons Colored/Coins/Coin"), Color.white, preserve: true);
                var txt = coinPanel.Find("CoinText") as RectTransform;
                Stretch(txt, 88, 14, 0, 4);
                Txt(txt, "100", 54, Color.white, OutlineMat("Navy", Navy));
            }

            var timer = FindDeep(root, "Timer Container") as RectTransform;
            if (timer)
            {
                Place(timer, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(20, -34), new Vector2(250, 96));
                timer.pivot = new Vector2(0.5f, 1); timer.anchoredPosition = new Vector2(20, -34);
                var ti = timer.GetComponent<Image>(); if (ti) { ti.sprite = null; ti.color = Color.clear; }
                var p = Pill(timer); p.SetSiblingIndex(0);
                var icon = Node(timer, "TimerIcon"); Place(icon, new Vector2(0, 0.5f), Center, new Vector2(28, 4), new Vector2(122, 122));
                Img(icon, Spr("Icons/Icons Colored/Time/Timer"), Color.white, preserve: true);
                var tt = timer.Find("Timer Text ") as RectTransform ?? timer.Find("Timer Text") as RectTransform;
                if (tt)
                {
                    Stretch(tt, 88, 14, 0, 4);
                    Txt(tt, "05:00", 56, Color.white, OutlineMat("Navy", Navy));
                    tt.SetAsLastSibling();
                }
            }

            var pause = FindDeep(root, "Pause Button") as RectTransform;
            if (pause)
            {
                Place(pause, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -26), new Vector2(112, 112));
                pause.pivot = new Vector2(1, 1); pause.anchoredPosition = new Vector2(-24, -26);
                Img(pause, Spr("Buttons/Square/Blue"), Color.white, raycast: true, preserve: true);
                var ic = Node(pause, "PauseIcon"); Stretch(ic, 30, 30, 28, 32);
                Img(ic, Spr("Icons/Icons White/Navigation/Pause"), Color.white, preserve: true);
                Punch(pause);
            }
        }

        // ═════════════════════════════ Building blocks ═══════════════════════════
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private static void TitleStyle(TextMeshProUGUI t, float size)
        {
            if (!t) return;
            t.font = Font;
            t.fontSharedMaterial = OutlineMat("Title", NavyDeep, 0.28f, true);
            t.fontSize = size;
            t.enableAutoSizing = false;
            t.lineSpacing = -28;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.color = Color.white;
            t.enableVertexGradient = true;
            t.colorGradient = new VertexGradient(GoldTop, GoldTop, GoldBottom, GoldBottom);
            t.raycastTarget = false;
            if (t.text.Contains("Match Them All") || string.IsNullOrWhiteSpace(t.text)) t.text = "MATCH\nTHEM ALL";
            if (!t.text.Contains("\n")) t.text = "MATCH\nTHEM ALL";
        }

        private static void HeroItem(RectTransform parent, string name, string sprite, Vector2 pos, float size, float rot, float phase)
        {
            var rt = Node(parent, name);
            Place(rt, Center, Center, pos, new Vector2(size, size));
            rt.localRotation = Quaternion.Euler(0, 0, rot);
            Img(rt, Spr(sprite), Color.white, preserve: true);
            Motion(rt, UISimpleMotion.Mode.Float, 10f, 0.4f, phase);
        }

        /// <summary>Square GUIPack tile with an icon and a coloured label pill underneath (lobby side buttons).</summary>
        private static void Tile(RectTransform btn, RectTransform parent, string labelName, string label, string icon, string pillSprite, Color outline, bool left)
        {
            if (!btn) return;
            btn.SetParent(parent, false);
            var anchor = left ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
            Place(btn, anchor, anchor, new Vector2(left ? 50 : -50, -420), new Vector2(230, 230));
            Img(btn, Spr("Buttons/Square/Light"), Color.white, raycast: true, preserve: true);

            var ic = Node(btn, "Icon"); Place(ic, Center, Center, new Vector2(0, 18), new Vector2(170, 170));
            Img(ic, Spr(icon), Color.white, preserve: true);
            Motion(ic, UISimpleMotion.Mode.Float, 6f, 0.6f, left ? 0f : 0.5f);

            var pill = Node(btn, "LabelPill"); Place(pill, new Vector2(0.5f, 0), Center, new Vector2(0, 6), new Vector2(left ? 300 : 230, 86));
            Img(pill, Spr(pillSprite), Color.white, type: Image.Type.Sliced, ppum: 1.17f);

            var lbl = btn.Find(labelName) as RectTransform ?? pill.Find(labelName) as RectTransform ?? Node(pill, labelName);
            lbl.SetParent(pill, false);
            Stretch(lbl, 12, 12, 4, 12);
            var t = Txt(lbl, label, 44, Color.white, OutlineMat(outline == GreenDark ? "Green" : "Orange", outline));
            t.enableAutoSizing = true; t.fontSizeMin = 24; t.fontSizeMax = 44;
            Punch(btn);
        }

        /// <summary>Rounded pill: white rim + navy fill (coin / timer counters).</summary>
        private static RectTransform Pill(RectTransform parent)
        {
            var rim = Node(parent, "PillBg"); Stretch(rim);
            Img(rim, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), new Color(1, 1, 1, 0.95f), type: Image.Type.Sliced, ppum: 2.7f);
            var fill = Node(rim, "Fill"); Stretch(fill, 7, 7, 7, 7);
            Img(fill, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), Hex("2F4BA8"), type: Image.Type.Sliced, ppum: 3.0f);
            var shine = Node(fill, "Shine"); Stretch(shine, 14, 14, 6, 44);
            Img(shine, Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px"), new Color(1, 1, 1, 0.14f), type: Image.Type.Sliced, ppum: 7f);
            rim.SetSiblingIndex(0);
            return rim;
        }

        private static void Clouds(RectTransform parent)
        {
            var c = Node(parent, "CloudsBottom");
            Place(c, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -10), new Vector2(1900, 340));
            Img(c, Spr("Backgrounds/Level Scene/Clouds Bottom"), new Color(1, 1, 1, 0.95f));
        }

        /// <summary>Glossy GUIPack circles scattered down the screen edges, gently floating (like gumballs).</summary>
        private static void Bubbles(RectTransform parent, int seed, int count, float alpha)
        {
            var holder = Node(parent, "Bubbles"); Stretch(holder);
            for (int i = holder.childCount - 1; i >= 0; i--) Object.DestroyImmediate(holder.GetChild(i).gameObject);
            string[] colors = { "Green", "Yellow", "Blue", "Purple", "Orange", "Pink", "Red", "Teal" };
            var rnd = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var b = Node(holder, "Bubble_" + i);
                bool leftSide = (i % 2) == 0;
                float x = leftSide ? Mathf.Lerp(0.03f, 0.2f, (float)rnd.NextDouble()) : Mathf.Lerp(0.8f, 0.97f, (float)rnd.NextDouble());
                float y = Mathf.Lerp(0.12f, 0.95f, (i + (float)rnd.NextDouble()) / count);
                float s = Mathf.Lerp(46f, 110f, (float)rnd.NextDouble());
                Place(b, new Vector2(x, y), new Vector2(x, y), Vector2.zero, new Vector2(s, s));
                b.pivot = Center; b.anchoredPosition = Vector2.zero;
                Img(b, Spr("Buttons/Circles/" + colors[rnd.Next(colors.Length)]), new Color(1, 1, 1, alpha), preserve: true);
                Motion(b, UISimpleMotion.Mode.Float, Mathf.Lerp(8f, 18f, (float)rnd.NextDouble()), Mathf.Lerp(0.2f, 0.45f, (float)rnd.NextDouble()), (float)rnd.NextDouble());
            }
        }

        /// <summary>Soft out-of-focus light spots (level map backdrop).</summary>
        private static void Bokeh(RectTransform parent, int seed)
        {
            var holder = Node(parent, "Bokeh"); Stretch(holder);
            for (int i = holder.childCount - 1; i >= 0; i--) Object.DestroyImmediate(holder.GetChild(i).gameObject);
            var rnd = new System.Random(seed);
            for (int i = 0; i < 10; i++)
            {
                var b = Node(holder, "Spot_" + i);
                float x = (i % 2 == 0) ? Mathf.Lerp(0f, 0.18f, (float)rnd.NextDouble()) : Mathf.Lerp(0.82f, 1f, (float)rnd.NextDouble());
                float y = Mathf.Lerp(0.05f, 0.95f, (i + (float)rnd.NextDouble()) / 10f);
                float s = Mathf.Lerp(120f, 260f, (float)rnd.NextDouble());
                Place(b, new Vector2(x, y), new Vector2(x, y), Vector2.zero, new Vector2(s, s));
                b.pivot = Center; b.anchoredPosition = Vector2.zero;
                Img(b, Spr("Shapes/Effects/Shine"), new Color(1, 1, 1, 0.35f), preserve: true);
                Motion(b, UISimpleMotion.Mode.Float, 14f, 0.15f, (float)rnd.NextDouble());
            }
        }

        // ═════════════════════════════ Low-level helpers ═════════════════════════
        private static TMP_FontAsset _font;
        private static TMP_FontAsset Font => _font ? _font : (_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));

        public static Sprite Spr(string rel)
        {
            foreach (var ext in new[] { ".png", ".psd", ".jpg" })
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(Gui + rel + ext);
                if (s) return s;
            }
            Debug.LogWarning("[UIOverhaul] Missing sprite: " + rel);
            return null;
        }

        private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        private static T FindRoot<T>(UnityEngine.SceneManagement.Scene scene, string name) where T : Component
        {
            foreach (var g in scene.GetRootGameObjects())
                if (g.name == name && g.TryGetComponent(out T c)) return c;
            foreach (var g in scene.GetRootGameObjects())
            {
                var c = g.GetComponentInChildren<T>(true);
                if (c && c.name == name) return c;
            }
            throw new System.Exception("[UIOverhaul] Root not found: " + name);
        }

        private static GameObject FindRootGo(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var g in scene.GetRootGameObjects()) if (g.name == name) return g;
            throw new System.Exception("[UIOverhaul] Root not found: " + name);
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = FindDeep(c, name); if (r) return r; }
            return null;
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t) return t as RectTransform ?? t.gameObject.AddComponent<RectTransform>();
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one;
            return rt;
        }

        private static RectTransform Stretch(RectTransform rt, float l = 0, float r = 0, float top = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = Center;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -top);
            rt.localScale = Vector3.one;
            return rt;
        }

        private static Image Img(Component c, Sprite s, Color col, bool raycast = false, Image.Type type = Image.Type.Simple, float ppum = 1f, bool preserve = false)
        {
            if (!c.TryGetComponent(out Image img)) img = c.gameObject.AddComponent<Image>();
            img.sprite = s; img.color = col; img.raycastTarget = raycast;
            img.type = type; img.pixelsPerUnitMultiplier = ppum; img.preserveAspect = preserve;
            if (type == Image.Type.Sliced) img.fillCenter = true;
            return img;
        }

        private static TextMeshProUGUI Txt(Component c, string text, float size, Color face, Material mat, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            if (!c.TryGetComponent(out TextMeshProUGUI t)) t = c.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            if (mat) t.fontSharedMaterial = mat;
            t.text = text; t.fontSize = size; t.color = face; t.alignment = align;
            t.enableVertexGradient = false; t.enableAutoSizing = false;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.characterSpacing = 0; t.lineSpacing = 0;
            return t;
        }

        private static void Motion(Component c, UISimpleMotion.Mode mode, float amount, float speed, float phase = 0f)
        {
            if (!c.TryGetComponent(out UISimpleMotion m)) m = c.gameObject.AddComponent<UISimpleMotion>();
            m.Setup(mode, amount, speed, phase);
            EditorUtility.SetDirty(m);
        }

        private static void Punch(Component c)
        {
            if (!c.GetComponent<UIButtonPunch>()) c.gameObject.AddComponent<UIButtonPunch>();
        }

        private static void SetScaler(Canvas canvas, float match)
        {
            var cs = canvas.GetComponent<CanvasScaler>();
            if (!cs) return;
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = match;
        }

        // ── Generated assets ──

        private static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Material preset for LilitaOne with outline + soft drop shadow.</summary>
        public static Material OutlineMat(string name, Color outline, float width = 0.22f, bool heavyShadow = false)
        {
            EnsureFolder(Art + "Fonts");
            string p = Art + "Fonts/LilitaOne SDF - " + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m)
            {
                m = new Material(Font.material);
                AssetDatabase.CreateAsset(m, p);
            }
            m.shader = Font.material.shader;
            m.SetTexture("_MainTex", Font.atlasTexture);
            m.EnableKeyword("OUTLINE_ON");
            m.EnableKeyword("UNDERLAY_ON");
            m.SetFloat("_FaceDilate", 0.2f);
            m.SetColor("_OutlineColor", outline);
            m.SetFloat("_OutlineWidth", width);
            m.SetFloat("_OutlineSoftness", 0f);
            m.SetColor("_UnderlayColor", new Color(outline.r * 0.6f, outline.g * 0.6f, outline.b * 0.6f, heavyShadow ? 0.9f : 0.75f));
            m.SetFloat("_UnderlayOffsetX", 0f);
            m.SetFloat("_UnderlayOffsetY", heavyShadow ? -0.9f : -0.6f);
            m.SetFloat("_UnderlayDilate", heavyShadow ? 0.45f : 0.3f);
            m.SetFloat("_UnderlaySoftness", 0.05f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Vertical gradient (first colour = top) saved as a PNG Sprite.</summary>
        public static Sprite Gradient(string name, params Color[] stops)
        {
            string p = WriteGradientPng(name, stops, flip: false);
            return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }

        /// <summary>Gradient as a Texture2D (first colour = v 0).</summary>
        public static Texture2D GradientTexture(string name, params Color[] stops)
        {
            string p = WriteGradientPng(name, stops, flip: true);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        private static string WriteGradientPng(string name, Color[] stops, bool flip)
        {
            EnsureFolder(Art + "Gradients");
            string p = Art + "Gradients/" + name + ".png";
            const int w = 8, h = 512;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float v = y / (h - 1f);           // 0 = bottom row of the PNG
                float t = flip ? v : 1f - v;      // t = 0 → first stop
                float seg = t * (stops.Length - 1);
                int i = Mathf.Min(Mathf.FloorToInt(seg), stops.Length - 2);
                float f = Mathf.SmoothStep(0f, 1f, seg - i);
                var c = Color.Lerp(stops[i], stops[i + 1], f);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            File.WriteAllBytes(p, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            imp.textureType = flip ? TextureImporterType.Default : TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
            return p;
        }

        private static Material LitMaterial(string name, Texture2D tex, float emission, float baseTint = 1f)
        {
            EnsureFolder(Art + "Materials");
            string p = Art + "Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", new Color(baseTint, baseTint, baseTint, 1f));
            m.SetFloat("_Smoothness", 0.05f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_SpecularHighlights", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", Color.white * emission);
            }
            else m.DisableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material UnlitTransparent(string name, Texture2D tex)
        {
            EnsureFolder(Art + "Materials");
            string p = Art + "Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);   // Transparent
            m.SetFloat("_Blend", 0f);     // Alpha
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 10;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Bakes the power-up tray: renders a small GUIPack UI composition (rounded panel + 4 slots)
        /// to a transparent PNG which is shown on a quad under the 3D power-ups.
        /// </summary>
        private static Texture2D BakePowerupTray()
        {
            EnsureFolder(Art + "Baked");
            string p = Art + "Baked/PowerupTray.png";
            const int W = 1060, H = 230;

            var holder = new GameObject("__TrayBake");
            holder.hideFlags = HideFlags.HideAndDontSave;
            var camGo = new GameObject("__TrayCam"); camGo.hideFlags = HideFlags.HideAndDontSave;
            camGo.transform.SetParent(holder.transform);
            camGo.transform.position = new Vector3(5000, 5000, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << 31; cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f; cam.enabled = false;
            var rtx = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rtx;

            var cgo = new GameObject("__TrayCanvas", typeof(RectTransform)); cgo.hideFlags = HideFlags.HideAndDontSave;
            cgo.transform.SetParent(holder.transform);
            cgo.layer = 31;
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 5;
            var root = (RectTransform)cgo.transform;

            void SetLayer(Transform t) { t.gameObject.layer = 31; foreach (Transform c in t) SetLayer(c); }

            var rr = Spr("Shapes/Shapes/Rectangle/Rounded Rectangle - 256px");
            var shadow = Node(root, "Shadow"); Stretch(shadow, 6, 6, 14, 0);
            Img(shadow, rr, new Color(0.05f, 0.08f, 0.25f, 0.35f), type: Image.Type.Sliced, ppum: 1.15f);
            var rim = Node(root, "Rim"); Stretch(rim, 6, 6, 6, 12);
            Img(rim, rr, Color.white, type: Image.Type.Sliced, ppum: 1.15f);
            var fill = Node(rim, "Fill"); Stretch(fill, 10, 10, 10, 10);
            Img(fill, rr, Hex("3558C8"), type: Image.Type.Sliced, ppum: 1.25f);
            var lower = Node(fill, "LowerShade"); Stretch(lower, 0, 0, 110, 0);
            Img(lower, rr, new Color(0.15f, 0.25f, 0.7f, 0.35f), type: Image.Type.Sliced, ppum: 1.25f);
            var gloss = Node(fill, "Gloss"); Stretch(gloss, 22, 22, 10, 120);
            Img(gloss, rr, new Color(1, 1, 1, 0.16f), type: Image.Type.Sliced, ppum: 2.2f);
            // Four slots centred under the power-ups (spot X = ±0.58, ±1.73 on a 5.3-wide tray).
            float[] xs = { -1.73f, -0.58f, 0.58f, 1.73f };
            for (int i = 0; i < xs.Length; i++)
            {
                var slot = Node(fill, "Slot" + i);
                float nx = 0.5f + xs[i] / 5.3f * (W / (W - 32f));
                Place(slot, new Vector2(nx, 0.5f), Center, new Vector2(0, -2), new Vector2(180, 180));
                Img(slot, Spr("Shapes/Shapes/Circle/Circle - 256px"), new Color(0.75f, 0.88f, 1f, 0.28f), preserve: true);
                var ring = Node(slot, "Ring"); Stretch(ring, -6, -6, -6, -6);
                Img(ring, Spr("Shapes/Shapes/Circle/Circle Cartoon - With Border"), new Color(1, 1, 1, 0.3f), preserve: true);
            }
            SetLayer(root);
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rtx;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(p, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null; rtx.Release(); Object.DestroyImmediate(rtx);
            Object.DestroyImmediate(holder);

            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            imp.textureType = TextureImporterType.Default; imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.npotScale = TextureImporterNPOTScale.None;
            imp.wrapMode = TextureWrapMode.Clamp; imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
    }
}
