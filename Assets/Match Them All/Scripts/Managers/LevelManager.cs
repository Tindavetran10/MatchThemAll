using System;
using NaughtyAttributes;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class LevelManager : MonoBehaviour, IGameStateListener
    {
        [Header("Data")]
        [Tooltip("Array of LevelDataSO assets — one per level. Order determines play sequence.")]
        [SerializeField] private LevelDataSO[] levels;

        [Header("Prefab")]
        [Tooltip("A single generic Level prefab (no baked items). Reused for every level.")]
        [SerializeField] private Level levelPrefab;

        private const string levelKey = "Level";
        private int levelIndex;
        private Level currentLevel;

        [Header("Actions")]
        public static Action<Level> levelSpawned;

        private void Awake() => LoadData();

        private void SpawnLevel()
        {
            transform.Clear();

            int index = levelIndex % levels.Length;

            // 1. Instantiate the generic Level prefab
            currentLevel = Instantiate(levelPrefab, transform);

            // 2. Initialize with SO data (spawns items, sets duration)
            //    Must happen BEFORE levelSpawned fires so listeners read correct data.
            currentLevel.Initialize(levels[index]);

            // 3. Notify listeners (TimerManager, GoalManager, etc.)
            levelSpawned?.Invoke(currentLevel);
        }

        private void LoadData() => levelIndex = PlayerPrefs.GetInt(levelKey, 0);

        private void SaveData() => PlayerPrefs.SetInt(levelKey, levelIndex);

        /// <summary>Resets saved level progress back to Level 1.</summary>
        [Button("Reset Level Progress")]
        public void ResetLevelProgress()
        {
            levelIndex = 0;
            PlayerPrefs.DeleteKey(levelKey);
            PlayerPrefs.Save();
            Debug.Log("Level progress reset to Level 1.");
        }

        public void GameStateChangedCallback(EGameState gameState)
        {
            if (gameState == EGameState.GAME)
                SpawnLevel();
            else if (gameState == EGameState.LEVELCOMPLETE)
            {
                levelIndex++;
                SaveData();
            }
        }
    }
}