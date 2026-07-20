using UnityEngine;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// Defines one tab in the shop. Create an asset per tab — no code change needed to add a new tab.
    /// The shop panel reads <see cref="ShopDatabaseSO.OrderedTabs"/> and spawns one button per asset.
    /// Mirrors the PowerupDataSO / LevelDataSO data-driven authoring pattern.
    /// </summary>
    [CreateAssetMenu(menuName = "Match Them All/Shop Tab")]
    public class ShopTabSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key used by ShopProductSO.tabId. Must be unique across all tab assets.")]
        public string id;

        [SerializeField] private string displayName;

        [Header("Layout")]
        [Tooltip("Lower numbers appear first in the tab row.")]
        [Min(0)] public int order;

        [Header("Icon (optional)")]
        public Sprite icon;

        // ── Accessors ────────────────────────────────────────────────────────
        /// <summary>Displayed label; falls back to the asset name when displayName is empty.</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}
