using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MatchThemAll.Scripts.UI
{
    public class DailyRewardPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private GameObject[] dayHighlights; // 7 highlights to turn on/off based on day
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform modalTransform;

        private Action _onClaimCallback;

        private void Awake()
        {
            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimClicked);
                
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);
        }

        public void Initialize(int streakDay, Action onClaimCallback)
        {
            _onClaimCallback = onClaimCallback;
            
            if (dayText != null)
                dayText.text = $"Day {streakDay}";

            // Turn on the highlight for the current day (1-indexed to 0-indexed)
            for (int i = 0; i < dayHighlights.Length; i++)
            {
                if (dayHighlights[i] != null)
                    dayHighlights[i].SetActive(i == (streakDay - 1));
            }

            // Animate Panel IN
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                LeanTween.alphaCanvas(canvasGroup, 1f, 0.3f).setEase(LeanTweenType.easeOutQuad);
            }
            
            Transform target = modalTransform != null ? modalTransform : transform;
            target.localScale = Vector3.one * 0.8f;
            LeanTween.scale(target.gameObject, Vector3.one, 0.3f).setEase(LeanTweenType.easeOutBack);
        }

        private void OnClaimClicked()
        {
            if (claimButton != null)
                claimButton.interactable = false;

            _onClaimCallback?.Invoke();
            ClosePanel();
        }

        public void ClosePanel()
        {
            Transform target = modalTransform != null ? modalTransform : transform;
            
            // Animate Panel OUT
            LeanTween.scale(target.gameObject, Vector3.one * 0.8f, 0.2f).setEase(LeanTweenType.easeInBack);
            if (canvasGroup != null)
            {
                LeanTween.alphaCanvas(canvasGroup, 0f, 0.2f)
                    .setEase(LeanTweenType.easeInQuad)
                    .setOnComplete(() => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
