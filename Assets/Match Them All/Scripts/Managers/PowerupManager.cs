using System;
using System.Collections.Generic;
using ZLinq;
using Match_Them_All.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using NaughtyAttributes;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class PowerupManager : MonoBehaviour
    {
        [Header("Vacuum Elements")]
        [SerializeField] private Vacuum vacuum;
        [SerializeField] private Transform vacuumSuckPosition;
        
        [Header("Fan Settings")]
        [SerializeField] private float fanMagnitude;

        [Header("Initial Powerup Charges")]
        [SerializeField] private int initialVacuumCount = 3;
        [SerializeField] private int initialSpringCount = 3;
        [SerializeField] private int initialFanCount    = 3;
        [SerializeField] private int initialFreezeCount = 3;

        [Header("Settings")] 
        private bool _isBusy;
        private bool _vacuumRequested;

        private int _vacuumItemToCollect;
        private int _vacuumCounter;

        // Optimized: pre-allocated list container reused on every call to avoid runtime list creations and GC allocations
        private readonly List<Item> _itemsToCollect = new(3);
        
        [Header("Actions")]
        public static Action<Item> ItemPickup;
        public static Action<Item> ItemBackToGame;

        [Header("Data")]
        private int _vacuumPuCount;
        private int _springPuCount;
        private int _fanPuCount;
        private int _freezePuCount;

        private void Awake()
        {
            LoadData();
            
            Vacuum.Started += OnVacuumStarted;
            InputManager.PowerupClicked += OnPowerupClicked;
        }

        private void OnDestroy()
        {
            Vacuum.Started -= OnVacuumStarted;
            InputManager.PowerupClicked -= OnPowerupClicked;
        }

        private void OnPowerupClicked(Powerup powerup)
        {
            if(_isBusy) 
                return;

            switch (powerup.Type)
            {
                case EPowerupType.Vacuum:
                    HandleVacuumClicked();
                    UpdateVacuumVisuals();
                    break;
                case EPowerupType.Spring:
                    SpringPowerup();
                    break;
                case EPowerupType.Fan:
                    FanPowerup();
                    break;
                case EPowerupType.FreezeGun:
                    FreezePowerup();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Vacuum Powerup
        private void HandleVacuumClicked()
        {
            _vacuumRequested = true;

            if (_vacuumPuCount <= 0)
            {
                _vacuumPuCount = 3;
                SaveData();
            }
            else
            {
                _isBusy = true;

                _vacuumPuCount--;
                SaveData();
                vacuum.Play();
            }
            
        }

        private void OnVacuumStarted()
        {
            if (!_vacuumRequested) return;
            _vacuumRequested = false;
            VacuumPowerup();
        }

        [Button]
        private void VacuumPowerup()
        {
            var items = LevelManager.Instance.Items;
            ItemLevelData[] goals = GoalManager.Instance.Goals;
            
            int greatestGoalIndex = GetGreatestGoalIndex(goals);
            if (greatestGoalIndex == -1)
                return;
            
            ItemLevelData goal = goals[greatestGoalIndex];
            
            //_isBusy = true;
            _vacuumCounter = 0;
            _itemsToCollect.Clear();

            if (items != null)
            {
                foreach (var item in items.AsValueEnumerable().Where(item => item != null).Where(item => item.ItemNameKey == goal.itemPrefab.ItemNameKey))
                {
                    _itemsToCollect.Add(item);
                    if (_itemsToCollect.Count >= 3)
                        break;
                }
            }
            
            _vacuumItemToCollect = _itemsToCollect.Count;

            if (_vacuumItemToCollect == 0)
            {
                _isBusy = false;
                return;
            }

            foreach (var itemToCollect in _itemsToCollect.AsValueEnumerable().Where(itemToCollect => itemToCollect != null))
            {
                itemToCollect.DisablePhysics();

                // 1. Vortex move to vacuum suck position
                var collect = itemToCollect;
                LeanTween.move(itemToCollect.gameObject, vacuumSuckPosition.position, 0.5f)
                    .setEase(LeanTweenType.easeInCubic)
                    .setOnComplete(() => ItemReachedVacuum(collect));

                // 2. Shrink down to 0
                LeanTween.scale(itemToCollect.gameObject, Vector3.zero, 0.5f)
                    .setEase(LeanTweenType.easeInCubic);

                // 3. Dynamic spin for aesthetic vortex feel
                LeanTween.rotateAroundLocal(itemToCollect.gameObject, Vector3.up, 720f, 0.5f)
                    .setEase(LeanTweenType.easeInCubic);
            }
            
            for (int i = _itemsToCollect.Count - 1; i >= 0; i--)
            {
                Item itemToCollect = _itemsToCollect[i];
                if (itemToCollect == null) continue;
                ItemPickup?.Invoke(itemToCollect);
            }
        }

        private void ItemReachedVacuum(Item item)
        {
            _vacuumCounter++;
            if (_vacuumCounter >= _vacuumItemToCollect)
                _isBusy = false;
            Destroy(item.gameObject);
        }
        
        private void UpdateVacuumVisuals() => vacuum.UpdateVisuals(_vacuumPuCount);
        #endregion

        #region Spring Powerup
        [Button]
        private void SpringPowerup()
        {
            ItemSpot spot = ItemSpotManager.Instance.GetRandomOccupiedSpot();
            
            if(spot == null)
                return;
            _isBusy = true;

            Item itemToRelease = spot.Item;
            
            spot.Clear();
            
            itemToRelease.UnassignSpot();
            itemToRelease.EnablePhysics();
            
            itemToRelease.transform.parent = LevelManager.Instance.ItemParent;
            itemToRelease.transform.localPosition = Vector3.up * 3f;
            itemToRelease.transform.localScale = Vector3.one;
            
            ItemBackToGame?.Invoke(itemToRelease);
            _isBusy = false;
        }
        #endregion

        #region Fan Powerup
        [Button]
        private void FanPowerup()
        {
            Item[] items = LevelManager.Instance.Items.ToArray();

            foreach (var item in items) 
                item.ApplyRandomForce(fanMagnitude);
        }
        

        #endregion

        #region Freeze Powerup
        [Button]
        private static void FreezePowerup() => 
            TimerManager.Instance.FreezeTimer();

        #endregion
        
        // Optimized: Returns clean goal array index to completely avoid Nullable struct boxing overhead
        private static int GetGreatestGoalIndex(ItemLevelData[] goals)
        {
            if (goals == null || goals.Length == 0)
                return -1;

            int max = 0;
            int goalIndex = -1;

            for (int i = 0; i < goals.Length; i++)
            {
                if (goals[i].amount > max)
                {
                    max = goals[i].amount;
                    goalIndex = i;
                }
            }
            
            return goalIndex;
        }
        
        private void LoadData()
        {
            var data = SaveManager.Load();

            // If count is 0 (first launch or wiped save), seed with the configured initial value
            _vacuumPuCount = data.vacuumCount > 0 ? data.vacuumCount : initialVacuumCount;
            _springPuCount = data.springCount > 0 ? data.springCount : initialSpringCount;
            _fanPuCount    = data.fanCount    > 0 ? data.fanCount    : initialFanCount;
            _freezePuCount = data.freezeCount > 0 ? data.freezeCount : initialFreezeCount;

            UpdateVacuumVisuals();
        }

        private void SaveData()
        {
            var data = SaveManager.Load();
            data.vacuumCount = _vacuumPuCount;
            data.springCount = _springPuCount;
            data.fanCount    = _fanPuCount;
            data.freezeCount = _freezePuCount;
            SaveManager.Save(data);
        }
    }
}