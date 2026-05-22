using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class PowerupManager : MonoBehaviour
    {
        [Header("Actions")]
        public static Action<Item> ItemPickup;
        
        [Button]
        private void VacuumPower()
        {
            Item[] items = LevelManager.Instance.Items;
            ItemLevelData[] goals = GoalManager.Instance.Goals;
            
            ItemLevelData? greatestGoal = GetGreatestGoal(goals);
            
            if (greatestGoal == null)
                return;
            
            ItemLevelData goal =  (ItemLevelData) greatestGoal;
            List<Item> itemToCollect = new List<Item>();

            foreach (var item in items)
            {
                if (item.ItemNameKey == goal.itemPrefab.ItemNameKey)
                {
                    itemToCollect.Add(item);
                    if(itemToCollect.Count >= 3)
                        break;
                }
            }
            
            for (int i = itemToCollect.Count-1; i >= 0; i--)
            {
                ItemPickup.Invoke(itemToCollect[i]);
                Destroy(itemToCollect[i].gameObject);
            }
        }

        private ItemLevelData? GetGreatestGoal(ItemLevelData[] goals)
        {
            int max = 0;
            int goalIndex = -1;

            for (int i = 0; i < goals.Length; i++)
            {
                if(goals[i].amount >= max)
                {
                    max = goals[i].amount;
                    goalIndex = i;
                }
            }
            
            if(goals.Length <= -1)
                return null;
            
            return goals[goalIndex];
        }
    }
}