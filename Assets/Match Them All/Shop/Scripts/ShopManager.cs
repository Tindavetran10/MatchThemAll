using System;
using System.Collections.Generic;
using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// Drives shop purchases. For soft-currency products: checks funds, spends, grants rewards.
    /// For real-money (IAP) products: routes through <see cref="IIapService"/> and grants only on success.
    /// One-time first-purchase bonus is granted + persisted on the first buy of a product.
    ///
    /// Singleton style mirrors PowerupManager. Assign the ShopDatabaseSO + (optional) IIapService in
    /// the Inspector; the service defaults to NullIapService so Phase 1 runs without the IAP package.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ShopDatabaseSO database;

        public static ShopManager Instance { get; private set; }

        // Phase 1: default NullIapService (no package). Phase 2: call SetIapService(...) with the Unity IAP impl.
        private IIapService _iap = new NullIapService();

        // In-flight IAP purchases keyed by product id — prevents double-tap → double-charge with an async service.
        private readonly HashSet<string> _purchasing = new();

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _iap.Initialize();
        }

        /// <summary>Inject the real IAP service (Phase 2). Initializes the new service (Awake only initialized the default).</summary>
        public void SetIapService(IIapService service)
        {
            _iap = service ?? new NullIapService();
            _iap.Initialize();
        }

        // ── Purchase ──────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to buy a product. For soft-currency: synchronous (returns the result).
        /// For IAP: async (returns false immediately; onComplete fires with the real result).
        /// </summary>
        public bool TryPurchase(ShopProductSO product, Action<bool> onComplete = null)
        {
            if (!product) { onComplete?.Invoke(false); return false; }

            if (product.IsIap)
            {
                // Guard against double-tap → double-charge with an async service.
                if (!_purchasing.Add(product.iapProductId))
                {
                    onComplete?.Invoke(false); // a purchase for this product is already in flight
                    return false;
                }

                _iap.Purchase(product.iapProductId, success =>
                {
                    _purchasing.Remove(product.iapProductId);
                    if (success) GrantProduct(product);
                    onComplete?.Invoke(success);
                });
                return false; // async — caller should rely on onComplete for the real result
            }

            // Soft-currency path.
            if (SaveManager.GetCurrency(product.priceCurrency) < product.priceAmount)
            {
                onComplete?.Invoke(false);
                return false;
            }
            if (!SaveManager.Spend(product.priceCurrency, product.priceAmount))
            {
                onComplete?.Invoke(false);
                return false;
            }

            GrantProduct(product);
            onComplete?.Invoke(true);
            return true;
        }

        /// <summary>Grants a product's rewards (+ first-purchase bonus if unclaimed). No fund check.</summary>
        private void GrantProduct(ShopProductSO product)
        {
            if (!product) return;
            GrantRewards(product.Rewards);

            // First-purchase bonus: claim BEFORE granting so a concurrent purchase can't double-grant.
            // Products with an empty id get no bonus (the id is the claim key; an empty key can't be made unique).
            if (product.FirstPurchaseBonus.Count > 0
                && !string.IsNullOrEmpty(product.id)
                && SaveManager.MarkFirstBonusClaimed(product.id)) // true = this caller won the claim
            {
                GrantRewards(product.FirstPurchaseBonus);
            }

            EventBus.Publish(new ShopPurchaseSucceededEvent(product));
            Debug.Log($"[Shop] Granted '{product.DisplayName}'.");
        }

        private static void GrantRewards(IReadOnlyList<ShopReward> rewards)
        {
            if (rewards == null) return;
            foreach (var r in rewards)
            {
                switch (r.kind)
                {
                    case ShopReward.EKind.Coins:         SaveManager.AddCoins(r.amount); break;
                    case ShopReward.EKind.Gems:          SaveManager.AddGems(r.amount); break;
                    case ShopReward.EKind.PowerupCharge: SaveManager.AddPowerupCharge(r.powerupId, r.amount); break;
                    // powerupId is reused as the entitlement key (tagged union — field name is cosmetic).
                    case ShopReward.EKind.Entitlement:   SaveManager.GrantEntitlement(r.powerupId); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        // ── Restore (IAP) ─────────────────────────────────────────────────────

        public void RestorePurchases(Action<bool> onComplete = null) => _iap.RestorePurchases(onComplete);
    }
}
