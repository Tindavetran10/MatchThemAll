#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using MatchThemAll.Scripts.Shop;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Shop Manager: tabs live in the top toolbar, products in the left list, the selected product
    /// in the detail pane. Same layout, palette and controls as the Template Editor (LevelEditorWindow).
    /// Open via: Match Them All → Shop Manager
    /// </summary>
    public class ShopEditorWindow : EditorWindow
    {
        private const string DatabaseAssetPath = "Assets/MatchThemAllTemplate/Resources/Shop/ShopDatabase.asset";

        private static readonly Color PanelBg      = new(0.18f, 0.18f, 0.20f);
        private static readonly Color CardBg       = new(0.22f, 0.22f, 0.25f);
        private static readonly Color ToolbarBg    = new(0.14f, 0.14f, 0.16f);
        private static readonly Color AccentBlue   = new(0.27f, 0.55f, 1.00f);
        private static readonly Color AccentGreen  = new(0.26f, 0.83f, 0.53f);
        private static readonly Color AccentRed    = new(0.90f, 0.30f, 0.30f);
        private static readonly Color AccentOrange = new(1.00f, 0.65f, 0.20f);
        private static readonly Color DividerColor = new(0.12f, 0.12f, 0.14f);
        private static readonly Color TextMuted    = new(0.60f, 0.60f, 0.65f);

        private const float DividerGrabRange = 6f;
        private const float MinColumnWidth = 200f;

        // ── State ────────────────────────────────────────────────────────────
        private ShopDatabaseSO _db;
        private int _selectedTabIdx = -1;
        private ShopProductSO _selectedProduct;
        private SerializedObject _productSO;
        private readonly List<ShopProductSO> _tabProducts = new();
        private Vector2 _listScroll, _detailScroll;
        private bool _isDirty;

        private bool _addingTab, _addingProduct;
        private string _newTabId = "", _newProductId = "";

        private float _listWidth = -1f;
        private bool _draggingDivider;

        // ── Styles ───────────────────────────────────────────────────────────
        private bool _stylesInitialized;
        private readonly List<Texture2D> _ownedTextures = new();
        private GUIStyle _cardStyle, _headerStyle, _subHeaderStyle, _rowStyle, _selectedRowStyle;
        private GUIStyle _badgeStyle, _emptyStyle;

        [MenuItem("Match Them All/Shop Manager")]
        public static void ShowWindow()
        {
            var w = GetWindow<ShopEditorWindow>("Shop Manager");
            w.minSize = new Vector2(780, 520);
            w.LoadDatabase();
        }

        private void OnEnable()
        {
            wantsMouseMove = true; // hover colours update instantly
            LoadDatabase();
        }

        private void OnDisable()
        {
            _stylesInitialized = false;
            foreach (var t in _ownedTextures) if (t) DestroyImmediate(t);
            _ownedTextures.Clear();
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            foreach (var t in _ownedTextures) if (t) DestroyImmediate(t);
            _ownedTextures.Clear();

            var cardTex = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            cardTex.SetPixels(new[] { CardBg, CardBg, CardBg, CardBg });
            cardTex.Apply();
            _ownedTextures.Add(cardTex);

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin  = new RectOffset(4, 4, 4, 4),
                normal  = { background = cardTex }
            };

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.8f, 0.85f) }
            };

            _rowStyle = EditorWindowStyles.CardButton(selected: false);
            _selectedRowStyle = EditorWindowStyles.CardButton(selected: true);
            _rowStyle.richText = _selectedRowStyle.richText = true;

            // Badges are tinted with GUI.backgroundColor over one white rounded texture.
            var white = EditorWindowStyles.MakeRounded(Color.white);
            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 2, 2),
                border = new RectOffset(EditorWindowStyles.RndBorder, EditorWindowStyles.RndBorder,
                                        EditorWindowStyles.RndBorder, EditorWindowStyles.RndBorder),
                normal = { background = white, textColor = new Color(0.1f, 0.1f, 0.12f) }
            };

            _emptyStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = TextMuted }
            };
        }

        private void LoadDatabase()
        {
            _db = AssetDatabase.LoadAssetAtPath<ShopDatabaseSO>(DatabaseAssetPath)
                  ?? Resources.Load<ShopDatabaseSO>("Shop/ShopDatabase");

            if (_db != null && _db.tabs != null && (_selectedTabIdx < 0 || _selectedTabIdx >= _db.tabs.Count))
            {
                _selectedTabIdx = -1;
                for (int i = 0; i < _db.tabs.Count; i++)
                    if (_db.tabs[i] != null) { _selectedTabIdx = i; break; }
            }
            _selectedProduct = null;
            _productSO = null;
            _isDirty = false;
            Repaint();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Layout
        // ═════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();
            EnsureStyles();

            if (!_db) { DrawNoDatabase(); return; }

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBg);
            DrawToolbar();
            if (_addingTab) DrawAddTabStrip();

            if (_listWidth < 0f) _listWidth = Mathf.Max(260f, position.width * 0.32f);
            float left = Mathf.Clamp(_listWidth, MinColumnWidth, position.width - MinColumnWidth);
            float right = position.width - left - 2;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(left));
            DrawProductList();
            GUILayout.EndVertical();

            DrawDivider();

            GUILayout.BeginVertical(GUILayout.Width(right));
            DrawDetail(right);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawDivider()
        {
            Rect r = GUILayoutUtility.GetRect(2, 0, GUILayout.Width(2), GUILayout.ExpandHeight(true));
            var grab = new Rect(r.x - DividerGrabRange, r.y, DividerGrabRange * 2 + 2, r.height);
            bool hovering = grab.Contains(Event.current.mousePosition);

            EditorGUIUtility.AddCursorRect(grab, MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(r, _draggingDivider || hovering ? AccentBlue : DividerColor);

            if (Event.current.type == EventType.MouseDown && hovering)
            {
                _draggingDivider = true;
                Event.current.Use();
            }
            if (_draggingDivider && Event.current.type == EventType.MouseDrag)
            {
                _listWidth = Mathf.Clamp(Event.current.mousePosition.x, MinColumnWidth, position.width - MinColumnWidth);
                Event.current.Use();
                Repaint();
            }
            if (_draggingDivider && Event.current.type == EventType.MouseUp)
            {
                _draggingDivider = false;
                Event.current.Use();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Toolbar (title · tabs · save)
        // ═════════════════════════════════════════════════════════════════════

        private void DrawToolbar()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 38), ToolbarBg);
            GUILayout.BeginHorizontal(GUILayout.Height(38));
            GUILayout.Space(12);

            GUI.color = AccentBlue;
            GUILayout.Label("🛒 Shop Manager", _headerStyle, GUILayout.Height(38));
            GUI.color = Color.white;
            GUILayout.Space(20);

            var tabs = _db.tabs ?? new List<ShopTabSO>();
            if (tabs.Count > 0)
            {
                var names = new string[tabs.Count];
                for (int i = 0; i < tabs.Count; i++) names[i] = tabs[i] ? tabs[i].DisplayName : "(missing)";

                int picked = GUILayout.Toolbar(_selectedTabIdx, names, GUILayout.Height(28),
                    GUILayout.Width(Mathf.Min(110f * tabs.Count, position.width * 0.5f)));
                if (picked != _selectedTabIdx)
                {
                    _selectedTabIdx = picked;
                    _selectedProduct = null;
                    _productSO = null;
                    _addingProduct = false;
                    GUI.FocusControl(null);
                }
            }

            GUILayout.Space(6);
            GUI.color = AccentGreen;
            if (GUILayout.Button(new GUIContent("+", "Add tab"), GUILayout.Width(26), GUILayout.Height(28)))
            {
                _addingTab = !_addingTab;
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;

            bool hasTab = _selectedTabIdx >= 0 && _selectedTabIdx < tabs.Count;
            if (hasTab)
            {
                GUI.enabled = _selectedTabIdx > 0;
                if (GUILayout.Button(new GUIContent("◀", "Move tab left"), GUILayout.Width(26), GUILayout.Height(28)))
                { MoveTab(_selectedTabIdx, -1); GUIUtility.ExitGUI(); }
                GUI.enabled = _selectedTabIdx < tabs.Count - 1;
                if (GUILayout.Button(new GUIContent("▶", "Move tab right"), GUILayout.Width(26), GUILayout.Height(28)))
                { MoveTab(_selectedTabIdx, 1); GUIUtility.ExitGUI(); }
                GUI.enabled = true;

                GUI.color = AccentRed;
                if (GUILayout.Button(new GUIContent("✕", "Remove selected tab"), GUILayout.Width(26), GUILayout.Height(28)))
                {
                    string label = tabs[_selectedTabIdx] ? tabs[_selectedTabIdx].DisplayName : "this tab";
                    if (EditorUtility.DisplayDialog("Remove Tab", $"Remove '{label}'? Its products stay in the database.", "Remove", "Cancel"))
                    { RemoveTab(_selectedTabIdx); GUIUtility.ExitGUI(); }
                }
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();

            if (_isDirty)
            {
                GUI.color = AccentOrange;
                GUILayout.Label("● Unsaved Changes", EditorStyles.boldLabel, GUILayout.Height(38));
                GUI.color = AccentGreen;
                if (GUILayout.Button("💾 Save", GUILayout.Height(28), GUILayout.Width(80)))
                {
                    SaveAll();
                    _isDirty = false;
                    GUIUtility.ExitGUI();
                }
                GUI.color = Color.white;
                GUILayout.Space(8);
            }

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

        private void DrawAddTabStrip()
        {
            GUILayout.BeginHorizontal(GUILayout.Height(30));
            GUILayout.Space(12);
            GUILayout.Label("New tab id", EditorStyles.miniLabel, GUILayout.Width(70), GUILayout.Height(22));
            _newTabId = EditorGUILayout.TextField(_newTabId, GUILayout.Width(180), GUILayout.Height(22));
            GUI.color = AccentGreen;
            if (GUILayout.Button("✓", GUILayout.Width(26), GUILayout.Height(22)))
            {
                CreateTab(_newTabId.Trim(), _newTabId.Trim());
                _newTabId = "";
                _addingTab = false;
                GUIUtility.ExitGUI();
            }
            GUI.color = AccentRed;
            if (GUILayout.Button("✕", GUILayout.Width(26), GUILayout.Height(22)))
            {
                _addingTab = false;
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Product list (left)
        // ═════════════════════════════════════════════════════════════════════

        private ShopTabSO SelectedTab =>
            _db.tabs != null && _selectedTabIdx >= 0 && _selectedTabIdx < _db.tabs.Count ? _db.tabs[_selectedTabIdx] : null;

        private void DrawProductList()
        {
            var tab = SelectedTab;
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label(tab ? $"PRODUCTS · {tab.DisplayName.ToUpper()}" : "PRODUCTS", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            if (tab)
            {
                GUI.color = AccentGreen;
                if (GUILayout.Button("+ New", GUILayout.Width(55), GUILayout.Height(22)))
                {
                    _addingProduct = !_addingProduct;
                    GUIUtility.ExitGUI();
                }
                GUI.color = Color.white;
            }
            GUILayout.Space(8);
            GUILayout.EndHorizontal();

            if (!tab)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Add or select a tab.", _emptyStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            if (_addingProduct)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(8);
                _newProductId = EditorGUILayout.TextField(_newProductId, GUILayout.Height(22));
                GUI.color = AccentGreen;
                if (GUILayout.Button("✓", GUILayout.Width(26), GUILayout.Height(22)))
                {
                    CreateProduct(_newProductId.Trim(), _newProductId.Trim(), tab.id);
                    _newProductId = "";
                    _addingProduct = false;
                    GUIUtility.ExitGUI();
                }
                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(26), GUILayout.Height(22)))
                {
                    _addingProduct = false;
                    GUIUtility.ExitGUI();
                }
                GUI.color = Color.white;
                GUILayout.Space(8);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            _tabProducts.Clear();
            if (_db.products != null)
                foreach (var p in _db.products)
                    if (p != null && p.tabId == tab.id) _tabProducts.Add(p);

            ShopProductSO pendingSelect = null, pendingDelete = null;

            _listScroll = GUILayout.BeginScrollView(_listScroll);
            foreach (var p in _tabProducts)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(8);
                var style = p == _selectedProduct ? _selectedRowStyle : _rowStyle;
                if (GUILayout.Button(RowLabel(p), style, GUILayout.Height(44)))
                    pendingSelect = p;

                GUI.color = new Color(0.7f, 0.3f, 0.3f);
                if (GUILayout.Button("✕", GUILayout.Width(26), GUILayout.Height(44)))
                    pendingDelete = p;
                GUI.color = Color.white;
                GUILayout.Space(8);
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            if (pendingSelect)
            {
                _selectedProduct = pendingSelect;
                _productSO = new SerializedObject(pendingSelect);
                GUI.FocusControl(null);
                GUIUtility.ExitGUI();
            }
            if (pendingDelete && EditorUtility.DisplayDialog("Remove Product", $"Remove '{pendingDelete.DisplayName}' from the shop?", "Remove", "Cancel"))
            {
                _db.products.Remove(pendingDelete);
                if (_selectedProduct == pendingDelete) { _selectedProduct = null; _productSO = null; }
                SaveAll();
                GUIUtility.ExitGUI();
            }

            GUILayout.FlexibleSpace();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), DividerColor);
            GUI.color = TextMuted;
            GUILayout.Label($"  {_tabProducts.Count} product(s) in this tab", EditorStyles.miniLabel, GUILayout.Height(22));
            GUI.color = Color.white;
        }

        /// <summary>Two-line row: name, then price and merchandising tags in muted text.</summary>
        private static string RowLabel(ShopProductSO p)
        {
            var tags = "";
            if (p.bestValue)   tags += "  ·  <color=#FFD91A>Best Value</color>";
            if (p.mostPopular) tags += "  ·  <color=#66B3FF>Popular</color>";
            if (p.isOneTime)   tags += "  ·  <color=#FFA633>One-Time</color>";
            if (p.IsIap)       tags += "  ·  <color=#CC4DFF>IAP</color>";
            return $"{p.DisplayName}\n<size=10><color=#9999A6>{FormatPrice(p)}</color>{tags}</size>";
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Product detail (right)
        // ═════════════════════════════════════════════════════════════════════

        private void DrawDetail(float width)
        {
            if (_selectedProduct == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a product to edit", _emptyStyle);
                GUILayout.FlexibleSpace();
                return;
            }

            _productSO ??= new SerializedObject(_selectedProduct);
            _productSO.Update();
            EditorGUIUtility.labelWidth = Mathf.Clamp(width * 0.28f, 110f, 200f);

            _detailScroll = GUILayout.BeginScrollView(_detailScroll);
            GUILayout.Space(10);

            // Header: name, move up/down
            int idx = _tabProducts.IndexOf(_selectedProduct);
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.Label(_selectedProduct.DisplayName, _headerStyle);
            GUILayout.FlexibleSpace();
            GUI.enabled = idx > 0;
            if (GUILayout.Button(new GUIContent("▲", "Move up in the shop"), GUILayout.Width(26), GUILayout.Height(22)))
            { MoveProductWithinTab(SelectedTab.id, idx, -1); GUIUtility.ExitGUI(); }
            GUI.enabled = idx >= 0 && idx < _tabProducts.Count - 1;
            if (GUILayout.Button(new GUIContent("▼", "Move down in the shop"), GUILayout.Width(26), GUILayout.Height(22)))
            { MoveProductWithinTab(SelectedTab.id, idx, 1); GUIUtility.ExitGUI(); }
            GUI.enabled = true;
            GUILayout.Space(12);
            GUILayout.EndHorizontal();

            // Badges
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            if (_selectedProduct.bestValue)   DrawBadge("BEST VALUE", new Color(1f, 0.85f, 0.1f));
            if (_selectedProduct.mostPopular) DrawBadge("POPULAR", new Color(0.4f, 0.7f, 1f));
            if (_selectedProduct.isOneTime)   DrawBadge("ONE-TIME", new Color(1f, 0.65f, 0.2f));
            if (_selectedProduct.IsIap)       DrawBadge("IAP", new Color(0.8f, 0.3f, 1f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            BeginSection("Identity");
            EditorGUILayout.PropertyField(_productSO.FindProperty("id"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("displayName"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("icon"));
            DrawTabPopup();
            EndSection();

            BeginSection("Price");
            EditorGUILayout.PropertyField(_productSO.FindProperty("priceCurrency"), new GUIContent("Currency"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("priceAmount"), new GUIContent("Amount"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("iapProductId"), new GUIContent("IAP Product Id"));
            EditorGUILayout.LabelField("Shown as", FormatPrice(_selectedProduct), EditorStyles.boldLabel);
            EndSection();

            BeginSection("Rewards");
            EditorGUILayout.PropertyField(_productSO.FindProperty("rewards"), true);
            EditorGUILayout.PropertyField(_productSO.FindProperty("firstPurchaseBonus"), true);
            EndSection();

            BeginSection("Merchandising");
            EditorGUILayout.PropertyField(_productSO.FindProperty("bestValue"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("mostPopular"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("isOneTime"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("description"));
            EndSection();

            GUILayout.EndScrollView();

            if (_productSO.ApplyModifiedProperties()) _isDirty = true;   // also marks the asset dirty; Save writes it
        }

        /// <summary>Tab picker instead of a raw string; moving a product follows it to the new tab.</summary>
        private void DrawTabPopup()
        {
            var tabs = _db.tabs ?? new List<ShopTabSO>();
            var prop = _productSO.FindProperty("tabId");
            var ids = new string[tabs.Count];
            var names = new string[tabs.Count];
            for (int i = 0; i < tabs.Count; i++)
            {
                ids[i] = tabs[i] ? tabs[i].id : "";
                names[i] = tabs[i] ? tabs[i].DisplayName : "(missing)";
            }

            int cur = System.Array.IndexOf(ids, prop.stringValue);
            int picked = EditorGUILayout.Popup("Tab", cur, names);
            if (picked != cur && picked >= 0)
            {
                prop.stringValue = ids[picked];
                _selectedTabIdx = picked;
            }
        }

        private void BeginSection(string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label(title.ToUpper(), _subHeaderStyle);
            GUILayout.Space(2);
        }

        private static void EndSection()
        {
            GUILayout.EndVertical();
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        private void DrawBadge(string text, Color color)
        {
            GUI.backgroundColor = color;
            GUILayout.Label(text, _badgeStyle, GUILayout.Height(16));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(4);
        }

        private void DrawNoDatabase()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("No ShopDatabase found.\nRun Tools > Shop > Create Default Shop Products first.", _emptyStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Data operations
        // ═════════════════════════════════════════════════════════════════════

        private void CreateTab(string id, string label)
        {
            if (string.IsNullOrEmpty(id)) { ShowDialog("Missing ID", "Enter a tab ID."); return; }
            EnsureDir();
            string path = $"Assets/MatchThemAllTemplate/Resources/Shop/Tabs/ShopTab_{id}.asset";
            var tab = AssetDatabase.LoadAssetAtPath<ShopTabSO>(path);
            if (!tab) { tab = CreateInstance<ShopTabSO>(); AssetDatabase.CreateAsset(tab, path); }
            var so = new SerializedObject(tab);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = string.IsNullOrEmpty(label) ? id : label;
            so.FindProperty("order").intValue = _db.tabs?.Count ?? 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            _db.tabs ??= new List<ShopTabSO>();
            _db.tabs.Add(tab);
            _selectedTabIdx = _db.tabs.Count - 1;
            _selectedProduct = null;
            _productSO = null;
            SaveAll();
        }

        private void CreateProduct(string id, string name, string tabId)
        {
            if (string.IsNullOrEmpty(id)) { ShowDialog("Missing ID", "Enter a product ID."); return; }
            EnsureDir();
            string path = $"Assets/MatchThemAllTemplate/Resources/Shop/Items/ShopProduct_{id}.asset";
            var p = AssetDatabase.LoadAssetAtPath<ShopProductSO>(path);
            if (!p) { p = CreateInstance<ShopProductSO>(); AssetDatabase.CreateAsset(p, path); }
            var so = new SerializedObject(p);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = string.IsNullOrEmpty(name) ? id : name;
            so.FindProperty("tabId").stringValue = tabId;
            so.FindProperty("rewards").ClearArray();
            so.FindProperty("firstPurchaseBonus").ClearArray();
            so.ApplyModifiedPropertiesWithoutUndo();
            _db.products ??= new List<ShopProductSO>();
            _db.products.Add(p);
            _selectedProduct = p;
            _productSO = new SerializedObject(p);
            SaveAll();
        }

        private void MoveTab(int idx, int dir)
        {
            var list = _db.tabs;
            int newIdx = idx + dir;
            if (newIdx < 0 || newIdx >= list.Count) return;
            (list[idx], list[newIdx]) = (list[newIdx], list[idx]);
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i]) continue;
                var so = new SerializedObject(list[i]);
                so.FindProperty("order").intValue = i;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(list[i]);
            }
            _selectedTabIdx = newIdx;
            SaveAll();
        }

        private void MoveProductWithinTab(string tabId, int filteredIdx, int dir)
        {
            var globals = new List<int>();
            for (int i = 0; i < _db.products.Count; i++)
                if (_db.products[i] != null && _db.products[i].tabId == tabId) globals.Add(i);

            int target = filteredIdx + dir;
            if (filteredIdx < 0 || target < 0 || target >= globals.Count) return;

            int a = globals[filteredIdx], b = globals[target];
            (_db.products[a], _db.products[b]) = (_db.products[b], _db.products[a]);
            SaveAll();
        }

        private void RemoveTab(int idx)
        {
            if (_db.tabs == null || idx < 0 || idx >= _db.tabs.Count) return;
            _db.tabs.RemoveAt(idx);
            if (_selectedTabIdx >= _db.tabs.Count) _selectedTabIdx = _db.tabs.Count - 1;
            _selectedProduct = null;
            _productSO = null;
            SaveAll();
        }

        private void SaveAll()
        {
            if (_db) EditorUtility.SetDirty(_db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDir()
        {
            Directory.CreateDirectory("Assets/MatchThemAllTemplate/Resources/Shop/Tabs");
            Directory.CreateDirectory("Assets/MatchThemAllTemplate/Resources/Shop/Items");
        }

        private static void ShowDialog(string title, string msg) => EditorUtility.DisplayDialog(title, msg, "OK");

        private static string FormatPrice(ShopProductSO p) =>
            p.IsIap ? $"${p.priceAmount / 100f:0.00}" : $"{p.priceAmount} {p.priceCurrency}";
    }
}
#endif
