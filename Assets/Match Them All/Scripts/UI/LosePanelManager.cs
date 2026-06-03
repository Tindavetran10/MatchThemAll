using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Controls the Game Over / Lose panel buttons.
    /// </summary>
    public class LosePanelManager : MonoBehaviour
    {
        /// <summary>Called by the Retry button. Replays the same level.</summary>
        public void OnRetryClicked()
        {
            // Replay the exact same level that just ended
            int currentLevel = LevelManager.Instance != null
                ? LevelManager.Instance.CurrentLevelIndex
                : -1;
            SceneLoader.LoadLevel(currentLevel);
        }

        /// <summary>Called by the Level Select button.</summary>
        public void OnLevelSelectClicked()
        {
            SceneLoader.Load(SceneLoader.LevelSelect);
        }
    }
}
