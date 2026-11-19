using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Stores all data required to configure a level.
    /// Create via: Assets → Right-click → Create → Match Them All → Level Data
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "Match Them All/Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key used by the save system (keyed by identity, not array position). " +
                 "Defaults to the asset filename; leave empty to auto-fill from the asset name.")]
        [SerializeField] private string levelId;

        /// <summary>Stable identity for save data. Falls back to the asset name (= filename) if unset.</summary>
        public string Id => string.IsNullOrEmpty(levelId) ? name : levelId;

        [Header("Theme")]
        [Tooltip("Optional background art shown behind this level's node on the saga map. Null = no themed background.")]
        [SerializeField] private Sprite themeBackground;
        [Tooltip("Optional theme/chapter name shown on the saga map.")]
        [SerializeField] private string themeName;
        public Sprite ThemeBackground => themeBackground;
        public string ThemeName => string.IsNullOrEmpty(themeName) ? name : themeName;

        [Header("Settings")]
        [Tooltip("How many slots are available for matching items in this level.")]
        [Range(5, 7)]
        public int spotCount = 7;

        [Tooltip("How many seconds the player has to complete this level.")]
        [SerializeField] public int duration = 60;

        [Tooltip("Seed used to randomize item positions. Change this to get a different layout.")]
        [SerializeField] public int seed;

        [Header("Items")]
        [SerializeField] public List<ItemLevelData> itemData;

        [Header("Tutorial")]
        [SerializeField] public List<Tutorial.TutorialStep> tutorialSteps = new();

        public enum RewardCalculationMode { FixedValue, BasePlusPerStar }

        [Header("Rewards & Monetization")]
        [Tooltip("How coins are rewarded. FixedValue ignores stars, BasePlusPerStar adds coins for each star earned.")]
        public RewardCalculationMode rewardMode = RewardCalculationMode.BasePlusPerStar;

        [Tooltip("Base amount of coins awarded for completing this level (or the fixed amount if using FixedValue).")]
        public int baseCoinReward = 50;

        [ShowIf(nameof(rewardMode), RewardCalculationMode.BasePlusPerStar)]
        [Tooltip("Amount of bonus coins awarded per star earned (only used in BasePlusPerStar mode).")]
        public int coinsPerStar = 10;

        [Header("Star System Thresholds")]
        [Tooltip("Time remaining (in seconds) required to earn 3 stars. Template users can customize how this is evaluated in WinPanelManager.cs")]
        public int timeFor3Stars = 40;
        
        [Tooltip("Time remaining (in seconds) required to earn 2 stars.")]
        public int timeFor2Stars = 20;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(levelId) && !string.IsNullOrEmpty(name))
                levelId = name; // auto-seed the save key from the asset filename

            // Items merge in threes, so every amount is kept a positive multiple of 3.
            if (itemData == null) return;
            for (int i = 0; i < itemData.Count; i++)
            {
                var row = itemData[i];
                int fixedAmount = Mathf.Max(3, (row.amount + 1) / 3 * 3);
                if (row.amount == fixedAmount) continue;
                row.amount = fixedAmount;
                itemData[i] = row;
            }
        }

        /// <summary>
        /// A fresh array of the goal rows. Never cache it: GoalManager counts the amounts down in place,
        /// and because ItemLevelData is a struct the copy keeps this asset's values intact.
        /// </summary>
        public ItemLevelData[] GetGoals() => itemData.FindAll(row => row.isGoal).ToArray();
    }
}
