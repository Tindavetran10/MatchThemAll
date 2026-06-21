using System;
using TMPro;
using UnityEngine;
using PrimeTween;

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

            Tween.Scale(gameObject.transform, Vector3.one, popDuration, Ease.OutBack)
                .OnComplete(StartRiseAndFade);
        }

        public void SetupPopAndStay(string text, Color color, Action<FloatingText> onComplete = null)
        {
            _onCompleteCallback = onComplete;
            PrepareText(text, color);

            Tween.Scale(gameObject.transform, new Vector3(1.5f, 1.5f, 1.5f), popDuration, Ease.OutBack)
                .OnComplete(() => 
                {
                    Tween.Scale(gameObject.transform, Vector3.one, stayDuration, Ease.InOutSine);
                    Tween.Custom(1f, 0f, 0.3f, onValueChange: (float alpha) =>
                        {
                            if (textMesh) textMesh.alpha = alpha;
                        }, startDelay: stayDuration)
                        .OnComplete(() => _onCompleteCallback?.Invoke(this));
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

            Tween.StopAll(gameObject.transform);
            _rect.localScale = Vector3.zero;
        }

        private void StartRiseAndFade()
        {
            float startY = _rect.anchoredPosition.y;
            float targetY = startY + riseDistance;

            Tween.Custom(startY, targetY, riseAndFadeDuration,
                onValueChange: (float v) => _rect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, v),
                ease: Ease.OutSine);

            Tween.Custom(1f, 0f, riseAndFadeDuration, onValueChange: (float alpha) =>
                {
                    if (textMesh) textMesh.alpha = alpha;
                }, ease: Ease.InQuad)
                .OnComplete(() => _onCompleteCallback?.Invoke(this));
        }
    }
}
