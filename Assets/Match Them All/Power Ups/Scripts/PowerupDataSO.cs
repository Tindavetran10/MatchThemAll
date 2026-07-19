using UnityEngine;
using NaughtyAttributes;

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
        [Min(0)] public int unlockLevel;          // player level (currentLevelIndex) required
        [Min(0)] public int defaultAmount;        // first-launch charge grant

        [Header("Economy")]
        public ECurrency buyCurrency = ECurrency.Coins;
        [Min(0)] public int buyCost = 50;          // spent when activating with 0 charges

        [Header("Runtime")]
        [SerializeField] private GameObject uiPrefab;          // the button prefab (carries PowerupUI)
        [SerializeReference] private PowerupEffect effect;     // polymorphic behavior

        // ── Public read-only accessors ──────────────────────────────────────
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public GameObject UIPrefab => uiPrefab;
        public PowerupEffect Effect => effect;

        /// <summary>True if this power-up is locked for the given player level.</summary>
        public bool IsLockedAt(int playerLevelIndex) => playerLevelIndex < unlockLevel;
    }
}
