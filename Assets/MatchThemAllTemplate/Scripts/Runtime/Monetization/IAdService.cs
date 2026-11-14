using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MatchThemAll.Scripts.Monetization
{
    public enum AdResult
    {
        Rewarded, // watched to the end: grant the reward
        Skipped,  // closed early: no reward
        Failed    // not loaded / failed to show: no reward
    }

    /// <summary>Privacy answers handed to the ad SDK before it starts.</summary>
    public readonly struct AdPrivacy
    {
        public readonly bool? GdprConsent; // null = not asked (consent popup disabled)
        public readonly bool CcpaDoNotSell;
        public readonly bool ChildDirected;

        public AdPrivacy(bool? gdprConsent, bool ccpaDoNotSell, bool childDirected)
        {
            GdprConsent = gdprConsent;
            CcpaDoNotSell = ccpaDoNotSell;
            ChildDirected = childDirected;
        }
    }

    /// <summary>
    /// One ad network. The game talks to AdManager, which talks to this; swap networks by registering
    /// a different factory in <see cref="MonetizationServices"/>.
    /// </summary>
    public interface IAdService
    {
        void Initialize(AdPrivacy privacy, Action<bool> onComplete);
        bool IsRewardedReady { get; }
        void ShowRewarded(string placement, Action<AdResult> onComplete);
        bool IsInterstitialReady { get; }
        void ShowInterstitial(string placement, Action onClosed);
    }

    /// <summary>
    /// Used when no ad SDK is installed (and in the Editor without one). "Plays" an ad for
    /// <see cref="MonetizationSettingsSO.mockAdSeconds"/>, and can be set to fail every time.
    /// </summary>
    public sealed class MockAdService : IAdService
    {
        private readonly MonetizationSettingsSO _settings;
        private bool _showing;

        public MockAdService(MonetizationSettingsSO settings) => _settings = settings;

        public void Initialize(AdPrivacy privacy, Action<bool> onComplete) => onComplete?.Invoke(true);

        public bool IsRewardedReady => !_settings.mockAdsFail && !_showing;
        public bool IsInterstitialReady => !_settings.mockAdsFail && !_showing;

        public async void ShowRewarded(string placement, Action<AdResult> onComplete)
        {
            try
            {
                if (!IsRewardedReady) { onComplete?.Invoke(AdResult.Failed); return; }
                Debug.Log($"[MockAds] Rewarded '{placement}'…");
                await Play();
                onComplete?.Invoke(AdResult.Rewarded);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async void ShowInterstitial(string placement, Action onClosed)
        {
            try
            {
                if (!IsInterstitialReady) { onClosed?.Invoke(); return; }
                Debug.Log($"[MockAds] Interstitial '{placement}'…");
                await Play();
                onClosed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async Task Play()
        {
            _showing = true;
            try { await Task.Delay(TimeSpan.FromSeconds(_settings.mockAdSeconds)); } // continues on the main thread
            finally { _showing = false; }
        }
    }
}
