using System;
using System.Collections.Generic;
using System.Linq;
using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;
using MatchThemAll.Scripts.Power_Ups;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>Kept for migration only — replaced by <see cref="ShopTabSO"/>. Do not use in new code.</summary>
    [Obsolete("Use ShopTabSO + ShopProductSO.tabId instead. This enum will be removed once all existing assets are regenerated.")]
    public enum EShopCategory { Coins, Gems, Powerups, Bundles }

    /// <summary>
    /// One grantable thing a product pays out. Kept as a flat tagged union (enum kind + amount +
    /// optional key) rather than [SerializeReference] subclasses — simpler, serializes cleanly,
    /// enough for the current reward kinds. Escalate to a hierarchy only if kinds proliferate.
    /// </summary>
    [Serializable]
    public class ShopReward
    {
        public enum EKind { Coins, Gems, PowerupCharge, Entitlement }
        public EKind kind;
        [Min(0)] public int amount;
        [Tooltip("PowerupCharge → a PowerupDataSO.id (e.g. 'vacuum'); Entitlement → an entitlement key (e.g. EntitlementIds.RemoveAds).")]
        public string powerupId;
    }

    /// <summary>
    /// Definition of one purchasable shop product: identity, category, cost, rewards, and
    /// merchandising. One asset per product. Designer-tunable; adding a product = new asset, no code.
    /// Mirrors the PowerupDataSO/PowerupDatabaseSO data-driven pattern.
    /// </summary>
    [CreateAssetMenu(menuName = "Match Them All/Shop Product")]
    public class ShopProductSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique key, e.g. 'powerup_vacuum_pack'.")]
        public string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [Tooltip("Id of the ShopTabSO this product belongs to (e.g. 'powerups'). Must match a ShopTabSO.id in ShopDatabaseSO.tabs.")]
        public string tabId;

        [Header("Cost (what the player pays)")]
        [Tooltip("Currency taken on purchase. Coins = soft-currency product; Gems = premium; for real-money IAP set iapProductId and this is ignored.")]
        public ECurrency priceCurrency = ECurrency.Coins;
        [Min(0)] public int priceAmount;

        [Header("Rewards (what the player gets)")]
        [SerializeField] private List<ShopReward> rewards = new();
        public IReadOnlyList<ShopReward> Rewards => rewards;

        [Header("First-Purchase Bonus (granted once)")]
        [Tooltip("Extra rewards granted only on the player's first purchase of this product.")]
        [SerializeField] private List<ShopReward> firstPurchaseBonus = new();
        public IReadOnlyList<ShopReward> FirstPurchaseBonus => firstPurchaseBonus;

        [Header("Merchandising")]
        public bool bestValue;
        public bool mostPopular;
        [TextArea] public string description;

        [Header("One-Time / Entitlement")]
        [Tooltip("If true, the product can only be bought once (e.g. Remove Ads, Starter Pack). Shows 'Owned' + disables Buy after purchase.")]
        public bool isOneTime;

        [Header("IAP (Phase 2)")]
        [Tooltip("Store product id for real-money purchases. Leave empty for soft-currency products.")]
        public string iapProductId;
        public bool IsIap => !string.IsNullOrEmpty(iapProductId);

        // ── Public read-only accessors ──────────────────────────────────────
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;

        /// <summary>
        /// True when the player can no longer benefit from buying this product: it grants an
        /// entitlement they already own, or it's a one-time product already claimed. (Reads SaveManager.)
        /// </summary>
        public bool IsFulfilled()
        {
            if (rewards != null)
            {
                if (rewards.Any(r => r.kind == ShopReward.EKind.Entitlement
                                     && !string.IsNullOrEmpty(r.powerupId)
                                     && SaveManager.OwnsEntitlement(r.powerupId)))
                {
                    return true;
                }
            }
            return isOneTime && !string.IsNullOrEmpty(id) && SaveManager.HasClaimedFirstBonus(id);
        }
    }
}
