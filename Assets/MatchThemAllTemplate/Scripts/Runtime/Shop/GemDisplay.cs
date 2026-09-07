using TMPro;
using UnityEngine;
using MatchThemAll.Scripts.SaveSystem;

namespace MatchThemAll.Scripts.Shop
{
    /// <summary>
    /// Mirrors <c>CoinDisplay</c>: shows the current gem count and refreshes on OnGemsChanged.
    /// Placed next to CoinDisplay in the top bar once gems are earnable (Phase 2).
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class GemDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _gemText;

        private void Awake() => _gemText = GetComponent<TextMeshProUGUI>();

        private void OnEnable()
        {
            SaveManager.OnGemsChanged += UpdateDisplay;
            UpdateDisplay(SaveManager.GetGems());
        }

        private void OnDisable() => SaveManager.OnGemsChanged -= UpdateDisplay;

        private void UpdateDisplay(int currentGems) => _gemText.text = currentGems.ToString("N0");
    }
}
