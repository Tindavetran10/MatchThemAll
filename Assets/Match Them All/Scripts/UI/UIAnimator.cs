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
        [SerializeField] private Transform targetToScale; // Optional: specify exactly what to scale

        private Transform _targetToScale;
        private Image _backgroundImage;
        private CanvasGroup _canvasGroup;
        private float _originalAlpha;
        private float _originalCanvasAlpha = 1f;
        private Vector3 _originalScale;

        private void Awake()
        {
            // Use assigned target, or find child named "Card", or null.
            _targetToScale = targetToScale ? targetToScale : transform.Find("Card");

            if (_targetToScale)
                _originalScale = _targetToScale.localScale; // Save the designer's custom scale

            _backgroundImage = GetComponent<Image>();
            if (_backgroundImage)
            {
                // Ensure we don't accidentally save an alpha of 0 if it was saved hidden
                _originalAlpha = _backgroundImage.color.a > 0.1f ? _backgroundImage.color.a : 0.78f;
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup)
            {
                _originalCanvasAlpha = _canvasGroup.alpha > 0.1f ? _canvasGroup.alpha : 1f;
            }
        }

        private void OnEnable()
        {
            if (_targetToScale)
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

            // Fade the CanvasGroup
            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                LeanTween.alphaCanvas(_canvasGroup, _originalCanvasAlpha, animationDuration)
                         .setEase(LeanTweenType.easeOutQuad)
                         .setIgnoreTimeScale(true);
            }
        }

        public void ClosePanel()
        {
            bool hasAnimation = false;

            if (_targetToScale)
            {
                hasAnimation = true;
                LeanTween.cancel(_targetToScale.gameObject);
                
                // Shrink the card
                LeanTween.scale(_targetToScale.gameObject, _originalScale * 0.5f, animationDuration)
                         .setEase(LeanTweenType.easeInBack)
                         .setIgnoreTimeScale(true)
                         .setOnComplete(() => gameObject.SetActive(false));
            }

            // Fade out the background overlay
            if (_backgroundImage)
            {
                hasAnimation = true;
                LeanTween.cancel(gameObject);
                LeanTween.value(gameObject, _backgroundImage.color.a, 0f, animationDuration)
                         .setEase(LeanTweenType.easeOutQuad)
                         .setIgnoreTimeScale(true)
                         .setOnUpdate((float a) => 
                         {
                             Color col = _backgroundImage.color;
                             col.a = a;
                             _backgroundImage.color = col;
                         })
                         .setOnComplete(() => { if (!_targetToScale) gameObject.SetActive(false); });
            }

            // Fade out CanvasGroup
            if (_canvasGroup)
            {
                hasAnimation = true;
                LeanTween.alphaCanvas(_canvasGroup, 0f, animationDuration)
                         .setEase(LeanTweenType.easeOutQuad)
                         .setIgnoreTimeScale(true)
                         .setOnComplete(() => { if (!_targetToScale && !_backgroundImage) gameObject.SetActive(false); });
            }

            if (!hasAnimation)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
