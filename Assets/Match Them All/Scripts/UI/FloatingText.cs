using System.Collections;
using TMPro;
using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textMesh;

        [Header("Animation")]
        [SerializeField] private float popDuration = 0.25f;
        [SerializeField] private float riseDistance = 150f;
        [SerializeField] private float riseAndFadeDuration = 0.9f;

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (textMesh == null) textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Setup(string text, Color color)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
                textMesh.alpha = 1f;
            }

            // Pop-in scale
            _rect.localScale = Vector3.zero;
            LeanTween.scale(gameObject, Vector3.one, popDuration)
                .setEaseOutBack()
                .setOnComplete(StartRiseAndFade);
        }

        private void StartRiseAndFade()
        {
            float startY = _rect.anchoredPosition.y;
            float targetY = startY + riseDistance;

            // Rise
            LeanTween.value(gameObject, startY, targetY, riseAndFadeDuration)
                .setEaseOutSine()
                .setOnUpdate((float v) => _rect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, v));

            // Fade out
            LeanTween.value(gameObject, 1f, 0f, riseAndFadeDuration)
                .setEaseInQuad()
                .setOnUpdate((float alpha) => { if (textMesh != null) textMesh.alpha = alpha; })
                .setOnComplete(() => Destroy(gameObject));
        }
    }
}
