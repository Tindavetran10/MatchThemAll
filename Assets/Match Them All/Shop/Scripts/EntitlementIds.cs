namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// Well-known entitlement keys (permanent unlocks granted by shop products). Designers can add
    /// their own keys for custom entitlements — these are just the ones the template gates built-in
    /// behavior on.
    /// </summary>
    public static class EntitlementIds
    {
        /// <summary>Suppresses all ad displays (rewarded + interstitial) when owned.</summary>
        public const string RemoveAds = "remove_ads";
    }
}
