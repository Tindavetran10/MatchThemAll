#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using MatchThemAll.Scripts.Shop;
using UnityEditor;
using UnityEngine;
using ZLinq;

namespace MatchThemAll.Scripts.Editor
{
    public class ShopEditorWindow : EditorWindow
    {
        private const string DatabaseAssetPath = "Assets/Match Them All/Resources/Shop/ShopDatabase.asset";
        private static readonly Color PanelBg       = new(0.18f, 0.18f, 0.20f);
        private static readonly Color CardBg         = new(0.22f, 0.22f, 0.25f);
        private static readonly Color AccentBlue     = new(0.27f, 0.55f, 1.00f);
        private static readonly Color AccentGreen    = new(0.26f, 0.83f, 0.53f);
        private static readonly Color BgLight        = new(0.25f, 0.25f, 0.28f);
        private static readonly Color BgHover        = new(0.30f, 0.30f, 0.34f);
        private static readonly Color AccentRed      = new(0.90f, 0.30f, 0.30f);
        private static readonly Color AccentOrange   = new(1.00f, 0.65f, 0.20f);
        private static readonly Color DividerColor   = new(0.12f, 0.12f, 0.14f);
        private static readonly Color TextMuted      = new(0.60f, 0.60f, 0.65f);

        private void OnGUI()
        {
            EnsureStyles();

            if (!_db) { DrawNoDatabase(); return; }

            // Full-window dark background (matches LevelEditorWindow / ItemManagerWindow)
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBg);

            // ── Toolbar ────────────────────────────────────────────────────────
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 38), new Color(0.14f, 0.14f, 0.16f));
            GUILayout.BeginHorizontal(GUILayout.Height(38));
            GUILayout.Space(12);
            GUI.color = AccentBlue;
            GUILayout.Label("🛒  Shop Manager", _headerStyle, GUILayout.Height(38));
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();

            GUI.color = AccentGreen;
            if (GUILayout.Button("💾 Save All", GUILayout.Height(28), GUILayout.Width(90)))
            {
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(_db);
                GUIUtility.ExitGUI();
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

            // ── Three-column body (resizable) ──────────────────────────────────
            float topY     = 38f;
            float bodyH    = position.height - topY;
            float tabW     = _tabWidth;
            float productW = _productWidth;
            float detailW  = position.width - tabW - productW - 4f;

            GUILayout.BeginArea(new Rect(0, topY, tabW, bodyH));
            DrawTabColumn();
            GUILayout.EndArea();

            DrawResizableDivider(tabW, topY, bodyH, 0);

            GUILayout.BeginArea(new Rect(tabW + 2, topY, productW, bodyH));
            DrawProductColumn();
            GUILayout.EndArea();

            DrawResizableDivider(tabW + 2 + productW, topY, bodyH, 1);

            GUILayout.BeginArea(new Rect(tabW + 2 + productW + 2, topY, detailW, bodyH));
            DrawDetailColumn();
            GUILayout.EndArea();
        }

        // ── Resizable column divider ──────────────────────────────────────────

        /// <summary>
        /// Draws a draggable divider at x. Shows a ↔ resize cursor on hover, highlights while dragging,
        /// and resizes the column to the left of the divider. dividerIndex 0 = tab/product, 1 = product/detail.
        /// </summary>
        private void DrawResizableDivider(float x, float topY, float bodyH, int dividerIndex)
        {
            Rect grabRect = new Rect(x - DividerGrabRange, topY, DividerGrabRange * 2, bodyH);
            bool hovering = grabRect.Contains(Event.current.mousePosition);
            bool active = _draggingDivider == dividerIndex || hovering;

            // Cursor: ↔ on hover
            EditorGUIUtility.AddCursorRect(grabRect, MouseCursor.ResizeHorizontal);

            // Divider line (highlighted when active)
            EditorGUI.DrawRect(new Rect(x, topY, 2, bodyH), active ? AccentBlue : DividerColor);

            // Start drag
            if (Event.current.type == EventType.MouseDown && hovering)
            {
                _draggingDivider = dividerIndex;
                Event.current.Use();
            }

            // During drag: update the column width
            if (_draggingDivider == dividerIndex && Event.current.type == EventType.MouseDrag)
            {
                float mouseX = Event.current.mousePosition.x;
                if (dividerIndex == 0)
                    _tabWidth = Mathf.Clamp(mouseX, MinColumnWidth, position.width - MinColumnWidth * 2);
                else
                    _productWidth = Mathf.Clamp(mouseX - (_tabWidth + 2), MinColumnWidth, position.width - _tabWidth - MinColumnWidth);
                Event.current.Use();
                Repaint();
            }

            // End drag
            if (_draggingDivider == dividerIndex && Event.current.type == EventType.MouseUp)
            {
                _draggingDivider = -1;
                Event.current.Use();
            }
        }

        private ShopDatabaseSO _db;
        private int _selectedTabIdx = -1;
        private ShopProductSO _selectedProduct;
        private Vector2 _tabScroll, _productScroll, _detailScroll;
        private bool _addingTab, _addingProduct;
        private string _newTabId = "", _newTabLabel = "";
        private string _newProductId = "", _newProductName = "";
        private SerializedObject _productSO;
        private List<ShopProductSO> _tabProducts = new();

        // ── Resizable columns ────────────────────────────────────────────────
        private float _tabWidth = 170f;
        private float _productWidth = 250f;
        private int _draggingDivider = -1; // -1 = none, 0 = tab/product, 1 = product/detail
        private const float DividerGrabRange = 6f;
        private const float MinColumnWidth = 100f;

        private bool _stylesInitialized;
        private readonly List<Texture2D> _ownedTextures = new();
        private GUIStyle _columnStyle, _headerStyle, _subHeaderStyle;
        private GUIStyle _tabButtonStyle, _selectedTabButtonStyle;
        private GUIStyle _productButtonStyle, _selectedProductButtonStyle;
        private GUIStyle _badgeStyle, _detailHeaderStyle, _toolbarStyle, _toolbarHeaderStyle;
        private GUIStyle _applyButtonStyle, _revertButtonStyle, _noDbLabelStyle;
        private GUIStyle _addButtonStyle, _addFormButtonStyle, _toolbarSaveButtonStyle;

        [MenuItem("Match Them All/Shop Manager")]
        public static void ShowWindow()
        {
            var w = GetWindow<ShopEditorWindow>("Shop Manager");
            w.minSize = new Vector2(800, 500);
            w.LoadDatabase();
        }

        private void OnEnable() => LoadDatabase();

        private void OnDisable()
        {
            _stylesInitialized = false;
            foreach (var t in _ownedTextures.AsValueEnumerable().Where(t => t))
                DestroyImmediate(t);
            _ownedTextures.Clear();
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            foreach (var t in _ownedTextures.AsValueEnumerable().Where(t => t))
                DestroyImmediate(t);
            _ownedTextures.Clear();

            var cardBg = MakeTex(2, 2, CardBg);
            var bgLight = MakeTex(2, 2, BgLight);
            var bgHover = MakeTex(2, 2, BgHover);
            var selectedBg = MakeTex(2, 2, new Color(AccentBlue.r * 0.7f, AccentBlue.g * 0.7f, AccentBlue.b * 0.7f));

            _columnStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(4, 4, 4, 4),
                normal = { background = cardBg }
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

            _tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 8, 8, 8),
                margin = new RectOffset(4, 4, 2, 2),
                fontSize = 12,
                normal = { background = bgLight, textColor = Color.white },
                hover = { background = bgHover }
            };

            _selectedTabButtonStyle = new GUIStyle(_tabButtonStyle)
            {
                normal = { background = selectedBg },
                fontStyle = FontStyle.Bold
            };

            _productButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 8, 8, 8),
                margin = new RectOffset(4, 4, 2, 2),
                fontSize = 12,
                normal = { background = bgLight, textColor = Color.white },
                hover = { background = bgHover }
            };

            _selectedProductButtonStyle = new GUIStyle(_productButtonStyle)
            {
                normal = { background = selectedBg },
                fontStyle = FontStyle.Bold
            };

            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(5, 5, 2, 2),
                normal = { textColor = Color.white }
            };

            _detailHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            _toolbarStyle = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 22,
                normal = { background = MakeTex(2, 2, PanelBg) }
            };

            _toolbarHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            _toolbarSaveButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                normal = { textColor = AccentGreen },
                fontStyle = FontStyle.Bold
            };

            _addButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = AccentGreen },
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };

            _addFormButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };

            _applyButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = AccentGreen },
                fontStyle = FontStyle.Bold
            };

            _revertButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = new Color(1f, 0.65f, 0.20f) }
            };

            _noDbLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.60f, 0.60f, 0.65f) }
            };
        }

        private Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (var i = 0; i < pix.Length; i++) pix[i] = col;
            var t = new Texture2D(w, h);
            t.SetPixels(pix);
            t.Apply();
            _ownedTextures.Add(t);
            return t;
        }

        private void LoadDatabase()
        {
            _db = AssetDatabase.LoadAssetAtPath<ShopDatabaseSO>(DatabaseAssetPath)
                  ?? Resources.Load<ShopDatabaseSO>("Shop/ShopDatabase");

            if (_db != null && _db.tabs != null && (_selectedTabIdx < 0 || _selectedTabIdx >= _db.tabs.Count))
            {
                for (int i = 0; i < _db.tabs.Count; i++)
                    if (_db.tabs[i] != null) { _selectedTabIdx = i; break; }
            }
            Repaint();
        }


        private void DrawTabColumn()
        {
            EditorGUILayout.BeginVertical(_columnStyle);
            GUILayout.Label("TABS", _headerStyle);

            if (_addingTab)
                DrawAddTabForm();
            else if (GUILayout.Button("+ Add Tab", _addButtonStyle, GUILayout.Height(20)))
                _addingTab = true;

            EditorGUILayout.Space(4);
            _tabScroll = EditorGUILayout.BeginScrollView(_tabScroll);
            var tabs = _db.tabs ?? new List<ShopTabSO>();
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] == null) continue;
                bool sel = (i == _selectedTabIdx);
                var style = sel ? _selectedTabButtonStyle : _tabButtonStyle;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(tabs[i].DisplayName, style, GUILayout.ExpandWidth(true)))
                {
                    _selectedTabIdx = i;
                    _selectedProduct = null;
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(20), GUILayout.Height(18))) { MoveTab(i, -1); GUIUtility.ExitGUI(); }
                GUI.enabled = i < tabs.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(20), GUILayout.Height(18))) { MoveTab(i, 1); GUIUtility.ExitGUI(); }
                GUI.enabled = true;
                if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(18)))
                {
                    if (EditorUtility.DisplayDialog("Remove Tab", $"Remove '{tabs[i].DisplayName}'?", "Remove", "Cancel"))
                    { RemoveTab(i); GUIUtility.ExitGUI(); }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawAddTabForm()
        {
            EditorGUILayout.Space(2);
            GUILayout.Label("New Tab", _subHeaderStyle);
            _newTabId = EditorGUILayout.TextField(_newTabId);
            _newTabLabel = EditorGUILayout.TextField(_newTabLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create", _addFormButtonStyle)) { CreateTab(_newTabId.Trim(), _newTabLabel.Trim()); _addingTab = false; }
            if (GUILayout.Button("Cancel", _addFormButtonStyle)) _addingTab = false;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawProductColumn()
        {
            ShopTabSO tab = (_db.tabs != null && _selectedTabIdx >= 0 && _selectedTabIdx < _db.tabs.Count)
                            ? _db.tabs[_selectedTabIdx] : null;

            EditorGUILayout.BeginVertical(_columnStyle);
            GUILayout.Label(tab != null ? $"PRODUCTS  ({tab.DisplayName})" : "PRODUCTS", _headerStyle);

            if (tab == null)
            {
                GUILayout.Label("Select a tab.", _subHeaderStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            if (_addingProduct)
                DrawAddProductForm();
            else if (GUILayout.Button("+ Add Product", _addButtonStyle, GUILayout.Height(20)))
                _addingProduct = true;

            EditorGUILayout.Space(4);
            _productScroll = EditorGUILayout.BeginScrollView(_productScroll);

            _tabProducts.Clear();
            foreach (var p in _db.products)
                if (p != null && p.tabId == tab.id) _tabProducts.Add(p);

            for (int i = 0; i < _tabProducts.Count; i++)
            {
                var p = _tabProducts[i];
                bool sel = (p == _selectedProduct);
                var btnStyle = sel ? _selectedProductButtonStyle : _productButtonStyle;

                EditorGUILayout.BeginVertical(GUIStyle.none);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(p.DisplayName, btnStyle, GUILayout.ExpandWidth(true)))
                {
                    _selectedProduct = p;
                    _productSO = new SerializedObject(p);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(20), GUILayout.Height(18))) { MoveProductWithinTab(tab.id, i, -1); GUIUtility.ExitGUI(); }
                GUI.enabled = i < _tabProducts.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(20), GUILayout.Height(18))) { MoveProductWithinTab(tab.id, i, 1); GUIUtility.ExitGUI(); }
                GUI.enabled = true;
                if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(18)))
                {
                    if (EditorUtility.DisplayDialog("Remove Product", $"Remove '{p.DisplayName}'?", "Remove", "Cancel"))
                    { _db.products.Remove(p); _selectedProduct = null; SaveAll(); GUIUtility.ExitGUI(); }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(18);
                EditorGUILayout.LabelField(FormatPrice(p), EditorStyles.boldLabel, GUILayout.Width(80));
                if (p.bestValue)   DrawBadge("Best Value",  new Color(1f, 0.85f, 0.1f));
                if (p.mostPopular) DrawBadge("Popular",     new Color(0.4f, 0.7f, 1f));
                if (p.isOneTime)   DrawBadge("One-Time",    new Color(1f, 0.65f, 0.2f));
                if (p.IsIap)       DrawBadge("IAP",         new Color(0.8f, 0.3f, 1f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawAddProductForm()
        {
            EditorGUILayout.Space(2);
            GUILayout.Label("New Product", _subHeaderStyle);
            _newProductId = EditorGUILayout.TextField(_newProductId);
            _newProductName = EditorGUILayout.TextField(_newProductName);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create", _addFormButtonStyle)) { CreateProduct(_newProductId.Trim(), _newProductName.Trim(), _db.tabs[_selectedTabIdx].id); _addingProduct = false; }
            if (GUILayout.Button("Cancel", _addFormButtonStyle)) _addingProduct = false;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetailColumn()
        {
            EditorGUILayout.BeginVertical(_columnStyle);
            if (_selectedProduct == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a product to edit", _noDbLabelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            if (_productSO == null) _productSO = new SerializedObject(_selectedProduct);
            _productSO.Update();

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            GUILayout.Label(_selectedProduct.DisplayName, _detailHeaderStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(_productSO.FindProperty("id"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("displayName"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("icon"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("tabId"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("priceCurrency"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("priceAmount"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("rewards"), true);
            EditorGUILayout.PropertyField(_productSO.FindProperty("firstPurchaseBonus"), true);
            EditorGUILayout.PropertyField(_productSO.FindProperty("bestValue"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("mostPopular"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("description"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("isOneTime"));
            EditorGUILayout.PropertyField(_productSO.FindProperty("iapProductId"));

            EditorGUILayout.EndScrollView();

            if (_productSO.hasModifiedProperties)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply", _applyButtonStyle)) { _productSO.ApplyModifiedProperties(); EditorUtility.SetDirty(_selectedProduct); AssetDatabase.SaveAssets(); }
                if (GUILayout.Button("Revert", _revertButtonStyle)) _productSO.Update();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawNoDatabase()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("No ShopDatabase found.\nRun Tools  Shop  Create Default Shop Products first.", _noDbLabelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void DrawBadge(string text, Color color)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Label(text, _badgeStyle, GUILayout.Height(16));
            GUI.backgroundColor = prev;
        }

        private void CreateTab(string id, string label)
        {
            if (string.IsNullOrEmpty(id)) { ShowDialog("Missing ID", "Enter a tab ID."); return; }
            EnsureDir();
            string path = $"Assets/Match Them All/Resources/Shop/Tabs/ShopTab_{id}.asset";
            var tab = AssetDatabase.LoadAssetAtPath<ShopTabSO>(path);
            if (!tab) { tab = CreateInstance<ShopTabSO>(); AssetDatabase.CreateAsset(tab, path); }
            var so = new SerializedObject(tab);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = string.IsNullOrEmpty(label) ? id : label;
            so.FindProperty("order").intValue = (_db.tabs?.Count ?? 0);
            so.ApplyModifiedPropertiesWithoutUndo();
            _db.tabs ??= new List<ShopTabSO>();
            _db.tabs.Add(tab);
            _selectedTabIdx = _db.tabs.Count - 1;
            SaveAll();
        }

        private void CreateProduct(string id, string name, string tabId)
        {
            if (string.IsNullOrEmpty(id)) { ShowDialog("Missing ID", "Enter a product ID."); return; }
            EnsureDir();
            string path = $"Assets/Match Them All/Resources/Shop/Items/ShopProduct_{id}.asset";
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
            Directory.CreateDirectory("Assets/Match Them All/Resources/Shop/Tabs");
            Directory.CreateDirectory("Assets/Match Them All/Resources/Shop/Items");
        }

        private static void ShowDialog(string title, string msg)
            => EditorUtility.DisplayDialog(title, msg, "OK");

        private static string FormatPrice(ShopProductSO p)
        {
            if (p.IsIap) return $"${p.priceAmount / 100f:0.00}";
            return $"{p.priceAmount} {p.priceCurrency}";
        }
    }
}
#endif
