using TMPro;
using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ComboVFX : MonoBehaviour
    {
        private TextMeshProUGUI _comboText;

        [Header("Settings")]
        [SerializeField] private float popDuration = 0.3f;
        [SerializeField] private float stayDuration = 0.5f;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Vector3 maxScale = new(1.5f, 1.5f, 1.5f);

        private void Awake()
        {
            _comboText = GetComponent<TextMeshProUGUI>();
            _comboText.alpha = 0f;
            
            ComboManager.OnComboUpdated += HandleComboUpdated;
        }

        private void OnDestroy()
        {
            ComboManager.OnComboUpdated -= HandleComboUpdated;
        }

        private void HandleComboUpdated(int combo)
        {
            // We only show VFX for combo >= 2
            if (combo < 2)
            {
                LeanTween.cancel(gameObject);
                _comboText.alpha = 0f;
                return;
            }

            // Update text
            _comboText.text = $"x{combo} COMBO!";

            // Play animation
            PlayAnimation();
        }

        private void PlayAnimation()
        {
            LeanTween.cancel(gameObject);

            // Reset start state
            transform.localScale = Vector3.zero;
            _comboText.alpha = 1f;

            // 1. Pop up
            LeanTween.scale(gameObject, maxScale, popDuration)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => 
                {
                    // 2. Settle to normal scale
                    LeanTween.scale(gameObject, Vector3.one, stayDuration).setEase(LeanTweenType.easeInOutSine);
                    
                    // 3. Fade out
                    LeanTween.value(gameObject, 1f, 0f, fadeDuration)
                        .setDelay(stayDuration)
                        .setOnUpdate(alpha => {
                            _comboText.alpha = alpha;
                        });
                });
        }
    }
}
