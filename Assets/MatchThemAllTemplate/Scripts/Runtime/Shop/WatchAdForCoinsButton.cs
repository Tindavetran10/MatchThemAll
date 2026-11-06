using MatchThemAll.Scripts.SaveSystem;
using MatchThemAll.Scripts.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// "Watch an ad for N coins". Coins are granted only after a rewarded ad was watched to the end.
    /// Attach alongside a Button; set coinsPerWatch. Hidden when the player owns
    /// <see cref="EntitlementIds.RemoveAds"/> (the free-coin video goes away with the ads).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WatchAdForCoinsButton : MonoBehaviour
    {
        [SerializeField] private int coinsPerWatch = 50;
        [Tooltip("How many times per app session the button pays out.")]
        [SerializeField] private int maxPerSession = 3;

        private static int _sessionClaims; // static: survives scene reloads

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void Start()
        {
            if (SaveManager.OwnsEntitlement(EntitlementIds.RemoveAds)) gameObject.SetActive(false);
        }

        private void OnClicked()
        {
            if (SaveManager.OwnsEntitlement(EntitlementIds.RemoveAds)) { gameObject.SetActive(false); return; }
            if (_sessionClaims >= maxPerSession || !AdManager.Instance) return;

            _button.interactable = false;
            AdManager.Instance.ShowRewardedAd(
                onRewardEarned: () =>
                {
                    _sessionClaims++;
                    SaveManager.AddCoins(coinsPerWatch);
                    if (_button) _button.interactable = _sessionClaims < maxPerSession;
                },
                onFailed: () => { if (_button) _button.interactable = true; },
                placement: "shop_coins");
        }
    }
}
