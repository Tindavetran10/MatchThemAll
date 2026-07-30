#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using MatchThemAll.Scripts.Shop;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Shop Manager editor window. Three-column layout using standard EditorGUILayout (no manual
    /// BeginArea/DrawRect/MakeTex — clean, fast, no per-frame texture leaks).
    ///
    /// Open: Match Them All → Shop Manager
    /// Left   — tabs (add / remove / reorder)
    /// Middle — products in the selected tab (add / remove / reorder)
    /// Right  — product detail editor (all fields + rewards)
    /// </summary>
    public class ShopEditorWindow : EditorWindow
    {
        private const string DatabaseAssetPath = "Assets/Match Them All/Resources/Shop/ShopDatabase.asset";

        private ShopDatabaseSO _db;
        private int _selectedTabIdx = -1;
        private ShopProductSO _selectedProduct;
        private Vector2 _tabScroll, _productScroll, _detailScroll;
        private bool _addingTab, _addingProduct;
        private string _newTabId = "", _newTabLabel = "";
        private string _newProductId = "", _newProductName = "";
        private SerializedObject _productSO;

        [MenuItem("Match Them All/Shop Manager")]
        public static void ShowWindow()
        {
            var w = GetWindow<ShopEditorWindow>("Shop Manager");
            w.minSize = new Vector2(800, 500);
            w.LoadDatabase();
        }

        private void OnEnable() => LoadDatabase();

        // ── Loading ───────────────────────────────────────────────────────────

        private void LoadDatabase()
        {
            _db = AssetDatabase.LoadAssetAtPath<ShopDatabaseSO>(DatabaseAssetPath)
                  ?? Resources.Load<ShopDatabaseSO>("Shop/ShopDatabase");

            // Auto-select the first valid tab so products are visible immediately.
            if (_db != null && _db.tabs != null && (_selectedTabIdx < 0 || _selectedTabIdx >= _db.tabs.Count))
            {
                for (int i = 0; i < _db.tabs.Count; i++)
                    if (_db.tabs[i] != null) { _selectedTabIdx = i; break; }
            }
            Repaint();
        }

        // ── Main Layout ───────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_db) { DrawNoDatabase(); return; }

            // Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.color = new Color(0.55f, 0.85f, 1f);
            GUILayout.Label("🛒 Shop Manager", EditorStyles.boldLabel);
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.5f);
            if (GUILayout.Button("Save All", EditorStyles.toolbarButton))
            {
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(_db);
            }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
                LoadDatabase();
            EditorGUILayout.EndHorizontal();

            // Three columns
            EditorGUILayout.BeginHorizontal();
            DrawTabColumn();
            DrawProductColumn();
            DrawDetailColumn();
            EditorGUILayout.EndHorizontal();
        }

        // ── Left: Tabs ────────────────────────────────────────────────────────

        private void DrawTabColumn()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(160));
            GUI.color = new Color(0.55f, 0.85f, 1f);
            GUILayout.Label("TABS", EditorStyles.boldLabel);
            GUI.color = Color.white;

            // Add-tab form
            if (_addingTab)
            {
                _newTabId = EditorGUILayout.TextField("ID", _newTabId);
                _newTabLabel = EditorGUILayout.TextField("Label", _newTabLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create")) { CreateTab(_newTabId.Trim(), _newTabLabel.Trim()); _addingTab = false; }
                if (GUILayout.Button("Cancel")) _addingTab = false;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("+ Add Tab")) _addingTab = true;
            }

            EditorGUILayout.Separator();
            _tabScroll = EditorGUILayout.BeginScrollView(_tabScroll);
            var tabs = _db.tabs ?? new List<ShopTabSO>();
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] == null) continue;
                bool sel = (i == _selectedTabIdx);
                EditorGUILayout.BeginHorizontal(sel ? EditorStyles.helpBox : GUIStyle.none);
                if (GUILayout.Button(tabs[i].DisplayName, EditorStyles.label, GUILayout.ExpandWidth(true)))
                {
                    _selectedTabIdx = i;
                    _selectedProduct = null;
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(22))) { MoveTab(i, -1); GUIUtility.ExitGUI(); }
                GUI.enabled = i < tabs.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(22))) { MoveTab(i, 1); GUIUtility.ExitGUI(); }
                GUI.enabled = true;
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    if (EditorUtility.DisplayDialog("Remove Tab", $"Remove '{tabs[i].DisplayName}'?", "Remove", "Cancel"))
                    { RemoveTab(i); GUIUtility.ExitGUI(); }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── Middle: Products ──────────────────────────────────────────────────

        private List<ShopProductSO> _tabProducts = new();

        private void DrawProductColumn()
        {
            ShopTabSO tab = (_db.tabs != null && _selectedTabIdx >= 0 && _selectedTabIdx < _db.tabs.Count)
                            ? _db.tabs[_selectedTabIdx] : null;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220));
            GUI.color = new Color(0.55f, 0.85f, 1f);
            GUILayout.Label(tab != null ? $"PRODUCTS ({tab.DisplayName})" : "PRODUCTS", EditorStyles.boldLabel);
            GUI.color = Color.white;

            if (tab == null)
            {
                GUILayout.Label("Select a tab.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            // Add-product form
            if (_addingProduct)
            {
                _newProductId = EditorGUILayout.TextField("ID", _newProductId);
                _newProductName = EditorGUILayout.TextField("Name", _newProductName);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create")) { CreateProduct(_newProductId.Trim(), _newProductName.Trim(), tab.id); _addingProduct = false; }
                if (GUILayout.Button("Cancel")) _addingProduct = false;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("+ Add Product")) _addingProduct = true;
            }

            EditorGUILayout.Separator();
            _productScroll = EditorGUILayout.BeginScrollView(_productScroll);

            // Cache the tab's products
            _tabProducts.Clear();
            foreach (var p in _db.products)
                if (p != null && p.tabId == tab.id) _tabProducts.Add(p);

            for (int i = 0; i < _tabProducts.Count; i++)
            {
                var p = _tabProducts[i];
                bool sel = (p == _selectedProduct);
                EditorGUILayout.BeginHorizontal(sel ? EditorStyles.helpBox : GUIStyle.none);
                if (GUILayout.Button(p.DisplayName, EditorStyles.boldLabel, GUILayout.ExpandWidth(true)))
                {
                    _selectedProduct = p;
                    _productSO = new SerializedObject(p);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(22))) { MoveProductWithinTab(tab.id, i, -1); GUIUtility.ExitGUI(); }
                GUI.enabled = i < _tabProducts.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(22))) { MoveProductWithinTab(tab.id, i, 1); GUIUtility.ExitGUI(); }
                GUI.enabled = true;
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    if (EditorUtility.DisplayDialog("Remove Product", $"Remove '{p.DisplayName}'?", "Remove", "Cancel"))
                    { _db.products.Remove(p); _selectedProduct = null; SaveAll(); GUIUtility.ExitGUI(); }
                }
                EditorGUILayout.EndHorizontal();

                // Price + badges row (colored mini-labels — zero texture allocation)
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(18);
                EditorGUILayout.LabelField(FormatPrice(p), EditorStyles.boldLabel);
                if (p.bestValue)   DrawBadge("Best Value",  new Color(1f, 0.85f, 0.1f));
                if (p.mostPopular) DrawBadge("Popular",     new Color(0.4f, 0.7f, 1f));
                if (p.isOneTime)   DrawBadge("One-Time",    new Color(1f, 0.65f, 0.2f));
                if (p.IsIap)       DrawBadge("IAP",         new Color(0.8f, 0.3f, 1f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── Right: Detail ─────────────────────────────────────────────────────

        private void DrawDetailColumn()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (_selectedProduct == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("← Select a product to edit", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            if (_productSO == null) _productSO = new SerializedObject(_selectedProduct);
            _productSO.Update();

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            GUILayout.Label(_selectedProduct.DisplayName, EditorStyles.boldLabel);

            // All standard fields via PropertyField — Unity renders them correctly.
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
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply")) { _productSO.ApplyModifiedProperties(); EditorUtility.SetDirty(_selectedProduct); AssetDatabase.SaveAssets(); }
                if (GUILayout.Button("Revert")) _productSO.Update();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        // ── No-database fallback ──────────────────────────────────────────────

        private void DrawNoDatabase()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("No ShopDatabase found.\nRun Tools → Shop → Create Default Shop Products first.", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ── CRUD ──────────────────────────────────────────────────────────────

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
            // Sync order fields so runtime OrderedTabs matches the editor.
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
            // Collect the global indices of products in this tab, then swap the two.
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

        // ── Helpers ───────────────────────────────────────────────────────────

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

        /// <summary>Draws a colored mini-badge — tints a built-in style, zero texture allocation.</summary>
        private static void DrawBadge(string text, Color color)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Label(text, EditorStyles.miniButtonMid, GUILayout.Height(16));
            GUI.backgroundColor = prev;
        }
    }
}
#endif
