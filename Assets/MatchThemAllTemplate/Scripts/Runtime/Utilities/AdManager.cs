using System;
using MatchThemAll.Scripts.Monetization;
using MatchThemAll.Scripts.SaveSystem;
using MatchThemAll.Scripts.Shop;
using UnityEngine;

namespace MatchThemAll.Scripts.Utilities
{
    /// <summary>
    /// The game's single entry point for ads. Creates itself on startup (no scene needs it), then:
    /// GDPR consent popup (first launch, if enabled) → iOS tracking prompt → privacy flags → ad SDK init.
    /// With the LevelPlay package installed the ads are real; otherwise <see cref="MockAdService"/> runs.
    ///
    /// Remove Ads (<see cref="EntitlementIds.RemoveAds"/>): interstitials stop; rewarded placements still
    /// give their reward, just without the video (the player paid to skip ads, not to lose rewards).
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        private const string ConsentPopupPath = "Monetization/ConsentPopup";

        private MonetizationSettingsSO _settings;
        private IAdService _ads;
        private bool _showing;
        private int _transitionsSinceInterstitial;
        private float _lastInterstitialTime = float.NegativeInfinity;

        public bool IsReady { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance || FindAnyObjectByType<AdManager>()) return;
            new GameObject(nameof(AdManager)).AddComponent<AdManager>();
        }

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _settings = MonetizationSettingsSO.Load();
            _ads = MonetizationServices.CreateAdService(_settings);
        }

        private void Start() => AskGdprConsent(() => TrackingConsent.Request(StartAds));

        #region Startup

        private void AskGdprConsent(Action next)
        {
            if (!_settings.askGdprConsent || SaveManager.GetGdprConsent().HasValue) { next(); return; }

            var prefab = Resources.Load<ConsentPopup>(ConsentPopupPath);
            if (!prefab)
            {
                Debug.LogWarning($"[Ads] No consent popup at Resources/{ConsentPopupPath}; starting ads without a GDPR answer.");
                next();
                return;
            }
            var popup = Instantiate(prefab);
            DontDestroyOnLoad(popup.gameObject);
            popup.Show(accepted =>
            {
                SaveManager.SetGdprConsent(accepted);
                next();
            });
        }

        private void StartAds()
        {
            var privacy = new AdPrivacy(
                _settings.askGdprConsent ? SaveManager.GetGdprConsent() : null,
                _settings.ccpaDoNotSell,
                _settings.childDirected);

            _ads.Initialize(privacy, ok =>
            {
                IsReady = ok;
                Debug.Log($"[Ads] {_ads.GetType().Name} {(ok ? "ready" : "failed to start")}.");
            });
        }

        #endregion

        #region Rewarded

        private static bool AdsRemoved => SaveManager.OwnsEntitlement(EntitlementIds.RemoveAds);

        /// <summary>True if tapping a "watch ad" button would show something right now.</summary>
        public bool IsRewardedAvailable => AdsRemoved || (IsReady && !_showing && _ads.IsRewardedReady);

        /// <summary>
        /// Shows a rewarded ad. <paramref name="onRewardEarned"/> runs only if it was watched to the end;
        /// otherwise <paramref name="onFailed"/> (not loaded, failed, or closed early).
        /// </summary>
        public void ShowRewardedAd(Action onRewardEarned, Action onFailed = null, string placement = "default")
        {
            if (AdsRemoved) { onRewardEarned?.Invoke(); return; }
            if (!IsRewardedAvailable)
            {
                Debug.Log($"[Ads] No rewarded ad ready for '{placement}'.");
                onFailed?.Invoke();
                return;
            }

            BeginAd();
            _ads.ShowRewarded(placement, result =>
            {
                EndAd();
                if (result == AdResult.Rewarded) onRewardEarned?.Invoke();
                else onFailed?.Invoke();
            });
        }

        #endregion

        #region Interstitial

        /// <summary>
        /// Call on a level transition (Next / Retry). Shows an interstitial if pacing allows, then runs
        /// <paramref name="next"/>; otherwise runs it straight away. Never blocks the game when no ad is ready.
        /// </summary>
        public void ShowInterstitialThen(Action next, string placement = "level_transition")
        {
            _transitionsSinceInterstitial++;
            if (!ShouldShowInterstitial()) { next?.Invoke(); return; }

            _transitionsSinceInterstitial = 0;
            _lastInterstitialTime = Time.realtimeSinceStartup;
            BeginAd();
            _ads.ShowInterstitial(placement, () =>
            {
                EndAd();
                next?.Invoke();
            });
        }

        private bool ShouldShowInterstitial()
        {
            if (AdsRemoved || !IsReady || _showing || !_ads.IsInterstitialReady) return false;
            // New players first: the level being left is number (completed + 1).
            if (SaveManager.GetCompletedLevelCount() + 1 < _settings.firstInterstitialLevel) return false;
            if (_transitionsSinceInterstitial < _settings.levelsBetweenInterstitials) return false;
            return Time.realtimeSinceStartup - _lastInterstitialTime >= _settings.minSecondsBetweenInterstitials;
        }

        #endregion

        // Game audio stays silent under a full-screen ad.
        private void BeginAd()
        {
            _showing = true;
            AudioListener.pause = true;
        }

        private void EndAd()
        {
            _showing = false;
            AudioListener.pause = false;
        }
    }
}
