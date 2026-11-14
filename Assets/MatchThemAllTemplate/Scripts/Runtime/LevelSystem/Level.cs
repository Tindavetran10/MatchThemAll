using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>The generic level prefab: holds the ItemPlacer and exposes the active LevelDataSO's settings.</summary>
    public class Level : MonoBehaviour
    {
        [SerializeField] private ItemPlacer itemPlacer;

        private LevelDataSO _data;

        public Transform ItemParent => itemPlacer.transform;
        public int Duration => _data ? _data.duration : 0;
        public int SpotCount => _data ? _data.spotCount : 7;

        public void Initialize(LevelDataSO data) => itemPlacer.Initialize(_data = data);

        public Task InitializeAsync(LevelDataSO data) => itemPlacer.InitializeAsync(_data = data);

        public ItemLevelData[] GetGoals() => _data ? _data.GetGoals() : System.Array.Empty<ItemLevelData>();

        public List<Tutorial.TutorialStep> GetTutorialSteps() => _data ? _data.tutorialSteps : null;

        public List<Item> GetItems() => itemPlacer.GetItems();
    }
}
