using System;
using NaughtyAttributes;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class LevelManager : MonoBehaviour, IGameStateListener
    {
        [Header(" Data ")]
        [SerializeField] private Level[] levels;
        private const string levelKey = "Level";

        private int levelIndex;

        [Header(" Settings")]
        private Level currentLevel;

        [Header(" Actions ")]
        public static Action<Level> levelSpawned;

        private void Awake()
        {
            LoadData();
        }

        private void SpawnLevel()
        {
            transform.Clear();

            int validateLevelIndex = levelIndex % levels.Length;
            currentLevel = Instantiate(levels[validateLevelIndex], transform);

            levelSpawned?.Invoke(currentLevel);
        }

        private void LoadData()
        {
            levelIndex = PlayerPrefs.GetInt(levelKey, 0);
        }

        private void SaveData()
        {
            PlayerPrefs.SetInt(levelKey, levelIndex);
        }

        /// <summary>
        /// Resets the saved level index back to 0 (Level 1).
        /// Call this from the Inspector button or from code when needed.
        /// </summary>
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
            {
                SpawnLevel();
            }
            else if (gameState == EGameState.LEVELCOMPLETE)
            {
                levelIndex++;
                SaveData();
            }
        }
    }
}