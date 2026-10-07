using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Controls a single level button in the Level Select grid.
    /// Displays level number, best stars, and locked/unlocked state.
    /// Optionally swaps the button art for completed / current / locked levels (saga-map style).
    /// </summary>
    public class LevelButtonUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button      button;
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private GameObject  lockIcon;
        [SerializeField] private GameObject[] starIcons; // array of 3 star GameObjects

        [Header("State Art (optional)")]
        [Tooltip("Image whose sprite is swapped per state. Leave empty to keep a single look.")]
        [SerializeField] private Image  stateImage;
        [SerializeField] private Sprite completedSprite;
        [SerializeField] private Sprite currentSprite;
        [SerializeField] private Sprite lockedSprite;
        [Tooltip("Shown only on the furthest unlocked level (e.g. a glow / shine).")]
        [SerializeField] private GameObject currentHighlight;
        [SerializeField] private Color unlockedTextColor = Color.white;
        [SerializeField] private Color lockedTextColor   = new Color(0.55f, 0.6f, 0.7f, 1f);

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
            bool isCurrent = levelIndex == currentProgress;

            // Level number label (1-based for display)
            if (levelNumberText)
            {
                levelNumberText.text = (levelIndex + 1).ToString();
                levelNumberText.color = _isUnlocked ? unlockedTextColor : lockedTextColor;
            }

            // Lock state
            if (lockIcon)
                lockIcon.SetActive(!_isUnlocked);

            if (button)
                button.interactable = _isUnlocked;

            // State art
            if (stateImage)
            {
                Sprite s = !_isUnlocked ? lockedSprite : isCurrent ? currentSprite : completedSprite;
                if (s) stateImage.sprite = s;
            }

            if (currentHighlight)
                currentHighlight.SetActive(isCurrent);

            // Stars
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i])
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
