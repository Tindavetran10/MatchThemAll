using UnityEngine;

namespace MatchThemAll.Scripts.Settings
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Match Them All/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        // ponytail: powerup initial-charges + spring/fan tuning moved to PowerupDataSO / *Effect (Stage 1-2).

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
