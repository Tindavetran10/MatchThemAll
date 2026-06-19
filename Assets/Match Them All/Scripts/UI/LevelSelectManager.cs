using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

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
            PlayerData data = SaveManager.Load();
            
            // Auto-detect total levels via Addressables
            int totalLevelsCount = 0;
            try 
            {
                var handle = Addressables.LoadAssetsAsync<LevelDataSO>("LevelData", null);
                var loadedLevels = await handle.Task;
                totalLevelsCount = loadedLevels?.Count ?? 0;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load LevelData from Addressables: {e.Message}");
            }

            GenerateButtons(data, totalLevelsCount);
        }

        private void GenerateButtons(PlayerData data, int totalLevelsCount)
        {
            for (int i = 0; i < totalLevelsCount; i++)
            {
                LevelButtonUI btn = Instantiate(levelButtonPrefab, buttonContainer);
                btn.Configure(
                    levelIndex:       i,
                    currentProgress:  data.currentLevelIndex,
                    bestStars:        data.GetLevelStars(i)
                );
            }
        }

        /// <summary>Called by the Back button. Returns to the Main Menu.</summary>
        public void OnBackClicked() => SceneLoader.Load(SceneLoader.MainMenu);
    }
}
