using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// The registry of all shop products — single source of truth consumed by ShopManager/ShopPanel.
    /// Mirrors PowerupDatabaseSO. Filter by category for the shop's tabbed sections.
    /// </summary>
    [CreateAssetMenu(menuName = "Match Them All/Shop Database")]
    public class ShopDatabaseSO : ScriptableObject
    {
        public List<ShopProductSO> products = new();

        public ShopProductSO FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var p in products)
                if (p != null && p.id == id) return p;
            return null;
        }

        /// <summary>All products in a category (null/invalid filtered), in list order.</summary>
        public IEnumerable<ShopProductSO> FindByCategory(EShopCategory category)
            => products.Where(p => p != null && p.category == category);
    }
}
