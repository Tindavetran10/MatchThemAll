#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.Shop;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Interactive Shop Manager editor window.
    /// Open via: Tools → Shop → Shop Manager
    ///
    /// Three-panel layout:
    ///   Left   — Tab list (add / remove / reorder)
    ///   Middle — Products in selected tab (add / remove / reorder)
    ///   Right  — Product detail editor (all fields, rewards, first-purchase bonus)
    /// </summary>
    public class ShopEditorWindow : EditorWindow
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const string ShopResDir        = "Assets/Match Them All/Resources/Shop";
        private const string DatabaseAssetPath = ShopResDir + "/ShopDatabase.asset";

        // ── Colors ────────────────────────────────────────────────────────────
        private static readonly Color PanelBg       = new(0.18f, 0.18f, 0.20f);
        private static readonly Color CardBg         = new(0.22f, 0.22f, 0.25f);
        private static readonly Color DividerColor   = new(0.12f, 0.12f, 0.14f);
        private static readonly Color AccentBlue     = new(0.27f, 0.55f, 1.00f);
        private static readonly Color AccentGreen    = new(0.26f, 0.83f, 0.53f);
        private static readonly Color AccentRed      = new(0.90f, 0.30f, 0.30f);
        private static readonly Color AccentOrange   = new(1.00f, 0.65f, 0.20f);
        private static readonly Color AccentYellow   = new(1.00f, 0.85f, 0.10f);
        private static readonly Color TextMuted      = new(0.60f, 0.60f, 0.65f);
        private static readonly Color SelectedBg     = new(0.20f, 0.38f, 0.70f);

        // ── State ─────────────────────────────────────────────────────────────
        private ShopDatabaseSO _db;

        // Tab panel
        private int          _selectedTabIdx = -1;
        private ShopTabSO    _selectedTab;
        private Vector2      _tabScroll;
        private bool         _addingTab;
        private string       _newTabId    = "";
        private string       _newTabLabel = "";

        // Product panel
        private int            _selectedProductIdx = -1;
        private ShopProductSO  _selectedProduct;
        private Vector2        _productScroll;
        private bool           _addingProduct;
        private string         _newProductId    = "";
        private string         _newProductName  = "";

        // Detail panel
        private Vector2 _detailScroll;
        private bool    _isDirty;

        // Styles (lazy init)
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _rowNormal;
        private GUIStyle _rowSelected;
        private GUIStyle _cardStyle;
        private bool     _stylesInit;

        // Cached serialized object for the selected product
        private SerializedObject   _productSO;
        private SerializedProperty _propId;
        private SerializedProperty _propDisplayName;
        private SerializedProperty _propIcon;
        private SerializedProperty _propTabId;
        private SerializedProperty _propPriceCurrency;
        private SerializedProperty _propPriceAmount;
        private SerializedProperty _propRewards;
        private SerializedProperty _propBonus;
        private SerializedProperty _propBestValue;
        private SerializedProperty _propMostPopular;
        private SerializedProperty _propDescription;
        private SerializedProperty _propIsOneTime;
        private SerializedProperty _propIapProductId;

        // ── Entry Point ───────────────────────────────────────────────────────

        [MenuItem("Match Them All/Shop Manager")]
        public static void ShowWindow()
        {
            var w = GetWindow<ShopEditorWindow>("Shop Manager");
            w.minSize = new Vector2(900, 560);
            w.LoadDatabase();
        }

        private void OnEnable()
        {
            LoadDatabase();
        }

        private void OnDisable()
        {
            _stylesInit = false;
        }

        // ── Database Loading ──────────────────────────────────────────────────

        private void LoadDatabase()
        {
            _db = AssetDatabase.LoadAssetAtPath<ShopDatabaseSO>(DatabaseAssetPath);
            if (!_db)
                _db = Resources.Load<ShopDatabaseSO>("Shop/ShopDatabase");

            // Restore selections after reload
            if (_selectedTab != null && _db != null)
            {
                _selectedTabIdx = _db.tabs?.IndexOf(_selectedTab) ?? -1;
                if (_selectedTabIdx < 0) SelectTab(-1);
            }
            if (_selectedProduct != null && _db != null)
            {
                _selectedProductIdx = _db.products?.IndexOf(_selectedProduct) ?? -1;
                if (_selectedProductIdx < 0) SelectProduct(-1);
            }

            Repaint();
        }

        // ── Main Layout ───────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            // Background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBg);

            DrawToolbar();

            if (!_db)
            {
                DrawNoDatabaseMessage();
                return;
            }

            float leftW   = Mathf.Max(150f, position.width * 0.17f);
            float midW    = Mathf.Max(200f, position.width * 0.30f);
            float rightW  = position.width - leftW - midW - 4f;
            float topY    = 42f;

            // Left panel — Tabs
            GUILayout.BeginArea(new Rect(0, topY, leftW, position.height - topY));
            DrawTabPanel(leftW);
            GUILayout.EndArea();

            // Divider
            EditorGUI.DrawRect(new Rect(leftW, topY, 2, position.height - topY), DividerColor);

            // Middle panel — Products
            GUILayout.BeginArea(new Rect(leftW + 2, topY, midW, position.height - topY));
            DrawProductPanel(midW);
            GUILayout.EndArea();

            // Divider
            EditorGUI.DrawRect(new Rect(leftW + 2 + midW, topY, 2, position.height - topY), DividerColor);

            // Right panel — Detail
            GUILayout.BeginArea(new Rect(leftW + 2 + midW + 2, topY, rightW, position.height - topY));
            DrawDetailPanel(rightW);
            GUILayout.EndArea();
        }

        // ── Toolbar ───────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 42), new Color(0.13f, 0.13f, 0.15f));
            GUILayout.BeginHorizontal(GUILayout.Height(42));
            GUILayout.Space(12);

            GUI.color = AccentBlue;
            GUILayout.Label("🛒  Shop Manager", _headerStyle, GUILayout.Height(42));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            if (_isDirty)
            {
                GUI.color = AccentOrange;
                GUILayout.Label("● Unsaved", EditorStyles.boldLabel, GUILayout.Height(42));
                GUI.color = Color.white;
                GUILayout.Space(6);
            }

            GUI.color = AccentGreen;
            if (GUILayout.Button("💾 Save All", GUILayout.Height(28), GUILayout.Width(90)))
            {
                SaveAll();
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;
            GUILayout.Space(6);

            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            if (GUILayout.Button("↻ Reload", GUILayout.Height(28), GUILayout.Width(80)))
            {
                LoadDatabase();
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
        }

        // ── Left Panel: Tabs ──────────────────────────────────────────────────

        private void DrawTabPanel(float width)
        {
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("TABS", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUI.color = AccentGreen;
            if (GUILayout.Button("+ Add", GUILayout.Width(50), GUILayout.Height(20)))
            {
                _addingTab  = !_addingTab;
                _newTabId   = "";
                _newTabLabel = "";
            }
            GUI.color = Color.white;
            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            // New tab form
            if (_addingTab)
            {
                DrawAddTabForm();
            }

            // Tab list
            _tabScroll = GUILayout.BeginScrollView(_tabScroll);

            var tabs = _db.tabs ?? new List<ShopTabSO>();
            int pendingSelect  = -1;
            int pendingDelete  = -1;
            int pendingMoveUp  = -1;
            int pendingMoveDown = -1;

            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (!tab) continue;

                bool selected = (i == _selectedTabIdx);
                var  bg       = selected ? SelectedBg : (i % 2 == 0 ? new Color(0.20f, 0.20f, 0.23f) : new Color(0.22f, 0.22f, 0.25f));
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(width, 34), bg);
                var lastRect = GUILayoutUtility.GetLastRect();

                // Row layout
                GUILayout.BeginArea(lastRect);
                GUILayout.BeginHorizontal(GUILayout.Height(34));
                GUILayout.Space(6);

                if (GUILayout.Button(tab.DisplayName, GUIStyle.none,
                        GUILayout.Height(34), GUILayout.ExpandWidth(true)))
                    pendingSelect = i;

                GUILayout.FlexibleSpace();

                // Reorder
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(20), GUILayout.Height(20))) pendingMoveUp   = i;
                GUI.enabled = i < tabs.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(20), GUILayout.Height(20))) pendingMoveDown = i;
                GUI.enabled = true;

                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(20))) pendingDelete = i;
                GUI.color = Color.white;

                GUILayout.Space(4);
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                GUILayout.Space(34); // reserve the height used by BeginArea
            }

            GUILayout.EndScrollView();

            // Footer count
            GUILayout.FlexibleSpace();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(width, 1), DividerColor);
            GUI.color = TextMuted;
            GUILayout.Label($"  {tabs.Count} tab(s)", EditorStyles.miniLabel, GUILayout.Height(20));
            GUI.color = Color.white;

            // Apply mutations after EndScrollView
            if (pendingSelect >= 0)  { SelectTab(pendingSelect); GUIUtility.ExitGUI(); }
            if (pendingMoveUp >= 0)  { MoveTab(pendingMoveUp, -1); GUIUtility.ExitGUI(); }
            if (pendingMoveDown >= 0){ MoveTab(pendingMoveDown, 1); GUIUtility.ExitGUI(); }
            if (pendingDelete >= 0)
            {
                var t = tabs[pendingDelete];
                if (EditorUtility.DisplayDialog("Remove Tab",
                    $"Remove tab '{t?.DisplayName}' from the database?\n\nThe .asset file is NOT deleted.",
                    "Remove", "Cancel"))
                {
                    RemoveTab(pendingDelete);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawAddTabForm()
        {
            var formBg = new GUIStyle { normal = { background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.18f)) }, padding = new RectOffset(0, 0, 4, 4) };
            GUILayout.BeginVertical(formBg, GUILayout.Height(80));

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("ID:", GUILayout.Width(28));
            _newTabId = EditorGUILayout.TextField(_newTabId);
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("Label:", GUILayout.Width(28));
            _newTabLabel = EditorGUILayout.TextField(_newTabLabel);
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.color = AccentGreen;
            if (GUILayout.Button("✓ Create", GUILayout.Width(70), GUILayout.Height(20)))
            {
                CreateTab(_newTabId.Trim(), _newTabLabel.Trim());
                _addingTab = false;
                GUIUtility.ExitGUI();
            }
            GUI.color = AccentRed;
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
                _addingTab = false;
            GUI.color = Color.white;
            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        // ── Middle Panel: Products ─────────────────────────────────────────────

        private void DrawProductPanel(float width)
        {
            GUILayout.Space(8);

            string tabLabel = _selectedTab != null ? _selectedTab.DisplayName : "—";
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label($"PRODUCTS  ({tabLabel})", _subHeaderStyle);
            GUILayout.FlexibleSpace();

            GUI.enabled = _selectedTab != null;
            GUI.color   = _selectedTab != null ? AccentGreen : new Color(0.4f, 0.4f, 0.4f);
            if (GUILayout.Button("+ Add", GUILayout.Width(50), GUILayout.Height(20)))
            {
                _addingProduct  = !_addingProduct;
                _newProductId   = "";
                _newProductName = "";
            }
            GUI.color   = Color.white;
            GUI.enabled = true;
            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            // New product form
            if (_addingProduct && _selectedTab != null)
                DrawAddProductForm();

            // Product list for selected tab
            _productScroll = GUILayout.BeginScrollView(_productScroll);

            var products = ProductsForSelectedTab();
            int pendingSelect   = -1;
            int pendingDelete   = -1;
            int pendingMoveUp   = -1;
            int pendingMoveDown = -1;

            for (int i = 0; i < products.Count; i++)
            {
                var p = products[i];
                if (!p) continue;

                bool selected = (p == _selectedProduct);
                var  bg       = selected ? SelectedBg : (i % 2 == 0 ? new Color(0.20f, 0.20f, 0.23f) : new Color(0.22f, 0.22f, 0.25f));

                EditorGUI.DrawRect(GUILayoutUtility.GetRect(width, 40), bg);
                var lastRect = GUILayoutUtility.GetLastRect();

                GUILayout.BeginArea(lastRect);
                GUILayout.BeginHorizontal(GUILayout.Height(40));
                GUILayout.Space(6);

                GUILayout.BeginVertical(GUILayout.Height(40));
                GUILayout.Space(4);
                GUILayout.Label(p.DisplayName, EditorStyles.boldLabel);

                // Badges row
                GUILayout.BeginHorizontal();
                if (p.bestValue)   DrawBadge("Best Value",  AccentYellow);
                if (p.mostPopular) DrawBadge("Popular",     AccentBlue);
                if (p.isOneTime)   DrawBadge("One-Time",    AccentOrange);
                if (p.IsIap)       DrawBadge("IAP",         new Color(0.8f, 0.3f, 1f));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Reorder
                int dbIdx = _db.products.IndexOf(p);
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(20), GUILayout.Height(20))) pendingMoveUp   = dbIdx;
                GUI.enabled = i < products.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(20), GUILayout.Height(20))) pendingMoveDown = dbIdx;
                GUI.enabled = true;

                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(20))) pendingDelete = dbIdx;
                GUI.color = Color.white;

                GUILayout.Space(4);
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                GUILayout.Space(40);

                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)
                    && Event.current.type == EventType.MouseDown)
                    pendingSelect = i;
            }

            GUILayout.EndScrollView();

            // Footer
            GUILayout.FlexibleSpace();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(width, 1), DividerColor);
            GUI.color = TextMuted;
            GUILayout.Label($"  {products.Count} product(s)", EditorStyles.miniLabel, GUILayout.Height(20));
            GUI.color = Color.white;

            // Apply mutations
            if (pendingSelect >= 0)
            {
                int dbIdx2 = _db.products.IndexOf(products[pendingSelect]);
                SelectProduct(dbIdx2);
                GUIUtility.ExitGUI();
            }
            if (pendingMoveUp >= 0)   { MoveProduct(pendingMoveUp,   -1); GUIUtility.ExitGUI(); }
            if (pendingMoveDown >= 0) { MoveProduct(pendingMoveDown,   1); GUIUtility.ExitGUI(); }
            if (pendingDelete >= 0)
            {
                var p = _db.products[pendingDelete];
                if (EditorUtility.DisplayDialog("Remove Product",
                    $"Remove '{p?.DisplayName}' from the database?\n\nThe .asset file is NOT deleted.",
                    "Remove", "Cancel"))
                {
                    RemoveProduct(pendingDelete);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawAddProductForm()
        {
            var formBg = new GUIStyle { normal = { background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.18f)) }, padding = new RectOffset(0, 0, 4, 4) };
            GUILayout.BeginVertical(formBg, GUILayout.Height(86));

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("ID:", GUILayout.Width(34));
            _newProductId = EditorGUILayout.TextField(_newProductId);
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("Name:", GUILayout.Width(34));
            _newProductName = EditorGUILayout.TextField(_newProductName);
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.color = AccentGreen;
            if (GUILayout.Button("✓ Create", GUILayout.Width(70), GUILayout.Height(20)))
            {
                CreateProduct(_newProductId.Trim(), _newProductName.Trim());
                _addingProduct = false;
                GUIUtility.ExitGUI();
            }
            GUI.color = AccentRed;
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
                _addingProduct = false;
            GUI.color = Color.white;
            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        // ── Right Panel: Product Detail ────────────────────────────────────────

        private void DrawDetailPanel(float width)
        {
            if (_selectedProduct == null || _productSO == null)
            {
                DrawEmptyDetail();
                return;
            }

            _productSO.Update();

            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            GUILayout.Space(10);

            // ── Header row ────────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUI.color = Color.white;
            GUILayout.Label(_selectedProduct.DisplayName, _headerStyle);
            GUILayout.FlexibleSpace();

            GUI.color = new Color(0.65f, 0.65f, 0.7f);
            if (GUILayout.Button("Ping Asset", GUILayout.Width(80), GUILayout.Height(24)))
                EditorGUIUtility.PingObject(_selectedProduct);
            GUI.color = Color.white;
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            float labelW = Mathf.Clamp(width * 0.28f, 100f, 180f);

            // ── Identity card ─────────────────────────────────────────────────
            BeginCard();
            GUILayout.Label("Identity", _subHeaderStyle);
            GUILayout.Space(4);

            DrawPropField(_propId,          "ID",           labelW);
            DrawPropField(_propDisplayName, "Display Name", labelW);
            DrawPropField(_propIcon,        "Icon",         labelW);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tab", GUILayout.Width(labelW));
            GUI.color = TextMuted;
            GUILayout.Label(_propTabId.stringValue, EditorStyles.boldLabel);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            EndCard();
            GUILayout.Space(6);

            // ── Cost card ─────────────────────────────────────────────────────
            BeginCard();
            GUILayout.Label("Cost", _subHeaderStyle);
            GUILayout.Space(4);
            DrawPropField(_propPriceCurrency, "Currency", labelW);
            DrawPropField(_propPriceAmount,   "Amount",   labelW);
            EndCard();
            GUILayout.Space(6);

            // ── Merchandising card ────────────────────────────────────────────
            BeginCard();
            GUILayout.Label("Merchandising", _subHeaderStyle);
            GUILayout.Space(4);
            DrawPropField(_propBestValue,   "Best Value",   labelW);
            DrawPropField(_propMostPopular, "Most Popular", labelW);
            DrawPropField(_propDescription, "Description",  labelW);
            EndCard();
            GUILayout.Space(6);

            // ── One-Time / IAP card ───────────────────────────────────────────
            BeginCard();
            GUILayout.Label("One-Time / IAP", _subHeaderStyle);
            GUILayout.Space(4);
            DrawPropField(_propIsOneTime,    "One-Time",     labelW);
            DrawPropField(_propIapProductId, "IAP Product ID", labelW);
            EndCard();
            GUILayout.Space(6);

            // ── Rewards list ──────────────────────────────────────────────────
            BeginCard();
            DrawRewardList(_propRewards, "Rewards");
            EndCard();
            GUILayout.Space(6);

            // ── First-Purchase Bonus list ─────────────────────────────────────
            BeginCard();
            DrawRewardList(_propBonus, "First-Purchase Bonus");
            EndCard();
            GUILayout.Space(12);

            GUILayout.EndScrollView();

            // ── Footer save/discard ───────────────────────────────────────────
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(width, 1), DividerColor);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (_productSO.hasModifiedProperties)
            {
                GUI.color = AccentOrange;
                GUILayout.Label("Unsaved changes", EditorStyles.miniLabel, GUILayout.Height(26));
                GUI.color = Color.white;
                GUILayout.Space(6);
            }

            GUI.color = AccentGreen;
            if (GUILayout.Button("💾 Save", GUILayout.Width(70), GUILayout.Height(26)))
            {
                ApplyAndSaveProduct();
                GUIUtility.ExitGUI();
            }
            GUI.color = AccentRed;
            if (GUILayout.Button("↩ Revert", GUILayout.Width(70), GUILayout.Height(26)))
            {
                _productSO.Update();
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        private void DrawRewardList(SerializedProperty listProp, string header)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(header, _subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUI.color = AccentGreen;
            if (GUILayout.Button("+ Add", GUILayout.Width(50), GUILayout.Height(20)))
            {
                listProp.arraySize++;
                var newEntry = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                newEntry.FindPropertyRelative("kind").enumValueIndex   = 0;
                newEntry.FindPropertyRelative("amount").intValue       = 1;
                newEntry.FindPropertyRelative("powerupId").stringValue = "";
                _isDirty = true;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (listProp.arraySize == 0)
            {
                GUI.color = TextMuted;
                GUILayout.Label("  (none)", EditorStyles.miniLabel);
                GUI.color = Color.white;
                return;
            }

            int toRemove = -1;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var entry     = listProp.GetArrayElementAtIndex(i);
                var propKind  = entry.FindPropertyRelative("kind");
                var propAmt   = entry.FindPropertyRelative("amount");
                var propPwId  = entry.FindPropertyRelative("powerupId");

                GUILayout.BeginHorizontal();

                // Kind dropdown
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(propKind, GUIContent.none, GUILayout.Width(140));
                if (EditorGUI.EndChangeCheck()) _isDirty = true;

                // Amount field
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(propAmt, GUIContent.none, GUILayout.Width(50));
                if (EditorGUI.EndChangeCheck()) _isDirty = true;

                // PowerupId / entitlement key (only meaningful for PowerupCharge and Entitlement kinds)
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(propPwId, GUIContent.none);
                if (EditorGUI.EndChangeCheck()) _isDirty = true;

                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
                    toRemove = i;
                GUI.color = Color.white;

                GUILayout.EndHorizontal();
            }

            if (toRemove >= 0)
            {
                listProp.DeleteArrayElementAtIndex(toRemove);
                _isDirty = true;
            }
        }

        private void DrawEmptyDetail()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.color = TextMuted;
            GUILayout.Label("← Select a product to edit", EditorStyles.boldLabel);
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ── CRUD operations ───────────────────────────────────────────────────

        private void CreateTab(string id, string label)
        {
            if (string.IsNullOrEmpty(id))
            {
                EditorUtility.DisplayDialog("Missing ID", "Please enter a tab ID before creating.", "OK");
                return;
            }

            EnsureDir(ShopResDir);
            string path = $"{ShopResDir}/ShopTab_{id}.asset";
            var tab = AssetDatabase.LoadAssetAtPath<ShopTabSO>(path);
            if (!tab)
            {
                tab = CreateInstance<ShopTabSO>();
                AssetDatabase.CreateAsset(tab, path);
            }

            var so = new SerializedObject(tab);
            so.FindProperty("id").stringValue          = id;
            so.FindProperty("displayName").stringValue = string.IsNullOrEmpty(label) ? id : label;
            so.FindProperty("order").intValue          = _db.tabs?.Count ?? 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tab);

            _db.tabs ??= new List<ShopTabSO>();
            if (!_db.tabs.Contains(tab))
                _db.tabs.Add(tab);

            EditorUtility.SetDirty(_db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _selectedTabIdx = _db.tabs.Count - 1;
            SelectTab(_selectedTabIdx);
            _isDirty = false;
        }

        private void RemoveTab(int idx)
        {
            if (_db.tabs == null || idx < 0 || idx >= _db.tabs.Count) return;
            _db.tabs.RemoveAt(idx);
            EditorUtility.SetDirty(_db);
            if (_selectedTabIdx >= _db.tabs.Count) SelectTab(_db.tabs.Count - 1);
            _isDirty = false;
            SaveAll();
        }

        private void MoveTab(int idx, int direction)
        {
            var list = _db.tabs;
            if (list == null) return;
            int newIdx = idx + direction;
            if (newIdx < 0 || newIdx >= list.Count) return;
            (list[idx], list[newIdx]) = (list[newIdx], list[idx]);
            EditorUtility.SetDirty(_db);
            _selectedTabIdx = newIdx;
            _isDirty = false;
            SaveAll();
        }

        private void CreateProduct(string id, string displayName)
        {
            if (_selectedTab == null)
            {
                EditorUtility.DisplayDialog("No Tab Selected", "Select a tab first.", "OK");
                return;
            }
            if (string.IsNullOrEmpty(id))
            {
                EditorUtility.DisplayDialog("Missing ID", "Please enter a product ID before creating.", "OK");
                return;
            }

            EnsureDir(ShopResDir);
            string path = $"{ShopResDir}/ShopProduct_{id}.asset";
            var product = AssetDatabase.LoadAssetAtPath<ShopProductSO>(path);
            if (!product)
            {
                product = CreateInstance<ShopProductSO>();
                AssetDatabase.CreateAsset(product, path);
            }

            var so = new SerializedObject(product);
            so.FindProperty("id").stringValue          = id;
            so.FindProperty("displayName").stringValue = string.IsNullOrEmpty(displayName) ? id : displayName;
            so.FindProperty("tabId").stringValue       = _selectedTab.id;
            so.FindProperty("rewards").ClearArray();
            so.FindProperty("firstPurchaseBonus").ClearArray();
            so.FindProperty("isOneTime").boolValue     = false;
            so.FindProperty("bestValue").boolValue     = false;
            so.FindProperty("iapProductId").stringValue = "";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(product);

            _db.products ??= new List<ShopProductSO>();
            if (!_db.products.Contains(product))
                _db.products.Add(product);

            EditorUtility.SetDirty(_db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectProduct(_db.products.Count - 1);
            _isDirty = false;
        }

        private void RemoveProduct(int dbIdx)
        {
            if (_db.products == null || dbIdx < 0 || dbIdx >= _db.products.Count) return;
            _db.products.RemoveAt(dbIdx);
            EditorUtility.SetDirty(_db);
            SelectProduct(-1);
            _isDirty = false;
            SaveAll();
        }

        private void MoveProduct(int dbIdx, int direction)
        {
            var list = _db.products;
            if (list == null) return;
            int newIdx = dbIdx + direction;
            if (newIdx < 0 || newIdx >= list.Count) return;
            (list[dbIdx], list[newIdx]) = (list[newIdx], list[dbIdx]);
            EditorUtility.SetDirty(_db);
            _selectedProductIdx = newIdx;
            _isDirty = false;
            SaveAll();
        }

        private void ApplyAndSaveProduct()
        {
            if (_productSO == null) return;
            _productSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selectedProduct);
            AssetDatabase.SaveAssetIfDirty(_selectedProduct);
            _isDirty = false;
        }

        private void SaveAll()
        {
            if (_productSO != null && _productSO.hasModifiedProperties)
                _productSO.ApplyModifiedProperties();
            if (_selectedProduct) EditorUtility.SetDirty(_selectedProduct);
            if (_db)              EditorUtility.SetDirty(_db);
            AssetDatabase.SaveAssets();
            _isDirty = false;
        }

        // ── Selection ─────────────────────────────────────────────────────────

        private void SelectTab(int idx)
        {
            var tabs = _db?.tabs;
            _selectedTabIdx = idx;
            _selectedTab    = (tabs != null && idx >= 0 && idx < tabs.Count) ? tabs[idx] : null;
            SelectProduct(-1);
            _productScroll  = Vector2.zero;
        }

        private void SelectProduct(int dbIdx)
        {
            var products = _db?.products;
            _selectedProductIdx = dbIdx;
            _selectedProduct    = (products != null && dbIdx >= 0 && dbIdx < products.Count) ? products[dbIdx] : null;
            _detailScroll       = Vector2.zero;
            CacheProductSerializedObject();
        }

        private void CacheProductSerializedObject()
        {
            if (_selectedProduct == null)
            {
                _productSO = null;
                return;
            }

            _productSO         = new SerializedObject(_selectedProduct);
            _propId            = _productSO.FindProperty("id");
            _propDisplayName   = _productSO.FindProperty("displayName");
            _propIcon          = _productSO.FindProperty("icon");
            _propTabId         = _productSO.FindProperty("tabId");
            _propPriceCurrency = _productSO.FindProperty("priceCurrency");
            _propPriceAmount   = _productSO.FindProperty("priceAmount");
            _propRewards       = _productSO.FindProperty("rewards");
            _propBonus         = _productSO.FindProperty("firstPurchaseBonus");
            _propBestValue     = _productSO.FindProperty("bestValue");
            _propMostPopular   = _productSO.FindProperty("mostPopular");
            _propDescription   = _productSO.FindProperty("description");
            _propIsOneTime     = _productSO.FindProperty("isOneTime");
            _propIapProductId  = _productSO.FindProperty("iapProductId");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private List<ShopProductSO> ProductsForSelectedTab()
        {
            var result = new List<ShopProductSO>();
            if (_selectedTab == null || _db?.products == null) return result;
            foreach (var p in _db.products)
                if (p != null && p.tabId == _selectedTab.id) result.Add(p);
            return result;
        }

        private static void DrawPropField(SerializedProperty prop, string label, float labelW)
        {
            if (prop == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelW));
            EditorGUILayout.PropertyField(prop, GUIContent.none);
            GUILayout.EndHorizontal();
        }

        private static void DrawBadge(string text, Color color)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Label(text, EditorStyles.miniButtonMid, GUILayout.Height(14));
            GUI.backgroundColor = prev;
        }

        private void DrawNoDatabaseMessage()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUI.color = AccentOrange;
            GUILayout.Label("⚠  No ShopDatabase found", _headerStyle);
            GUI.color = Color.white;
            GUILayout.Space(8);
            GUILayout.Label($"Expected at: {DatabaseAssetPath}", EditorStyles.miniLabel);
            GUILayout.Space(12);
            if (GUILayout.Button("Run  Tools → Shop → Create Default Shop Products  first",
                    GUILayout.Height(30)))
                EditorApplication.ExecuteMenuItem("Tools/Shop/Create Default Shop Products");
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        // ── Card layout helpers ───────────────────────────────────────────────

        private void BeginCard()  => GUILayout.BeginVertical(_cardStyle);
        private void EndCard()    => GUILayout.EndVertical();

        // ── Styles ────────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 15,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = Color.white }
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal   = { textColor = new Color(0.80f, 0.80f, 0.85f) }
            };

            _rowNormal = new GUIStyle
            {
                normal = { background = MakeTex(2, 2, new Color(0.20f, 0.20f, 0.23f)) }
            };

            _rowSelected = new GUIStyle
            {
                normal   = { background = MakeTex(2, 2, SelectedBg) },
                fontStyle = FontStyle.Bold
            };

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin  = new RectOffset(6, 6, 0, 0),
                normal  = { background = MakeTex(2, 2, CardBg) }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
#endif
