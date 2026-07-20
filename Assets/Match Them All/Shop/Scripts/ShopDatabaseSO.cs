using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// The registry of all shop products and tabs — single source of truth consumed by ShopManager/ShopPanel.
    /// Mirrors PowerupDatabaseSO. Products are grouped by tab via <see cref="ShopProductSO.tabId"/>;
    /// tabs are authored as <see cref="ShopTabSO"/> assets listed in <see cref="tabs"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Match Them All/Shop Database")]
    public class ShopDatabaseSO : ScriptableObject
    {
        [Header("Products")]
        public List<ShopProductSO> products = new();

        [Header("Tabs (data-driven — order determines display order)")]
        public List<ShopTabSO> tabs = new();

        // ── Tab queries ──────────────────────────────────────────────────────

        /// <summary>All tabs sorted by <see cref="ShopTabSO.order"/>, nulls excluded.</summary>
        public IReadOnlyList<ShopTabSO> OrderedTabs
        {
            get
            {
                var result = new List<ShopTabSO>();
                if (tabs == null) return result;
                foreach (var t in tabs)
                    if (t != null) result.Add(t);
                result.Sort((a, b) => a.order.CompareTo(b.order));
                return result;
            }
        }

        /// <summary>All products whose <see cref="ShopProductSO.tabId"/> matches the given tab id, in list order.</summary>
        public IEnumerable<ShopProductSO> ProductsByTab(string tabId)
        {
            if (string.IsNullOrEmpty(tabId) || products == null) yield break;
            foreach (var p in products)
                if (p != null && p.tabId == tabId) yield return p;
        }

        // ── Product queries ──────────────────────────────────────────────────

        public ShopProductSO FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var p in products)
                if (p != null && p.id == id) return p;
            return null;
        }

        /// <summary>Find a tab SO by id. Returns null if not found.</summary>
        public ShopTabSO FindTabById(string tabId)
        {
            if (string.IsNullOrEmpty(tabId) || tabs == null) return null;
            foreach (var t in tabs)
                if (t != null && t.id == tabId) return t;
            return null;
        }

        /// <summary>
        /// Deprecated — use <see cref="ProductsByTab"/> instead.
        /// Kept temporarily so existing code referencing it still compiles during migration.
        /// </summary>
        [Obsolete("Use ProductsByTab(string tabId) and ShopTabSO assets instead.")]
        public IEnumerable<ShopProductSO> FindByCategory(EShopCategory category)
            => Array.Empty<ShopProductSO>();
    }
}
