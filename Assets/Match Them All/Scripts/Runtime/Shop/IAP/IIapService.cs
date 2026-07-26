using System;
using UnityEngine;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// Abstraction over real-money IAP so the shop doesn't hard-couple to Unity IAP.
    /// Phase 1 ships with a <see cref="NullIapService"/> stub (no package installed); Phase 2 swaps in
    /// a real Unity-IAP-backed implementation.
    /// </summary>
    public interface IIapService
    {
        bool IsInitialized { get; }
        void Initialize(Action<bool> onComplete = null);
        void Purchase(string iapProductId, Action<bool> onComplete);
        void RestorePurchases(Action<bool> onComplete = null);
    }

    /// <summary>
    /// Phase-1 stub: no IAP package installed yet. Real-money purchases are reported as unavailable
    /// so the shop compiles and runs entirely on soft currency until Phase 2.
    /// </summary>
    public sealed class NullIapService : IIapService
    {
        public bool IsInitialized => true;

        public void Initialize(Action<bool> onComplete = null)
            => onComplete?.Invoke(true);

        public void Purchase(string iapProductId, Action<bool> onComplete)
        {
            Debug.LogWarning($"[IAP] Real-money purchase unavailable (IAP package not installed). Cannot buy '{iapProductId}'.");
            onComplete?.Invoke(false);
        }

        public void RestorePurchases(Action<bool> onComplete = null)
            => onComplete?.Invoke(true);
    }
}
