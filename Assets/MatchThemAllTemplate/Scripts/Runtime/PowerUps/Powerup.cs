using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.Power_Ups
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
        [Tooltip("The power-up this slot represents. Assigned by PowerupManager when it spawns the slot.")]
        [SerializeField] private PowerupDataSO data;
        [Tooltip("The PowerupDataSO.id of the assigned data (informational; set on spawn).")]
        [SerializeField] private string powerupId;

        [Header("UI References")]
        [SerializeField] private Animator animator;
        [SerializeField] private TextMeshPro amountText;
        [SerializeField] private GameObject videoIcons;
        [SerializeField] private GameObject lockBadge;     // shown when locked
        [SerializeField] private TextMeshPro levelLabel;   // "Lv N" when locked
        [SerializeField] private Image iconImage;

        [Header("Custom Slot (PowerupSlot only)")]
        [Tooltip("Where PowerupDataSO.trayModel is instantiated. Leave empty on built-in slots.")]
        [SerializeField] private Transform modelAnchor;
        [Tooltip("Shows PowerupDataSO.icon when the power-up has no tray model.")]
        [SerializeField] private SpriteRenderer iconSprite;

        private const float IconSize = 0.8f; // icon's largest side, in slot units (slot pitch is ~1.15)

        [Header("Animation Event")]
        [Tooltip("Fired by the Animator 'Started' animation event. The Vacuum effect waits on this so its collection runs in sync with the suck animation.")]
        public static Action Started;

        public PowerupDataSO Data => data;
        /// <summary>True if an Animator is wired — used by PowerupManager to decide whether to sync the vacuum effect to its animation.</summary>
        public bool HasAnimator => animator != null;

        private void TriggerPowerupStart() => Started?.Invoke();
        public void Play() { if (animator) animator.Play("Activate"); }

        /// <summary>Assigns the data and builds the slot's look. Called by PowerupManager when it spawns the slot.</summary>
        public void Configure(PowerupDataSO so)
        {
            data = so;
            if (!so) return;
            powerupId = so.id;
            if (iconImage && so.Icon) iconImage.sprite = so.Icon;

            if (!modelAnchor) return; // built-in slot: its prop is part of the prefab
            bool hasModel = so.TrayModel;
            if (hasModel)
            {
                var model = Instantiate(so.TrayModel, modelAnchor, false);
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false; // the slot's own collider takes taps
            }
            if (iconSprite)
            {
                iconSprite.gameObject.SetActive(!hasModel);
                iconSprite.sprite = so.Icon;
                if (so.Icon) // fit any icon size to the slot
                    iconSprite.transform.localScale = Vector3.one * (IconSize / Mathf.Max(so.Icon.bounds.size.x, so.Icon.bounds.size.y));
            }
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
                // unlockLevel is a 0-based level index; players see level numbers from 1.
                if (locked && data != null) levelLabel.text = $"Lv {data.unlockLevel + 1}";
            }
            // Non-interactable when locked: disable the collider so InputManager's raycast can't hit it.
            var col = GetComponent<Collider>();
            if (col) col.enabled = !locked;
        }
    }
}
