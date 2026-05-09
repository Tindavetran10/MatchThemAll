using System;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class LevelManager : MonoBehaviour
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

        private void Start()
        {
            SpawnLevel();
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
            levelIndex = PlayerPrefs.GetInt(levelKey);
        }

        private void SaveData()
        {
            PlayerPrefs.SetInt(levelKey, levelIndex);
        }
    }
}