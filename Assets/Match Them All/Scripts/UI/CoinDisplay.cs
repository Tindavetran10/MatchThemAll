using UnityEngine;
using TMPro;
using MatchThemAll.Scripts.SaveSystem;

namespace MatchThemAll.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class CoinDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _coinText;

        private void Awake() => 
            _coinText = GetComponent<TextMeshProUGUI>();

        private void OnEnable()
        {
            SaveManager.OnCoinsChanged += UpdateDisplay;
            
            // Set initial value
            var data = SaveManager.Load();
            UpdateDisplay(data.coins);
        }

        private void OnDisable() => 
            SaveManager.OnCoinsChanged -= UpdateDisplay;

        private void UpdateDisplay(int currentCoins) => 
            _coinText.text = currentCoins.ToString("N0");
    }
}
