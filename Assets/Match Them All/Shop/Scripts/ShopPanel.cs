using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// The shop overlay panel. Toggled open/closed, spawns one <see cref="ShopProductCard"/> per
    /// product in the active category, with tab switching. Mirrors the ContinuePanelManager/DailyRewardPanel
    /// show/hide pattern. No Canvas needed in code — it's a child of the scene's UI Canvas.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ShopDatabaseSO database;

        [Header("Card Spawn")]
        [SerializeField] private ShopProductCard cardPrefab;
        [SerializeField] private Transform cardContainer;

        [Header("Tabs (optional — if empty, shows all products)")]
        [SerializeField] private Button coinsTabButton;
        [SerializeField] private Button gemsTabButton;
        [SerializeField] private Button powerupsTabButton;
        [SerializeField] private Button bundlesTabButton;

        [Header("Chrome")]
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject root; // the panel root GO toggled for show/hide; defaults to this GO.

        private readonly List<ShopProductCard> _cards = new();
        private EShopCategory _activeCategory = EShopCategory.Powerups;

        private void Awake()
        {
            if (!root) root = gameObject;
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (coinsTabButton)    coinsTabButton.onClick.AddListener(() => ShowCategory(EShopCategory.Coins));
            if (gemsTabButton)     gemsTabButton.onClick.AddListener(() => ShowCategory(EShopCategory.Gems));
            if (powerupsTabButton) powerupsTabButton.onClick.AddListener(() => ShowCategory(EShopCategory.Powerups));
            if (bundlesTabButton)  bundlesTabButton.onClick.AddListener(() => ShowCategory(EShopCategory.Bundles));
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void Open() { if (root != null) root.SetActive(true); Refresh(); }
        public void Close() { if (root != null) root.SetActive(false); }
        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>Opens the panel and jumps to a category (used by out-of-coins auto-open, etc.).</summary>
        public void OpenAtCategory(EShopCategory category)
        {
            _activeCategory = category;
            Open();
        }

        public void ShowCategory(EShopCategory category)
        {
            _activeCategory = category;
            Refresh();
        }

        private void Refresh()
        {
            ClearCards();
            if (database == null || cardPrefab == null || cardContainer == null) return;

            foreach (var product in database.FindByCategory(_activeCategory))
            {
                ShopProductCard card = Instantiate(cardPrefab, cardContainer);
                card.Configure(product);
                _cards.Add(card);
            }
        }

        private void ClearCards()
        {
            foreach (var c in _cards)
                if (c != null) Destroy(c.gameObject);
            _cards.Clear();
        }
    }
}
