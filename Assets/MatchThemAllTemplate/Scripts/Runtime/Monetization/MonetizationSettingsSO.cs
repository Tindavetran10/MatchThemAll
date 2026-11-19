using UnityEngine;

namespace MatchThemAll.Scripts.Monetization
{
    /// <summary>
    /// Every monetization key and switch in one asset: Resources/Monetization/MonetizationSettings.
    /// Ad keys come from the LevelPlay dashboard; product ids live on each ShopProductSO (iapProductId).
    /// </summary>
    [CreateAssetMenu(fileName = "MonetizationSettings", menuName = "Match Them All/Monetization Settings")]
    public class MonetizationSettingsSO : ScriptableObject
    {
        public const string ResourcePath = "Monetization/MonetizationSettings";

        [Header("LevelPlay — App Keys (dashboard ▸ Apps)")]
        public string androidAppKey;
        public string iosAppKey;

        [Header("LevelPlay — Ad Unit IDs (dashboard ▸ Ad units)")]
        public string androidRewardedAdUnitId;
        public string iosRewardedAdUnitId;
        public string androidInterstitialAdUnitId;
        public string iosInterstitialAdUnitId;

        [Tooltip("Opens the LevelPlay Test Suite after init. Development builds only; it is ignored in release builds.")]
        public bool launchTestSuite;

        [Header("Interstitial pacing")]
        [Tooltip("No interstitials until the player has reached this level (1-based).")]
        [Min(1)] public int firstInterstitialLevel = 4;
        [Tooltip("Show at most one interstitial every N level transitions (Next / Retry).")]
        [Min(1)] public int levelsBetweenInterstitials = 3;
        [Tooltip("And never more often than this many real seconds.")]
        [Min(0)] public float minSecondsBetweenInterstitials = 360f;

        [Header("Privacy (set before the ad SDK starts; not legal advice)")]
        [Tooltip("Ask for GDPR consent once on first launch and pass the answer to the ad SDK.")]
        public bool askGdprConsent = true;
        [Tooltip("CCPA: report that the user opted out of the sale of personal data.")]
        public bool ccpaDoNotSell;
        [Tooltip("COPPA: the app is directed at children under 13.")]
        public bool childDirected;
        [Tooltip("iOS App Tracking Transparency prompt text (Info.plist NSUserTrackingUsageDescription).")]
        public string iosTrackingUsageDescription = "Your data will be used to show you more relevant ads and to keep the game free.";

        [Header("Editor / no-SDK mock ads")]
        [Tooltip("Seconds the mock 'plays' an ad.")]
        [Min(0)] public float mockAdSeconds = 1f;
        [Tooltip("Make every mock ad fail, to test the 'ad unavailable' paths.")]
        public bool mockAdsFail;

        public string AppKey => Application.platform == RuntimePlatform.IPhonePlayer ? iosAppKey : androidAppKey;
        public string RewardedAdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? iosRewardedAdUnitId : androidRewardedAdUnitId;
        public string InterstitialAdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? iosInterstitialAdUnitId : androidInterstitialAdUnitId;

        private static MonetizationSettingsSO _loaded;

        /// <summary>The project's settings asset, or defaults if it is missing.</summary>
        public static MonetizationSettingsSO Load()
        {
            if (_loaded) return _loaded;
            _loaded = Resources.Load<MonetizationSettingsSO>(ResourcePath);
            if (!_loaded)
            {
                Debug.LogWarning($"[Monetization] No settings at Resources/{ResourcePath}; using defaults (mock ads only).");
                _loaded = CreateInstance<MonetizationSettingsSO>();
            }
            return _loaded;
        }
    }
}
