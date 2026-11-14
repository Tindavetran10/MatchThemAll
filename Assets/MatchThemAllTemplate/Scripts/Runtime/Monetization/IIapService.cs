using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>A real-money product the store should know about.</summary>
    public readonly struct IapProduct
    {
        public readonly string Id;
        public readonly bool IsConsumable; // false = non-consumable (bought once, restorable)

        public IapProduct(string id, bool isConsumable)
        {
            Id = id;
            IsConsumable = isConsumable;
        }
    }

    /// <summary>
    /// Grants a store purchase. Return true once the reward is granted AND saved (or was already granted
    /// for this transaction); only then is the purchase confirmed with the store. Returning false leaves
    /// it pending, so the store delivers it again on the next launch instead of losing it.
    /// </summary>
    public delegate bool IapGrantHandler(string productId, string transactionId);

    /// <summary>
    /// Abstraction over real-money purchases so the shop doesn't hard-couple to a store SDK.
    /// <see cref="NullIapService"/> is used when Unity IAP is not installed.
    /// </summary>
    public interface IIapService
    {
        bool IsInitialized { get; }

        /// <summary>Connects to the store and registers the products. Pending purchases may be granted right away.</summary>
        void Initialize(IReadOnlyList<IapProduct> products, IapGrantHandler grant, Action<bool> onComplete = null);

        /// <summary>Opens the store's purchase dialog. onComplete(true) after the grant handler accepted it.</summary>
        void Purchase(string productId, Action<bool> onComplete);

        /// <summary>Re-delivers owned non-consumables through the grant handler (required on iOS).</summary>
        void RestorePurchases(Action<bool> onComplete = null);

        /// <summary>The store's price string in the player's currency (e.g. "€1.09"), or null until known.</summary>
        string GetLocalizedPrice(string productId);

        /// <summary>Raised when store prices arrive or change.</summary>
        event Action PricesUpdated;
    }

    /// <summary>No IAP package installed: real-money products are unavailable; everything else works.</summary>
    public sealed class NullIapService : IIapService
    {
        public bool IsInitialized => true;

        public void Initialize(IReadOnlyList<IapProduct> products, IapGrantHandler grant, Action<bool> onComplete = null)
            => onComplete?.Invoke(true);

        public void Purchase(string productId, Action<bool> onComplete)
        {
            Debug.LogWarning($"[IAP] Real-money purchase unavailable (Unity IAP is not installed). Cannot buy '{productId}'.");
            onComplete?.Invoke(false);
        }

        public void RestorePurchases(Action<bool> onComplete = null) => onComplete?.Invoke(true);

        public string GetLocalizedPrice(string productId) => null;

        public event Action PricesUpdated { add { } remove { } }
    }
}
