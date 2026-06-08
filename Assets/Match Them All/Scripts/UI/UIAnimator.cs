using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Attach this to a UI Panel. It expects the root to have the background Image (which it fades)
    /// and a child named "Card" (which it scales up).
    /// </summary>
    public class UIAnimator : MonoBehaviour
    {
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private LeanTweenType easeType = LeanTweenType.easeOutBack;

        private Transform targetToScale;
        private Image backgroundImage;
        private float originalAlpha;
        private Vector3 originalScale;

        private void Awake()
        {
            // Find the child named "Card" to scale. If not found, fallback to scaling everything.
            targetToScale = transform.Find("Card");
            if (targetToScale == null) targetToScale = transform;

            originalScale = targetToScale.localScale; // Save the designer's custom scale

            backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                // Ensure we don't accidentally save an alpha of 0 if it was saved hidden
                originalAlpha = backgroundImage.color.a > 0.1f ? backgroundImage.color.a : 0.78f;
            }
        }

        private void OnEnable()
        {
            LeanTween.cancel(targetToScale.gameObject);
            
            // Pop up the card using its original scale
            targetToScale.localScale = originalScale * 0.5f;
            LeanTween.scale(targetToScale.gameObject, originalScale, animationDuration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true);

            // Fade the background overlay
            if (backgroundImage != null)
            {
                LeanTween.cancel(gameObject);
                Color c = backgroundImage.color;
                c.a = 0f;
                backgroundImage.color = c;

                LeanTween.value(gameObject, 0f, originalAlpha, animationDuration)
                         .setEase(LeanTweenType.easeOutQuad)
                         .setIgnoreTimeScale(true)
                         .setOnUpdate((float a) => 
                         {
                             Color col = backgroundImage.color;
                             col.a = a;
                             backgroundImage.color = col;
                         });
            }
        }

        public void ClosePanel()
        {
            LeanTween.cancel(targetToScale.gameObject);
            
            // Shrink the card
            LeanTween.scale(targetToScale.gameObject, originalScale * 0.5f, animationDuration)
                     .setEase(LeanTweenType.easeInBack)
                     .setIgnoreTimeScale(true)
                     .setOnComplete(() => gameObject.SetActive(false));

            // Fade out the background overlay
            if (backgroundImage != null)
            {
                LeanTween.cancel(gameObject);
                LeanTween.value(gameObject, backgroundImage.color.a, 0f, animationDuration)
                         .setEase(LeanTweenType.easeOutQuad)
                         .setIgnoreTimeScale(true)
                         .setOnUpdate((float a) => 
                         {
                             Color col = backgroundImage.color;
                             col.a = a;
                             backgroundImage.color = col;
                         });
            }
        }
    }
}
