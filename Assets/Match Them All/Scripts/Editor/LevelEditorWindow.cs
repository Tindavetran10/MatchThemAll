using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MatchThemAll.Scripts;
using UnityEditor;
using UnityEngine;

namespace Match_Them_All.Scripts.Editor
{
    /// <summary>
    /// A self-contained Level Editor window.
    /// Open via: Match Them All → Level Editor
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        // ── State ────────────────────────────────────────────────────────────
        private List<LevelDataSO> _levels = new();
        private int _selectedLevelIndex = -1;
        private LevelDataSO _selectedLevel;

        private List<GameObject> _itemPrefabs = new();

        private Vector2 _levelListScroll;
        private Vector2 _itemListScroll;

        private bool _isDirty;

        // New level creation
        private string _newLevelName = "";
        private bool _showNewLevelField;

        private const string ItemPrefabFolder = "Assets/Match Them All/Prefabs/Items";
        private const string LevelDataFolder  = "Assets/Match Them All/Level Data";

        // ── Styles (lazy init) ────────────────────────────────────────────────
        private GUIStyle _cardStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _levelButtonStyle;
        private GUIStyle _selectedLevelButtonStyle;
        private GUIStyle _goalBadgeStyle;
        private GUIStyle _rowStyleEven;
        private GUIStyle _rowStyleOdd;
        private bool _stylesInitialized;

        // ── Colors ───────────────────────────────────────────────────────────
        private static readonly Color PanelBg       = new(0.18f, 0.18f, 0.20f);
        private static readonly Color CardBg         = new(0.22f, 0.22f, 0.25f);
        private static readonly Color AccentBlue     = new(0.27f, 0.55f, 1.00f);
        private static readonly Color AccentGreen    = new(0.26f, 0.83f, 0.53f);
        private static readonly Color AccentRed      = new(0.90f, 0.30f, 0.30f);
        private static readonly Color AccentOrange   = new(1.00f, 0.65f, 0.20f);
        private static readonly Color GoalBadgeColor = new(0.26f, 0.83f, 0.53f);
        private static readonly Color TextMuted      = new(0.60f, 0.60f, 0.65f);

        // ── Entry Point ──────────────────────────────────────────────────────
        [MenuItem("Match Them All/Level Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(780, 540);
            window.LoadAll();
        }

        private void OnEnable()  => LoadAll();
        private void OnDisable() => _stylesInitialized = false; // force style rebuild after domain reload

        // ── Data Loading ─────────────────────────────────────────────────────
        private void LoadAll()
        {
            // Levels
            _levels.Clear();
            var guids = AssetDatabase.FindAssets("t:LevelDataSO");
            foreach (var g in guids)
                _levels.Add(AssetDatabase.LoadAssetAtPath<LevelDataSO>(AssetDatabase.GUIDToAssetPath(g)));

            // Sort by asset name so the list is stable
            _levels.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // Item prefabs
            _itemPrefabs.Clear();
            var pGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ItemPrefabFolder });
            foreach (var g in pGuids)
                _itemPrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)));

            // Restore selection
            if (_selectedLevel != null)
            {
                _selectedLevelIndex = _levels.IndexOf(_selectedLevel);
                if (_selectedLevelIndex < 0) SelectLevel(-1);
            }
        }

        private void SelectLevel(int idx)
        {
            _selectedLevelIndex = idx;
            _selectedLevel = idx >= 0 && idx < _levels.Count ? _levels[idx] : null;
            _isDirty = false;
        }

        // ── Styles Init ──────────────────────────────────────────────────────
        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin  = new RectOffset(4, 4, 4, 4)
            };
            _cardStyle.normal.background = MakeTex(2, 2, CardBg);

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

            _levelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 8, 8, 8),
                margin  = new RectOffset(4, 4, 2, 2),
                fontSize = 12
            };
            _levelButtonStyle.normal.background  = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.28f));
            _levelButtonStyle.hover.background   = MakeTex(2, 2, new Color(0.30f, 0.30f, 0.34f));
            _levelButtonStyle.normal.textColor   = Color.white;

            _selectedLevelButtonStyle = new GUIStyle(_levelButtonStyle);
            _selectedLevelButtonStyle.normal.background = MakeTex(2, 2, new Color(AccentBlue.r * 0.7f, AccentBlue.g * 0.7f, AccentBlue.b * 0.7f));
            _selectedLevelButtonStyle.fontStyle = FontStyle.Bold;

            _goalBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize  = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding   = new RectOffset(5, 5, 2, 2),
                normal    = { textColor = Color.white }
            };

            _rowStyleEven = new GUIStyle();
            _rowStyleEven.normal.background = MakeTex(2, 2, new Color(0.20f, 0.20f, 0.23f));

            _rowStyleOdd = new GUIStyle();
            _rowStyleOdd.normal.background = MakeTex(2, 2, new Color(0.23f, 0.23f, 0.26f));
        }

        // ── Main OnGUI ───────────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();

            // Background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBg);

            // Top toolbar
            DrawToolbar();

            // Two-column layout — left panel is 27% of window width, min 180 px
            float leftWidth  = Mathf.Max(180f, position.width * 0.27f);
            float rightWidth = position.width - leftWidth - 2;

            GUILayout.BeginHorizontal();

            // LEFT: Level list
            GUILayout.BeginVertical(GUILayout.Width(leftWidth));
            DrawLevelList(leftWidth);
            GUILayout.EndVertical();

            // Divider
            EditorGUI.DrawRect(new Rect(leftWidth, 38, 2, position.height - 38), new Color(0.12f, 0.12f, 0.14f));
            GUILayout.Space(2);

            // RIGHT: Level detail
            GUILayout.BeginVertical(GUILayout.Width(rightWidth));
            DrawLevelDetail(rightWidth);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        // ── Toolbar ──────────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 38), new Color(0.14f, 0.14f, 0.16f));
            GUILayout.BeginHorizontal(GUILayout.Height(38));
            GUILayout.Space(12);

            GUI.color = AccentBlue;
            GUILayout.Label("⚙ Level Editor", _headerStyle, GUILayout.Height(38));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            // Save dirty indicator
            if (_isDirty)
            {
                GUI.color = AccentOrange;
                GUILayout.Label("● Unsaved Changes", EditorStyles.boldLabel, GUILayout.Height(38));
                GUI.color = Color.white;
                GUILayout.Space(8);
            }

            if (_isDirty && _selectedLevel != null)
            {
                GUI.color = AccentGreen;
                if (GUILayout.Button("💾 Save", GUILayout.Height(28), GUILayout.Width(80)))
                {
                    SaveLevel();
                    GUIUtility.ExitGUI();
                }
                GUI.color = Color.white;
            }

            GUILayout.Space(8);

            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            if (GUILayout.Button("↻ Reload", GUILayout.Height(28), GUILayout.Width(80)))
            {
                LoadAll();
                Repaint();
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;

            GUILayout.Space(12);
            GUILayout.EndHorizontal();
        }

        // ── Level List (left column) ─────────────────────────────────────────
        private void DrawLevelList(float width)
        {
            GUILayout.Space(10);

            // Section header
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label("LEVELS", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUI.color = AccentGreen;
            if (GUILayout.Button("+ New", GUILayout.Width(55), GUILayout.Height(22)))
            {
                _showNewLevelField = !_showNewLevelField;
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;
            GUILayout.Space(8);
            GUILayout.EndHorizontal();

            // New level creation field
            if (_showNewLevelField)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(8);
                _newLevelName = EditorGUILayout.TextField(_newLevelName, GUILayout.Height(22));
                GUI.color = AccentGreen;
                if (GUILayout.Button("✓", GUILayout.Width(26), GUILayout.Height(22)))
                {
                    CreateNewLevel();
                    GUIUtility.ExitGUI();
                }
                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(26), GUILayout.Height(22)))
                {
                    _showNewLevelField = false;
                    GUIUtility.ExitGUI();
                }
                GUI.color = Color.white;
                GUILayout.Space(8);
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            GUILayout.Space(4);

            // Level list scroll — defer mutations until after EndScrollView
            int pendingDeleteIndex = -1;
            int pendingSelectIndex = -1;

            _levelListScroll = GUILayout.BeginScrollView(_levelListScroll);
            for (int i = 0; i < _levels.Count; i++)
            {
                var lv = _levels[i];
                bool selected = i == _selectedLevelIndex;
                var style = selected ? _selectedLevelButtonStyle : _levelButtonStyle;

                GUILayout.BeginHorizontal();
                GUILayout.Space(8);

                if (GUILayout.Button($"  {i + 1:00}  {lv.name}", style, GUILayout.Height(36)))
                    pendingSelectIndex = i;

                // Delete button — only show dialog, queue the actual delete
                GUI.color = new Color(0.7f, 0.3f, 0.3f);
                if (GUILayout.Button("✕", GUILayout.Width(26), GUILayout.Height(36)))
                    pendingDeleteIndex = i;
                GUI.color = Color.white;

                GUILayout.Space(8);
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();

            // Apply pending actions now that all groups are closed
            if (pendingSelectIndex >= 0)
            {
                SelectLevel(pendingSelectIndex);
                GUIUtility.ExitGUI();
            }

            if (pendingDeleteIndex >= 0)
            {
                var lv = _levels[pendingDeleteIndex];
                if (EditorUtility.DisplayDialog("Delete Level",
                        $"Delete '{lv.name}'? This cannot be undone.", "Delete", "Cancel"))
                {
                    DeleteLevel(pendingDeleteIndex);
                    GUIUtility.ExitGUI();
                }
            }

            // Footer: level count
            GUILayout.FlexibleSpace();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(width, 1), new Color(0.12f, 0.12f, 0.14f));
            GUI.color = TextMuted;
            GUILayout.Label($"  {_levels.Count} level(s) total", EditorStyles.miniLabel, GUILayout.Height(22));
            GUI.color = Color.white;
        }

        // ── Level Detail (right column) ──────────────────────────────────────
        private void DrawLevelDetail(float panelWidth = 0)
        {
            if (_selectedLevel == null)
            {
                DrawEmptyState();
                return;
            }

            // Proportional label column: 22% of panel, clamped 120–220 px
            float labelW = Mathf.Clamp(panelWidth * 0.22f, 120f, 220f);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.Label(_selectedLevel.name, _headerStyle);
            GUILayout.FlexibleSpace();

            // Open in Project button
            GUI.color = new Color(0.65f, 0.65f, 0.7f);
            if (GUILayout.Button("Ping Asset", GUILayout.Width(90), GUILayout.Height(26)))
                EditorGUIUtility.PingObject(_selectedLevel);
            GUI.color = Color.white;

            GUILayout.Space(12);
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // ─ Settings Card ─
            BeginCard();

            GUILayout.Label("Level Settings", _subHeaderStyle);
            GUILayout.Space(6);

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Duration (seconds)", GUILayout.Width(labelW));
            int newDuration = EditorGUILayout.IntSlider(_selectedLevel.duration, 15, 300);
            if (newDuration != _selectedLevel.duration)
            {
                Undo.RecordObject(_selectedLevel, "Set Duration");
                _selectedLevel.duration = newDuration;
                MarkDirty();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Random Seed", GUILayout.Width(labelW));
            int newSeed = EditorGUILayout.IntField(_selectedLevel.seed);
            if (newSeed != _selectedLevel.seed)
            {
                Undo.RecordObject(_selectedLevel, "Set Seed");
                _selectedLevel.seed = newSeed;
                MarkDirty();
            }
            GUI.color = new Color(0.65f, 0.65f, 0.7f);
            if (GUILayout.Button("🎲 Random", GUILayout.Width(88)))
            {
                Undo.RecordObject(_selectedLevel, "Randomize Seed");
                _selectedLevel.seed = UnityEngine.Random.Range(0, 99999);
                MarkDirty();
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            EditorGUI.EndChangeCheck();

            EndCard();

            GUILayout.Space(8);

            // ─ Item List Card ─
            BeginCard();
            DrawItemsSection(panelWidth);
            EndCard();

            // ─ Summary footer ─
            GUILayout.Space(8);
            DrawLevelSummary(panelWidth);
        }

        // ── Items Section ────────────────────────────────────────────────────
        private void DrawItemsSection(float panelWidth = 0)
        {
            // Fixed columns: icon=36, total=52, goal=56, remove=30, padding=24
            // Remaining space split ~40% name popup, ~60% amount slider
            const float iconW      = 36f;
            const float totalW     = 52f;
            const float goalW      = 56f;
            const float removeW    = 30f;
            const float fixedW     = iconW + totalW + goalW + removeW + 24f;
            // Subtract card padding (12+12) + card margin (4+4) + a few px of GUILayout inter-element spacing
            const float cardOverhead = 40f;
            float drawableW = Mathf.Max(0f, panelWidth - cardOverhead);
            float flex      = Mathf.Max(60f, drawableW - fixedW);
            float nameW     = Mathf.Max(80f, flex * 0.38f);
            float sliderW   = Mathf.Max(80f, flex * 0.62f);

            // Item list grows to fill remaining vertical space (window height - toolbar - cards above - summary)
            float scrollH  = Mathf.Max(80f, position.height - 340f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Items", _subHeaderStyle);
            GUILayout.FlexibleSpace();

            GUI.color = AccentGreen;
            if (GUILayout.Button("+ Add Item", GUILayout.Width(90), GUILayout.Height(22)))
                ShowAddItemMenu();
            GUI.color = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // Early-out: show placeholder WITHOUT returning (BeginCard is open in the caller)
            if (_selectedLevel.itemData == null || _selectedLevel.itemData.Count == 0)
            {
                GUI.color = TextMuted;
                GUILayout.Label("No items yet. Click '+ Add Item' to begin.", EditorStyles.centeredGreyMiniLabel);
                GUI.color = Color.white;
                // do NOT return — let the method exit normally so the caller's EndCard() runs
                return;
            }

            // Header row
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), new Color(0.15f, 0.15f, 0.18f));
            GUILayout.BeginHorizontal();
            GUI.color = TextMuted;
            GUILayout.Label("",       GUILayout.Width(iconW));
            GUILayout.Label("Item",   GUILayout.Width(nameW));
            GUILayout.Label("Amount", GUILayout.Width(sliderW));
            GUILayout.Label("Total",  GUILayout.Width(totalW));
            var centeredLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Goal?",  centeredLabel, GUILayout.Width(goalW));
            GUILayout.Label("",       GUILayout.Width(removeW));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), new Color(0.15f, 0.15f, 0.18f));
            GUILayout.Space(4);

            // Pending change — applied AFTER the scroll view closes to avoid GUILayout mismatch
            int pendingChangeIndex   = -1;
            ItemLevelData pendingEntry = default;
            int removeIndex          = -1;

            _itemListScroll = GUILayout.BeginScrollView(_itemListScroll, GUILayout.Height(scrollH));

            for (int i = 0; i < _selectedLevel.itemData.Count; i++)
            {
                var entry = _selectedLevel.itemData[i];
                var rowStyle = i % 2 == 0 ? _rowStyleEven : _rowStyleOdd;

                // Begin the row group with the pre-baked background style
                GUILayout.BeginHorizontal(rowStyle, GUILayout.Height(38));

                // Icon
                GUILayout.Space(6);
                Texture2D preview = entry.itemPrefab != null
                    ? AssetPreview.GetAssetPreview(entry.itemPrefab.gameObject) : null;
                if (preview != null)
                    GUILayout.Label(preview, GUILayout.Width(iconW - 6), GUILayout.Height(30));
                else
                    GUILayout.Label("◉", GUILayout.Width(iconW - 6), GUILayout.Height(30));

                // Item popup — read only, queue change
                int currentIdx   = _itemPrefabs.FindIndex(p => p == entry.itemPrefab?.gameObject);
                string[] prefabNames = _itemPrefabs.Select(p => p.name).ToArray();
                int newIdx = EditorGUILayout.Popup(currentIdx, prefabNames, GUILayout.Width(nameW));
                if (newIdx != currentIdx && newIdx >= 0)
                {
                    var itemComp = _itemPrefabs[newIdx].GetComponent<MatchThemAll.Scripts.Item>();
                    entry.itemPrefab = itemComp;
                    pendingEntry = entry;
                    pendingChangeIndex = i;
                }

                // Amount slider — read only, queue change
                int rawAmt  = EditorGUILayout.IntSlider(entry.amount, 3, 30, GUILayout.Width(sliderW));
                int snapped = Mathf.Max(3, Mathf.RoundToInt(rawAmt / 3f) * 3);
                if (snapped != entry.amount)
                {
                    entry.amount = snapped;
                    if (pendingChangeIndex != i) { pendingEntry = entry; pendingChangeIndex = i; }
                    else pendingEntry.amount = snapped;
                }

                // Total (display current or pending amount)
                int displayAmt = (pendingChangeIndex == i) ? pendingEntry.amount : entry.amount;
                GUI.color = TextMuted;
                GUILayout.Label($"×{displayAmt}", GUILayout.Width(totalW));
                GUI.color = Color.white;

                // Goal toggle — queue change
                bool wasGoal = entry.isGoal;
                bool displayGoal = (pendingChangeIndex == i) ? pendingEntry.isGoal : wasGoal;
                bool newGoal;
                if (displayGoal)
                {
                    var savedBg = GUI.backgroundColor;
                    GUI.backgroundColor = GoalBadgeColor;
                    newGoal = GUILayout.Toggle(displayGoal, "GOAL", _goalBadgeStyle, GUILayout.Width(goalW));
                    GUI.backgroundColor = savedBg;
                }
                else
                {
                    newGoal = GUILayout.Toggle(displayGoal, "goal", EditorStyles.miniButton, GUILayout.Width(goalW));
                }
                if (newGoal != wasGoal)
                {
                    entry.isGoal = newGoal;
                    if (pendingChangeIndex != i) { pendingEntry = entry; pendingChangeIndex = i; }
                    else pendingEntry.isGoal = newGoal;
                }

                // Remove
                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(removeW), GUILayout.Height(24)))
                    removeIndex = i;
                GUI.color = Color.white;

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            // Apply all pending mutations now that all layout groups are closed
            if (pendingChangeIndex >= 0)
            {
                Undo.RecordObject(_selectedLevel, "Edit Item");
                _selectedLevel.itemData[pendingChangeIndex] = pendingEntry;
                MarkDirty();
            }

            if (removeIndex >= 0)
            {
                Undo.RecordObject(_selectedLevel, "Remove Item");
                _selectedLevel.itemData.RemoveAt(removeIndex);
                MarkDirty();
                GUIUtility.ExitGUI();
            }
        }

        // ── Level Summary ────────────────────────────────────────────────────
        private void DrawLevelSummary(float panelWidth = 0)
        {
            if (_selectedLevel?.itemData == null) return; // safe: no BeginCard open here

            int totalItems = _selectedLevel.itemData.Sum(i => i.amount);
            int goalItems  = _selectedLevel.itemData.Where(i => i.isGoal).Sum(i => i.amount);
            int goalTypes  = _selectedLevel.itemData.Count(i => i.isGoal);
            bool valid     = _selectedLevel.itemData.Count > 0 && goalTypes > 0;

            // Stat block width scales: 4 stats share 55% of panel, rest goes to validation label
            float statW = Mathf.Max(70f, panelWidth * 0.55f / 4f);

            BeginCard();
            GUILayout.BeginHorizontal();

            DrawStat("Total Items", totalItems.ToString(), AccentBlue,    statW);
            DrawStat("Goal Items",  goalItems.ToString(),  AccentGreen,   statW);
            DrawStat("Goal Types",  goalTypes.ToString(),  AccentOrange,  statW);
            DrawStat("Duration",    $"{_selectedLevel.duration}s", new Color(0.7f, 0.5f, 1f), statW);

            GUILayout.FlexibleSpace();

            if (valid)
            {
                GUI.color = AccentGreen;
                GUILayout.Label("✔ Valid level", EditorStyles.boldLabel, GUILayout.Height(40));
            }
            else
            {
                GUI.color = AccentRed;
                GUILayout.Label("⚠ Needs at least 1 goal", EditorStyles.boldLabel, GUILayout.Height(40));
            }
            GUI.color = Color.white;

            GUILayout.Space(4);
            GUILayout.EndHorizontal(); // ← must close Horizontal BEFORE EndCard (EndVertical)
            EndCard();
        }

        private void DrawStat(string label, string value, Color color, float width = 100f)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
            GUI.color = color;
            GUILayout.Label(value, new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 }, GUILayout.Height(24));
            GUI.color = TextMuted;
            GUILayout.Label(label, EditorStyles.miniLabel);
            GUI.color = Color.white;
            GUILayout.EndVertical();
        }

        // ── Empty State ──────────────────────────────────────────────────────
        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(300));
            GUI.color = new Color(0.4f, 0.4f, 0.45f);
            GUILayout.Label("Select a level on the left\nor create a new one.", 
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 13 },
                GUILayout.Height(60));
            GUI.color = Color.white;
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ── Menus ─────────────────────────────────────────────────────────────
        private void ShowAddItemMenu()
        {
            var menu = new GenericMenu();
            foreach (var prefab in _itemPrefabs)
            {
                var captured = prefab;
                menu.AddItem(new GUIContent(prefab.name), false, () => AddItem(captured));
            }
            if (_itemPrefabs.Count == 0)
                menu.AddDisabledItem(new GUIContent("No item prefabs found"));
            menu.ShowAsContext();
        }

        private void AddItem(GameObject prefab)
        {
            Undo.RecordObject(_selectedLevel, "Add Item");
            _selectedLevel.itemData ??= new List<ItemLevelData>();
            var itemComp = prefab.GetComponent<MatchThemAll.Scripts.Item>();
            _selectedLevel.itemData.Add(new ItemLevelData
            {
                itemPrefab = itemComp,
                amount = 3,
                isGoal = false
            });
            MarkDirty();
            Repaint();
        }

        // ── CRUD Operations ───────────────────────────────────────────────────
        private void CreateNewLevel()
        {
            if (string.IsNullOrWhiteSpace(_newLevelName)) return;

            if (!Directory.Exists(LevelDataFolder))
                Directory.CreateDirectory(LevelDataFolder);

            string safeName = _newLevelName.Trim();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{LevelDataFolder}/{safeName}.asset");

            var newLevel = CreateInstance<LevelDataSO>();
            newLevel.duration = 60;
            newLevel.seed = UnityEngine.Random.Range(0, 99999);
            newLevel.itemData = new List<ItemLevelData>();

            AssetDatabase.CreateAsset(newLevel, path);
            AssetDatabase.SaveAssets();

            _newLevelName = "";
            _showNewLevelField = false;

            LoadAll();
            // Auto-select the new level
            SelectLevel(_levels.IndexOf(newLevel));
            EditorGUIUtility.PingObject(newLevel);
            Repaint();
        }

        private void DeleteLevel(int idx)
        {
            var lv = _levels[idx];
            string path = AssetDatabase.GetAssetPath(lv);
            AssetDatabase.DeleteAsset(path);

            LoadAll();
            SelectLevel(-1);
            Repaint();
        }

        private void SaveLevel()
        {
            if (_selectedLevel == null) return;
            EditorUtility.SetDirty(_selectedLevel);
            AssetDatabase.SaveAssets();
            _isDirty = false;
            Repaint();
        }

        private void MarkDirty()
        {
            _isDirty = true;
            EditorUtility.SetDirty(_selectedLevel);
            Repaint();
        }

        // ── Layout Helpers ─────────────────────────────────────────────────────
        private void BeginCard()  => GUILayout.BeginVertical(_cardStyle);
        private void EndCard()    => GUILayout.EndVertical();

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var t = new Texture2D(w, h);
            t.SetPixels(pix);
            t.Apply();
            return t;
        }
    }
}
