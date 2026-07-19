using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// One product tile in the shop. Driven by its <see cref="ShopProductSO"/>. Shows icon, name,
    /// price, and merchandising badges; the Buy button calls <see cref="ShopManager.TryPurchase"/>.
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

        private ShopProductSO _product;

        private void Awake()
        {
            if (buyButton) buyButton.onClick.AddListener(OnBuyClicked);
        }

        public void Configure(ShopProductSO product)
        {
            _product = product;
            if (!_product) return;

            if (iconImage)
            {
                iconImage.sprite = _product.Icon;
                iconImage.enabled = _product.Icon;
            }
            if (nameText) nameText.text = _product.DisplayName;
            if (priceText) priceText.text = FormatPrice(_product);
            if (bestValueBadge) bestValueBadge.SetActive(_product.bestValue);
            if (mostPopularBadge) mostPopularBadge.SetActive(_product.mostPopular);
        }

        private static string FormatPrice(ShopProductSO p)
        {
            if (p.IsIap) return $"${p.priceAmount / 100f:0.00}"; // IAP prices conventionally stored in cents
            return $"{p.priceAmount} {p.priceCurrency}";
        }

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
                    Debug.Log($"[Shop] Could not complete '{_product.DisplayName}' (insufficient funds or IAP unavailable).");
            });
        }
    }
}
