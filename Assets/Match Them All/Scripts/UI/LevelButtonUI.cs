using MatchThemAll.Scripts.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Controls a single level button in the Level Select grid.
    /// Displays level number, best stars, and locked/unlocked state.
    /// </summary>
    public class LevelButtonUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button      button;
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private GameObject  lockIcon;
        [SerializeField] private GameObject[] starIcons; // array of 3 star GameObjects

        private int _levelIndex;
        private bool _isUnlocked;

        /// <summary>
        /// Initializes the button for a specific level.
        /// </summary>
        /// <param name="levelIndex">Zero-based level index.</param>
        /// <param name="currentProgress">The player's current unlocked level index.</param>
        /// <param name="bestStars">Best stars earned on this level (0-3).</param>
        public void Configure(int levelIndex, int currentProgress, int bestStars)
        {
            _levelIndex  = levelIndex;
            _isUnlocked  = levelIndex <= currentProgress;

            // Level number label (1-based for display)
            if (levelNumberText != null)
                levelNumberText.text = (levelIndex + 1).ToString();

            // Lock state
            if (lockIcon != null)
                lockIcon.SetActive(!_isUnlocked);

            if (button != null)
                button.interactable = _isUnlocked;

            // Stars
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                    starIcons[i].SetActive(i < bestStars);
            }
        }

        /// <summary>Called by the Button's OnClick event.</summary>
        public void OnClicked()
        {
            if (!_isUnlocked) return;
            SceneLoader.LoadLevel(_levelIndex);
        }
    }
}
