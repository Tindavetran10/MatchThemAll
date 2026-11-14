using UnityEngine;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// Single source of truth for one power-up: identity, progression, economy,
    /// UI binding, and the polymorphic effect. One asset per power-up.
    /// Adding a power-up = new asset (+ new PowerupEffect subclass if the logic is new).
    /// </summary>
    [CreateAssetMenu(menuName = "Match Them All/Powerup Data")]
    public class PowerupDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;                         // unique key, e.g. "vacuum" — replaces EPowerupType
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Progression")]
        [Min(0)] public int order;                // left→right display order
        [Tooltip("0-based level index: 0 = from the start, 4 = unlocks on the level players see as 5. " +
                 "The Power-up Manager window edits this in player-facing numbers.")]
        [Min(0)] public int unlockLevel;          // player level (currentLevelIndex) required
        [Min(0)] public int defaultAmount;        // first-launch charge grant

        [Header("Economy")]
        public ECurrency buyCurrency = ECurrency.Coins;
        [Min(0)] public int buyCost = 50;          // spent when activating with 0 charges

        [Header("Runtime")]
        [Tooltip("Slot Prefab: the tray slot spawned for this power-up (PowerupSlot, or a built-in Slot_* prefab).")]
        [SerializeField] private GameObject uiPrefab;          // slot prefab (name kept for serialization)
        [Tooltip("Optional 3D model shown in a PowerupSlot. Empty = the icon is shown instead.")]
        [SerializeField] private GameObject trayModel;
        [Tooltip("Moves the model inside its tray slot: X right, Y up, Z away from the camera.")]
        [SerializeField] private Vector3 modelOffset;
        [Tooltip("Turns the model around its own centre, on its own axes (degrees).")]
        [SerializeField] private Vector3 modelRotation;
        [SerializeReference] private PowerupEffect effect;     // polymorphic behavior

        [Header("Sound")]
        [Tooltip("Played when the power-up activates. Empty = silent.")]
        [SerializeField] private AudioClip activateSound;
        [Range(0f, 1f)] [SerializeField] private float activateVolume = 1f;
        [Range(0.5f, 2f)] [SerializeField] private float activatePitch = 1f;

        [Header("VFX")]
        [Tooltip("Plays once when the powerup activates. Required for VFX; null = no effect.")]
        [SerializeField] private ParticleSystem activateVfx;
        [Tooltip("Plays once when the effect resolves (e.g. portal collapse). Optional.")]
        [SerializeField] private ParticleSystem endVfx;

        // ── Public read-only accessors ──────────────────────────────────────
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public GameObject UIPrefab => uiPrefab;
        public GameObject TrayModel => trayModel;
        public Vector3 ModelOffset => modelOffset;
        public Vector3 ModelRotation => modelRotation;
        public AudioClip ActivateSound => activateSound;
        public float ActivateVolume => activateVolume;
        public float ActivatePitch => activatePitch;
        public PowerupEffect Effect => effect;
        public ParticleSystem ActivateVfx => activateVfx;
        public ParticleSystem EndVfx => endVfx;

        /// <summary>True if this power-up is locked for the given player level.</summary>
        public bool IsLockedAt(int playerLevelIndex) => playerLevelIndex < unlockLevel;
    }
}
