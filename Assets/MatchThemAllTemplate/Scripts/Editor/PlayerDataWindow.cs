#if UNITY_EDITOR
using System.Collections.Generic;
using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Developer tool: view and set coins, gems, power-up charges and the furthest level (which drives power-up locks) in the save file.
    /// Works in and out of Play mode; every change goes through SaveManager, then Flush().
    /// Open via: Match Them All → Player Data
    /// </summary>
    public class PlayerDataWindow : EditorWindow
    {
        private int _coins, _gems;
        private readonly Dictionary<string, int> _charges = new();
        private PowerupDatabaseSO _db;
        private readonly List<string> _levelIds = new();
        private string[] _levelLabels = System.Array.Empty<string>();
        private int _furthest, _loadedFurthest;

        [MenuItem("Match Them All/Player Data")]
        private static void Open() => GetWindow<PlayerDataWindow>("Player Data");

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Reload();
        }

        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private void OnFocus() => Reload();

        private void OnPlayModeChanged(PlayModeStateChange _) => Reload();

        private void Reload()
        {
            // Out of Play mode the file is the truth; in Play mode the live cache is (it may hold unflushed progress).
            if (!Application.isPlaying) SaveManager.ForceReload();
            _db = Resources.Load<PowerupDatabaseSO>("Powerups/PowerupDatabase");
            _coins = SaveManager.GetCoins();
            _gems = SaveManager.GetGems();
            _charges.Clear();
            if (_db)
                foreach (var so in _db.Ordered)
                    if (so) _charges[so.id] = SaveManager.GetPowerupCount(so.id);

            // Same order as LevelManager: LevelDataSO assets sorted by name.
            var levels = new List<LevelDataSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:LevelDataSO"))
                levels.Add(AssetDatabase.LoadAssetAtPath<LevelDataSO>(AssetDatabase.GUIDToAssetPath(guid)));
            levels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            _levelIds.Clear();
            _levelLabels = new string[levels.Count];
            for (int i = 0; i < levels.Count; i++)
            {
                _levelIds.Add(levels[i].Id);
                _levelLabels[i] = $"Level {i + 1}  ({levels[i].name})";
            }
            _furthest = _loadedFurthest = Mathf.Max(0, _levelIds.IndexOf(SaveManager.GetFurthestLevelId()));
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(Application.isPlaying
                ? "Play mode: changes apply to the running game and are saved."
                : "Edit mode: changes are written straight to the save file.", MessageType.None);

            EditorGUILayout.LabelField("Currencies", EditorStyles.boldLabel);
            _coins = Mathf.Max(0, EditorGUILayout.IntField("Coins", _coins));
            _gems = Mathf.Max(0, EditorGUILayout.IntField("Gems", _gems));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Power-up Charges", EditorStyles.boldLabel);
            if (!_db) EditorGUILayout.HelpBox("Resources/Powerups/PowerupDatabase not found.", MessageType.Warning);
            else
                foreach (var so in _db.Ordered)
                    if (so) _charges[so.id] = Mathf.Max(0, EditorGUILayout.IntField(
                        string.IsNullOrEmpty(so.DisplayName) ? so.id : so.DisplayName, _charges[so.id]));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Progress", EditorStyles.boldLabel);
            _furthest = EditorGUILayout.Popup(new GUIContent("Furthest level",
                "Highest unlocked level. Power-ups unlock from it, so Level 1 locks them all again. Stars of this level and later ones are cleared."), _furthest, _levelLabels);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload")) Reload();
                if (GUILayout.Button("Apply"))
                {
                    // Only when changed, so editing coins never touches progress. Level 1 = empty, like a fresh save.
                    if (_furthest != _loadedFurthest) SaveManager.SetFurthestLevel(_furthest == 0 ? "" : _levelIds[_furthest], _levelIds);
                    Apply(_coins, _gems, _charges);
                    Reload();
                }
            }
        }

        /// <summary>Sets exact values through SaveManager and writes the save file. Callable from editor scripts.</summary>
        public static void Apply(int coins, int gems, IReadOnlyDictionary<string, int> charges)
        {
            // ponytail: SaveManager has no setters; a delta through Add* sets the exact value and fires the change events.
            SaveManager.AddCoins(coins - SaveManager.GetCoins());
            SaveManager.AddGems(gems - SaveManager.GetGems());
            foreach (var (id, target) in charges)
                SaveManager.AddPowerupCharge(id, target - SaveManager.GetPowerupCount(id));
            SaveManager.Flush();
        }
    }
}
#endif
