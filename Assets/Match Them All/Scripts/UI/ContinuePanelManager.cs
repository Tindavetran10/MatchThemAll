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
            {
                ShowPanel();
            }
            else
            {
                HidePanel();
            }
        }

        private void ShowPanel()
        {
            if (continuePanel)
                continuePanel.SetActive(true);

            // Update UI
            if (coinsCostText)
                coinsCostText.text = continueCost.ToString();

            // Disable coin button if not enough coins
            var playerData = SaveManager.Load();
            payCoinsButton.interactable = playerData.coins >= continueCost;
        }

        private void HidePanel()
        {
            if (continuePanel && continuePanel.activeSelf)
            {
                var anim = continuePanel.GetComponent<UIAnimator>();
                if (anim) anim.ClosePanel();
                else continuePanel.SetActive(false);
            }
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
                
                // Spawn floating text for minus coins
                if (FloatingTextSpawner.Instance != null) 
                    FloatingTextSpawner.Instance.Spawn($"-{continueCost}", payCoinsButton.transform.position, Color.red);

                ContinueGame();
            }
            else Debug.LogWarning("[ContinuePanel] Not enough coins!");
        }

        private static void OnGiveUpClicked() => 
            GameManager.Instance.SetGameState(EGameState.GAMEOVER);

        private void ContinueGame()
        {
            TimerManager.Instance.AddTime(timeBonusSeconds);
            GameManager.Instance.SetGameState(EGameState.GAME);
        }
    }
}
