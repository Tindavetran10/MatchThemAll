using UnityEngine;

namespace MatchThemAll.Scripts.Settings
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Match Them All/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        [Header("Initial Powerup Charges")]
        public int initialVacuumCount = 3;
        public int initialSpringCount = 3;
        public int initialFanCount    = 3;
        public int initialFreezeCount = 3;

        [Header("Spring Powerup Settings")]
        [Tooltip("Min/Max horizontal throw force")]
        public Vector2 springHorizontalForceRange = new Vector2(4f, 7f);
        [Tooltip("Min/Max vertical throw force")]
        public Vector2 springVerticalForceRange = new Vector2(6f, 9f);
        [Tooltip("Min/Max spin speed")]
        public Vector2 springSpinSpeedRange = new Vector2(5f, 12f);
        [Tooltip("1 for forward (positive Z), -1 for backward (negative Z)")]
        public float springThrowZDirection = 1f;

        [Header("Fan Powerup Settings")]
        public float fanMagnitude = 30f;

        [Header("Monetization & Ads")]
        [Tooltip("The amount of coins required to use the 'Save Me' (Continue) feature. " +
                 "Default is intentionally high (900) to create monetization pressure. " +
                 "Adjust to match your per-level coin rewards so the ratio feels fair to players.")]
        public int continueCoinCost = 900;
        
        [Tooltip("How many seconds are added when using the 'Save Me' feature.")]
        public int continueTimeBonus = 30;

        [Tooltip("Should players be allowed to watch a Rewarded Ad to continue if they are out of coins?")]
        public bool allowAdContinue = true;
    }
}
