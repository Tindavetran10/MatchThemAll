using System.Collections.Generic;
using MatchThemAll.Scripts;
using MatchThemAll.Scripts.UI;
using UnityEngine;

public class GoalManager : MonoBehaviour
{
    [Header(" References ")]
    [SerializeField] private Transform goalCardParent;
    [SerializeField] private GoalCard goalCardPrefab;
    
    [Header(" Data ")]
    private ItemLevelData[] _goals;
    private List<GoalCard> goalCards = new List<GoalCard>();
    
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
        for (var i = 0; i < _goals.Length; i++)
        {
            if(!_goals[i].itemPrefab.ItemNameKey.Equals(item.ItemNameKey))
                continue;
            
            _goals[i].amount--;

            if (_goals[i].amount <= 0)
                CompleteGoals(i);
            else
                goalCards[i].UpdateAmount(_goals[i].amount);
            
            break;
        }
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
            if(_goals[i].amount > 0)
                return;
        }
        
        Debug.Log("Level Complete!");
    }
}
