using MatchThemAll.Scripts.SaveSystem;
using MatchThemAll.Scripts.Settings;
using MatchThemAll.Scripts.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    public class ContinuePanelManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject continuePanel;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button payCoinsButton;
        [SerializeField] private Button giveUpButton;
        [SerializeField] private TextMeshProUGUI coinsCostText;
        [SerializeField] private GameSettingsSO gameSettings;

        [Header("Transition")]
        [Tooltip("Should match the animationDuration on the UIAnimator component.")]
        [SerializeField] private float closeDuration = 0.35f;

        private void Awake()
        {
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
            payCoinsButton.onClick.AddListener(OnPayCoinsClicked);
            giveUpButton.onClick.AddListener(OnGiveUpClicked);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            watchAdButton.onClick.RemoveListener(OnWatchAdClicked);
            payCoinsButton.onClick.RemoveListener(OnPayCoinsClicked);
            giveUpButton.onClick.RemoveListener(OnGiveUpClicked);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == EGameState.OUTOFTIME)
                ShowPanel();
            else
                HidePanel();
        }

        private void ShowPanel()
        {
            if (!continuePanel) return;
            continuePanel.SetActive(true); // UIAnimator.OnEnable() handles the pop-in

            int cost = gameSettings != null ? gameSettings.continueCoinCost : 900;

            if (coinsCostText)
                coinsCostText.text = cost.ToString();

            bool hasCoins = SaveManager.GetCoins() >= cost;

            // Prioritize Coins. If they don't have enough, show Ads (if allowed by settings).
            bool allowAds = gameSettings == null || gameSettings.allowAdContinue;
            
            payCoinsButton.gameObject.SetActive(hasCoins);
            watchAdButton.gameObject.SetActive(!hasCoins && allowAds);

            payCoinsButton.interactable = true;
            giveUpButton.interactable = true;
            watchAdButton.interactable = true;
        }

        private void HidePanel()
        {
            if (!continuePanel || !continuePanel.activeSelf) return;
            var anim = continuePanel.GetComponent<UIAnimator>();
            if (anim) anim.ClosePanel();
            else continuePanel.SetActive(false);
        }

        private void OnWatchAdClicked()
        {
            // Using our AdManagerMock. Template users will replace the mock logic with their real SDK.
            if (AdManagerMock.Instance != null)
            {
                AdManagerMock.Instance.ShowRewardedAd(
                    onRewardEarned: () => ContinueGame(),
                    onFailed: () => Debug.LogWarning("[ContinuePanel] Ad failed or skipped.")
                );
            }
            else
            {
                Debug.LogWarning("[ContinuePanel] AdManagerMock not found in scene! " +
                    "Place it (or your real Ad SDK) in the bootstrap scene.");
                // Do NOT grant a free continue — no SDK found means no ad, means no reward.
            }
        }

        private void OnPayCoinsClicked()
        {
            int cost = gameSettings != null ? gameSettings.continueCoinCost : 900;

            if (SaveManager.SpendCoins(cost))
            {
                Debug.Log($"[ContinuePanel] Spent {cost} coins. Remaining: {SaveManager.GetCoins()}");
                
                if (FloatingTextSpawner.Instance) 
                    FloatingTextSpawner.Instance.Spawn($"-{cost}", payCoinsButton.transform.position, Color.red);

                ContinueGame();
            }
            else Debug.LogWarning("[ContinuePanel] Not enough coins!");
        }

        private void OnGiveUpClicked()
        {
            // Disable all buttons immediately so the player can't double-tap
            giveUpButton.interactable = false;
            watchAdButton.interactable = false;
            payCoinsButton.interactable = false;

            // Trigger GAMEOVER immediately. OnGameStateChanged will handle HidePanel().
            GameManager.Instance.SetGameState(EGameState.GAMEOVER);
        }

        private void ContinueGame()
        {
            int timeBonus = gameSettings != null ? gameSettings.continueTimeBonus : 30;

            TimerManager.Instance.AddTime(timeBonus);
            GameManager.Instance.SetGameState(EGameState.GAME);
        }
    }
}
