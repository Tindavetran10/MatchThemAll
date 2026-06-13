using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class Level : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private ItemPlacer itemPlacer;

        public Transform ItemParent => itemPlacer.transform;

        private LevelDataSO _data;

        /// <summary>
        /// Called by LevelManager immediately after instantiation.
        /// Sets the level's data and triggers runtime item spawning.
        /// </summary>
        public void Initialize(LevelDataSO data)
        {
            _data = data;
            itemPlacer.Initialize(data);
        }

        public int Duration => _data ? _data.duration : 0;

        public ItemLevelData[] GetGoals() =>
            _data ? _data.GetGoals() : System.Array.Empty<ItemLevelData>();

        public System.Collections.Generic.List<Tutorial.TutorialStep> GetTutorialSteps() =>
            _data ? _data.tutorialSteps : null;

        public System.Collections.Generic.List<Item> GetItems() => itemPlacer.GetItems();
    }
}