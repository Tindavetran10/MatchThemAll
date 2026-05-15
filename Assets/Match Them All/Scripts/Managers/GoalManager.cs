using System.Collections.Generic;
using MatchThemAll.Scripts;
using MatchThemAll.Scripts.UI;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class GoalManager : MonoBehaviour
    {
        [Header(" References ")]
        [SerializeField] private Transform goalCardParent;
        [SerializeField] private GoalCard goalCardPrefab;

        [Header(" Data ")]
        private ItemLevelData[] _goals;
        private List<GoalCard> goalCards = new List<GoalCard>();

        // O(1) lookup: item name -> index into _goals / goalCards
        private readonly Dictionary<EItemName, int> _goalIndexByName = new Dictionary<EItemName, int>();

        private void Awake()
        {
            LevelManager.levelSpawned += OnLevelSpawned;
            ItemSpotManager.ItemPickedUp += OnItemPickedUp;
        }

        private void OnDestroy()
        {
            LevelManager.levelSpawned -= OnLevelSpawned;
            ItemSpotManager.ItemPickedUp -= OnItemPickedUp;
        }

        private void OnLevelSpawned(Level level)
        {
            _goals = level.GetGoals();

            // Build lookup dictionary so OnItemPickedUp is O(1) instead of O(n)
            _goalIndexByName.Clear();
            for (int i = 0; i < _goals.Length; i++)
                _goalIndexByName[_goals[i].itemPrefab.ItemNameKey] = i;

            GenerateGoalCards();
        }

        private void GenerateGoalCards()
        {
            foreach (var goal in _goals)
                GenerateGoalCard(goal);
        }

        private void GenerateGoalCard(ItemLevelData goal)
        {
            var cardInstance = Instantiate(goalCardPrefab, goalCardParent);
            cardInstance.Configure(goal.amount, goal.itemPrefab.Icon);
            goalCards.Add(cardInstance);
        }

        private void OnItemPickedUp(Item item)
        {
            // O(1) dictionary lookup instead of linear scan
            if (!_goalIndexByName.TryGetValue(item.ItemNameKey, out int i))
                return;

            _goals[i].amount--;

            if (_goals[i].amount <= 0)
                CompleteGoals(i);
            else
                goalCards[i].UpdateAmount(_goals[i].amount);
        }

        private void CompleteGoals(int goalIndex)
        {
            Debug.Log($"Goal {goalIndex} completed!");
            goalCards[goalIndex].Complete();
            CheckForLevelComplete();
        }

        private void CheckForLevelComplete()
        {
            for (var i = 0; i < _goals.Length; i++)
            {
                if (_goals[i].amount > 0)
                    return;
            }

            GameManager.instance.SetGameState(EGameState.LEVELCOMPLETE);
        }
    }
}
