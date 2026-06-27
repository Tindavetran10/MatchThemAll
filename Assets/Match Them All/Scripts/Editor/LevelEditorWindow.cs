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
    /// A self-contained Template Editor window for Levels, Items, and Settings.
    /// Open via: Match Them All → Template Editor
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        #region State & Fields
        // ── Tabs ─────────────────────────────────────────────────────────────
        private int _currentTab;
        private readonly string[] _tabNames = { "Levels", "Items", "Settings" };
        // ── State ────────────────────────────────────────────────────────────
        private readonly List<LevelDataSO> _levels = new();
        private int _selectedLevelIndex = -1;
        private LevelDataSO _selectedLevel;

        private readonly List<GameObject> _itemPrefabs = new();
        private readonly List<GameObject> _trashPrefabs = new();

        private Vector2 _levelListScroll;
        private Vector2 _itemListScroll;
        private Vector2 _detailScroll;

        private bool _isDirty;

        // New level creation
        private string _newLevelName = "";
        private bool _showNewLevelField;
        
        private string _itemSearchQuery = "";
        
        // Item Creation State
        private EItemName _newItemName;
        private Sprite _newItemIcon;
        private GameObject _newItemModelPrefab;
        private bool _isAddingNewItemType;
        private string _newItemTypeName = "";
        private bool _autoGenerateIcon = true;
        
        // Icon Settings State
        private Vector3 _iconRotation = new Vector3(0f, 0f, 90f);
        private float _iconPadding = 1.4f;
        private Vector3 _iconOffset = Vector3.zero;
        private Texture2D _previewIconTexture;
        private bool _previewDirty;
        private double _previewDirtyTime;
        private const double PreviewRenderThrottle = 0.08; // ponytail: coalesce rapid slider drags into fewer full renders

        // Undo State — a list of records persisted to SessionState so undo survives a second
        // delete and a domain reload / window reopen. Only primitive fields are stored; the Item
        // prefab reference is re-resolved from the restored prefab on restore (it is a struct copy
        // and the asset GUID is stable across MoveAsset, so we never trust a stale captured ref).
        [Serializable]
        private class RemovedLevelEntry
        {
            public string levelGuid;
            public int index; // original position in level.itemData; restore re-inserts here to keep spawn order
            public bool isGoal;
            public int multiplier;
            public int amount;
        }

        [Serializable]
        private class DeletedItemRecord
        {
            public string originalPrefabPath;
            public string trashPrefabPath;
            public string originalIconPath;
            public string trashIconPath;
            public List<RemovedLevelEntry> removedFromLevels = new();
        }

        [Serializable]
        private class DeletedItemRecordList { public List<DeletedItemRecord> records = new(); }

        private readonly List<DeletedItemRecord> _deletedRecords = new();
        private const string TrashUndoSessionKey = "MTA_TemplateEditor_TrashUndo";

        // Settings State
        private MatchThemAll.Scripts.Settings.GameSettingsSO _gameSettings;
        private UnityEditor.Editor _gameSettingsEditor;

        private const string ItemPrefabFolder       = "Assets/Match Them All/_START_HERE/Items";
        private const string LevelDataFolder         = "Assets/Match Them All/_START_HERE/Levels";
        private const string LevelTemplatePrefabPath = "Assets/Match Them All/Level System/Prefabs/LevelTemplate.prefab";

        // ── Preview State ────────────────────────────────────────────────────
        // Tracks which level is currently previewed in the scene so we can warn
        // the user if they've switched selection without re-running the preview.
        private LevelDataSO _previewedLevel;

        // ── Styles (lazy init) ────────────────────────────────────────────────
        private GUIStyle _cardStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _levelButtonStyle;
        private GUIStyle _selectedLevelButtonStyle;
        private GUIStyle _goalBadgeStyle;
        private GUIStyle _rowStyleEven;
        private GUIStyle _rowStyleOdd;
        private GUIStyle _iconCardSmallButtonStyle; // shared ✕/⟲ button on item & trash cards
        private GUIStyle _iconCardLabelStyle;       // shared name label under item & trash cards
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
        #endregion

        #region Entry Point
        [MenuItem("Match Them All/Template Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelEditorWindow>("Template Editor");
            window.minSize = new Vector2(780, 540);
            window.LoadAll();
        }

        private void OnEnable()
        {
            LoadAll();
            LoadTrashUndo();
        }
        private void OnDisable() => _stylesInitialized = false; // force style rebuild after domain reload
        #endregion

        #region Data Loading
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
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.Contains("/Trash/"))
                {
                    _itemPrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                }
            }

            // Trash prefabs
            _trashPrefabs.Clear();
            string trashFolder = "Assets/Match Them All/_START_HERE/Items/Trash";
            if (AssetDatabase.IsValidFolder(trashFolder))
            {
                var trashGuids = AssetDatabase.FindAssets("t:Prefab", new[] { trashFolder });
                foreach (var g in trashGuids)
                    _trashPrefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)));
            }

            // Restore selection
            if (_selectedLevel)
            {
                _selectedLevelIndex = _levels.IndexOf(_selectedLevel);
                if (_selectedLevelIndex < 0) SelectLevel(-1);
            }
            
            // Settings
            if (!_gameSettings) 
                _gameSettings = Resources.Load<MatchThemAll.Scripts.Settings.GameSettingsSO>("GameSettings");
            if (_gameSettings) 
                _gameSettingsEditor = UnityEditor.Editor.CreateEditor(_gameSettings);
        }

        private void SelectLevel(int idx)
        {
            _selectedLevelIndex = idx;
            _selectedLevel = idx >= 0 && idx < _levels.Count ? _levels[idx] : null;
            _isDirty = false;
        }
        #endregion

        #region Styles
        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin  = new RectOffset(4, 4, 4, 4),
                normal =
                {
                    background = MakeTex(2, 2, CardBg)
                }
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

            _levelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 8, 8, 8),
                margin  = new RectOffset(4, 4, 2, 2),
                fontSize = 12,
                normal =
                {
                    background = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.28f)),
                    textColor = Color.white
                },
                hover =
                {
                    background = MakeTex(2, 2, new Color(0.30f, 0.30f, 0.34f))
                }
            };

            _selectedLevelButtonStyle = new GUIStyle(_levelButtonStyle)
            {
                normal =
                {
                    background = MakeTex(2, 2, new Color(AccentBlue.r * 0.7f, AccentBlue.g * 0.7f, AccentBlue.b * 0.7f))
                },
                fontStyle = FontStyle.Bold
            };

            _goalBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize  = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding   = new RectOffset(5, 5, 2, 2),
                normal    = { textColor = Color.white }
            };

            _rowStyleEven = new GUIStyle
            {
                normal =
                {
                    background = MakeTex(2, 2, new Color(0.20f, 0.20f, 0.23f))
                }
            };

            _rowStyleOdd = new GUIStyle
            {
                normal =
                {
                    background = MakeTex(2, 2, new Color(0.23f, 0.23f, 0.26f))
                }
            };

            _iconCardSmallButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(0, 0, 0, 0),
                fontSize = 10
            };

            _iconCardLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter
            };
        }
        #endregion

        #region Main Layout
        private void OnGUI()
        {
            EnsureStyles();

            // Background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), PanelBg);

            // Top toolbar
            DrawToolbar();

            // Render current tab
            switch (_currentTab)
            {
                case 0:
                    DrawLevelEditorTab();
                    break;
                case 1:
                    DrawItemsTab();
                    break;
                case 2:
                    DrawSettingsTab();
                    break;
            }
        }

        private void DrawLevelEditorTab()
        {
            // Two-column layout — left panel is 27% of window width, min 180 px
            var leftWidth  = Mathf.Max(180f, position.width * 0.27f);
            var rightWidth = position.width - leftWidth - 2;

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
        #endregion

        #region Toolbar
        private void DrawToolbar()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, 38), new Color(0.14f, 0.14f, 0.16f));
            GUILayout.BeginHorizontal(GUILayout.Height(38));
            GUILayout.Space(12);

            GUI.color = AccentBlue;
            GUILayout.Label("⚙ Template Editor", _headerStyle, GUILayout.Height(38));
            GUI.color = Color.white;
            
            GUILayout.Space(20);
            
            // Draw Tabs
            int newTab = GUILayout.Toolbar(_currentTab, _tabNames, GUILayout.Height(28), GUILayout.Width(250));
            if (newTab != _currentTab)
            {
                _currentTab = newTab;
                GUI.FocusControl(null); // Clear focus when switching tabs
            }

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
        #endregion

        #region Level List
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
            var pendingDeleteIndex = -1;
            var pendingSelectIndex = -1;

            _levelListScroll = GUILayout.BeginScrollView(_levelListScroll);
            for (var i = 0; i < _levels.Count; i++)
            {
                var lv = _levels[i];
                var selected = i == _selectedLevelIndex;
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
        #endregion

        #region Level Detail
        private void DrawLevelDetail(float panelWidth = 0)
        {
            if (!_selectedLevel)
            {
                DrawEmptyState();
                return;
            }

            _detailScroll = GUILayout.BeginScrollView(_detailScroll);

            // Proportional label column: 22% of panel, clamped 120–220 px
            var labelW = Mathf.Clamp(panelWidth * 0.22f, 120f, 220f);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUI.color = Color.white; // Defensive reset before the header row
            GUILayout.Label(_selectedLevel.name, _headerStyle);
            GUILayout.FlexibleSpace();

            // Warn if the scene preview is showing a different level than the current selection
            if (_previewedLevel != null && _previewedLevel != _selectedLevel)
            {
                GUI.color = AccentOrange;
                GUILayout.Label($"⚠ Scene shows '{_previewedLevel.name}'", EditorStyles.miniLabel, GUILayout.Height(26));
                GUI.color = Color.white;
                GUILayout.Space(4);
            }

            GUI.color = AccentBlue;
            if (GUILayout.Button("👁 Preview Layout", GUILayout.Width(130), GUILayout.Height(26)))
            {
                var placer = FindAnyObjectByType<ItemPlacer>();
                if (!placer)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelTemplatePrefabPath);
                    if (prefab)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        placer = go.GetComponentInChildren<ItemPlacer>();
                        Undo.RegisterCreatedObjectUndo(go, "Spawn Level Template");
                        Debug.Log("Template Editor: Automatically spawned LevelTemplate into the scene.");
                    }
                    else
                    {
                        Debug.LogWarning($"Template Editor: LevelTemplate prefab not found at '{LevelTemplatePrefabPath}'. Please update the path in LevelEditorWindow.");
                    }
                }
                
                if (placer) 
                {
                    Selection.activeGameObject = placer.gameObject;
                    placer.PreviewSpawnFromEditor(_selectedLevel);
                    _previewedLevel = _selectedLevel;
                }
                else 
                {
                    Debug.LogWarning("Template Editor: Could not find or spawn ItemPlacer.");
                }
                GUIUtility.ExitGUI();
            }
            
            GUILayout.Space(4);
            GUI.color = AccentGreen;
            if (GUILayout.Button("▶ Play Level", GUILayout.Width(110), GUILayout.Height(26)))
            {
                // Guard: the Game scene must be in the build settings
                bool gameSceneInBuild = false;
                foreach (var buildScene in EditorBuildSettings.scenes)
                {
                    if (buildScene.enabled && buildScene.path.Contains("MainScene"))
                    {
                        gameSceneInBuild = true;
                        break;
                    }
                }

                if (!gameSceneInBuild)
                {
                    EditorUtility.DisplayDialog(
                        "Scene Not in Build Settings",
                        "The Game scene ('MainScene') was not found in Build Settings. " +
                        "Please add it via File → Build Settings before using Play Level.",
                        "OK");
                    GUIUtility.ExitGUI();
                    return;
                }

                // Clean up any preview instances before playing so there are no duplicates
                var placer = FindAnyObjectByType<ItemPlacer>();
                if (placer && !EditorApplication.isPlaying)
                {
                    DestroyImmediate(placer.transform.root.gameObject);
                    _previewedLevel = null;
                }

                var path = AssetDatabase.GetAssetPath(_selectedLevel);
                EditorPrefs.SetString("EditorTestLevelPath", path);
                if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;

                GUIUtility.ExitGUI();
            }

            GUILayout.Space(4);
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
            GUILayout.Label("Spot Count", GUILayout.Width(labelW));
            var newSpotCount = EditorGUILayout.IntSlider(_selectedLevel.spotCount, 5, 7);
            if (newSpotCount != _selectedLevel.spotCount)
            {
                Undo.RecordObject(_selectedLevel, "Set Spot Count");
                _selectedLevel.spotCount = newSpotCount;
                MarkDirty();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Duration (seconds)", GUILayout.Width(labelW));
            var newDuration = EditorGUILayout.IntSlider(_selectedLevel.duration, 15, 300);
            if (newDuration != _selectedLevel.duration)
            {
                Undo.RecordObject(_selectedLevel, "Set Duration");
                _selectedLevel.duration = newDuration;
                MarkDirty();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Random Seed", GUILayout.Width(labelW));
            var newSeed = EditorGUILayout.IntField(_selectedLevel.seed);
            if (newSeed != _selectedLevel.seed)
            {
                Undo.RecordObject(_selectedLevel, "Set Seed");
                _selectedLevel.seed = newSeed;
                MarkDirty();
            }
            GUI.color = new Color(0.65f, 0.65f, 0.7f);
            if (GUILayout.Button("🎲 Random", GUILayout.Width(88)))
            {
                GUI.FocusControl(null); // Clear IMGUI focus so integer fields update properly
                Undo.RecordObject(_selectedLevel, "Randomize Level");
                _selectedLevel.seed = UnityEngine.Random.Range(0, 99999);
                
                var availablePrefabs = _itemPrefabs?.ToList() ?? new List<GameObject>();
                // Pick between 2 and 6 item types, capped by how many prefabs actually exist
                var minTypes = Mathf.Min(2, availablePrefabs.Count);
                var maxTypes = Mathf.Min(7, availablePrefabs.Count + 1);
                var typeCount = availablePrefabs.Count > 0 ? UnityEngine.Random.Range(minTypes, maxTypes) : 3;

                _selectedLevel.itemData = new List<ItemLevelData>();
                var goalCount = UnityEngine.Random.Range(1, Mathf.Min(4, typeCount + 1));
                var shuffled = Enumerable.Range(0, typeCount).OrderBy(x => UnityEngine.Random.value).ToList();
                
                for (var i = 0; i < typeCount; i++)
                {
                    var entry = new ItemLevelData();
                    if (availablePrefabs.Count > 0)
                    {
                        var r = UnityEngine.Random.Range(0, availablePrefabs.Count);
                        entry.itemPrefab = availablePrefabs[r].GetComponent<Item>();
                        availablePrefabs.RemoveAt(r); // Ensure no duplicate items
                    }
                    entry.amount = UnityEngine.Random.Range(1, 11) * 3;
                    entry.multiplier = UnityEngine.Random.Range(1, 6);
                    entry.isGoal = shuffled.IndexOf(i) < goalCount;
                    _selectedLevel.itemData.Add(entry);
                }

                MarkDirty();
                GUIUtility.ExitGUI(); // Force abort GUI pass so layout redraws cleanly with new data
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

            // ─ Item Library Card ─
            GUILayout.Space(8);
            BeginCard();
            DrawItemLibrarySection(panelWidth);
            EndCard();

            // ─ Summary footer ─
            GUILayout.Space(8);
            DrawLevelSummary(panelWidth);

            // ─ Tutorial Card ─
            GUILayout.Space(8);
            BeginCard();
            DrawTutorialSection(panelWidth);
            EndCard();

            GUILayout.EndScrollView();
        }
        #endregion

        #region Items Section
        private void DrawItemsSection(float panelWidth = 0)
        {
            // Fixed columns: icon=36, total=52, goal=56, remove=30, padding=24
            // Remaining space split ~40% name popup, ~60% amount slider
            const float iconW      = 36f;
            const float totalW     = 64f;
            const float goalW      = 56f;
            const float removeW    = 30f;
            const float fixedW     = iconW + totalW + goalW + removeW + 24f;
            // Subtract card padding (12+12) + card margin (4+4) + a few px of GUILayout inter-element spacing
            const float cardOverhead = 40f;
            var drawableW = Mathf.Max(0f, panelWidth - cardOverhead);
            var flex      = Mathf.Max(60f, drawableW - fixedW);
            var nameW     = Mathf.Max(80f, flex * 0.38f);
            var sliderW   = Mathf.Max(80f, flex * 0.62f);

            // Item list uses a fixed height in the scrollable detail view
            var scrollH  = 200f;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Configured Items", _subHeaderStyle);
            GUILayout.FlexibleSpace();
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
            GUILayout.BeginHorizontal(GUILayout.Height(24));
            
            GUILayout.BeginVertical(GUILayout.Height(24)); 
            GUILayout.Space(4); // Fixed padding for optical text centering
            GUILayout.BeginHorizontal();
            GUI.color = TextMuted;
            var centeredLabel = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("",           centeredLabel, GUILayout.Width(iconW));
            GUILayout.Label("Item",       centeredLabel, GUILayout.Width(nameW));
            GUILayout.Label("Amount",     centeredLabel, GUILayout.Width(sliderW));
            GUILayout.Label("Multiplier", centeredLabel, GUILayout.Width(totalW));
            GUILayout.Label("Goal?",      centeredLabel, GUILayout.Width(goalW));
            GUILayout.Label("",           centeredLabel, GUILayout.Width(removeW));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), new Color(0.15f, 0.15f, 0.18f));
            GUILayout.Space(4);

            // Pending change — applied AFTER the scroll view closes to avoid GUILayout mismatch
            var pendingChangeIndex   = -1;
            ItemLevelData pendingEntry = default;
            var removeIndex          = -1;

            _itemListScroll = GUILayout.BeginScrollView(_itemListScroll, GUILayout.Height(scrollH));

            for (var i = 0; i < _selectedLevel.itemData.Count; i++)
            {
                var entry = _selectedLevel.itemData[i];
                var rowStyle = i % 2 == 0 ? _rowStyleEven : _rowStyleOdd;

                // Begin the row group with the pre-baked background style
                GUILayout.BeginHorizontal(rowStyle, GUILayout.Height(38));

                // Icon - vertically centered
                GUILayout.Space(6);
                GUILayout.BeginVertical(GUILayout.Height(38)); 
                GUILayout.Space(4); // (38-30)/2 = 4px padding
                if (entry.itemPrefab != null)
                    GUILayout.Label(GetItemIcon(entry.itemPrefab.gameObject), GUILayout.Width(iconW - 6), GUILayout.Height(30));
                else
                    GUILayout.Label("◉", GUILayout.Width(iconW - 6), GUILayout.Height(30));
                GUILayout.EndVertical();

                // Group the remaining controls and center them vertically
                GUILayout.BeginVertical(GUILayout.Height(38)); 
                GUILayout.Space(11); // Push down by 11px for optical Midline centering of 18px controls
                GUILayout.BeginHorizontal();

                // Item popup — read only, queue change
                var currentIdx   = _itemPrefabs.FindIndex(p => p == entry.itemPrefab?.gameObject);
                var prefabNames = _itemPrefabs.Select(p => p.name).ToArray();
                var newIdx = EditorGUILayout.Popup(currentIdx, prefabNames, GUILayout.Width(nameW));
                if (newIdx != currentIdx && newIdx >= 0)
                {
                    var itemComp = _itemPrefabs[newIdx].GetComponent<Item>();
                    entry.itemPrefab = itemComp;
                    pendingEntry = entry;
                    pendingChangeIndex = i;
                }

                // Amount slider — read only, queue change
                var rawAmt  = EditorGUILayout.IntSlider(entry.amount, 3, 30, GUILayout.Width(sliderW));
                var snapped = Mathf.Max(3, Mathf.RoundToInt(rawAmt / 3f) * 3);
                if (snapped != entry.amount)
                {
                    entry.amount = snapped;
                    if (pendingChangeIndex != i) { pendingEntry = entry; pendingChangeIndex = i; }
                    else pendingEntry.amount = snapped;
                }

                // Multiplier
                var displayMult = (pendingChangeIndex == i) ? pendingEntry.multiplier : entry.multiplier;
                var rawMult = EditorGUILayout.IntField(displayMult, GUILayout.Width(totalW));
                if (rawMult != entry.multiplier)
                {
                    entry.multiplier = Mathf.Max(1, rawMult); // ensure it's at least 1
                    if (pendingChangeIndex != i) { pendingEntry = entry; pendingChangeIndex = i; }
                    else pendingEntry.multiplier = entry.multiplier;
                }

                // Goal toggle — queue change
                var wasGoal = entry.isGoal;
                var displayGoal = (pendingChangeIndex == i) ? pendingEntry.isGoal : wasGoal;
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

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                // Remove - vertically centered for 24px height (38-24)/2 = 7px
                GUILayout.BeginVertical(GUILayout.Height(38));
                GUILayout.Space(7);
                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(removeW), GUILayout.Height(24)))
                    removeIndex = i;
                GUI.color = Color.white;
                GUILayout.EndVertical();

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
        #endregion

        #region Level Summary
        private void DrawLevelSummary(float panelWidth = 0)
        {
            if (_selectedLevel?.itemData == null) return; // safe: no BeginCard open here

            var totalItems = _selectedLevel.itemData.Sum(i => i.amount);
            var goalItems  = _selectedLevel.itemData.Where(i => i.isGoal).Sum(i => i.amount);
            var goalTypes  = _selectedLevel.itemData.Count(i => i.isGoal);
            var valid     = _selectedLevel.itemData.Count > 0 && goalTypes > 0;

            // Stat block width scales: 4 stats share 55% of panel, rest goes to validation label
            var statW = Mathf.Max(70f, panelWidth * 0.55f / 4f);

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
        #endregion

        #region Tutorial Section
        private void DrawTutorialSection(float panelWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tutorial Steps", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            
            GUI.color = AccentBlue;
            if (GUILayout.Button("+ Add Step", GUILayout.Width(90), GUILayout.Height(22)))
            {
                Undo.RecordObject(_selectedLevel, "Add Tutorial Step");
                _selectedLevel.tutorialSteps ??= new List<MatchThemAll.Scripts.Tutorial.TutorialStep>();
                _selectedLevel.tutorialSteps.Add(new MatchThemAll.Scripts.Tutorial.TutorialStep());
                MarkDirty();
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (_selectedLevel.tutorialSteps == null || _selectedLevel.tutorialSteps.Count == 0)
            {
                GUI.color = TextMuted;
                GUILayout.Label("No tutorial steps for this level.", EditorStyles.centeredGreyMiniLabel);
                GUI.color = Color.white;
                return;
            }

            var labelW = Mathf.Clamp(panelWidth * 0.22f, 120f, 220f);
            var stepsToRemove = new List<int>();

            for (int i = 0; i < _selectedLevel.tutorialSteps.Count; i++)
            {
                var step = _selectedLevel.tutorialSteps[i];
                
                GUILayout.BeginVertical("box");
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Step {i + 1}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUI.color = AccentRed;
                if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
                {
                    stepsToRemove.Add(i);
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Message", GUILayout.Width(labelW));
                step.message = EditorGUILayout.TextArea(step.message, GUILayout.Height(40));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Highlight Target", GUILayout.Width(labelW));
                step.highlightTarget = (MatchThemAll.Scripts.Tutorial.EHighlightTarget)EditorGUILayout.EnumPopup(step.highlightTarget);
                GUILayout.EndHorizontal();

                if (step.highlightTarget == MatchThemAll.Scripts.Tutorial.EHighlightTarget.SpecificItem || 
                    step.highlightTarget == MatchThemAll.Scripts.Tutorial.EHighlightTarget.GoalCard)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Item Name", GUILayout.Width(labelW));
                    step.itemName = (EItemName)EditorGUILayout.EnumPopup(step.itemName);
                    GUILayout.EndHorizontal();
                }
                else if (step.highlightTarget == MatchThemAll.Scripts.Tutorial.EHighlightTarget.Powerup)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Powerup Type", GUILayout.Width(labelW));
                    step.powerupType = (Power_Ups.EPowerupType)EditorGUILayout.EnumPopup(step.powerupType);
                    GUILayout.EndHorizontal();
                }
                else if (step.highlightTarget == MatchThemAll.Scripts.Tutorial.EHighlightTarget.Manual)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Manual Targets", GUILayout.Width(labelW));
                    GUILayout.BeginVertical();
                    step.manualTargets ??= new List<GameObject>();
                    
                    for (int j = 0; j < step.manualTargets.Count; j++)
                    {
                        GUILayout.BeginHorizontal();
                        step.manualTargets[j] = (GameObject)EditorGUILayout.ObjectField(step.manualTargets[j], typeof(GameObject), true);
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            step.manualTargets.RemoveAt(j);
                            j--;
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("+ Add Target", GUILayout.Width(100)))
                    {
                        step.manualTargets.Add(null);
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Completion Condition", GUILayout.Width(labelW));
                step.completionCondition = (MatchThemAll.Scripts.Tutorial.ECompletionCondition)EditorGUILayout.EnumPopup(step.completionCondition);
                GUILayout.EndHorizontal();

                if (step.completionCondition == MatchThemAll.Scripts.Tutorial.ECompletionCondition.OnTimer)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Auto Complete Delay", GUILayout.Width(labelW));
                    step.autoCompleteDelay = EditorGUILayout.FloatField(step.autoCompleteDelay);
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Start Delay", GUILayout.Width(labelW));
                step.startDelay = EditorGUILayout.FloatField(step.startDelay);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Pause Timer", GUILayout.Width(labelW));
                step.pauseTimer = EditorGUILayout.Toggle(step.pauseTimer);
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_selectedLevel, "Edit Tutorial Step");
                    MarkDirty();
                }

                GUILayout.EndVertical();
                GUILayout.Space(8);
            }

            if (stepsToRemove.Count > 0)
            {
                Undo.RecordObject(_selectedLevel, "Remove Tutorial Step");
                for (int i = stepsToRemove.Count - 1; i >= 0; i--)
                {
                    _selectedLevel.tutorialSteps.RemoveAt(stepsToRemove[i]);
                }
                MarkDirty();
            }
        }
        #endregion

        #region Utilities
        private static void DrawStat(string label, string value, Color color, float width = 100f)
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
        private static void DrawEmptyState()
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

        #region Item Library Section
        private Vector2 _libraryScroll;

        private void DrawItemLibrarySection(float panelWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prefab Library", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            
            // Search bar
            GUILayout.Label("Search:", GUILayout.Width(50));
            _itemSearchQuery = EditorGUILayout.TextField(_itemSearchQuery, GUILayout.Width(150));
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                _itemSearchQuery = "";
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);

            var filteredItems = _itemPrefabs.Where(p => string.IsNullOrEmpty(_itemSearchQuery) || p.name.IndexOf(_itemSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (filteredItems.Count == 0)
            {
                GUI.color = TextMuted;
                GUILayout.Label("No prefabs found matching search.", EditorStyles.centeredGreyMiniLabel);
                GUI.color = Color.white;
                return;
            }

            // Fixed height for the library
            _libraryScroll = GUILayout.BeginScrollView(_libraryScroll, GUILayout.Height(240));

            // Subtract card padding/margin overhead
            float drawableW = Mathf.Max(100f, panelWidth - 40f);
            int columns = Mathf.Max(1, Mathf.FloorToInt(drawableW / 75f));

            int i = 0;
            var pendingDeletePrefab = (GameObject)null;

            GUILayout.BeginHorizontal();
            foreach (var prefab in filteredItems)
            {
                if (i > 0 && i % columns == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.Space(8);
                    GUILayout.BeginHorizontal();
                }

                GUILayout.BeginVertical(GUILayout.Width(70), GUILayout.Height(90));
                
                var itemComp = prefab.GetComponent<Item>();
                Texture tex = GetItemIcon(prefab);
                var rect = GUILayoutUtility.GetRect(64, 64);
                var deleteRect = new Rect(rect.xMax - 18, rect.yMin + 2, 16, 16);
                
                // Disable the main button if we are hovering the delete button so the click passes through
                var e = Event.current;
                bool isHoveringDelete = deleteRect.Contains(e.mousePosition);
                
                EditorGUI.BeginDisabledGroup(isHoveringDelete);
                if (GUI.Button(rect, tex))
                {
                    AddItem(prefab);
                }
                EditorGUI.EndDisabledGroup();
                
                GUI.color = AccentRed;
                if (GUI.Button(deleteRect, "✕", _iconCardSmallButtonStyle))
                {
                    pendingDeletePrefab = prefab;
                }
                GUI.color = Color.white;
                
                GUILayout.Label(prefab.name, _iconCardLabelStyle, GUILayout.Width(64));
                
                GUILayout.EndVertical();
                i++;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndScrollView();

            if (pendingDeletePrefab != null)
            {
                SoftDeleteItem(pendingDeletePrefab);
                GUIUtility.ExitGUI();
            }
            
            GUILayout.Space(12);
            DrawTrashLibrarySection(panelWidth);
        }
        
        private Vector2 _trashScroll;

        private void DrawTrashLibrarySection(float panelWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Trash Bin", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);

            if (_trashPrefabs == null || _trashPrefabs.Count == 0)
            {
                GUI.color = TextMuted;
                GUILayout.Label("Trash is empty.", EditorStyles.centeredGreyMiniLabel);
                GUI.color = Color.white;
                return;
            }

            _trashScroll = GUILayout.BeginScrollView(_trashScroll, GUILayout.Height(140));

            float drawableW = Mathf.Max(100f, panelWidth - 40f);
            int columns = Mathf.Max(1, Mathf.FloorToInt(drawableW / 75f));

            int i = 0;
            var pendingRestorePrefab = (GameObject)null;

            GUILayout.BeginHorizontal();
            foreach (var prefab in _trashPrefabs)
            {
                if (i > 0 && i % columns == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.Space(8);
                    GUILayout.BeginHorizontal();
                }

                GUILayout.BeginVertical(GUILayout.Width(70), GUILayout.Height(90));
                
                Texture tex = GetItemIcon(prefab);
                
                var rect = GUILayoutUtility.GetRect(64, 64);
                var restoreRect = new Rect(rect.xMax - 18, rect.yMin + 2, 16, 16);
                
                // Dim item to show it's in trash
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                GUI.color = Color.white;
                
                GUI.color = AccentGreen;
                if (GUI.Button(restoreRect, "⟲", _iconCardSmallButtonStyle))
                {
                    pendingRestorePrefab = prefab;
                }
                GUI.color = Color.white;
                
                GUILayout.Label(prefab.name, _iconCardLabelStyle, GUILayout.Width(64));
                
                GUILayout.EndVertical();
                i++;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndScrollView();

            if (pendingRestorePrefab != null)
            {
                RestoreFromTrash(pendingRestorePrefab);
                GUIUtility.ExitGUI();
            }
        }
        #endregion

        /// <summary>Resolved icon texture for an item: its generated icon, else the prefab preview, else black.</summary>
        private Texture GetItemIcon(GameObject prefab)
        {
            var itemComp = prefab != null ? prefab.GetComponent<Item>() : null;
            if (itemComp != null && itemComp.Icon != null) return itemComp.Icon.texture;
            var preview = AssetPreview.GetAssetPreview(prefab);
            return preview != null ? preview : Texture2D.blackTexture;
        }

        private void SoftDeleteItem(GameObject prefab)
        {
            var itemComp = prefab.GetComponent<Item>();
            int usedCount = 0;
            var affectedLevels = new List<LevelDataSO>();

            foreach (var level in _levels)
            {
                if (level.itemData != null && level.itemData.Any(i => i.itemPrefab == itemComp))
                {
                    usedCount++;
                    affectedLevels.Add(level);
                }
            }

            string warning = $"Are you sure you want to delete '{prefab.name}'? It will be moved to the Trash folder.";
            if (usedCount > 0)
            {
                warning += $"\n\n⚠ It is currently used in {usedCount} level(s). Deleting it will remove it from those levels. You can undo this action later.";
            }

            if (!EditorUtility.DisplayDialog("Delete Item", warning, "Delete", "Cancel"))
                return;

            string trashParent = "Assets/Match Them All/_START_HERE/Items";
            string trashFolder = trashParent + "/Trash";
            if (!AssetDatabase.IsValidFolder(trashFolder))
            {
                AssetDatabase.CreateFolder(trashParent, "Trash");
            }

            var record = new DeletedItemRecord
            {
                originalPrefabPath = AssetDatabase.GetAssetPath(prefab),
                trashPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{trashFolder}/{prefab.name}.prefab"),
            };

            // Remove from levels, capturing each entry's primitive state (not the Item ref) for restore
            foreach (var level in affectedLevels)
            {
                Undo.RecordObject(level, "Remove Deleted Item");
                var entry = level.itemData.First(i => i.itemPrefab == itemComp);
                record.removedFromLevels.Add(new RemovedLevelEntry
                {
                    levelGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(level)),
                    index = level.itemData.IndexOf(entry),
                    isGoal = entry.isGoal,
                    multiplier = entry.multiplier,
                    amount = entry.amount,
                });
                level.itemData.Remove(entry);
                EditorUtility.SetDirty(level);
            }

            // Move Prefab
            string error = AssetDatabase.MoveAsset(record.originalPrefabPath, record.trashPrefabPath);
            if (!string.IsNullOrEmpty(error)) Debug.LogError($"[Template Editor] Failed to move prefab to trash: {error}");

            // Move Icon if exists
            string originalIconPath = $"Assets/Match Them All/Sprites/Icons/icon_{prefab.name.ToLowerInvariant()}.png";
            if (File.Exists(originalIconPath))
            {
                string iconTrashParent = "Assets/Match Them All/Sprites/Icons";
                string iconTrashFolder = iconTrashParent + "/Trash";
                if (!AssetDatabase.IsValidFolder(iconTrashFolder))
                {
                    AssetDatabase.CreateFolder(iconTrashParent, "Trash");
                }

                record.originalIconPath = originalIconPath;
                record.trashIconPath = AssetDatabase.GenerateUniqueAssetPath($"{iconTrashFolder}/icon_{prefab.name.ToLowerInvariant()}.png");

                string iconError = AssetDatabase.MoveAsset(originalIconPath, record.trashIconPath);
                if (!string.IsNullOrEmpty(iconError)) Debug.LogError($"[Template Editor] Failed to move icon to trash: {iconError}");
            }

            _deletedRecords.Add(record);
            SaveTrashUndo();

            AssetDatabase.SaveAssets();
            LoadAll();
            Repaint();
        }

        private void RestoreFromTrash(GameObject trashPrefab)
        {
            string trashPrefabPath = AssetDatabase.GetAssetPath(trashPrefab);
            string originalPrefabPath = $"Assets/Match Them All/_START_HERE/Items/{trashPrefab.name}.prefab";

            string originalIconPath = $"Assets/Match Them All/Sprites/Icons/icon_{trashPrefab.name.ToLowerInvariant()}.png";
            string trashIconPath = $"Assets/Match Them All/Sprites/Icons/Trash/icon_{trashPrefab.name.ToLowerInvariant()}.png";

            // Move Icon back (secondary: warn but continue on failure)
            if (AssetDatabase.LoadMainAssetAtPath(trashIconPath) != null)
            {
                string iconError = AssetDatabase.MoveAsset(trashIconPath, originalIconPath);
                if (!string.IsNullOrEmpty(iconError)) Debug.LogWarning($"[Template Editor] Could not restore icon: {iconError}");
            }

            // Move Prefab back — abort if missing or the move fails; there is nothing valid to re-attach.
            if (AssetDatabase.LoadMainAssetAtPath(trashPrefabPath) == null)
            {
                Debug.LogError($"[Template Editor] Restore aborted: trashed prefab not found at {trashPrefabPath}");
                return;
            }
            string prefabError = AssetDatabase.MoveAsset(trashPrefabPath, originalPrefabPath);
            if (!string.IsNullOrEmpty(prefabError))
            {
                Debug.LogError($"[Template Editor] Restore aborted, prefab move failed: {prefabError}");
                return;
            }

            // Re-add to levels from the stored undo record (if any). The Item prefab reference is
            // re-resolved from the restored prefab, never trusted from the captured record.
            var record = _deletedRecords.FirstOrDefault(r => r.trashPrefabPath == trashPrefabPath);
            if (record != null)
            {
                var restoredPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(originalPrefabPath);
                var itemComp = restoredPrefab != null ? restoredPrefab.GetComponent<Item>() : null;
                if (itemComp == null)
                    Debug.LogWarning($"[Template Editor] Restored '{trashPrefab.name}' has no Item component; skipped level re-attach.");

                foreach (var e in record.removedFromLevels)
                {
                    var level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(AssetDatabase.GUIDToAssetPath(e.levelGuid));
                    if (level == null || itemComp == null) continue;
                    Undo.RecordObject(level, "Restore Deleted Item");
                    level.itemData ??= new List<ItemLevelData>();
                    level.itemData.Insert(Mathf.Clamp(e.index, 0, level.itemData.Count), new ItemLevelData { itemPrefab = itemComp, isGoal = e.isGoal, multiplier = e.multiplier, amount = e.amount });
                    EditorUtility.SetDirty(level);
                }
                _deletedRecords.Remove(record);
                SaveTrashUndo();
            }
            else
            {
                Debug.LogWarning($"[Template Editor] Restored '{trashPrefab.name}' but no undo record exists (deleted before this session or record lost). Re-add it to levels manually if needed.");
            }

            AssetDatabase.SaveAssets();
            LoadAll();
            Repaint();
        }

        private void SaveTrashUndo()
        {
            if (_deletedRecords.Count == 0) { SessionState.EraseString(TrashUndoSessionKey); return; }
            SessionState.SetString(TrashUndoSessionKey, JsonUtility.ToJson(new DeletedItemRecordList { records = _deletedRecords }));
        }

        private void LoadTrashUndo()
        {
            _deletedRecords.Clear();
            var json = SessionState.GetString(TrashUndoSessionKey, "");
            if (string.IsNullOrEmpty(json)) return;
            var wrapper = JsonUtility.FromJson<DeletedItemRecordList>(json);
            if (wrapper?.records != null) _deletedRecords.AddRange(wrapper.records);
        }

        private void AddItem(GameObject prefab)
        {
            Undo.RecordObject(_selectedLevel, "Add Item");
            _selectedLevel.itemData ??= new List<ItemLevelData>();
            var itemComp = prefab.GetComponent<Item>();
            _selectedLevel.itemData.Add(new ItemLevelData
            {
                itemPrefab = itemComp,
                amount = 3,
                isGoal = false,
                multiplier = 1
            });
            MarkDirty();
            Repaint();
        }

        // ── CRUD Operations ───────────────────────────────────────────────────
        private void CreateNewLevel()
        {
            var safeName = string.IsNullOrWhiteSpace(_newLevelName) 
                ? $"LevelData{_levels.Count + 1:D2}" 
                : _newLevelName.Trim();

            if (!Directory.Exists(LevelDataFolder))
                Directory.CreateDirectory(LevelDataFolder);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{LevelDataFolder}/{safeName}.asset");

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
            var path = AssetDatabase.GetAssetPath(lv);
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
        private static void EndCard()    => GUILayout.EndVertical();

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (var i = 0; i < pix.Length; i++) pix[i] = col;
            var t = new Texture2D(w, h);
            t.SetPixels(pix);
            t.Apply();
            return t;
        }
        #endregion

        // ── Items Tab ────────────────────────────────────────────────────────
        #region Items Tab
        private void DrawItemsTab()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginVertical();

            GUILayout.Label("ITEM PREFAB GENERATOR", _headerStyle);
            GUILayout.Space(10);

            BeginCard();
            GUILayout.Label("Create New Item Prefab", _subHeaderStyle);
            GUILayout.Space(10);
            
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            _newItemName = (EItemName)EditorGUILayout.EnumPopup("Item Name Type", _newItemName);
            if (GUILayout.Button("New Type...", GUILayout.Width(100)))
            {
                _isAddingNewItemType = !_isAddingNewItemType;
                _newItemTypeName = "";
            }
            EditorGUILayout.EndHorizontal();

            if (_isAddingNewItemType)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                _newItemTypeName = EditorGUILayout.TextField(_newItemTypeName);
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    AddNewItemType(_newItemTypeName);
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            _autoGenerateIcon = EditorGUILayout.Toggle("Auto-Generate Icon", _autoGenerateIcon);

            if (_autoGenerateIcon)
            {
                GUILayout.Space(10);
                GUILayout.Label("Icon Camera Settings", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel++;
                _iconRotation = EditorGUILayout.Vector3Field("Model Rotation", _iconRotation);
                _iconPadding = EditorGUILayout.Slider("Zoom Padding", _iconPadding, 1f, 3f);
                _iconOffset = EditorGUILayout.Vector3Field("Camera Offset", _iconOffset);
                EditorGUI.indentLevel--;
                bool settingsChanged = EditorGUI.EndChangeCheck();
                
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                
                // Left side: Preview Button and help text
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                GUI.color = AccentBlue;
                if (GUILayout.Button("Manual Refresh", GUILayout.Height(30)))
                {
                    settingsChanged = true;
                }
                GUI.color = Color.white;
                
                if (_newItemModelPrefab == null)
                {
                    EditorGUILayout.HelpBox("Assign a 3D Model Prefab to preview.", MessageType.Info);
                }
                EditorGUILayout.EndVertical();

                // Right side: Preview Box
                GUILayout.Space(10);
                TryConsumeIconPreview();
                if (_previewIconTexture != null)
                {
                    Rect outerRect = GUILayoutUtility.GetRect(84, 84, GUILayout.ExpandWidth(false));
                    Rect texRect = new Rect(outerRect.x + 2, outerRect.y + 2, 80, 80);
                    
                    // Draw a subtle border
                    EditorGUI.DrawRect(outerRect, new Color(0.3f, 0.3f, 0.3f));
                    // Draw a darker background behind the icon
                    EditorGUI.DrawRect(texRect, new Color(0.15f, 0.15f, 0.15f));
                    
                    GUI.DrawTexture(texRect, _previewIconTexture, ScaleMode.ScaleToFit);
                }
                
                EditorGUILayout.EndHorizontal();

                if (settingsChanged && _newItemModelPrefab != null)
                    ScheduleIconPreview();
            }
            else
            {
                GUILayout.Space(5);
                _newItemIcon = (Sprite)EditorGUILayout.ObjectField("UI Icon (Sprite)", _newItemIcon, typeof(Sprite), false);
            }
            
            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            _newItemModelPrefab = (GameObject)EditorGUILayout.ObjectField("3D Model Prefab", _newItemModelPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && _newItemModelPrefab != null && _autoGenerateIcon)
                ScheduleIconPreview();

            if (EditorGUI.EndChangeCheck())
            {
                // Unsaved changes handler if needed
            }

            GUILayout.Space(15);

            GUI.color = AccentGreen;
            bool canGenerate = _newItemModelPrefab && (_autoGenerateIcon || _newItemIcon);
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button("🚀 Generate Item Prefab", GUILayout.Height(30)))
            {
                GenerateItemPrefab();
            }
            EditorGUI.EndDisabledGroup();
            GUI.color = Color.white;
            
            if (!canGenerate)
            {
                EditorGUILayout.HelpBox("Please assign a 3D Model Prefab (and an Icon if auto-generation is disabled).", MessageType.Warning);
            }

            EndCard();

            GUILayout.EndVertical();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
        }

        private void GenerateItemPrefab()
        {
            if (!_newItemModelPrefab || (!_autoGenerateIcon && !_newItemIcon)) return;

            string formattedName = "Item_" + _newItemName;

            if (!Directory.Exists(ItemPrefabFolder))
            {
                Directory.CreateDirectory(ItemPrefabFolder);
            }

            string path = $"{ItemPrefabFolder}/{formattedName}.prefab";

            if (File.Exists(path))
            {
                if (!EditorUtility.DisplayDialog("Overwrite?", $"A prefab named {formattedName} already exists. Overwrite?", "Yes", "Cancel"))
                    return;
            }

            // 0. Auto-generate icon if requested
            Sprite finalIcon = _newItemIcon;
            if (_autoGenerateIcon)
            {
                finalIcon = CaptureItemIcon(_newItemModelPrefab, formattedName);
            }

            // 1. Create root
            GameObject root = new GameObject(formattedName);
            
            // 2. Instantiate visual child
            GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(_newItemModelPrefab);
            visualInstance.transform.SetParent(root.transform);
            
            // Apply standard transforms
            // [FIXED Y-OFFSET: Changed from 0.04f to 0.00f]
            visualInstance.transform.localPosition = new Vector3(0.00f, 0.00f, -0.75f);
            visualInstance.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            visualInstance.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            
            visualInstance.name = "Renderer";

            // 3. Setup physics on visual child
            Collider col = visualInstance.GetComponentInChildren<Collider>();
            if (!col)
            {
                MeshFilter mf = visualInstance.GetComponentInChildren<MeshFilter>();
                if (mf)
                {
                    var meshCollider = mf.gameObject.AddComponent<MeshCollider>();
                    meshCollider.convex = true;
                    col = meshCollider;
                }
                else
                {
                    col = visualInstance.AddComponent<BoxCollider>();
                }
            }
            else if (col is MeshCollider mc)
            {
                // Must be convex to work properly with non-kinematic Rigidbody
                mc.convex = true; 
            }

            Renderer rend = visualInstance.GetComponentInChildren<Renderer>();
            if (!rend)
            {
                Debug.LogWarning("No Renderer found in the assigned 3D model.");
            }

            // 4. Setup root components
            Rigidbody rb = root.AddComponent<Rigidbody>();
            Item item = root.AddComponent<Item>();

            // Set Layer
            int matchLayer = LayerMask.NameToLayer("Match Stuff");
            if (matchLayer != -1)
            {
                SetLayerRecursively(root, matchLayer);
            }
            else
            {
                Debug.LogWarning("[Template Editor] Layer 'Match Stuff' not found! Skipping layer assignment.");
            }

            // 5. Wire up serialized fields on Item.cs
            var so = new SerializedObject(item);
            so.FindProperty("itemNameKey").intValue = (int)_newItemName;
            so.FindProperty("icon").objectReferenceValue = finalIcon;
            
            if (rend) 
                so.FindProperty("_renderer").objectReferenceValue = rend;
                
            so.ApplyModifiedProperties();

            // 6. Save prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);

            EditorGUIUtility.PingObject(savedPrefab);
            Debug.Log($"[Template Editor] Successfully generated Item Prefab at {path}");
            
            // Reload prefabs list
            LoadAll();
        }

        private void ScheduleIconPreview()
        {
            _previewDirty = true;
            _previewDirtyTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        // ponytail: throttle — render at most once per PreviewRenderThrottle seconds. While a slider is
        // being dragged each notch only refreshes the dirty time; the final value renders once the drag
        // pauses. The first-ever render (no texture yet) is immediate so assigning a model feels instant.
        private void TryConsumeIconPreview()
        {
            if (!_previewDirty || _newItemModelPrefab == null) return;
            if (_previewIconTexture == null || EditorApplication.timeSinceStartup - _previewDirtyTime >= PreviewRenderThrottle)
            {
                if (_previewIconTexture != null) DestroyImmediate(_previewIconTexture);
                _previewIconTexture = GetIconTexture2D(_newItemModelPrefab);
                _previewDirty = false;
            }
            else
            {
                Repaint(); // keep polling until the drag pauses past the throttle window
            }
        }

        private Texture2D GetIconTexture2D(GameObject modelPrefab)
        {
            var previewUtility = new PreviewRenderUtility();
            try
            {
                previewUtility.camera.orthographic = true;
                previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewUtility.camera.backgroundColor = new Color(0, 0, 0, 0);

                // Match the IconScene lighting
                previewUtility.lights[0].intensity = 2f;
                previewUtility.lights[0].color = Color.white;
                previewUtility.lights[0].transform.eulerAngles = new Vector3(53.516f, 349.741f, 49.274f);
                previewUtility.lights[1].intensity = 0f;

                previewUtility.BeginPreview(new Rect(0, 0, 512, 512), GUIStyle.none);

                var model = previewUtility.InstantiatePrefabInScene(modelPrefab);

                // Show the item and apply user-configured rotation
                model.transform.rotation = Quaternion.Euler(_iconRotation);
                model.transform.localScale = Vector3.one;
                model.transform.position = Vector3.zero;

                // Auto-framing: calculate the exact bounds of the model's renderers
                Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
                bool hasBounds = false;
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                if (hasBounds)
                {
                    // Center the camera on the object and pull it back. Apply user offset.
                    previewUtility.camera.transform.position = bounds.center - Vector3.forward * 5f + _iconOffset;
                    previewUtility.camera.transform.rotation = Quaternion.identity;

                    // Set orthographic size to precisely fit the model (with configured padding)
                    float maxExtent = Mathf.Max(bounds.extents.y, bounds.extents.x);
                    // Ensure size isn't 0
                    previewUtility.camera.orthographicSize = Mathf.Max(maxExtent * _iconPadding, 0.1f);
                }
                else
                {
                    // Fallback
                    previewUtility.camera.transform.position = new Vector3(0, 0, -5f) + _iconOffset;
                    previewUtility.camera.transform.rotation = Quaternion.identity;
                    previewUtility.camera.orthographicSize = 5f;
                }

                previewUtility.camera.Render();

                Texture rt = previewUtility.EndPreview();

                RenderTexture.active = (RenderTexture)rt;
                var tex2d = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex2d.Apply();
                return tex2d;
            }
            finally
            {
                // Always release the hidden preview scene/RT and clear the active target, even if the
                // render/read above throws — otherwise the PreviewRenderUtility and RenderTexture leak.
                RenderTexture.active = null;
                previewUtility.Cleanup();
            }
        }

        private Sprite CaptureItemIcon(GameObject modelPrefab, string formattedName)
        {
            var iconFolder = "Assets/Match Them All/Sprites/Icons";
            if (!Directory.Exists(iconFolder)) Directory.CreateDirectory(iconFolder);

            string path = $"{iconFolder}/icon_{formattedName.ToLowerInvariant()}.png";

            var tex2d = GetIconTexture2D(modelPrefab);
            byte[] bytes = tex2d.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            DestroyImmediate(tex2d);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite s) return s;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private void AddNewItemType(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            newName = newName.Replace(" ", ""); // Remove spaces
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(newName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                EditorUtility.DisplayDialog("Invalid Name", "The type name must be a valid C# identifier (no spaces, special characters).", "OK");
                return;
            }

            if (Enum.TryParse(typeof(EItemName), newName, out _))
            {
                EditorUtility.DisplayDialog("Already Exists", $"The type '{newName}' already exists in EItemName.", "OK");
                return;
            }

            const string enumPath = "Assets/Match Them All/Scripts/Enums/EItemName.cs";
            if (!File.Exists(enumPath))
            {
                EditorUtility.DisplayDialog("Error", "Could not find EItemName.cs!", "OK");
                return;
            }

            string fileContent = File.ReadAllText(enumPath);
            
            int enumStartIndex = fileContent.IndexOf("enum EItemName", StringComparison.Ordinal);
            if (enumStartIndex == -1) return;

            int firstBrace = fileContent.IndexOf("{", enumStartIndex, StringComparison.Ordinal);
            int closeBrace = fileContent.IndexOf("}", firstBrace, StringComparison.Ordinal);
            
            string enumBody = fileContent.Substring(firstBrace + 1, closeBrace - firstBrace - 1);
            
            int highestVal = Enum.GetValues(typeof(EItemName)).Cast<int>().Prepend(-1).Max();
            int nextVal = highestVal + 1;

            string newEnumBody = enumBody.TrimEnd();
            if (!newEnumBody.EndsWith(","))
            {
                newEnumBody += ",";
            }
            newEnumBody += $"\n        {newName} = {nextVal}\n    ";

            string newFileContent = fileContent[..(firstBrace + 1)] + newEnumBody + fileContent[closeBrace..];
            
            File.WriteAllText(enumPath, newFileContent);
            
            _isAddingNewItemType = false;
            AssetDatabase.Refresh();
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (!obj) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        #endregion

        // ── Settings Tab ─────────────────────────────────────────────────────
        #region Settings Tab
        private void DrawSettingsTab()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginVertical();

            GUILayout.Label("GLOBAL GAME SETTINGS", _headerStyle);
            GUILayout.Space(10);

            if (!_gameSettings)
            {
                EditorGUILayout.HelpBox("GameSettings.asset could not be loaded from Resources. Please ensure it exists.", MessageType.Error);
                if (GUILayout.Button("Create GameSettings Asset"))
                {
                    _gameSettings = CreateInstance<MatchThemAll.Scripts.Settings.GameSettingsSO>();
                    if (!Directory.Exists("Assets/Match Them All/Resources"))
                    {
                        Directory.CreateDirectory("Assets/Match Them All/Resources");
                    }
                    AssetDatabase.CreateAsset(_gameSettings, "Assets/Match Them All/Resources/GameSettings.asset");
                    AssetDatabase.SaveAssets();
                    _gameSettingsEditor = UnityEditor.Editor.CreateEditor(_gameSettings);
                }
            }
            else
            {
                if (!_gameSettingsEditor) 
                    _gameSettingsEditor = UnityEditor.Editor.CreateEditor(_gameSettings);

                _detailScroll = GUILayout.BeginScrollView(_detailScroll);
                BeginCard();
                _gameSettingsEditor.OnInspectorGUI();
                EndCard();
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
        }
        #endregion
    }
}
