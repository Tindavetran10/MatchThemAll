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
        [Tooltip("The badge the charge count sits on (Amount Container/Container). Hidden while locked.")]
        [SerializeField] private GameObject counterBadge;
        [SerializeField] private Image iconImage;

        [Header("Custom Slot (PowerupSlot only)")]
        [Tooltip("Where PowerupDataSO.trayModel is instantiated. Leave empty on built-in slots.")]
        [SerializeField] private Transform modelAnchor;
        [Tooltip("Shows PowerupDataSO.icon when the power-up has no tray model.")]
        [SerializeField] private SpriteRenderer iconSprite;

        private const float IconSize = 0.8f; // icon's largest side, in slot units (slot pitch is ~1.15)
        private static readonly Color LockedTint = new(0.4f, 0.4f, 0.45f, 1f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static MaterialPropertyBlock _tintBlock;

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

            if (!modelAnchor)
            {
                ApplyModelPose(transform, so.ModelOffset, so.ModelRotation); // built-in slot: this object is the prop
                return;
            }
            bool hasModel = so.TrayModel;
            if (hasModel)
            {
                var model = Instantiate(so.TrayModel, modelAnchor, false);
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false; // the slot's own collider takes taps
                ApplyModelPose(model.transform, so.ModelOffset, so.ModelRotation);
            }
            if (iconSprite)
            {
                iconSprite.gameObject.SetActive(!hasModel);
                iconSprite.sprite = so.Icon;
                if (so.Icon) // fit any icon size to the slot
                    iconSprite.transform.localScale = Vector3.one * (IconSize / Mathf.Max(so.Icon.bounds.size.x, so.Icon.bounds.size.y));
                if (!hasModel) ApplyModelPose(iconSprite.transform, so.ModelOffset, so.ModelRotation);
            }
        }

        /// <summary>
        /// Turns <paramref name="t"/> around its own visual centre, on its own axes, by <paramref name="euler"/>,
        /// then moves it by <paramref name="offset"/> in its parent's space (the slot). Zero values leave it untouched.
        /// </summary>
        private static void ApplyModelPose(Transform t, Vector3 offset, Vector3 euler)
        {
            if (offset == Vector3.zero && euler == Vector3.zero) return;

            var renderers = t.GetComponentsInChildren<Renderer>();
            Vector3 centre = t.position;
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                centre = b.center;
            }

            var parent = t.parent;
            Vector3 c = parent ? parent.InverseTransformPoint(centre) : centre;  // centre in parent space
            Vector3 toCentre = c - t.localPosition;
            var baseRot = t.localRotation;
            var turn = baseRot * Quaternion.Euler(euler) * Quaternion.Inverse(baseRot); // own-axes turn, in parent space
            t.localRotation = baseRot * Quaternion.Euler(euler);
            t.localPosition = c - turn * toCentre + offset;                     // keeps the centre in place, then offsets
        }

        public void UpdateVisuals(int amount, bool locked)
        {
            if (videoIcons) videoIcons.SetActive(!locked && amount <= 0);
            if (amountText && amountText.gameObject) amountText.gameObject.SetActive(!locked && amount > 0);
            if (amountText) amountText.text = amount.ToString();

            if (counterBadge) counterBadge.SetActive(!locked);
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
            SetDarkened(locked);
        }

        /// <summary>
        /// Darkens the model (prop, tray model or icon) while locked. Uses a per-renderer property block, so the
        /// shared M_Palette material is untouched; unlocking clears the block.
        /// </summary>
        private void SetDarkened(bool dark)
        {
            _tintBlock ??= new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is SpriteRenderer sprite) { sprite.color = dark ? LockedTint : Color.white; continue; }
                if (!dark) { r.SetPropertyBlock(null); continue; }
                r.GetPropertyBlock(_tintBlock);
                _tintBlock.SetColor(BaseColorId, LockedTint);
                r.SetPropertyBlock(_tintBlock);
            }
        }
    }
}
