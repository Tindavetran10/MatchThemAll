using System;
using System.Collections.Generic;
using MatchThemAll.Scripts.SaveSystem;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

namespace MatchThemAll.Scripts
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance;
        
        [Header("Data")]
        [Tooltip("Array of LevelDataSO assets — one per level. Order determines play sequence.")]
        [SerializeField] private LevelDataSO[] levels;

        [Header("Prefab")]
        [Tooltip("A single generic Level prefab (no baked items). Reused for every level.")]
        [SerializeField] private Level levelPrefab;

        private int _savedProgressIndex;  // the player's real progress (never overwritten by replay)
        private Level _currentLevel;

        public List<Item> Items        => _currentLevel.GetItems();
        public Transform ItemParent    => _currentLevel.ItemParent;
        public int CurrentLevelIndex { get; private set; }
        public LevelDataSO CurrentLevelData => levels != null && levels.Length > 0 ? levels[CurrentLevelIndex % levels.Length] : null;

        public int TotalLevelDuration  => _currentLevel != null ? _currentLevel.Duration : 1;
        public int TotalLevelCount     => levels != null ? levels.Length : 0;

        /// <summary>Ordered list of level ids (LevelDataSO.Id), matching the play sequence. Source of truth for keyed save data.
        /// Empty until LoadDataAsync completes. Safe to call at any time (never throws).</summary>
        public System.Collections.Generic.IReadOnlyList<string> OrderedLevelIds
        {
            get
            {
                // Cached & valid only after LoadDataAsync finishes. Before that, `levels` may be the
                // serialized Inspector array (possibly with null/missing entries), so return empty.
                if (_idsReady && _orderedIds != null) return _orderedIds;
                return System.Array.Empty<string>();
            }
        }
        private System.Collections.Generic.List<string> _orderedIds;
        private bool _idsReady;

        [Header("Actions")]
        public static Action<Level> LevelSpawned;

        public Task LoadTask { get; private set; }

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            LoadTask = LoadDataAsync();
        }

        private void OnDestroy() => EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

        public bool IsLevelReady { get; private set; }

        private bool _isSpawning;

        private async void SpawnLevel()
        {
            if (_isSpawning) return;
            _isSpawning = true;

            try
            {
                IsLevelReady = false;
            
                if (LoadTask != null) await LoadTask;

                int requested = SceneLoader.RequestedLevelIndex;
                CurrentLevelIndex = requested >= 0 && requested < levels.Length
                    ? requested
                    : _savedProgressIndex;

                int index = CurrentLevelIndex % levels.Length;

                // 1. Instantiate the generic Level prefab only if it doesn't exist yet
                if (_currentLevel == null)
                {
                    transform.Clear();
                    _currentLevel = Instantiate(levelPrefab, transform);
                }

                // 2. Initialize with SO data (spawns items, sets duration)
                //    This will automatically release old items back to the ItemPoolManager.
#if UNITY_EDITOR
                LevelDataSO overrideLevel = null;
                if (UnityEditor.EditorPrefs.HasKey("EditorTestLevelPath"))
                {
                    string path = UnityEditor.EditorPrefs.GetString("EditorTestLevelPath");
                    UnityEditor.EditorPrefs.DeleteKey("EditorTestLevelPath"); // Consume it
                    if (!string.IsNullOrEmpty(path))
                    {
                        overrideLevel = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
                    }
                }

                if (overrideLevel != null)
                {
                    await _currentLevel.InitializeAsync(overrideLevel);
                }
                else
                {
                    await _currentLevel.InitializeAsync(levels[index]);
                }
#else
                await _currentLevel.InitializeAsync(levels[index]);
#endif

                // 3. Notify listeners (TimerManager, GoalManager, etc.)
                LevelSpawned?.Invoke(_currentLevel);
            
                IsLevelReady = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isSpawning = false;
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var handle = Addressables.LoadAssetsAsync<LevelDataSO>("LevelData");
                var loadedLevels = await handle.Task;

                if (loadedLevels is { Count: > 0 })
                {
                    var list = new List<LevelDataSO>(loadedLevels);
                    list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                    levels = list.ToArray();

                    // Build the id cache from the fully-loaded list and mark it ready.
                    _orderedIds = new List<string>(levels.Length);
                    for (int i = 0; i < levels.Length; i++) _orderedIds.Add(levels[i].Id);
                    _idsReady = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load LevelData from Addressable: {e.Message}");
            }

            // Read saved progress AFTER Addressables load so OrderedLevelIds is populated.
            // GetCurrentLevelIndex() looks up furthestLevelId in OrderedLevelIds; if called
            // before _idsReady it always returns 0, causing the player to re-play Level 1.
            _savedProgressIndex = SaveManager.GetCurrentLevelIndex();
            CurrentLevelIndex = _savedProgressIndex;

            if (levels == null || levels.Length == 0)
                Debug.LogError("LevelManager: No LevelDataSO assets loaded. Mark the LevelData .asset files Addressable in the Default Local Group and tag them with the 'LevelData' label.");
        }


        public void SaveLevelComplete(int starsEarned)
        {
            if (levels == null || levels.Length == 0) return;
            int idx = CurrentLevelIndex % levels.Length;
            string id = levels[idx].Id;

            // OrderedLevelIds is only populated after LoadDataAsync (Addressables) finishes.
            // If it's still empty (rare async race), fall back to the local levels array so
            // SetLevelComplete always receives a valid ordered list and can advance furthestLevelId.
            IReadOnlyList<string> ids = OrderedLevelIds;
            if (ids == null || ids.Count == 0)
            {
                var fallback = new System.Collections.Generic.List<string>(levels.Length);
                for (int i = 0; i < levels.Length; i++) fallback.Add(levels[i].Id);
                ids = fallback;
            }

            string furthest = SaveManager.GetFurthestLevelId();
            int furthestIdx = IndexOfId(ids, furthest);
            bool isFurthest = string.IsNullOrEmpty(furthest) ? idx == 0 : (furthestIdx >= 0 && idx == furthestIdx);

            SaveManager.SetLevelComplete(id, starsEarned, isFurthest, ids);

            // Advance _savedProgressIndex to the NEXT level so SpawnLevel picks the correct one
            // if it reads this value before the next LoadDataAsync completes.
            _savedProgressIndex = Mathf.Min(idx + 1, levels.Length - 1);
            CurrentLevelIndex = _savedProgressIndex;
        }

        private static int IndexOfId(System.Collections.Generic.IReadOnlyList<string> ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == id) return i;
            return -1;
        }

        /// <summary>Wipes all save data and resets to Level 1.</summary>
        [Button("Reset Level Progress")]
        public void ResetLevelProgress()
        {
            SaveManager.Wipe();
            _savedProgressIndex = CurrentLevelIndex = 0;
            Debug.Log("[LevelManager] Save wiped; progress back to level 1.");
        }

        /// <summary>States that return to GAME with the current level still on the board.</summary>
        private static bool IsResumeFrom(EGameState previous) =>
            previous is EGameState.PAUSED or EGameState.OUTOFTIME or EGameState.POWERUPOFFER;

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            var state = evt.NewState;
            if (state == EGameState.GAME)
            {
                if (!IsResumeFrom(GameManager.Instance.PreviousState)) SpawnLevel();
                return;
            }

            // Every other known state leaves the level alone (stars are saved by WinPanelManager).
            // An unknown state is a bug: adding an EGameState must be a conscious decision here.
            if (state is EGameState.LEVELCOMPLETE or EGameState.MENU or EGameState.GAMEOVER
                or EGameState.PAUSED or EGameState.OUTOFTIME or EGameState.POWERUPOFFER) return;
            throw new ArgumentOutOfRangeException(nameof(evt.NewState), state, null);
        }
    }
}
