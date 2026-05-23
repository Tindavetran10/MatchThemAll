using UnityEngine;
using NaughtyAttributes; // Critical: Added missing namespace!

namespace MatchThemAll.Scripts
{
    [System.Serializable]
    public struct ItemLevelData
    {
        [SerializeField] public Item itemPrefab; // Explicit serialization
        [SerializeField] public bool isGoal;


        [Header("Data Configuration")] // Optional: Groups fields in Inspector
        // ⬜Restores the slider UI in Unity Editor
        [ValidateInput(nameof(ValidateAmount), "Amount must be a multiple of 3")] // ✅Validation callback
        [SerializeField] [Range(1, 100)] public int amount;

        public bool ValidateAmount(int value) // ✅Public method with required signature
            => value % 3 == 0;
    }
}