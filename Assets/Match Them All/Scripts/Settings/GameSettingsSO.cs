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
    }
}
