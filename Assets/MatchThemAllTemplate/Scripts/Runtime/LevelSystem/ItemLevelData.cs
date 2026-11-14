using NaughtyAttributes;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// One row of a level's item list. <see cref="amount"/> is the goal size (a multiple of 3);
    /// the board spawns amount × multiplier copies so extra sets can act as decoys.
    /// </summary>
    [System.Serializable]
    public struct ItemLevelData
    {
        public Item itemPrefab;
        [Tooltip("Show this item as a goal card. Non-goal items are filler.")]
        public bool isGoal;
        [Tooltip("Board copies = amount × multiplier (values below 1 count as 1).")]
        public int multiplier;

        [ValidateInput(nameof(ValidateAmount), "Amount must be a multiple of 3")]
        [Range(1, 100)] public int amount;

        public bool ValidateAmount(int value) => value % 3 == 0;
    }
}
