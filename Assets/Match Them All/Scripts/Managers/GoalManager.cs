using System.Collections.Generic;
using MatchThemAll.Scripts.UI;
using UnityEngine;
using ZLinq;

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

        private int[] _initialGoalAmounts;

        private void Awake()
        {
            if (!Instance)
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
            // Find the LAST goal of this type that is not full
            // (i.e. we fill the latest partially-completed or fully-completed goal)
            for (int i = Goals.Length - 1; i >= 0; i--)
            {
                if (Goals[i].itemPrefab.ItemNameKey == releasedItem.ItemNameKey && 
                    Goals[i].amount < _initialGoalAmounts[i])
                {
                    bool wasCompleted = Goals[i].amount <= 0;
                    
                    Goals[i].amount++;
                    
                    if (wasCompleted)
                    {
                        _goalCards[i].Uncomplete();
                    }
                    
                    _goalCards[i].UpdateAmount(Goals[i].amount);
                    return; // Only restore one item
                }
            }
        }

        private void OnLevelSpawned(Level level)
        {
            Goals = level.GetGoals();

            _initialGoalAmounts = new int[Goals.Length];
            for (int i = 0; i < Goals.Length; i++)
            {
                _initialGoalAmounts[i] = Goals[i].amount;
            }

            GenerateGoalCards();
        }

        private void GenerateGoalCards()
        {
            foreach (var card in _goalCards.AsValueEnumerable().Where(card => card))
            {
                Destroy(card.gameObject);
            }
            _goalCards.Clear();

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
            // Find the FIRST incomplete goal for this item type
            for (int i = 0; i < Goals.Length; i++)
            {
                if (Goals[i].itemPrefab.ItemNameKey == item.ItemNameKey && Goals[i].amount > 0)
                {
                    Goals[i].amount--;

                    if (Goals[i].amount <= 0)
                        CompleteGoals(i);
                    else
                        _goalCards[i].UpdateAmount(Goals[i].amount);
                        
                    return; // We decreased one goal, we shouldn't decrease others
                }
            }
        }

        private void CompleteGoals(int goalIndex)
        {
            Debug.Log($"Goal {goalIndex} completed!");
            
            // Add time bonus!
            if (TimerManager.Instance)
                TimerManager.Instance.AddTime(5);

            // Spawn floating text
            if (FloatingTextSpawner.Instance && _goalCards[goalIndex])
            {
                FloatingTextSpawner.Instance.Spawn("+5s", _goalCards[goalIndex].transform.position, Color.yellow);
            }

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
