using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

namespace Match_Them_All.Scripts.Power_Ups
{
    /// <summary>
    /// Generic, data-driven power-up button. Driven by its <see cref="PowerupDataSO"/>.
    /// Concrete and placeable directly. The Vacuum/Spring/Fan/FreezeGun subclasses remain
    /// as thin aliases only so the scene's pre-existing component references keep resolving
    /// until those buttons are migrated to a plain Powerup (Stage 3b).
    /// </summary>
    public class Powerup : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("The power-up this button represents. If null, resolved at boot from PowerupManager by powerupId.")]
        [SerializeField] private PowerupDataSO data;
        [Tooltip("Matches a PowerupDataSO.id in the database — used to auto-resolve `data` on boot.")]
        [SerializeField] private string powerupId;

        [Header("UI References")]
        [SerializeField] private Animator animator;
        [SerializeField] private TextMeshPro amountText;
        [SerializeField] private GameObject videoIcons;
        [SerializeField] private GameObject lockBadge;     // shown when locked
        [SerializeField] private TextMeshPro levelLabel;   // "Lv N" when locked
        [SerializeField] private Image iconImage;

        [Header("Animation Event")]
        [Tooltip("Fired by the Animator 'Started' animation event. The Vacuum effect waits on this so its collection runs in sync with the suck animation.")]
        public static Action Started;

        public PowerupDataSO Data => data;
        /// <summary>True if an Animator is wired — used by PowerupManager to decide whether to sync the vacuum effect to its animation.</summary>
        public bool HasAnimator => animator != null;

        private void TriggerPowerupStart() => Started?.Invoke();
        public void Play() { if (animator) animator.Play("Activate"); }

        /// <summary>Assigns the data and wires icon/name. Called by PowerupManager on boot.</summary>
        public void Configure(PowerupDataSO so)
        {
            data = so;
            if (!string.IsNullOrEmpty(powerupId) && so != null) powerupId = so.id;
            if (iconImage != null && so != null && so.Icon != null) iconImage.sprite = so.Icon;
        }

        /// <summary>Resolves `data` from the database if only powerupId was set in the Inspector.</summary>
        public bool TryAutoResolve(PowerupDatabaseSO database)
        {
            if (data != null) return true;
            if (database == null || string.IsNullOrEmpty(powerupId)) return false;
            data = database.FindById(powerupId);
            if (data != null && iconImage != null && data.Icon != null) iconImage.sprite = data.Icon;
            return data != null;
        }

        public void UpdateVisuals(int amount, bool locked)
        {
            if (videoIcons) videoIcons.SetActive(!locked && amount <= 0);
            if (amountText && amountText.gameObject) amountText.gameObject.SetActive(!locked && amount > 0);
            if (amountText) amountText.text = amount.ToString();

            if (lockBadge) lockBadge.SetActive(locked);
            if (levelLabel)
            {
                levelLabel.gameObject.SetActive(locked);
                if (locked && data != null) levelLabel.text = $"Lv {data.unlockLevel}";
            }
            // Non-interactable when locked: disable the collider so InputManager's raycast can't hit it.
            var col = GetComponent<Collider>();
            if (col) col.enabled = !locked;
        }
    }
}
