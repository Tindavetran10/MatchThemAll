using System;
using TMPro;
using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textMesh;

        [Header("Animation Settings")]
        [SerializeField] private float popDuration = 0.25f;
        [SerializeField] private float riseDistance = 150f;
        [SerializeField] private float riseAndFadeDuration = 0.9f;
        [SerializeField] private float stayDuration = 0.5f;

        private RectTransform _rect;
        private Action<FloatingText> _onCompleteCallback;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (!textMesh) textMesh = GetComponentInChildren<TextMeshProUGUI>();
            
            // Start hidden until Setup is called
            if (textMesh) textMesh.alpha = 0f;
        }

        public void SetupFloat(string text, Color color, Action<FloatingText> onComplete = null)
        {
            _onCompleteCallback = onComplete;
            PrepareText(text, color);

            LeanTween.scale(gameObject, Vector3.one, popDuration)
                .setEaseOutBack()
                .setOnComplete(StartRiseAndFade);
        }

        public void SetupPopAndStay(string text, Color color, Action<FloatingText> onComplete = null)
        {
            _onCompleteCallback = onComplete;
            PrepareText(text, color);

            LeanTween.scale(gameObject, new Vector3(1.5f, 1.5f, 1.5f), popDuration)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => 
                {
                    LeanTween.scale(gameObject, Vector3.one, stayDuration).setEase(LeanTweenType.easeInOutSine);
                    LeanTween.value(gameObject, 1f, 0f, 0.3f)
                        .setDelay(stayDuration)
                        .setOnUpdate(alpha => { if (textMesh) textMesh.alpha = alpha; })
                        .setOnComplete(() => _onCompleteCallback?.Invoke(this));
                });
        }

        private void PrepareText(string text, Color color)
        {
            if (textMesh)
            {
                textMesh.text = text;
                textMesh.color = color;
                textMesh.alpha = 1f;
            }

            LeanTween.cancel(gameObject);
            _rect.localScale = Vector3.zero;
        }

        private void StartRiseAndFade()
        {
            float startY = _rect.anchoredPosition.y;
            float targetY = startY + riseDistance;

            LeanTween.value(gameObject, startY, targetY, riseAndFadeDuration)
                .setEaseOutSine()
                .setOnUpdate(v => _rect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, v));

            LeanTween.value(gameObject, 1f, 0f, riseAndFadeDuration)
                .setEaseInQuad()
                .setOnUpdate(alpha => { if (textMesh) textMesh.alpha = alpha; })
                .setOnComplete(() => _onCompleteCallback?.Invoke(this));
        }
    }
}
