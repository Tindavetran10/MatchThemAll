using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Handles the Main Menu scene buttons.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            // Apply saved settings when arriving at the main menu
            SettingsManager.ApplyFromSave();
        }

        /// <summary>Called by the Play button. Goes to Level Select.</summary>
        public void OnPlayClicked() => SceneLoader.Load(SceneLoader.LevelSelect);

        /// <summary>Called by the Settings button. Opens the settings overlay.</summary>
        public void OnSettingsClicked()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        /// <summary>Called by the Settings close button.</summary>
        public void OnSettingsCloseClicked()
        {
            if (!settingsPanel) return;

            var animator = settingsPanel.GetComponent<UIAnimator>();
            if (animator) animator.ClosePanel();
            else settingsPanel.SetActive(false);
        }
    }
}
