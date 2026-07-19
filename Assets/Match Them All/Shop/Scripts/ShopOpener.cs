using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// Tiny glue: a Button (e.g. the coin/gem counter in the top bar) that opens the ShopPanel.
    /// Attach alongside a Button and wire the ShopPanel reference. Industry-standard surfacing.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ShopOpener : MonoBehaviour
    {
        [SerializeField] private ShopPanel shopPanel;

        private void Awake()
        {
            if (TryGetComponent(out Button btn))
                btn.onClick.AddListener(Open);
        }

        private void Open()
        {
            if (shopPanel != null) shopPanel.Open();
            else Debug.LogWarning("[ShopOpener] No ShopPanel assigned.", this);
        }
    }
}
