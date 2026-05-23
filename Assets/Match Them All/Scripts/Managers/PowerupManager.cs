using System;
using System.Collections.Generic;
using Match_Them_All.Scripts.Power_Ups;
using NaughtyAttributes;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class PowerupManager : MonoBehaviour
    {
        [Header("Vacuum Elements")]
        [SerializeField] private Vacuum vacuum;
        [SerializeField] private Transform vacuumSuckPosition;

        [Header("Settings")] 
        private bool _isBusy;
        private bool _vacuumRequested;

        private int _vacuumItemToCollect;
        private int _vacuumCounter;
        
        [Header("Actions")]
        public static Action<Item> ItemPickup;

        private void Awake()
        {
            Vacuum.Started += OnVacuumStarted;
            InputManager.powerupClicked += OnPowerupClicked;
        }

        private void OnDestroy()
        {
            Vacuum.Started -= OnVacuumStarted;
            InputManager.powerupClicked -= OnPowerupClicked;
        }

        private void OnPowerupClicked(Powerup powerup)
        {
            if(_isBusy) 
                return;

            switch (powerup.Type)
            {
                case EPowerupType.Vacuum:
                    HandleVacuumClicked();
                    break;
                case EPowerupType.Spring:
                case EPowerupType.Fan:
                case EPowerupType.FreezeGun:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleVacuumClicked()
        {
            _vacuumRequested = true;
            vacuum.Play();
        }

        private void OnVacuumStarted()
        {
            if (!_vacuumRequested) return;
            _vacuumRequested = false;
            VacuumPower();
        }

        [Button]
        private void VacuumPower()
        {
            Item[] items = LevelManager.Instance.Items;
            ItemLevelData[] goals = GoalManager.Instance.Goals;
            
            ItemLevelData? greatestGoal = GetGreatestGoal(goals);
            
            if (greatestGoal == null)
                return;
            
            ItemLevelData goal = (ItemLevelData) greatestGoal;
            
            _isBusy = true;
            _vacuumCounter = 0;
            
            List<Item> itemsToCollect = new List<Item>();

            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item == null) continue;
                    if (item.ItemNameKey == goal.itemPrefab.ItemNameKey)
                    {
                        itemsToCollect.Add(item);
                        if(itemsToCollect.Count >= 3)
                            break;
                    }
                }
            }
            
            _vacuumItemToCollect = itemsToCollect.Count;

            if (_vacuumItemToCollect == 0)
            {
                _isBusy = false;
                return;
            }

            for (int i = 0; i < itemsToCollect.Count; i++)
            {
                if (itemsToCollect[i] == null) continue;
                itemsToCollect[i].DisablePhysics();

                Item itemToCollect = itemsToCollect[i];
                
                LeanTween.move(itemsToCollect[i].gameObject, vacuumSuckPosition.position, .5f)
                    .setEase(LeanTweenType.easeInCubic)
                    .setOnComplete(() => ItemReachedVacuum(itemToCollect));

                LeanTween.scale(itemsToCollect[i].gameObject, Vector3.zero, .5f);
            }
            
            for (int i = itemsToCollect.Count - 1; i >= 0; i--)
            {
                if (itemsToCollect[i] == null) continue;
                ItemPickup.Invoke(itemsToCollect[i]);
                //Destroy(itemToCollect[i].gameObject);
            }
        }

        private void ItemReachedVacuum(Item item)
        {
            _vacuumCounter++;
            if (_vacuumCounter >= _vacuumItemToCollect)
                _isBusy = false;
            Destroy(item.gameObject);
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