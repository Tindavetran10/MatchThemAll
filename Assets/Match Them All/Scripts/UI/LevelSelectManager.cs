using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;

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

        private void Start()
        {
            PlayerData data = SaveManager.Load();
            
            // Auto-detect total levels from the Resources folder (same as LevelManager)
            var loadedLevels = Resources.LoadAll<LevelDataSO>("Levels");
            int totalLevelsCount = loadedLevels != null ? loadedLevels.Length : 0;

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
