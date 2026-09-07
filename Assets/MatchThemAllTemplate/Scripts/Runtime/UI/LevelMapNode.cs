using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// One node on the scrolling level saga map. Composes the existing <see cref="LevelButtonUI"/>
    /// (number / lock / stars / click) and adds a theme background behind it.
    ///
    /// Configure takes the live ordered index (for LevelButtonUI's display + SceneLoader.LoadLevel)
    /// plus the level SO for theme art. Lock/stars are computed by the manager from keyed save state.
    /// </summary>
    public class LevelMapNode : MonoBehaviour
    {
        [SerializeField] private Image themeBackground;
        [SerializeField] private LevelButtonUI button;

        /// <summary>The composed button (number/lock/stars/click). Manager reads it for positioning + sizing.</summary>
        public LevelButtonUI Button => button;

        /// <summary>
        /// Configures the node.
        /// </summary>
        /// <param name="level">The level SO (for theme art).</param>
        /// <param name="orderedIndex">This level's position in the current ordered list (1-based display, index for LoadLevel).</param>
        /// <param name="currentProgressIndex">Furthest unlocked index (for LevelButtonUI's lock rule).</param>
        /// <param name="bestStars">Best stars earned (0-3).</param>
        public void Configure(LevelDataSO level, int orderedIndex, int currentProgressIndex, int bestStars)
        {
            if (themeBackground)
            {
                if (level && level.ThemeBackground)
                {
                    themeBackground.sprite = level.ThemeBackground;
                    themeBackground.enabled = true;
                }
                else
                {
                    themeBackground.enabled = false;
                }
            }

            if (button)
                button.Configure(levelIndex: orderedIndex, currentProgress: currentProgressIndex, bestStars: bestStars);
        }
    }
}
