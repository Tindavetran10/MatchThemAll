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

        private Transform _targetToScale;
        private Image _backgroundImage;
        private float _originalAlpha;
        private Vector3 _originalScale;

        private void Awake()
        {
            // Find the child named "Card" to scale. If not found, we just won't scale anything.
            _targetToScale = transform.Find("Card");

            if (_targetToScale != null)
                _originalScale = _targetToScale.localScale; // Save the designer's custom scale

            _backgroundImage = GetComponent<Image>();
            if (_backgroundImage)
            {
                // Ensure we don't accidentally save an alpha of 0 if it was saved hidden
                _originalAlpha = _backgroundImage.color.a > 0.1f ? _backgroundImage.color.a : 0.78f;
            }
        }

        private void OnEnable()
        {
            if (_targetToScale != null)
            {
                // Pop up the card using its original scale
                _targetToScale.localScale = _originalScale * 0.5f;
                LeanTween.scale(_targetToScale.gameObject, _originalScale, animationDuration)
                         .setEase(easeType)
                         .setIgnoreTimeScale(true);
            }

            // Fade the background overlay
            if (_backgroundImage)
            {
                LeanTween.cancel(gameObject);
                Color c = _backgroundImage.color;
                c.a = 0f;
                _backgroundImage.color = c;

                LeanTween.value(gameObject, 0f, _originalAlpha, animationDuration)
                         .setEase(LeanTweenType.easeOutQuad)
                         .setIgnoreTimeScale(true)
                         .setOnUpdate((float a) => 
                         {
                             Color col = _backgroundImage.color;
                             col.a = a;
                             _backgroundImage.color = col;
                         });
            }
        }

        public void ClosePanel()
        {
            if (_targetToScale != null)
            {
                LeanTween.cancel(_targetToScale.gameObject);
                
                // Shrink the card
                LeanTween.scale(_targetToScale.gameObject, _originalScale * 0.5f, animationDuration)
                         .setEase(LeanTweenType.easeInBack)
                         .setIgnoreTimeScale(true)
                         .setOnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                // If there's no target, just set inactive after the background fades
                LeanTween.delayedCall(gameObject, animationDuration, () => gameObject.SetActive(false)).setIgnoreTimeScale(true);
            }

            // Fade out the background overlay
            if (_backgroundImage)
            {
                LeanTween.cancel(gameObject);
                LeanTween.value(gameObject, _backgroundImage.color.a, 0f, animationDuration)
                         .setEase(LeanTweenType.easeOutQuad)
                         .setIgnoreTimeScale(true)
                         .setOnUpdate((float a) => 
                         {
                             Color col = _backgroundImage.color;
                             col.a = a;
                             _backgroundImage.color = col;
                         });
            }
        }
    }
}
