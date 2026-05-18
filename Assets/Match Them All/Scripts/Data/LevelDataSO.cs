using System.Collections.Generic;
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
        [Header("Settings")]
        [Tooltip("How many seconds the player has to complete this level.")]
        [SerializeField] public int duration = 60;

        [Tooltip("Seed used to randomize item positions. Change this to get a different layout.")]
        [SerializeField] public int seed;

        [Header("Items")]
        [SerializeField] public List<ItemLevelData> itemData;

        /// <summary>Returns only the entries marked as goals.</summary>
        /// <summary>
        /// Snaps every item's amount to the nearest multiple of 3.
        /// Unity calls this automatically whenever a value changes in the Inspector.
        /// </summary>
        private void OnValidate()
        {
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

        
public ItemLevelData[] GetGoals()
        {
            var goals = new List<ItemLevelData>();
            for (int i = 0; i < itemData.Count; i++)
                if (itemData[i].isGoal)
                    goals.Add(itemData[i]);
            return goals.ToArray();
        }
    }
}
