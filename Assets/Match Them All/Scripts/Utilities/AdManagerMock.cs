using System;
using UnityEngine;

namespace MatchThemAll.Scripts.Utilities
{
    /// <summary>
    /// A mock class to simulate Ad network integrations (like Unity Ads, AppLovin, etc.).
    /// Template users can replace the logic inside these methods with their actual Ad SDK calls.
    /// </summary>
    public class AdManagerMock : MonoBehaviour
    {
        public static AdManagerMock Instance { get; private set; }

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Simulates showing a Rewarded Video Ad.
        /// Replace this with your SDK's ShowRewardedAd method.
        /// </summary>
        /// <param name="onRewardEarned">Callback when the user finishes watching the ad.</param>
        /// <param name="onFailed">Callback if the ad failed to load or the user skipped it early.</param>
        public void ShowRewardedAd(Action onRewardEarned, Action onFailed = null)
        {
            Debug.Log("[AdManagerMock] Showing Rewarded Ad...");
            
            // Simulating a successful ad watch immediately for template testing purposes.
            // In a real game, you would wait for the SDK's OnAdCompleted callback.
            onRewardEarned?.Invoke();
        }

        /// <summary>
        /// Simulates showing an Interstitial (non-rewarded, skippable) Ad.
        /// Replace this with your SDK's ShowInterstitialAd method.
        /// </summary>
        public void ShowInterstitialAd()
        {
            Debug.Log("[AdManagerMock] Showing Interstitial Ad...");
        }
    }
}
