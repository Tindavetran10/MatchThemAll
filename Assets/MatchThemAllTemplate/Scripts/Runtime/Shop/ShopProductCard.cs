using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// One product tile in the shop. Driven by its <see cref="ShopProductSO"/>. Shows icon, name,
    /// price, and merchandising badges; the Buy button calls <see cref="ShopManager.TryPurchase"/>.
    /// When the product is fulfilled (entitlement already owned, or one-time product claimed) the card
    /// switches to an "Owned" state: shows <see cref="ownedLabel"/>, hides the price, disables Buy.
    /// Mirrors the LevelButtonUI configure-from-data pattern.
    /// </summary>
    public class ShopProductCard : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject bestValueBadge;
        [SerializeField] private GameObject mostPopularBadge;

        [Header("Owned State (optional)")]
        [Tooltip("Label/badge shown when the product is already purchased/owned. Leave null to hide the price text instead.")]
        [SerializeField] private GameObject ownedLabel;

        private ShopProductSO _product;

        private void Awake()
        {
            if (buyButton) buyButton.onClick.AddListener(OnBuyClicked);
            EventBus.Subscribe<ShopPurchaseSucceededEvent>(OnPurchaseSucceeded);
            if (ShopManager.Instance) ShopManager.Instance.PricesUpdated += RefreshOwnedState;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ShopPurchaseSucceededEvent>(OnPurchaseSucceeded);
            if (ShopManager.Instance) ShopManager.Instance.PricesUpdated -= RefreshOwnedState;
        }

        public void Configure(ShopProductSO product)
        {
            _product = product;
            if (!_product) return;

            if (iconImage)
            {
                iconImage.sprite = _product.Icon;
                iconImage.enabled = _product.Icon != null;
            }
            if (nameText) nameText.text = _product.DisplayName;
            if (bestValueBadge) bestValueBadge.SetActive(_product.bestValue);
            if (mostPopularBadge) mostPopularBadge.SetActive(_product.mostPopular);

            RefreshOwnedState();
        }

        /// <summary>Refreshes the Owned / Buy state without re-binding the product.</summary>
        private void RefreshOwnedState()
        {
            if (!_product) return;

            bool fulfilled = _product.IsFulfilled();
            string price = FormatPrice(_product);
            bool buyable = !fulfilled && price != null; // a store product without a store price can't be bought yet

            if (ownedLabel) ownedLabel.SetActive(fulfilled);
            if (priceText)
            {
                priceText.gameObject.SetActive(!fulfilled);
                if (!fulfilled) priceText.text = price ?? "…";
            }
            if (buyButton) buyButton.interactable = buyable;
        }

        // Re-check after any purchase (another product may share the entitlement key, or this very card was bought).
        private void OnPurchaseSucceeded(ShopPurchaseSucceededEvent _) => RefreshOwnedState();

        // Real-money prices always come from the store, in the player's own currency (null until fetched).
        private static string FormatPrice(ShopProductSO p) =>
            p.IsIap ? ShopManager.Instance ? ShopManager.Instance.GetLocalizedPrice(p) : null
                    : $"{p.priceAmount} {p.priceCurrency}";

        private void OnBuyClicked()
        {
            if (!_product) return;

            if (ShopManager.Instance == null)
            {
                Debug.LogWarning("[Shop] No ShopManager in scene — purchase unavailable.", this);
                return;
            }

            // Log a failure exactly once. TryPurchase's onComplete fires for both soft-currency and IAP paths;
            // for soft-currency it ALSO returns false synchronously, so we must not log in both places.
            ShopManager.Instance.TryPurchase(_product, success =>
            {
                if (!success)
                {
                    SoundManager.Sfx(SoundEvent.Error);
                    Debug.Log($"[Shop] Could not complete '{_product.DisplayName}' (insufficient funds or IAP unavailable).");
                }
                else
                    RefreshOwnedState();
            });
        }
    }
}
