using System;
using MatchThemAll.Scripts.Shop;

namespace MatchThemAll.Scripts.Monetization
{
    /// <summary>
    /// Where the optional SDK adapters plug in. Each adapter assembly only compiles when its package is
    /// installed, and registers a factory here before the first scene loads. Without one, the game uses
    /// <see cref="MockAdService"/> / <see cref="NullIapService"/>.
    /// </summary>
    public static class MonetizationServices
    {
        public static Func<MonetizationSettingsSO, IAdService> AdServiceFactory;
        public static Func<IIapService> IapServiceFactory;

        private static IIapService _iap;

        public static IAdService CreateAdService(MonetizationSettingsSO settings) =>
            AdServiceFactory?.Invoke(settings) ?? new MockAdService(settings);

        /// <summary>One IAP service for the whole app (the store connection outlives scenes).</summary>
        public static IIapService Iap => _iap ??= IapServiceFactory?.Invoke() ?? new NullIapService();

        /// <summary>Clears the cached service (Enter Play Mode without domain reload).</summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _iap = null;
    }
}
