using System.Collections.Generic;
using MatchThemAll.Scripts.UI;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class GoalManager : MonoBehaviour
    {
        public static GoalManager Instance;
        
        [Header(" References ")]
        [SerializeField] private Transform goalCardParent;
        [SerializeField] private GoalCard goalCardPrefab;

        private readonly List<GoalCard> _goalCards = new();
        
        [field: Header(" Data ")]
        public ItemLevelData[] Goals { get; private set; }

        // O(1) lookup: item name -> index into _goals / goalCards
        private readonly Dictionary<EItemName, int> _goalIndexByName = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else Destroy(gameObject);
            
            LevelManager.LevelSpawned += OnLevelSpawned;
            ItemSpotManager.ItemPickedUp += OnItemPickedUp;
            
            PowerupManager.ItemPickup += OnItemPickedUp;
            PowerupManager.ItemBackToGame += OnItemBackToGame;
        }

        private void OnDestroy()
        {
            LevelManager.LevelSpawned -= OnLevelSpawned;
            ItemSpotManager.ItemPickedUp -= OnItemPickedUp;
            
            PowerupManager.ItemPickup -= OnItemPickedUp;
            PowerupManager.ItemBackToGame -= OnItemBackToGame;
        }

        private void OnItemBackToGame(Item releasedItem)
        {
            // Loop through our goals
            // if this item is a goal
            // Increase the goal amount
            // Update the cards

            for (int i = 0; i < Goals.Length; i++)
            {
                if(Goals[i].itemPrefab.ItemNameKey != releasedItem.ItemNameKey)
                    continue;

                Goals[i].amount++;
                _goalCards[i].UpdateAmount(Goals[i].amount);
            }
        }

        private void OnLevelSpawned(Level level)
        {
            Goals = level.GetGoals();

            // Build lookup dictionary so OnItemPickedUp is O(1) instead of O(n)
            _goalIndexByName.Clear();
            for (int i = 0; i < Goals.Length; i++)
                _goalIndexByName[Goals[i].itemPrefab.ItemNameKey] = i;

            GenerateGoalCards();
        }

        private void GenerateGoalCards()
        {
            foreach (var goal in Goals)
                GenerateGoalCard(goal);
        }

        private void GenerateGoalCard(ItemLevelData goal)
        {
            var cardInstance = Instantiate(goalCardPrefab, goalCardParent);
            cardInstance.Configure(goal.amount, goal.itemPrefab.Icon);
            _goalCards.Add(cardInstance);
        }

        private void OnItemPickedUp(Item item)
        {
            // O(1) dictionary lookup instead of linear scan
            if (!_goalIndexByName.TryGetValue(item.ItemNameKey, out int i))
                return;

            Goals[i].amount--;

            if (Goals[i].amount <= 0)
                CompleteGoals(i);
            else
                _goalCards[i].UpdateAmount(Goals[i].amount);
        }

        private void CompleteGoals(int goalIndex)
        {
            Debug.Log($"Goal {goalIndex} completed!");
            _goalCards[goalIndex].Complete();
            CheckForLevelComplete();
        }

        private void CheckForLevelComplete()
        {
            for (var i = 0; i < Goals.Length; i++)
                if (Goals[i].amount > 0) return;

            GameManager.Instance.SetGameState(EGameState.LEVELCOMPLETE);
        }
    }
}
