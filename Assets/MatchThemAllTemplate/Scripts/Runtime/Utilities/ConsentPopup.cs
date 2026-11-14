using System;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Utilities
{
    /// <summary>
    /// First-launch GDPR question ("personalized ads?"). Spawned by AdManager from
    /// Resources/Monetization/ConsentPopup before the ad SDK starts; the answer is saved and the popup
    /// never appears again. Restyle the prefab freely; only the two buttons are required.
    /// </summary>
    public class ConsentPopup : MonoBehaviour
    {
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;

        private Action<bool> _onAnswer;

        public void Show(Action<bool> onAnswer)
        {
            _onAnswer = onAnswer;
            acceptButton.onClick.AddListener(() => Answer(true));
            declineButton.onClick.AddListener(() => Answer(false));
            gameObject.SetActive(true);
        }

        private void Answer(bool accepted)
        {
            acceptButton.interactable = declineButton.interactable = false;
            var done = _onAnswer;
            _onAnswer = null;
            Destroy(gameObject);
            done?.Invoke(accepted);
        }
    }
}
