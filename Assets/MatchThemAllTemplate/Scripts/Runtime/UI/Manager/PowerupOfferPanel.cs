using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using MatchThemAll.Scripts.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Popup for a power-up with 0 charges (<see cref="PowerupOfferRequestedEvent"/>): the player chooses to pay the
    /// power-up's buyCost or watch a rewarded ad for one charge, which is then used right away. Closing costs nothing.
    /// While open the game sits in <see cref="EGameState.POWERUPOFFER"/> (timer and board input paused).
    /// </summary>
    public class PowerupOfferPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject offerPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button payCoinsButton;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button closeButton;

        private PowerupDataSO _offer;

        private void Awake()
        {
            payCoinsButton.onClick.AddListener(OnPayCoinsClicked);
            watchAdButton.onClick.AddListener(OnWatchAdClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
            EventBus.Subscribe<PowerupOfferRequestedEvent>(OnOfferRequested);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            payCoinsButton.onClick.RemoveListener(OnPayCoinsClicked);
            watchAdButton.onClick.RemoveListener(OnWatchAdClicked);
            closeButton.onClick.RemoveListener(OnCloseClicked);
            EventBus.Unsubscribe<PowerupOfferRequestedEvent>(OnOfferRequested);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnOfferRequested(PowerupOfferRequestedEvent evt)
        {
            if (evt.Powerup == null || !GameManager.Instance.IsGame()) return;

            _offer = evt.Powerup;
            if (titleText)
                titleText.text = $"Out of {(string.IsNullOrEmpty(_offer.DisplayName) ? _offer.id : _offer.DisplayName)}!";
            if (costText)
                costText.text = _offer.buyCost.ToString();
            RefreshButtons();

            offerPanel.SetActive(true); // UIAnimator.OnEnable() handles the pop-in
            GameManager.Instance.SetGameState(EGameState.POWERUPOFFER);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == EGameState.POWERUPOFFER) return;

            // Left the popup (closed, paid, ad watched, or the level ended meanwhile): nothing is pending any more.
            _offer = null;
            if (!offerPanel || !offerPanel.activeSelf) return;
            var anim = offerPanel.GetComponent<UIAnimator>();
            if (anim) anim.ClosePanel();
            else offerPanel.SetActive(false);
        }

        private void OnPayCoinsClicked()
        {
            var so = _offer;
            if (so == null) return;

            if (!SaveManager.Spend(so.buyCurrency, so.buyCost))
            {
                SoundManager.Sfx(SoundEvent.Error);
                RefreshButtons();
                return;
            }

            SetButtonsInteractable(false);
            if (FloatingTextSpawner.Instance)
                FloatingTextSpawner.Instance.Spawn($"-{so.buyCost}", payCoinsButton.transform.position, Color.red);
            GrantAndUse(so);
        }

        private void OnWatchAdClicked()
        {
            var so = _offer;
            if (so == null) return;

            // Using our AdManagerMock. Template users will replace the mock logic with their real SDK.
            if (!AdManagerMock.Instance)
            {
                Debug.LogWarning("[PowerupOffer] AdManagerMock not found in scene! " +
                    "Place it (or your real Ad SDK) in the bootstrap scene.");
                return; // no ad, no reward
            }

            SetButtonsInteractable(false);
            AdManagerMock.Instance.ShowRewardedAd(
                onRewardEarned: () => { if (_offer == so) GrantAndUse(so); },
                onFailed: () =>
                {
                    Debug.LogWarning("[PowerupOffer] Ad failed or skipped.");
                    if (_offer == so) RefreshButtons();
                });
        }

        private void OnCloseClicked()
        {
            SetButtonsInteractable(false);
            GameManager.Instance.SetGameState(EGameState.GAME); // OnGameStateChanged closes the panel
        }

        /// <summary>Adds the bought charge, returns to play, then re-sends the tap so PowerupManager uses it normally.</summary>
        private static void GrantAndUse(PowerupDataSO so)
        {
            SaveManager.AddPowerupCharge(so.id, 1);
            GameManager.Instance.SetGameState(EGameState.GAME);
            EventBus.Publish(new PowerupClickedEvent(so));
        }

        private void RefreshButtons()
        {
            payCoinsButton.interactable = _offer != null && SaveManager.GetCurrency(_offer.buyCurrency) >= _offer.buyCost;
            watchAdButton.interactable = _offer != null;
            closeButton.interactable = true;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            payCoinsButton.interactable = interactable;
            watchAdButton.interactable = interactable;
            closeButton.interactable = interactable;
        }
    }
}
