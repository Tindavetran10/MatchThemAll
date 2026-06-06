using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Attach this to any UI Panel to give it an automatic pop-in animation when it turns on.
    /// Uses LeanTween to scale the panel up smoothly.
    /// </summary>
    public class UIAnimator : MonoBehaviour
    {
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private LeanTweenType easeType = LeanTweenType.easeOutBack;

        private void OnEnable()
        {
            // Start at a smaller scale (e.g., 50%)
            transform.localScale = Vector3.one * 0.5f;

            // Animate to full scale. We use setIgnoreTimeScale(true) so it still animates
            // even if the game is paused (Time.timeScale = 0).
            LeanTween.scale(gameObject, Vector3.one, animationDuration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true);
        }

        /// <summary>
        /// Smoothly shrinks the panel and deactivates it upon completion.
        /// </summary>
        public void ClosePanel()
        {
            LeanTween.cancel(gameObject); // Cancel any opening tweens
            LeanTween.scale(gameObject, Vector3.one * 0.5f, animationDuration)
                     .setEase(LeanTweenType.easeInBack)
                     .setIgnoreTimeScale(true)
                     .setOnComplete(() => gameObject.SetActive(false));
        }
    }
}
