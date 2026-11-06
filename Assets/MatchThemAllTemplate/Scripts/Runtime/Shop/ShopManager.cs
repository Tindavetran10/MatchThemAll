using System;
using System.Collections.Generic;
using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// Drives shop purchases. For soft-currency products: checks funds, spends, grants rewards.
    /// For real-money (IAP) products: routes through <see cref="IIapService"/> (Unity IAP when installed,
    /// otherwise <see cref="NullIapService"/>). Store purchases are granted by <see cref="GrantStorePurchase"/>,
    /// which saves before the store is told to confirm, and never grants one transaction twice.
    /// One-time first-purchase bonus is granted + persisted on the first buy of a product.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        private const string DatabasePath = "Shop/ShopDatabase";

        [Tooltip("Product registry. Loaded from Resources/" + DatabasePath + " if empty.")]
        [SerializeField] private ShopDatabaseSO database;

        private static ShopDatabaseSO _database;
        private IIapService _iap;

        // In-flight IAP purchases keyed by product id — prevents double-tap → double-charge with an async service.
        private readonly HashSet<string> _purchasing = new();

        /// <summary>Raised when store prices arrive, so cards can show the local price.</summary>
        public event Action PricesUpdated;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (database) _database = database;
            SetIapService(Monetization.MonetizationServices.Iap);
        }

        private void OnDestroy()
        {
            if (_iap != null) _iap.PricesUpdated -= OnPricesUpdated;
        }

        /// <summary>Uses a different purchase backend. The store connection is made once per app.</summary>
        public void SetIapService(IIapService service)
        {
            if (_iap != null) _iap.PricesUpdated -= OnPricesUpdated;
            _iap = service ?? new NullIapService();
            _iap.PricesUpdated += OnPricesUpdated;
            _iap.Initialize(CollectIapProducts(), GrantStorePurchase, _ => OnPricesUpdated());
        }

        private void OnPricesUpdated() => PricesUpdated?.Invoke();

        private static ShopDatabaseSO Database => _database ? _database : _database = Resources.Load<ShopDatabaseSO>(DatabasePath);

        /// <summary>Every product with an iapProductId; one-time products are non-consumable (restorable).</summary>
        private static List<IapProduct> CollectIapProducts()
        {
            var list = new List<IapProduct>();
            if (!Database) return list;
            foreach (var p in Database.products)
                if (p && p.IsIap) list.Add(new IapProduct(p.iapProductId, isConsumable: !p.isOneTime));
            return list;
        }

        /// <summary>The store's price string for an IAP product, or null while unknown.</summary>
        public string GetLocalizedPrice(ShopProductSO product) =>
            product && product.IsIap ? _iap.GetLocalizedPrice(product.iapProductId) : null;

        /// <summary>Re-delivers non-consumables bought on another install (the iOS "Restore Purchases" button).</summary>
        public void RestorePurchases(Action<bool> onComplete = null) => _iap.RestorePurchases(onComplete);

        /// <summary>
        /// Grant handler for store purchases (new, re-delivered or restored). True = granted and saved
        /// (or already granted), so the store may confirm it; false keeps it pending for a later retry.
        /// </summary>
        private static bool GrantStorePurchase(string iapProductId, string transactionId)
        {
            if (SaveManager.IsIapTransactionProcessed(transactionId)) return true; // delivered again

            ShopProductSO product = null;
            if (Database)
                foreach (var p in Database.products)
                    if (p && p.iapProductId == iapProductId) { product = p; break; }
            if (!product)
            {
                Debug.LogError($"[Shop] Store sold '{iapProductId}', but no ShopProductSO has that iapProductId.");
                return false;
            }

            // A restored one-time product the player already owns: nothing to grant again.
            if (!(product.isOneTime && product.IsFulfilled())) GrantProduct(product);
            SaveManager.MarkIapTransactionProcessed(transactionId);
            return SaveManager.TryFlush();
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

                // The reward is granted by GrantStorePurchase before the store confirms; this only reports back.
                _iap.Purchase(product.iapProductId, success =>
                {
                    _purchasing.Remove(product.iapProductId);
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
        private static void GrantProduct(ShopProductSO product)
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

            // A one-time product is fulfilled after its first purchase even with no first-purchase bonus
            // (e.g. a one-time coin pack). Reuse the claim set so IsFulfilled() reflects it.
            if (product.isOneTime && !string.IsNullOrEmpty(product.id))
                SaveManager.MarkFirstBonusClaimed(product.id);

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
    }
}
