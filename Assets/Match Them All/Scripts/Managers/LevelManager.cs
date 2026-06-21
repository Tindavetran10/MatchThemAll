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

        public int TotalLevelDuration  => _currentLevel != null ? _currentLevel.Duration : 1;
        public int TotalLevelCount     => levels.Length;

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
                await _currentLevel.InitializeAsync(levels[index]);

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
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load LevelData from Addressable: {e.Message}");
            }
        }

        /// <summary>
        /// Saves progress and stars on level complete.
        /// Only advances currentLevelIndex if the player just beat their furthest level
        /// (not a replay of an old level).
        /// </summary>
        public void SaveLevelComplete(int starsEarned)
        {
            SaveManager.SaveLevelComplete(CurrentLevelIndex, _savedProgressIndex, starsEarned, out int newProgressIndex);
            _savedProgressIndex = newProgressIndex;
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
