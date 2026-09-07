using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    [RequireComponent(typeof(FloatingText))]
    public class ComboVFX : MonoBehaviour
    {
        private FloatingText _floatingText;

        private void Awake()
        {
            _floatingText = GetComponent<FloatingText>();
            ComboManager.OnComboUpdated += HandleComboUpdated;
        }

        private void OnDestroy() => ComboManager.OnComboUpdated -= HandleComboUpdated;

        private void HandleComboUpdated(int combo)
        {
            // We only show VFX for combo >= 2
            if (combo < 2)
            {
                // Optionally clear text if combo breaks while visible
                return;
            }

            _floatingText.SetupPopAndStay($"x{combo} COMBO!", Color.yellow);
        }
    }
}
