#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using MatchThemAll.Scripts.Power_Ups;
using TMPro;
using UnityEditor;
using UnityEngine;
using MatchThemAll.Scripts.Shop;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Phase-1 shop bootstrap. Two menu items:
    ///
    ///   Tools / Shop / Create Default Shop Products
    ///     → generates placeholder ShopProductSO assets (coin packs, power-up charge packs) + a
    ///       ShopDatabaseSO, all under Resources/Shop/. Re-runnable; seeds sensible test values.
    ///
    ///   Tools / Shop / Build Shop Panel
    ///     → in the OPEN scene: builds a Screen Space Canvas + ShopPanel + ShopProductCard prefab +
    ///       category tabs + close button + a ShopManager + a ShopOpener on a top-bar button. Re-runnable.
    ///
    /// Placeholder visuals — swap sprites/fonts later.
    /// </summary>
    public static class ShopSetup
    {
        private const string SHOP_RES_DIR = "Assets/Match Them All/Resources/Shop";
        private const string CARD_PREFAB_PATH = "Assets/Match Them All/UI/Prefabs/ShopProductCard.prefab";

        // ─────────────────────────────────────────────────────────────────────
        // 1. Default products + database
        // ─────────────────────────────────────────────────────────────────────
        [MenuItem("Tools/Shop/Create Default Shop Products")]
        public static void CreateDefaultProducts()
        {
            if (!Directory.Exists(SHOP_RES_DIR)) Directory.CreateDirectory(SHOP_RES_DIR);

            var powerupPacks = new[]
            {
                ("vacuum", "Vacuum Pack", 120, 5),
                ("spring", "Spring Pack", 120, 5),
                ("fan",    "Fan Pack",    120, 5),
                ("freeze", "Freeze Pack", 120, 5),
            };

            var created = new List<ShopProductSO>();

            // Coin-for-coins value packs (buy coins with... coins makes no sense; these are placeholder
            // "bulk" offers — replace with gem-priced coin packs in Phase 2). Kept minimal here.
            // Instead, the meaningful Phase-1 products are power-up packs priced in coins.
            foreach (var (id, name, price, charges) in powerupPacks)
            {
                var so = CreateSO($"powerup_{id}_pack", name, EShopCategory.Powerups,
                    priceCurrency: ECurrency.Coins, price: price);
                AddReward(so, ShopReward.EKind.PowerupCharge, charges, id);
                EditorUtility.SetDirty(so);
                created.Add(so);
            }

            // One bundle: mix of charges, priced in coins.
            var bundle = CreateSO("bundle_starter", "Starter Bundle", EShopCategory.Bundles,
                priceCurrency: ECurrency.Coins, price: 300);
            bundle.mostPopular = true;
            AddReward(bundle, ShopReward.EKind.PowerupCharge, 2, "vacuum");
            AddReward(bundle, ShopReward.EKind.PowerupCharge, 2, "spring");
            AddReward(bundle, ShopReward.EKind.Coins, 50, null);
            EditorUtility.SetDirty(bundle);
            created.Add(bundle);

            // Build the database.
            string dbPath = $"{SHOP_RES_DIR}/ShopDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<ShopDatabaseSO>(dbPath);
            if (!db)
            {
                db = ScriptableObject.CreateInstance<ShopDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.products = created;
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = db;
            Debug.Log($"[ShopSetup] Created {created.Count} products + ShopDatabase under {SHOP_RES_DIR}. Tune prices/rewards in the Inspector.");
        }

        private static ShopProductSO CreateSO(string id, string displayName, EShopCategory category, ECurrency priceCurrency, int price)
        {
            string path = $"{SHOP_RES_DIR}/ShopProduct_{id}.asset";
            var so = AssetDatabase.LoadAssetAtPath<ShopProductSO>(path);
            if (!so)
            {
                so = ScriptableObject.CreateInstance<ShopProductSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("category").enumValueIndex = (int)category;
            serialized.FindProperty("priceCurrency").enumValueIndex = (int)priceCurrency;
            serialized.FindProperty("priceAmount").intValue = price;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        private static void AddReward(ShopProductSO so, ShopReward.EKind kind, int amount, string powerupId)
        {
            var serialized = new SerializedObject(so);
            // Always populate the main rewards list — firstPurchaseBonus is a separate, one-time-only field.
            // (Routing IAP rewards into firstPurchaseBonus made repeat real-money purchases grant nothing.)
            var rewards = serialized.FindProperty("rewards");
            rewards.arraySize++;
            var entry = rewards.GetArrayElementAtIndex(rewards.arraySize - 1);
            entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            entry.FindPropertyRelative("amount").intValue = amount;
            entry.FindPropertyRelative("powerupId").stringValue = powerupId ?? "";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. Build the shop panel inside the Lobby scene's existing Canvas
        // ─────────────────────────────────────────────────────────────────────
        private const string LOBBY_SCENE_PATH = "Assets/Match Them All/Scenes/Lobby.unity";

        [MenuItem("Tools/Shop/Build Shop Panel")]
        public static void BuildPanel()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[ShopSetup] Exit Play mode first.");
                return;
            }

            ShopDatabaseSO db = Resources.Load<ShopDatabaseSO>("Shop/ShopDatabase");
            if (!db)
            {
                Debug.LogError("[ShopSetup] No ShopDatabase at Resources/Shop/ShopDatabase. Run 'Create Default Shop Products' first.");
                return;
            }

            // ── Open / locate Lobby scene ────────────────────────────────────
            var lobbyScene = SceneManager.GetSceneByPath(LOBBY_SCENE_PATH);
            bool wasAlreadyOpen = lobbyScene.isLoaded;
            if (!wasAlreadyOpen)
                lobbyScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    LOBBY_SCENE_PATH, UnityEditor.SceneManagement.OpenSceneMode.Additive);

            // ── Find the single Canvas in Lobby ──────────────────────────────
            Canvas lobbyCanvas = null;
            foreach (var root in lobbyScene.GetRootGameObjects())
            {
                lobbyCanvas = root.GetComponent<Canvas>();
                if (lobbyCanvas) break;
            }

            if (!lobbyCanvas)
            {
                Debug.LogError("[ShopSetup] Could not find a Canvas root in Lobby.unity. Aborting.");
                if (!wasAlreadyOpen)
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(lobbyScene, true);
                return;
            }

            var canvasGo = lobbyCanvas.gameObject;
            var canvasTf = canvasGo.transform;

            // Remove stale copies so re-running is idempotent
            var oldPanel = canvasTf.Find("ShopPanel");
            if (oldPanel) Object.DestroyImmediate(oldPanel.gameObject);
            var oldOpener = canvasTf.Find("ShopOpenerButton");
            if (oldOpener) Object.DestroyImmediate(oldOpener.gameObject);
            var oldManager = canvasGo.GetComponent<ShopManager>();
            if (oldManager) Object.DestroyImmediate(oldManager);
            EnsureEventSystem();

            // ── Panel root (toggled for show/hide) ───────────────────────────
            var panelGo = new GameObject("ShopPanel");
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.SetParent(canvasTf, false);
            StretchFill(panelRt);
            var backing = panelGo.AddComponent<Image>();
            backing.color = new Color(0, 0, 0, 0.75f);
            panelGo.SetActive(false); // closed by default

            // Center card area
            var areaGo = new GameObject("CardArea");
            var areaRt = areaGo.AddComponent<RectTransform>();
            areaRt.SetParent(panelRt, false);
            StretchFill(areaRt, margin: 120f);
            var areaImg = areaGo.AddComponent<Image>();
            areaImg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Tabs row (top of card area)
            var tabsGo = new GameObject("Tabs");
            var tabsRt = tabsGo.AddComponent<RectTransform>();
            tabsRt.SetParent(areaRt, false);
            AnchorTopStretch(tabsRt, height: 110f);
            var tabsHlg = tabsGo.AddComponent<HorizontalLayoutGroup>();
            tabsHlg.childAlignment = TextAnchor.UpperCenter;
            tabsHlg.spacing = 20f; tabsHlg.childControlWidth = true; tabsHlg.childControlHeight = true;
            tabsHlg.childForceExpandWidth = true; tabsHlg.childForceExpandHeight = true;
            Button coinsTab    = MakeTabButton(tabsGo.transform, "CoinsTab",    "Coins");
            Button gemsTab     = MakeTabButton(tabsGo.transform, "GemsTab",     "Gems");
            Button powerupsTab = MakeTabButton(tabsGo.transform, "PowerupsTab", "Power-ups");
            Button bundlesTab  = MakeTabButton(tabsGo.transform, "BundlesTab",  "Bundles");

            // Scroll area for cards
            var viewport = MakeRect("Viewport", areaRt);
            viewport.SetParent(areaRt, false);
            StretchFill(viewport, top: 120f, bottom: 120f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = MakeRect("Content", viewport);
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot     = new Vector2(0.5f, 1f);
            var contentHlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentHlg.childAlignment = TextAnchor.UpperCenter;
            contentHlg.spacing = 30f; contentHlg.childControlWidth = true; contentHlg.childControlHeight = true;
            contentHlg.childForceExpandWidth = true; contentHlg.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Close button (bottom of card area)
            var closeGo  = new GameObject("CloseButton");
            var closeRt  = closeGo.AddComponent<RectTransform>();
            closeRt.SetParent(areaRt, false);
            AnchorBottomStretch(closeRt, height: 100f);
            closeGo.AddComponent<Image>().color = new Color(0.85f, 0.3f, 0.3f, 0.95f);
            var closeBtn = closeGo.AddComponent<Button>();
            MakeLabel(closeGo.transform, "Close", 56, Color.white);

            // Card prefab
            ShopProductCard cardPrefab = BuildCardPrefab();

            // ShopPanel component, wired
            var panel = panelGo.AddComponent<ShopPanel>();
            var ser   = new SerializedObject(panel);
            ser.FindProperty("database").objectReferenceValue          = db;
            ser.FindProperty("cardPrefab").objectReferenceValue        = cardPrefab;
            ser.FindProperty("cardContainer").objectReferenceValue     = content;
            ser.FindProperty("coinsTabButton").objectReferenceValue    = coinsTab;
            ser.FindProperty("gemsTabButton").objectReferenceValue     = gemsTab;
            ser.FindProperty("powerupsTabButton").objectReferenceValue = powerupsTab;
            ser.FindProperty("bundlesTabButton").objectReferenceValue  = bundlesTab;
            ser.FindProperty("closeButton").objectReferenceValue       = closeBtn;
            ser.FindProperty("root").objectReferenceValue              = panelGo;
            ser.ApplyModifiedPropertiesWithoutUndo();

            // ShopManager on the Lobby Canvas
            canvasGo.AddComponent<ShopManager>();

            // Opener button (top-right corner; move onto your HUD later)
            var openerGo = new GameObject("ShopOpenerButton");
            var openerRt = openerGo.AddComponent<RectTransform>();
            openerRt.SetParent(canvasTf, false);
            openerRt.anchorMin        = new Vector2(1f, 1f);
            openerRt.anchorMax        = new Vector2(1f, 1f);
            openerRt.pivot            = new Vector2(1f, 1f);
            openerRt.anchoredPosition = new Vector2(-40f, -40f);
            openerRt.sizeDelta        = new Vector2(160f, 90f);
            openerGo.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.95f, 0.95f);
            openerGo.AddComponent<Button>();
            var opener    = openerGo.AddComponent<ShopOpener>();
            var openerSer = new SerializedObject(opener);
            openerSer.FindProperty("shopPanel").objectReferenceValue = panel;
            openerSer.ApplyModifiedPropertiesWithoutUndo();
            MakeLabel(openerGo.transform, "Shop", 40, Color.white);

            // ── Save Lobby scene ─────────────────────────────────────────────
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(lobbyScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(lobbyScene);

            // If we opened it additively just for this, close it again
            if (!wasAlreadyOpen)
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(lobbyScene, false);

            Selection.activeObject = panelGo;
            Debug.Log("[ShopSetup] Shop panel built inside Lobby.unity's Canvas and scene saved. " +
                      "Re-run to rebuild idempotently.");
        }

        private static ShopProductCard BuildCardPrefab()
        {
            var root = new GameObject("ShopProductCard");
            var rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900f, 220f);
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.22f, 0.27f, 0.95f);
            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(30, 30, 20, 20);
            hlg.spacing = 30f; hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var icon = MakeImage("Icon", rt, new Color(1, 1, 1, 0.15f));
            SetFixedWidth(icon.GetComponent<RectTransform>(), 160f);

            var nameGo = MakeLabel(rt, "Name", 48, Color.white);
            SetFlexibleWidth(nameGo.GetComponent<LayoutElement>());

            var priceBtnGo = new GameObject("PriceButton");
            var priceRt = priceBtnGo.AddComponent<RectTransform>();
            priceRt.SetParent(rt, false);
            priceBtnGo.AddComponent<Image>().color = new Color(0.26f, 0.7f, 0.4f, 0.95f);
            var priceBtn = priceBtnGo.AddComponent<Button>();
            var priceGo = MakeLabel(priceBtnGo.transform, "Price", 44, Color.white);
            SetFixedWidth(priceRt, 280f);

            var bestBadge = MakeImage("BestValueBadge", rt, new Color(1f, 0.8f, 0.1f, 0.95f));
            bestBadge.SetActive(false);
            var popularBadge = MakeImage("MostPopularBadge", rt, new Color(0.5f, 0.8f, 1f, 0.95f));
            popularBadge.SetActive(false);

            var card = root.AddComponent<ShopProductCard>();
            var ser = new SerializedObject(card);
            ser.FindProperty("iconImage").objectReferenceValue = icon.GetComponent<Image>();
            ser.FindProperty("nameText").objectReferenceValue  = nameGo.GetComponent<TextMeshProUGUI>();
            ser.FindProperty("priceText").objectReferenceValue = priceGo.GetComponent<TextMeshProUGUI>();
            ser.FindProperty("buyButton").objectReferenceValue = priceBtn;
            ser.FindProperty("bestValueBadge").objectReferenceValue = bestBadge;
            ser.FindProperty("mostPopularBadge").objectReferenceValue = popularBadge;
            ser.ApplyModifiedPropertiesWithoutUndo();

            string dir = Path.GetDirectoryName(CARD_PREFAB_PATH);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CARD_PREFAB_PATH).GetComponent<ShopProductCard>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ── uGUI helpers ──────────────────────────────────────────────────────

        private static Button MakeTabButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.3f, 0.45f, 0.7f, 0.95f);
            var btn = go.AddComponent<Button>();
            MakeLabel(go.transform, label, 44, Color.white);
            return btn;
        }

        private static GameObject MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(40f, 40f); // layout group will control size
            go.AddComponent<Image>().color = color;
            return go;
        }

        /// <summary>Bare RectTransform under a parent (no Image) — for viewport/content containers.</summary>
        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        // TMP label — matches the project's UI standard (CoinDisplay/LevelButtonUI use TextMeshProUGUI).
        // Loads the project's TMP font once; if unavailable, TMP falls back to its default.
        private static TMP_FontAsset _tmpFont;
        private static TMP_FontAsset TmpFont =>
            _tmpFont ? _tmpFont
                     : _tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                         AssetDatabase.GUIDToAssetPath("7f4a1b33fd0654a4490c583e3dd3b5df"));

        private static GameObject MakeLabel(Transform parent, string text, int size, Color color)
        {
            var go = new GameObject(text + "_Label");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            if (TmpFont != null) label.font = TmpFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return go;
        }

        private static void SetFixedWidth(RectTransform rt, float w)
        {
            var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.minWidth = w;
        }

        private static void SetFlexibleWidth(LayoutElement le)
        {
            if (!le) return;
            le.flexibleWidth = 1f;
        }

        private static void StretchFill(RectTransform rt, float margin = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin + bottom);
            rt.offsetMax = new Vector2(-margin, -margin - top);
        }

        private static void AnchorTopStretch(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height); rt.anchoredPosition = Vector2.zero;
        }

        private static void AnchorBottomStretch(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, height); rt.anchoredPosition = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
#endif
