using MatchThemAll.Scripts.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    public class ContinuePanelManager : MonoBehaviour, IGameStateListener
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject continuePanel;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button payCoinsButton;
        [SerializeField] private Button giveUpButton;
        [SerializeField] private TextMeshProUGUI coinsCostText;
        
        [Header("Settings")]
        [SerializeField] private int continueCost = 100;
        [SerializeField] private int timeBonusSeconds = 30;

        [Header("Transition")]
        [Tooltip("Should match the animationDuration on the UIAnimator component.")]
        [SerializeField] private float closeDuration = 0.35f;

        private void Awake()
        {
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
            payCoinsButton.onClick.AddListener(OnPayCoinsClicked);
            giveUpButton.onClick.AddListener(OnGiveUpClicked);
        }

        private void OnDestroy()
        {
            watchAdButton.onClick.RemoveListener(OnWatchAdClicked);
            payCoinsButton.onClick.RemoveListener(OnPayCoinsClicked);
            giveUpButton.onClick.RemoveListener(OnGiveUpClicked);
        }

        public void GameStateChangedCallback(EGameState gameState)
        {
            if (gameState == EGameState.OUTOFTIME)
                ShowPanel();
            else
                HidePanel();
        }

        private void ShowPanel()
        {
            if (!continuePanel) return;
            continuePanel.SetActive(true); // UIAnimator.OnEnable() handles the pop-in

            if (coinsCostText)
                coinsCostText.text = continueCost.ToString();

            var playerData = SaveManager.Load();
            payCoinsButton.interactable = playerData.coins >= continueCost;
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
            // TODO: Integrate actual Ads SDK (Unity Ads, AppLovin, etc.)
            Debug.Log("[ContinuePanel] Simulating successful ad watch!");
            ContinueGame();
        }

        private void OnPayCoinsClicked()
        {
            if (SaveManager.SpendCoins(continueCost))
            {
                Debug.Log($"[ContinuePanel] Spent {continueCost} coins. Remaining: {SaveManager.Load().coins}");
                
                if (FloatingTextSpawner.Instance) 
                    FloatingTextSpawner.Instance.Spawn($"-{continueCost}", payCoinsButton.transform.position, Color.red);

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

            // Play the PrimeTween close animation, then switch state after it finishes
            var anim = continuePanel ? continuePanel.GetComponent<UIAnimator>() : null;
            if (anim)
            {
                anim.ClosePanel();
                // Wait for the animation to finish before showing Game Over
                PrimeTween.Tween.Delay(closeDuration, () =>
                {
                    GameManager.Instance.SetGameState(EGameState.GAMEOVER);
                }, useUnscaledTime: true);
            }
            else
            {
                // Fallback: no animator, switch immediately
                GameManager.Instance.SetGameState(EGameState.GAMEOVER);
            }
        }

        private void ContinueGame()
        {
            TimerManager.Instance.AddTime(timeBonusSeconds);
            GameManager.Instance.SetGameState(EGameState.GAME);
        }
    }
}
