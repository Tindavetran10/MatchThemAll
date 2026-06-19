using System;
using System.Collections.Generic;
using MatchThemAll.Scripts.SaveSystem;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

namespace MatchThemAll.Scripts
{
    public class LevelManager : MonoBehaviour, IGameStateListener
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
        
        private Task _loadTask;
        public Task LoadTask => _loadTask;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else Destroy(gameObject);

            _loadTask = LoadDataAsync();
        }

        private async void SpawnLevel()
        {
            if (_loadTask != null) await _loadTask;

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
        }

        private async Task LoadDataAsync()
        {
            _savedProgressIndex = SaveManager.Load().currentLevelIndex;
            CurrentLevelIndex = _savedProgressIndex;
            
            try
            {
                var handle = Addressables.LoadAssetsAsync<LevelDataSO>("LevelData", null);
                var loadedLevels = await handle.Task;

                if (loadedLevels != null && loadedLevels.Count > 0)
                {
                    var list = new List<LevelDataSO>(loadedLevels);
                    list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                    levels = list.ToArray();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load LevelData from Addressables: {e.Message}");
            }
        }

        /// <summary>
        /// Saves progress and stars on level complete.
        /// Only advances currentLevelIndex if the player just beat their furthest level
        /// (not a replay of an old level).
        /// </summary>
        public void SaveLevelComplete(int starsEarned)
        {
            PlayerData data = SaveManager.Load();

            // Only advance progress if this was a new level (not a replay)
            bool isNewLevel = CurrentLevelIndex == _savedProgressIndex;
            if (isNewLevel)
            {
                _savedProgressIndex++;
                data.currentLevelIndex = _savedProgressIndex;
            }

            // Always save best star score for this level
            data.SetLevelStars(CurrentLevelIndex, starsEarned);
            SaveManager.Save(data);
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

        public void GameStateChangedCallback(EGameState gameState)
        {
            switch (gameState)
            {
                // Only spawn a fresh level when transitioning from a non-pause and non-continue state.
                // Resuming from PAUSED or OUTOFTIME should never re-spawn the current level.
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
                    throw new ArgumentOutOfRangeException(nameof(gameState), gameState, null);
            }
        }
    }
}
