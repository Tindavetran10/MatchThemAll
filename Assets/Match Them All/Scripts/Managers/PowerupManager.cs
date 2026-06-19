using System;
using System.Collections.Generic;
using ZLinq;
using Match_Them_All.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using NaughtyAttributes;
using UnityEngine;
using PrimeTween;

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

        private Powerup[] _powerupUIElements;

        private void Awake()
        {
            _powerupUIElements = FindObjectsByType<Powerup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

            if (GameManager.Instance.State != EGameState.GAME)
                return;

            if (ItemSpotManager.Instance.IsBusy)
                return;

            if (!CanUsePowerup(powerup.Type))
                return;

            // Try to use a charge, if not, try to buy one
            if (!TryUsePowerupCharge(powerup.Type))
            {
                const int powerupCost = 50;
                if (SaveManager.SpendCoins(powerupCost))
                {
                    // Successfully bought
                }
                else
                {
                    // Not enough coins
                    Debug.Log($"Not enough coins to buy {powerup.Type}! Needs {powerupCost}.");
                    return;
                }
            }

            switch (powerup.Type)
            {
                case EPowerupType.Vacuum:
                    _vacuumRequested = true;
                    _isBusy = true;
                    vacuum.Play();
                    UpdateAllPowerupVisuals();
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

        private bool CanUsePowerup(EPowerupType type)
        {
            switch (type)
            {
                case EPowerupType.Vacuum:
                    return true;
                case EPowerupType.Spring:
                    return ItemSpotManager.Instance.GetRandomOccupiedSpot() != null;
                case EPowerupType.Fan:
                    return true;
                case EPowerupType.FreezeGun:
                    return !TimerManager.Instance.IsFrozen;
                default:
                    return false;
            }
        }

        private bool TryUsePowerupCharge(EPowerupType type)
        {
            switch (type)
            {
                case EPowerupType.Vacuum:
                    if (_vacuumPuCount <= 0) return false;
                    _vacuumPuCount--;
                    SaveData();
                    UpdateAllPowerupVisuals();
                    return true;
                case EPowerupType.Spring:
                    if (_springPuCount <= 0) return false;
                    _springPuCount--;
                    SaveData();
                    UpdateAllPowerupVisuals();
                    return true;
                case EPowerupType.Fan:
                    if (_fanPuCount <= 0) return false;
                    _fanPuCount--;
                    SaveData();
                    UpdateAllPowerupVisuals();
                    return true;
                case EPowerupType.FreezeGun:
                    if (_freezePuCount <= 0) return false;
                    _freezePuCount--;
                    SaveData();
                    UpdateAllPowerupVisuals();
                    return true;
                default:
                    return false;
            }
        }

        #region Vacuum Powerup

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
            {
                _isBusy = false;
                return;
            }
            
            ItemLevelData goal = goals[greatestGoalIndex];
            
            //_isBusy = true;
            _vacuumCounter = 0;
            _itemsToCollect.Clear();

            if (items != null)
            {
                foreach (var item in items.AsValueEnumerable().Where(item => item && item.gameObject.activeInHierarchy).Where(item => item.ItemNameKey == goal.itemPrefab.ItemNameKey && item.Spot == null && !item.IsMovingToSpot))
                {
                    _itemsToCollect.Add(item);
                    if (_itemsToCollect.Count >= 3)
                        break;
                }
            }
            
            _vacuumItemToCollect = _itemsToCollect.Count;

            if (_vacuumItemToCollect == 0)
            {
                // Delay clearing busy state until the visual animation finishes (~2.5s)
                Tween.Delay(2.5f).OnComplete(() => _isBusy = false);
                return;
            }

            foreach (var itemToCollect in _itemsToCollect.AsValueEnumerable().Where(itemToCollect => itemToCollect != null))
            {
                itemToCollect.DisablePhysics();

                // 1. Vortex move to vacuum suck position
                var collect = itemToCollect;
                Tween.Position(itemToCollect.transform, vacuumSuckPosition.position, 0.5f, Ease.InCubic)
                    .OnComplete(() => ItemReachedVacuum(collect));

                // 2. Shrink down to 0
                Tween.Scale(itemToCollect.transform, Vector3.zero, 0.5f, Ease.InCubic);

                // 3. Dynamic spin for aesthetic vortex feel
                Tween.LocalEulerAngles(itemToCollect.transform, itemToCollect.transform.localEulerAngles,
                    itemToCollect.transform.localEulerAngles + new Vector3(0, 720f, 0), 0.5f, Ease.InCubic);
            }
            
            for (int i = _itemsToCollect.Count - 1; i >= 0; i--)
            {
                Item itemToCollect = _itemsToCollect[i];
                if (!itemToCollect) continue;
                ItemPickup?.Invoke(itemToCollect);
            }
            
            // Wait for the full vacuum animation to complete before allowing another powerup
            Tween.Delay(2.5f).OnComplete(() => _isBusy = false);
        }

        private void ItemReachedVacuum(Item item)
        {
            _vacuumCounter++;
            ItemPoolManager.Instance.ReleaseItem(item);
        }
        private void UpdateAllPowerupVisuals()
        {
            foreach (var pu in _powerupUIElements)
            {
                switch (pu.Type)
                {
                    case EPowerupType.Vacuum: pu.UpdateVisuals(_vacuumPuCount); break;
                    case EPowerupType.Spring: pu.UpdateVisuals(_springPuCount); break;
                    case EPowerupType.Fan: pu.UpdateVisuals(_fanPuCount); break;
                    case EPowerupType.FreezeGun: pu.UpdateVisuals(_freezePuCount); break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        #endregion

        #region Spring Powerup
        [Button]
        private void SpringPowerup()
        {
            ItemSpot spot = ItemSpotManager.Instance.GetRandomOccupiedSpot();
            
            if(!spot)
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
            foreach (var item in LevelManager.Instance.Items) 
            {
                if (item && item.gameObject.activeInHierarchy)
                    item.ApplyRandomForce(fanMagnitude);
            }
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

            UpdateAllPowerupVisuals();
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