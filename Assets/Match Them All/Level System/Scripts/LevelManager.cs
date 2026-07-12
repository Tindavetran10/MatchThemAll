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
            if (Instance == null)
                Instance = this;
            else Destroy(gameObject);

            LoadTask = LoadDataAsync();
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
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
            _savedProgressIndex = SaveManager.GetCurrentLevelIndex();
            CurrentLevelIndex = _savedProgressIndex;
            
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

                    // ponytail: migration runs here, not in SaveManager.Initialize, because SaveManager
                    // loads before the level list exists. This is the first moment orderedIds are known.
                    SaveManager.MigrateLevelsToKeyed(OrderedLevelIds);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load LevelData from Addressable: {e.Message}");
            }

            if (levels == null || levels.Length == 0)
                Debug.LogError("LevelManager: No LevelDataSO assets loaded. Mark the LevelData .asset files Addressable in the Default Local Group and tag them with the 'LevelData' label.");
        }

        /// <summary>
        /// Saves progress and stars on level complete (keyed by level id).
        /// Advances the furthest-unlocked frontier if this was the furthest level.
        /// </summary>
        public void SaveLevelComplete(int starsEarned)
        {
            if (levels == null || levels.Length == 0) return;
            int idx = CurrentLevelIndex % levels.Length;
            string id = levels[idx].Id;
            var ids = OrderedLevelIds;
            string furthest = SaveManager.GetFurthestLevelId();
            int furthestIdx = IndexOfId(ids, furthest);
            bool isFurthest = string.IsNullOrEmpty(furthest) ? idx == 0 : (furthestIdx >= 0 && idx == furthestIdx);

            SaveManager.SetLevelComplete(id, starsEarned, isFurthest, ids);

            // Keep the legacy index in sync for any code still reading it during the transition.
            _savedProgressIndex = Mathf.Max(0, idx);
            CurrentLevelIndex = idx;
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
            _savedProgressIndex = 0;
            CurrentLevelIndex = 0;
            SaveManager.Wipe();
            Debug.Log("All save data wiped. Starting from Level 1.");
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                // Only spawn a fresh level when transitioning from a non-pause and non-continue state.
                // Resuming from PAUSED or TIMEOUT should never re-spawn the current level.
                case EGameState.GAME when GameManager.Instance.PreviousState != EGameState.PAUSED && GameManager.Instance.PreviousState != EGameState.OUTOFTIME:
                    SpawnLevel();
                    break;
                case EGameState.GAME: // resuming from pause/out of time — do nothing, level already exists
                    break;
                case EGameState.LEVELCOMPLETE:
                    // Stars are saved by WinPanelManager after calculating time remaining
                    break;
                case EGameState.MENU:
                case EGameState.GAMEOVER:
                case EGameState.PAUSED:
                case EGameState.OUTOFTIME:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(evt.NewState), evt.NewState, null);
            }
        }
    }
}
