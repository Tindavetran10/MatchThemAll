using UnityEngine;
using UnityEngine.UI;
using MatchThemAll.Scripts.SaveSystem;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// "Watch Ad for N Coins" button. Phase-1 stub: no ad SDK installed yet, so it grants coins
    /// directly. Replace OnAdComplete with a real Rewarded-Ad callback (Unity Ads / UGS Ads) later.
    /// Attach alongside a Button; set coinsPerWatch.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WatchAdForCoinsButton : MonoBehaviour
    {
        [SerializeField] private int coinsPerWatch = 50;
        [Tooltip("Max times this button can grant coins per app session (dev-stub safety; replace with real ad-verified grants).")]
        [SerializeField] private int maxPerSession = 3;

        // Session-only cap so the dev stub can't be spammed for unlimited coins. Static so it survives reloads.
        private static int _sessionClaims;

        private void Awake()
        {
            if (TryGetComponent(out Button btn))
                btn.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            // TODO(ad-sdk): wire the Rewarded Ad SDK; call OnAdComplete only on a verified view.
            // For now (no ad package installed), grant coins immediately so the flow is testable — but cap it.
            if (_sessionClaims >= maxPerSession)
            {
                Debug.Log("[Shop] Watch-ad reward cap reached for this session (dev stub).");
                return;
            }
            OnAdComplete();
        }

        private void OnAdComplete()
        {
            _sessionClaims++;
            SaveManager.AddCoins(coinsPerWatch);
        }
    }
}
