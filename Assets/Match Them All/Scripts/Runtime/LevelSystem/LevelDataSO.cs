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

        /// <summary>
        /// Snaps every item's amount to the nearest multiple of 3.
        /// Unity calls this automatically whenever a value changes in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            _cachedGoals = null; // Invalidate cache when edited in Inspector
            if (string.IsNullOrEmpty(levelId) && !string.IsNullOrEmpty(name))
                levelId = name; // auto-seed the save key from the asset filename
            if (itemData == null) return;
            for (int i = 0; i < itemData.Count; i++)
            {
                var entry = itemData[i];
                int snapped = Mathf.RoundToInt(entry.amount / 3f) * 3;
                snapped = Mathf.Max(3, snapped); // minimum 3
                if (entry.amount != snapped)
                {
                    entry.amount = snapped;
                    itemData[i] = entry;
                }
            }
        }

        // Cached to avoid rebuilding the array on every call (Vacuum, level spawn, etc.)
        private ItemLevelData[] _cachedGoals;
        
        public ItemLevelData[] GetGoals()
        {
            if (_cachedGoals != null) return _cachedGoals;
            
            var goals = new List<ItemLevelData>();
            for (int i = 0; i < itemData.Count; i++)
                if (itemData[i].isGoal)
                    goals.Add(itemData[i]);
            _cachedGoals = goals.ToArray();
            return _cachedGoals;
        }
    }
}
