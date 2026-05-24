using System;
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

        private const string LevelKey = "Level";
        private int _levelIndex;
        private Level _currentLevel;
        public System.Collections.Generic.List<Item> Items => _currentLevel.GetItems();

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

            int index = _levelIndex % levels.Length;

            // 1. Instantiate the generic Level prefab
            _currentLevel = Instantiate(levelPrefab, transform);

            // 2. Initialize with SO data (spawns items, sets duration)
            //    Must happen BEFORE levelSpawned fires so listeners read correct data.
            _currentLevel.Initialize(levels[index]);

            // 3. Notify listeners (TimerManager, GoalManager, etc.)
            LevelSpawned?.Invoke(_currentLevel);
        }

        private void LoadData() => _levelIndex = PlayerPrefs.GetInt(LevelKey, 0);

        private void SaveData() => PlayerPrefs.SetInt(LevelKey, _levelIndex);

        /// <summary>Resets saved level progress back to Level 1.</summary>
        [Button("Reset Level Progress")]
        public void ResetLevelProgress()
        {
            _levelIndex = 0;
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.Save();
            Debug.Log("Level progress reset to Level 1.");
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
                case EGameState.LEVELCOMPLETE:
                    _levelIndex++;
                    SaveData();
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