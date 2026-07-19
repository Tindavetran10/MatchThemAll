using System;
using System.Collections.Generic;
using UnityEngine;
using MatchThemAll.Scripts.Power_Ups;

namespace MatchThemAll.Scripts.Shop
{
    public enum EShopCategory { Coins, Gems, Powerups, Bundles }

    /// <summary>
    /// One grantable thing a product pays out. Kept as a flat tagged struct (enum kind + amount +
    /// optional powerupId) rather than [SerializeReference] subclasses — simpler, serializes cleanly,
    /// enough for the current reward kinds. Escalate to a hierarchy only if kinds proliferate.
    /// </summary>
    [Serializable]
    public class ShopReward
    {
        public enum EKind { Coins, Gems, PowerupCharge }
        public EKind kind;
        [Min(0)] public int amount;
        [Tooltip("Required when kind = PowerupCharge. Matches a PowerupDataSO.id (e.g. 'vacuum').")]
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
        public EShopCategory category;

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

        [Header("IAP (Phase 2)")]
        [Tooltip("Store product id for real-money purchases. Leave empty for soft-currency products.")]
        public string iapProductId;
        public bool IsIap => !string.IsNullOrEmpty(iapProductId);

        // ── Public read-only accessors ──────────────────────────────────────
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
    }
}
