using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class Level : MonoBehaviour
    {
        [Header(" Elements ")] [SerializeField]
        private ItemPlacer itemPlacer;

        public ItemLevelData[] GetGoals() => itemPlacer.GetGoals();
    }
}