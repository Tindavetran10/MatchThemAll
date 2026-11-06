using System;
using System.Collections.Generic;
using MatchThemAll.Scripts.Shop;
using UnityEngine;
using UnityEngine.Purchasing;

namespace MatchThemAll.Scripts.Monetization
{
    /// <summary>
    /// Real-money purchases through Unity IAP 5 (com.unity.purchasing). This assembly only compiles when
    /// that package is installed, and registers itself on startup.
    ///
    /// Purchase flow (IAP 5 is two-step): store reports a PendingOrder → the shop's grant handler grants
    /// the reward and saves → only then ConfirmPurchase. If the grant or save fails the order stays
    /// pending and the store re-delivers it on the next launch, so a paid purchase is never lost.
    /// In the Editor Unity IAP uses its fake store, so the whole flow can be tested without a device.
    /// </summary>
    public sealed class UnityIapService : IIapService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => MonetizationServices.IapServiceFactory = () => new UnityIapService();

        private StoreController _store;
        private IapGrantHandler _grant;
        private Action<bool> _onInit;
        private readonly List<ProductDefinition> _definitions = new();
        private readonly Dictionary<string, Action<bool>> _inFlight = new();

        public bool IsInitialized { get; private set; }
        public event Action PricesUpdated;

        #region Init

        public async void Initialize(IReadOnlyList<IapProduct> products, IapGrantHandler grant, Action<bool> onComplete = null)
        {
            if (_store != null) { onComplete?.Invoke(IsInitialized); return; } // one store connection per app
            _grant = grant;
            _onInit = onComplete;

            _definitions.Clear();
            foreach (var p in products)
                _definitions.Add(new ProductDefinition(p.Id, p.IsConsumable ? ProductType.Consumable : ProductType.NonConsumable));

            _store = UnityIAPServices.StoreController();

            // Every event is subscribed before Connect: orders left pending last session arrive right away.
            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += f => Debug.LogWarning($"[IAP] Store disconnected: {f.Message}");
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += f => { Debug.LogWarning($"[IAP] Product fetch failed: {f.FailureReason}"); FinishInit(); };
            _store.OnPurchasesFetched += _ => { };
            _store.OnPurchasesFetchFailed += f => Debug.LogWarning($"[IAP] Purchase fetch failed: {f.Message}");
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;

            try { await _store.Connect(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAP] Could not connect to the store: {e.Message}");
                FinishInit();
            }
        }

        private void OnStoreConnected()
        {
            _store.FetchProducts(_definitions);
            _store.FetchPurchases(); // also re-delivers pending orders to OnPurchasePending
        }

        private void OnProductsFetched(List<Product> fetched)
        {
            IsInitialized = true;
            PricesUpdated?.Invoke();
            FinishInit();
        }

        private void FinishInit()
        {
            var done = _onInit;
            _onInit = null;
            done?.Invoke(IsInitialized);
        }

        #endregion

        #region Purchase

        public void Purchase(string productId, Action<bool> onComplete)
        {
            var product = IsInitialized ? _store.GetProductById(productId) : null;
            if (product == null || !product.availableToPurchase)
            {
                Debug.LogWarning($"[IAP] '{productId}' is not available (store not ready or product not set up).");
                onComplete?.Invoke(false);
                return;
            }
            if (_inFlight.ContainsKey(productId)) { onComplete?.Invoke(false); return; }

            _inFlight[productId] = onComplete;
            _store.PurchaseProduct(product);
        }

        private void OnPurchasePending(PendingOrder order)
        {
            string id = FirstProductId(order);
            bool granted = id != null && _grant != null && _grant(id, order.Info?.TransactionID);
            if (granted) _store.ConfirmPurchase(order);
            else Debug.LogWarning($"[IAP] '{id}' not granted yet; it stays pending and will be retried next launch.");
            Complete(id, granted);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
                Debug.LogWarning($"[IAP] Confirm failed for '{FirstProductId(order)}': {failed.FailureReason} {failed.Details}");
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            Debug.Log($"[IAP] Purchase of '{FirstProductId(order)}' failed: {order.FailureReason} {order.Details}");
            Complete(FirstProductId(order), false);
        }

        // Ask-to-Buy / pending payment: nothing is granted until the store sends the PendingOrder.
        private void OnPurchaseDeferred(DeferredOrder order)
        {
            Debug.Log($"[IAP] Purchase of '{FirstProductId(order)}' is waiting for approval.");
            Complete(FirstProductId(order), false);
        }

        private void Complete(string productId, bool success)
        {
            if (productId == null || !_inFlight.Remove(productId, out var callback)) return;
            callback?.Invoke(success);
        }

        private static string FirstProductId(Order order)
        {
            var items = order?.CartOrdered?.Items();
            return items is { Count: > 0 } ? items[0].Product?.definition?.id : null;
        }

        #endregion

        public void RestorePurchases(Action<bool> onComplete = null)
        {
            if (_store == null) { onComplete?.Invoke(false); return; }
            _store.RestoreTransactions((ok, error) =>
            {
                if (!ok) Debug.LogWarning($"[IAP] Restore failed: {error}");
                onComplete?.Invoke(ok); // each restored item arrives through OnPurchasePending
            });
        }

        public string GetLocalizedPrice(string productId)
        {
            var product = IsInitialized ? _store.GetProductById(productId) : null;
            return product?.metadata?.localizedPriceString;
        }
    }
}
