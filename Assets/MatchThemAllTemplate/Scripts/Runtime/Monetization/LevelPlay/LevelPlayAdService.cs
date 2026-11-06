using System;
using System.Threading.Tasks;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace MatchThemAll.Scripts.Monetization
{
    /// <summary>
    /// Ads through Unity LevelPlay (package "Ads Mediation", com.unity.services.levelplay 9.5+).
    /// This assembly only compiles when that package is installed, and registers itself on startup.
    /// Rewarded and interstitial ads are preloaded and reloaded after every show; failed loads retry
    /// with a growing delay. Privacy flags are applied before LevelPlay.Init, as LevelPlay requires.
    /// </summary>
    public sealed class LevelPlayAdService : IAdService
    {
        private const float MaxRetrySeconds = 60f;
        private const int LateRewardGraceMs = 500; // some networks report the reward just after "closed"

        private readonly MonetizationSettingsSO _settings;
        private LevelPlayRewardedAd _rewarded;
        private LevelPlayInterstitialAd _interstitial;
        private Action<bool> _onInit;

        private Action<AdResult> _rewardedDone;
        private bool _rewardEarned;
        private Action _interstitialDone;
        private float _rewardedRetry = 2f, _interstitialRetry = 2f;

#if UNITY_ANDROID || UNITY_IOS
        // LevelPlay only serves (mock) ads on Android/iOS targets; elsewhere the game keeps the mock service.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => MonetizationServices.AdServiceFactory = s => new LevelPlayAdService(s);
#endif

        public LevelPlayAdService(MonetizationSettingsSO settings) => _settings = settings;

        public bool IsRewardedReady => _rewarded != null && _rewarded.IsAdReady();
        public bool IsInterstitialReady => _interstitial != null && _interstitial.IsAdReady();

        #region Init

        public void Initialize(AdPrivacy privacy, Action<bool> onComplete)
        {
            if (string.IsNullOrEmpty(_settings.AppKey))
            {
                Debug.LogWarning("[LevelPlay] No App Key in MonetizationSettings for this platform; ads disabled.");
                onComplete?.Invoke(false);
                return;
            }

            // Privacy must be set before Init.
            if (privacy.GdprConsent.HasValue) LevelPlayPrivacySettings.SetGDPRConsent(privacy.GdprConsent.Value);
            LevelPlayPrivacySettings.SetCCPA(privacy.CcpaDoNotSell);
            LevelPlayPrivacySettings.SetCOPPA(privacy.ChildDirected);
            if (_settings.launchTestSuite && Debug.isDebugBuild) LevelPlay.SetMetaData("is_test_suite", "enable");

            _onInit = onComplete;
            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(_settings.AppKey);
        }

        private void OnInitSuccess(LevelPlayConfiguration config)
        {
            Unsubscribe();
            Debug.Log("[LevelPlay] Initialized.");
            CreateAds();
            if (_settings.launchTestSuite && Debug.isDebugBuild) LevelPlay.LaunchTestSuite();
            Finish(true);
        }

        private void OnInitFailed(LevelPlayInitError error)
        {
            Unsubscribe();
            Debug.LogError($"[LevelPlay] Init failed: {error.ErrorMessage}");
            Finish(false);
        }

        private void Unsubscribe()
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
        }

        private void Finish(bool ok)
        {
            var done = _onInit;
            _onInit = null;
            done?.Invoke(ok);
        }

        private void CreateAds()
        {
            if (!string.IsNullOrEmpty(_settings.RewardedAdUnitId))
            {
                _rewarded = new LevelPlayRewardedAd(_settings.RewardedAdUnitId);
                _rewarded.OnAdLoaded += _ => _rewardedRetry = 2f;
                _rewarded.OnAdLoadFailed += e => RetryLoad(e, isRewarded: true);
                _rewarded.OnAdRewarded += (_, _) => _rewardEarned = true;
                _rewarded.OnAdClosed += _ => OnRewardedClosed();
                _rewarded.OnAdDisplayFailed += (_, e) => EndRewarded(AdResult.Failed, e.ErrorMessage);
                _rewarded.LoadAd();
            }
            else Debug.LogWarning("[LevelPlay] No rewarded ad unit id for this platform.");

            if (!string.IsNullOrEmpty(_settings.InterstitialAdUnitId))
            {
                _interstitial = new LevelPlayInterstitialAd(_settings.InterstitialAdUnitId);
                _interstitial.OnAdLoaded += _ => _interstitialRetry = 2f;
                _interstitial.OnAdLoadFailed += e => RetryLoad(e, isRewarded: false);
                _interstitial.OnAdClosed += _ => EndInterstitial();
                _interstitial.OnAdDisplayFailed += (_, e) => EndInterstitial(e.ErrorMessage);
                _interstitial.LoadAd();
            }
            else Debug.LogWarning("[LevelPlay] No interstitial ad unit id for this platform.");
        }

        private async void RetryLoad(LevelPlayAdError error, bool isRewarded)
        {
            float wait = isRewarded ? _rewardedRetry : _interstitialRetry;
            Debug.LogWarning($"[LevelPlay] {(isRewarded ? "Rewarded" : "Interstitial")} load failed ({error.ErrorMessage}); retrying in {wait:0}s.");
            if (isRewarded) _rewardedRetry = Mathf.Min(wait * 2f, MaxRetrySeconds);
            else _interstitialRetry = Mathf.Min(wait * 2f, MaxRetrySeconds);

            await Task.Delay(TimeSpan.FromSeconds(wait));
            if (isRewarded) _rewarded?.LoadAd();
            else _interstitial?.LoadAd();
        }

        #endregion

        #region Rewarded

        public void ShowRewarded(string placement, Action<AdResult> onComplete)
        {
            if (!IsRewardedReady || _rewardedDone != null)
            {
                onComplete?.Invoke(AdResult.Failed);
                return;
            }
            _rewardEarned = false;
            _rewardedDone = onComplete;
            _rewarded.ShowAd();
        }

        private async void OnRewardedClosed()
        {
            if (!_rewardEarned) await Task.Delay(LateRewardGraceMs);
            EndRewarded(_rewardEarned ? AdResult.Rewarded : AdResult.Skipped);
        }

        private void EndRewarded(AdResult result, string error = null)
        {
            if (error != null) Debug.LogWarning($"[LevelPlay] Rewarded failed to show: {error}");
            var done = _rewardedDone;
            _rewardedDone = null;
            _rewarded?.LoadAd(); // next one
            done?.Invoke(result);
        }

        #endregion

        #region Interstitial

        public void ShowInterstitial(string placement, Action onClosed)
        {
            if (!IsInterstitialReady || _interstitialDone != null)
            {
                onClosed?.Invoke();
                return;
            }
            _interstitialDone = onClosed;
            _interstitial.ShowAd();
        }

        private void EndInterstitial(string error = null)
        {
            if (error != null) Debug.LogWarning($"[LevelPlay] Interstitial failed to show: {error}");
            var done = _interstitialDone;
            _interstitialDone = null;
            _interstitial?.LoadAd();
            done?.Invoke();
        }

        #endregion
    }
}
