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

        [Header("Settings")]
        [Tooltip("Total number of levels in the game. Must match LevelManager's levels array length.")]
        [SerializeField] private int totalLevels = 10;

        private void Start()
        {
            PlayerData data = SaveManager.Load();
            GenerateButtons(data);
        }

        private void GenerateButtons(PlayerData data)
        {
            for (int i = 0; i < totalLevels; i++)
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
        public void OnBackClicked()
        {
            SceneLoader.Load(SceneLoader.MainMenu);
        }
    }
}
