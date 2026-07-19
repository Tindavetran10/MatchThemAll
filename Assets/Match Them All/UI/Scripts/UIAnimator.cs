using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Attach this to a UI Panel. It expects the root to have the background Image (which it fades)
    /// and a child named "Card" (which it scales up).
    /// </summary>
    public class UIAnimator : MonoBehaviour
    {
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease easeType = Ease.OutBack;
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
                Tween.Scale(_targetToScale.gameObject.transform,
                    _originalScale,
                    animationDuration,
                    easeType,
                    useUnscaledTime: true);
            }

            // Fade the background overlay
            if (_backgroundImage)
            {
                Tween.StopAll(gameObject.transform);
                Color c = _backgroundImage.color;
                c.a = 0f;
                _backgroundImage.color = c;

                Tween.Custom(0f, _originalAlpha, animationDuration, onValueChange: a => 
                {
                    Color col = _backgroundImage.color;
                    col.a = a;
                    _backgroundImage.color = col;
                }, ease: Ease.OutQuad, useUnscaledTime: true);
            }

            // Fade the CanvasGroup
            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0f;
                Tween.Alpha(_canvasGroup,
                    _originalCanvasAlpha,
                    animationDuration,
                    Ease.OutQuad,
                    useUnscaledTime: true);
            }
        }

        public void ClosePanel()
        {
            bool hasAnimation = false;

            if (_targetToScale)
            {
                hasAnimation = true;
                Tween.StopAll(_targetToScale.gameObject.transform);
                
                // Shrink the card
                Tween.Scale(_targetToScale.gameObject.transform,
                        _originalScale * 0.5f,
                        animationDuration,
                        Ease.InBack,
                        useUnscaledTime: true)
                         .OnComplete(() => gameObject.SetActive(false));
            }

            // Fade out the background overlay
            if (_backgroundImage)
            {
                hasAnimation = true;
                Tween.StopAll(gameObject.transform);
                Tween.Custom(_backgroundImage.color.a, 0f, animationDuration, onValueChange: a => 
                {
                    Color col = _backgroundImage.color;
                    col.a = a;
                    _backgroundImage.color = col;
                }, ease: Ease.OutQuad, useUnscaledTime: true)
                         .OnComplete(() => { if (!_targetToScale) gameObject.SetActive(false); });
            }

            // Fade out CanvasGroup
            if (_canvasGroup)
            {
                hasAnimation = true;
                Tween.Alpha(_canvasGroup, 0f, animationDuration, Ease.OutQuad, useUnscaledTime: true)
                         .OnComplete(() => { if (!_targetToScale && !_backgroundImage) gameObject.SetActive(false); });
            }

            if (!hasAnimation) gameObject.SetActive(false);
        }
    }
}
