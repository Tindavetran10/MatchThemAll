#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using MatchThemAll.Scripts.SaveSystem;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// DEVELOPMENT ONLY — grants currency on click so the shop's success path (spend → grant) can be
    /// tested without playing levels. Compiled out of release builds. Remove or gate behind a debug
    /// menu before shipping. Attach alongside a Button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DebugGrantCurrencyButton : MonoBehaviour
    {
        [SerializeField] private int coinsPerTap = 500;
        [SerializeField] private int gemsPerTap = 50;

        private void Awake()
        {
            if (TryGetComponent(out Button btn))
                btn.onClick.AddListener(Grant);
        }

        private void Grant()
        {
            SaveManager.AddCoins(coinsPerTap);
            SaveManager.AddGems(gemsPerTap);
            Debug.Log($"[Shop-Debug] +{coinsPerTap} coins, +{gemsPerTap} gems (development grant).");
        }
    }
}
#endif
