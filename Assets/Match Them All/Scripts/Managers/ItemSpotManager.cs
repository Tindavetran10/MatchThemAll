using System;
using System.Collections.Generic;
using ZLinq;
using UnityEngine;
using PrimeTween;

namespace MatchThemAll.Scripts
{
    public class ItemSpotManager : MonoBehaviour
    {
        public static ItemSpotManager Instance { get; private set; }
        
        [Header("Elements")]
        [SerializeField] private Transform itemSpotParent;
        private ItemSpot[] _spots;
        private int _activeSpotCount = 7;

        [Header("Settings")]
        [SerializeField] private Vector3 itemLocalPositionOnSpot;
        [SerializeField] private Vector3 itemLocalScaleOnSpot;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.15f;
        [SerializeField] private Ease animationEase = Ease.InOutCubic;

        [Header("Actions")]
        public static Action<Item> ItemPickedUp;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else Destroy(gameObject);
            
            if (!gameObject.GetComponent<MatchSystem>())
            {
                gameObject.AddComponent<MatchSystem>();
            }
            
            EventBus.Subscribe<ItemClickedEvent>(OnItemClickedEvent);
            EventBus.Subscribe<MergeStartedEvent>(OnMergeStarted);
            LevelManager.LevelSpawned += OnLevelSpawned;

            StoreSpot();
        }

        private void Start()
        {
            if (PowerupManager.Instance)
                PowerupManager.Instance.OnItemBackToGame += OnItemBackToGame;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ItemClickedEvent>(OnItemClickedEvent);
            EventBus.Unsubscribe<MergeStartedEvent>(OnMergeStarted);
            LevelManager.LevelSpawned -= OnLevelSpawned;
            if (PowerupManager.Instance) PowerupManager.Instance.OnItemBackToGame -= OnItemBackToGame;
        }

        private void OnItemBackToGame(Item releasedItem)
        {
            if (releasedItem.Spot != null)
                releasedItem.Spot.Clear();
        }

        private void OnLevelSpawned(Level level)
        {
            _activeSpotCount = level.SpotCount;
            if (_spots != null)
            {
                for (int i = 0; i < _spots.Length; i++)
                {
                    if (_spots[i])
                        _spots[i].gameObject.SetActive(i < _activeSpotCount);
                }
                
                if (itemSpotParent.TryGetComponent<ItemSpotLayout>(out var layout))
                {
                    layout.UpdateLayout();
                }
            }
            ResetSpotsAndState();
        }

        private void ResetSpotsAndState()
        {
            if (_spots != null)
            {
                foreach (var spot in _spots)
                {
                    if (spot)
                    {
                        if (spot.Item)
                        {
                            Tween.StopAll(spot.Item.transform);
                            ItemPoolManager.Instance.ReleaseItem(spot.Item);
                        }
                        spot.Clear();
                    }
                }
            }
        }

        private void OnItemClickedEvent(ItemClickedEvent evt)
        {
            var item = evt.ClickedItem;

            if (!IsFreeSpotAvailable())
            {
                Debug.Log("No free spots available!");
                return;
            }

            ItemPickedUp?.Invoke(item);
            HandleItemClick(item);
        }

        private void HandleItemClick(Item item)
        {
            if (HasItemOnBoard(item.ItemNameKey))
            {
                var idealSpot = GetIdealSpotFor(item);
                TryMoveItemToIdealSpot(item, idealSpot);
            }
            else
            {
                MoveItemToFirstFreeSpot(item);
            }
        }

        private ItemSpot GetIdealSpotFor(Item item)
        {
            int maxIndex = -1;
            for (int i = 0; i < _activeSpotCount; i++)
            {
                if (!_spots[i].IsEmpty() && _spots[i].Item.ItemNameKey == item.ItemNameKey)
                    maxIndex = i;
            }
            int idealSpotIndex = Mathf.Clamp(maxIndex + 1, 0, _activeSpotCount - 1);
            return _spots[idealSpotIndex];
        }

        private void TryMoveItemToIdealSpot(Item item, ItemSpot idealSpot)
        {
            if (!idealSpot.IsEmpty())
            {
                MoveAllItemsToTheRightFrom(idealSpot, item);
                return;
            }

            MoveItemToSpot(item, idealSpot, () => HandleItemReachedSpot(item));
        }

        private void MoveItemToSpot(Item item, ItemSpot targetSpot, Action completeCallback)
        {
            item.IsMovingToSpot = true;
            targetSpot.Populate(item);

            // Stop any existing tweens (like hint pulsing animations) to prevent conflicts
            Tween.StopAll(item.transform);

            Tween.LocalPosition(item.transform, itemLocalPositionOnSpot, animationDuration, animationEase);
            Tween.Scale(item.transform, itemLocalScaleOnSpot, animationDuration, animationEase);
            Tween.LocalRotation(item.transform, Vector3.zero, animationDuration, animationEase)
                .OnComplete(completeCallback);

            item.DisableShadow();
            item.DisablePhysics();
        }

        private void HandleItemReachedSpot(Item item, bool checkForMerge = true)
        {
            item.IsMovingToSpot = false;
            item.Spot.BumpDown();

            if (!checkForMerge) return;

            EventBus.Publish(new ItemReachedSpotEvent(item));
        }

        private void OnMergeStarted(MergeStartedEvent evt)
        {
            foreach (var item in evt.MergedItems)
                item.Spot.Clear();

            MoveAllItemsToTheLeft();
        }

        private void MoveAllItemsToTheLeft()
        {
            for (int i = 1; i < _activeSpotCount; i++)
            {
                ItemSpot spot = _spots[i];
                if (!spot.gameObject.activeInHierarchy || spot.IsEmpty()) continue;

                int targetIndex = -1;
                for (int j = 0; j < i; j++)
                {
                    if (_spots[j].IsEmpty())
                    {
                        targetIndex = j;
                        break;
                    }
                }

                if (targetIndex != -1)
                {
                    Item item = spot.Item;
                    ItemSpot targetSpot = _spots[targetIndex];
                    
                    spot.Clear();
                    MoveItemToSpot(item, targetSpot, () => HandleItemReachedSpot(item, false));
                }
            }
        }

        private void MoveAllItemsToTheRightFrom(ItemSpot idealSpot, Item itemToPlace)
        {
            int spotIndex = idealSpot.transform.GetSiblingIndex();
            
            int emptySpotIndex = -1;
            for (int i = spotIndex + 1; i < _activeSpotCount; i++)
            {
                if (_spots[i].IsEmpty())
                {
                    emptySpotIndex = i;
                    break;
                }
            }
            
            if (emptySpotIndex == -1)
            {
                for (int i = 0; i < _activeSpotCount; i++)
                {
                    if (_spots[i].IsEmpty())
                    {
                        emptySpotIndex = i;
                        break;
                    }
                }
            }
            
            if (emptySpotIndex == -1)
            {
                Debug.LogError("No empty spot found at all!");
                return;
            }

            if (emptySpotIndex > spotIndex)
            {
                for (int i = emptySpotIndex - 1; i >= spotIndex; i--)
                {
                    ItemSpot spot = _spots[i];
                    if (!spot.gameObject.activeInHierarchy || spot.IsEmpty()) continue;

                    Item item = spot.Item;
                    spot.Clear();

                    ItemSpot targetSpot = _spots[i + 1];
                    MoveItemToSpot(item, targetSpot, () => HandleItemReachedSpot(item, false));
                }
            }
            else
            {
                idealSpot = _spots[emptySpotIndex];
            }

            MoveItemToSpot(itemToPlace, idealSpot, () => HandleItemReachedSpot(itemToPlace));
        }

        private void MoveItemToFirstFreeSpot(Item item)
        {
            var targetSpot = GetFreeSpot();

            if (!targetSpot)
            {
                Debug.Log("Target spot not found — should not happen");
                return;
            }

            MoveItemToSpot(item, targetSpot, () => HandleItemReachedSpot(item));
        }

        // Helpers

        private ItemSpot GetFreeSpot() => _spots.AsValueEnumerable().Take(_activeSpotCount).FirstOrDefault(t => t.IsEmpty());

        private bool IsFreeSpotAvailable() => GetFreeSpot();

        private void StoreSpot()
        {
            _spots = new ItemSpot[itemSpotParent.childCount];
            for (int i = 0; i < itemSpotParent.childCount; i++)
                _spots[i] = itemSpotParent.GetChild(i).GetComponent<ItemSpot>();
        }

        public ItemSpot GetRandomOccupiedSpot()
        {
            List<ItemSpot> occupiedSpots = _spots.AsValueEnumerable()
                .Take(_activeSpotCount)
                .Where(t => !t.IsEmpty() && !t.Item.IsMovingToSpot)
                .ToList();
            return occupiedSpots.Count <= 0 ? null : occupiedSpots[UnityEngine.Random.Range(0, occupiedSpots.Count)];
        }

        public bool HasItemOnBoard(EItemName itemName)
        {
            for (int i = 0; i < _activeSpotCount; i++)
            {
                if (!_spots[i].IsEmpty() && _spots[i].Item.ItemNameKey == itemName) return true;
            }
            return false;
        }

        public List<Item> GetItemsOnBoard(EItemName itemName)
        {
            List<Item> items = new List<Item>();
            for (int i = 0; i < _activeSpotCount; i++)
            {
                if (!_spots[i].IsEmpty() && _spots[i].Item.ItemNameKey == itemName)
                    items.Add(_spots[i].Item);
            }
            return items;
        }

        public IEnumerable<EItemName> GetOccupiedItemTypes()
        {
            HashSet<EItemName> types = new HashSet<EItemName>();
            for (int i = 0; i < _activeSpotCount; i++)
            {
                if (!_spots[i].IsEmpty())
                    types.Add(_spots[i].Item.ItemNameKey);
            }
            return types;
        }

        public bool IsBoardFullAndNoItemsMoving()
        {
            if (GetFreeSpot() != null) return false;
            for (int i = 0; i < _activeSpotCount; i++)
            {
                if (_spots[i].Item != null && _spots[i].Item.IsMovingToSpot) return false;
            }
            return true;
        }

        public List<Item> GetBestHintItems(int count = 3)
        {
            var allItems = LevelManager.Instance.Items;
            if (allItems == null) return new List<Item>();

            EItemName? targetType = null;
            var occupiedTypes = GetOccupiedItemTypes();

            // Tier 1: Combo Nudge (Items already partially collected)
            foreach (var type in occupiedTypes)
            {
                if (allItems.AsValueEnumerable().Any(i => i != null && i.gameObject.activeInHierarchy && i.ItemNameKey == type && i.Spot == null && !i.IsMovingToSpot))
                {
                    targetType = type;
                    break;
                }
            }

            // Tier 2: Objective Nudge
            if (targetType == null)
            {
                var goals = GoalManager.Instance.Goals;
                if (goals is { Length: > 0 })
                {
                    int maxGoalAmount = -1;
                    foreach (var goal in goals)
                    {
                        if (goal.amount > maxGoalAmount && goal.amount > 0)
                        {
                            if (allItems.AsValueEnumerable().Any(i => i != null && i.gameObject.activeInHierarchy && i.ItemNameKey == goal.itemPrefab.ItemNameKey && i.Spot == null && !i.IsMovingToSpot))
                            {
                                maxGoalAmount = goal.amount;
                                targetType = goal.itemPrefab.ItemNameKey;
                            }
                        }
                    }
                }
            }

            // Tier 3: Progress Nudge (Random fallback)
            if (targetType == null)
            {
                var fallback = allItems.AsValueEnumerable().FirstOrDefault(i => i != null && i.gameObject.activeInHierarchy && i.Spot == null && !i.IsMovingToSpot);
                if (fallback)
                {
                    targetType = fallback.ItemNameKey;
                }
            }

            // Collect up to 'count' items of the chosen type, grouped by proximity
            if (targetType.HasValue)
            {
                var availableItems = allItems.AsValueEnumerable()
                    .Where(i => i && i.gameObject.activeInHierarchy && i.ItemNameKey == targetType.Value && !i.Spot && !i.IsMovingToSpot)
                    .ToList();

                if (availableItems.Count <= count)
                    return availableItems;

                var seed = availableItems[UnityEngine.Random.Range(0, availableItems.Count)];
                Vector3 seedPos = seed.transform.position;
                availableItems.Sort((a, b) =>
                    Vector3.SqrMagnitude(a.transform.position - seedPos)
                    .CompareTo(Vector3.SqrMagnitude(b.transform.position - seedPos)));
                return availableItems.GetRange(0, count);
            }

            return new List<Item>();
        }
    }
}