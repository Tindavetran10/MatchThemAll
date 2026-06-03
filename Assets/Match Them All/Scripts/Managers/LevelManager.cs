using System;
using System.Collections.Generic;
using MatchThemAll.Scripts.SaveSystem;
using NaughtyAttributes;
using UnityEngine;

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
        private int _levelIndex;          // the actual level being played this session (may differ during replay)
        private Level _currentLevel;

        public List<Item> Items        => _currentLevel.GetItems();
        public Transform ItemParent    => _currentLevel.ItemParent;
        public int CurrentLevelIndex   => _levelIndex;
        public int TotalLevelDuration  => _currentLevel != null ? _currentLevel.Duration : 1;
        public int TotalLevelCount     => levels.Length;

        [Header("Actions")]
        public static Action<Level> LevelSpawned;
        
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else Destroy(gameObject);

            LoadData();
        }

        private void SpawnLevel()
        {
            transform.Clear();

            // Use the level requested by SceneLoader (replay) if set, otherwise use saved progress
            int requested = SceneLoader.RequestedLevelIndex;
            _levelIndex = (requested >= 0 && requested < levels.Length)
                ? requested
                : _savedProgressIndex;

            int index = _levelIndex % levels.Length;

            // 1. Instantiate the generic Level prefab
            _currentLevel = Instantiate(levelPrefab, transform);

            // 2. Initialize with SO data (spawns items, sets duration)
            //    Must happen BEFORE levelSpawned fires so listeners read correct data.
            _currentLevel.Initialize(levels[index]);

            // 3. Notify listeners (TimerManager, GoalManager, etc.)
            LevelSpawned?.Invoke(_currentLevel);
        }

        private void LoadData()
        {
            _savedProgressIndex = SaveManager.Load().currentLevelIndex;
            _levelIndex = _savedProgressIndex;
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
            bool isNewLevel = _levelIndex == _savedProgressIndex;
            if (isNewLevel)
            {
                _savedProgressIndex++;
                data.currentLevelIndex = _savedProgressIndex;
            }

            // Always save best star score for this level
            data.SetLevelStars(_levelIndex, starsEarned);
            SaveManager.Save(data);
        }

        /// <summary>Wipes all save data and resets to Level 1.</summary>
        [Button("Reset Level Progress")]
        public void ResetLevelProgress()
        {
            _savedProgressIndex = 0;
            _levelIndex = 0;
            SaveManager.Wipe();
            Debug.Log("All save data wiped. Starting from Level 1.");
        }

        public void GameStateChangedCallback(EGameState gameState)
        {
            switch (gameState)
            {
                // Only spawn a fresh level when transitioning from a non-pause state.
                // Resuming from PAUSED should never re-spawn the current level.
                case EGameState.GAME when GameManager.Instance.PreviousState != EGameState.PAUSED:
                    SpawnLevel();
                    break;
                case EGameState.GAME: // resuming from pause — do nothing, level already exists
                    break;
                case EGameState.LEVELCOMPLETE:
                    // Stars are saved by WinPanelManager after calculating time remaining
                    break;
                case EGameState.MENU:
                case EGameState.GAMEOVER:
                case EGameState.PAUSED:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gameState), gameState, null);
            }
        }
    }
}
