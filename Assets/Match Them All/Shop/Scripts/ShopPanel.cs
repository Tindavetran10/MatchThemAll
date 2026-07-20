using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// The shop overlay panel. Toggled open/closed, spawns one <see cref="ShopProductCard"/> per
    /// product in the active tab. Tabs are data-driven: one <see cref="ShopTabSO"/> asset = one tab button,
    /// built at runtime from <see cref="ShopDatabaseSO.OrderedTabs"/>. No code change needed to add a tab.
    /// Mirrors the ContinuePanelManager/DailyRewardPanel show/hide pattern.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ShopDatabaseSO database;

        [Header("Card Spawn")]
        [SerializeField] private ShopProductCard cardPrefab;
        [SerializeField] private Transform cardContainer;

        [Header("Tabs (data-driven)")]
        [Tooltip("Prefab with a Button + TMP child for the tab label. Created by ShopSetup if missing.")]
        [SerializeField] private Button tabButtonPrefab;
        [Tooltip("Parent transform (HorizontalLayoutGroup) that tab buttons are spawned into.")]
        [SerializeField] private Transform tabContainer;

        [Header("Chrome")]
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject root; // the panel root GO toggled for show/hide; defaults to this GO.

        private readonly List<ShopProductCard> _cards = new();
        private readonly List<Button> _tabButtons = new();
        private string _activeTabId;

        private void Awake()
        {
            if (!root) root = gameObject;
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (closeButton) closeButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if (root) root.SetActive(true);
            BuildTabs();
            // Default to the first tab if none is active yet (or the active one no longer exists).
            if (string.IsNullOrEmpty(_activeTabId) || !database.FindTabById(_activeTabId))
            {
                var ordered = database ? database.OrderedTabs : null;
                _activeTabId = ordered is { Count: > 0 } ? ordered[0].id : null;
            }
            Refresh();
        }

        public void Close() { if (root) root.SetActive(false); }
        public bool IsOpen => root && root.activeSelf;

        /// <summary>Opens the panel and jumps directly to a tab by its id.</summary>
        public void OpenAtTab(string tabId)
        {
            _activeTabId = tabId;
            Open();
        }

        public void ShowTab(string tabId)
        {
            _activeTabId = tabId;
            Refresh();
        }

        // ── Tab building ──────────────────────────────────────────────────────

        private void BuildTabs()
        {
            // Clear stale tab buttons
            foreach (var b in _tabButtons.Where(b => b)) Destroy(b.gameObject);
            _tabButtons.Clear();

            if (!database || !tabButtonPrefab || !tabContainer) return;

            foreach (var tab in database.OrderedTabs)
            {
                if (!tab) continue;
                var btn = Instantiate(tabButtonPrefab, tabContainer);
                // Set the label text via TMP child (first TMP found in the prefab hierarchy)
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label) label.text = tab.DisplayName;

                // Capture for the lambda
                string tabId = tab.id;
                btn.onClick.AddListener(() => ShowTab(tabId));
                _tabButtons.Add(btn);
            }
        }

        // ── Card spawning ─────────────────────────────────────────────────────

        private void Refresh()
        {
            ClearCards();
            if (!database || !cardPrefab || !cardContainer) return;
            if (string.IsNullOrEmpty(_activeTabId)) return;

            foreach (var product in database.ProductsByTab(_activeTabId))
            {
                ShopProductCard card = Instantiate(cardPrefab, cardContainer);
                card.Configure(product);
                _cards.Add(card);
            }
        }

        private void ClearCards()
        {
            foreach (var c in _cards.Where(c => c)) Destroy(c.gameObject);
            _cards.Clear();
        }
    }
}
