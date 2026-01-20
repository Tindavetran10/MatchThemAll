using MatchThemAll.Scripts;
using UnityEngine;

public class GoalManager : MonoBehaviour
{
    [Header(" Data ")]
    private ItemLevelData[] _goals;
    
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
            
            break;
        }
    }
    
    private void CompleteGoals(int goalIndex)
    {
        Debug.Log($"Goal {goalIndex} completed!");
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
