using System;
using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Manages the Level Select scene. Reads save data to determine which levels
    /// are unlocked, then spawns a LevelButtonUI for each level.
    /// </summary>
    public class LevelSelectManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelButtonUI levelButtonPrefab;
        [SerializeField] private Transform     buttonContainer;

        private async void Start()
        {
            try
            {
                int currentProgress = SaveManager.GetCurrentLevelIndex();
            
                // Auto-detect total levels via Addressable
                int totalLevelsCount = 0;
                try 
                {
                    var handle = Addressables.LoadAssetsAsync<LevelDataSO>("LevelData");
                    var loadedLevels = await handle.Task;
                    totalLevelsCount = loadedLevels?.Count ?? 0;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load LevelData from Addressable: {e.Message}");
                }

                GenerateButtons(currentProgress, totalLevelsCount);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void GenerateButtons(int currentProgress, int totalLevelsCount)
        {
            for (int i = 0; i < totalLevelsCount; i++)
            {
                LevelButtonUI btn = Instantiate(levelButtonPrefab, buttonContainer);
                btn.Configure(
                    levelIndex:       i,
                    currentProgress:  currentProgress,
                    bestStars:        SaveManager.GetLevelStars(i)
                );
            }
        }

        /// <summary>Called by the Back button. Returns to the Main Menu.</summary>
        public void OnBackClicked() => SceneLoader.Load(SceneLoader.MainMenu);
    }
}
