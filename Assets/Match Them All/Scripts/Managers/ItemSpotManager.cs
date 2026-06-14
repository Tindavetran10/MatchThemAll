using System;
using System.Collections.Generic;
using ZLinq;
using UnityEngine;

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

        public bool IsBusy { get; private set; }

        [Header("Data")]
        private readonly Dictionary<EItemName, ItemMergeData> _itemMergeDataDictionary = new();

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.15f;
        [SerializeField] private LeanTweenType animationEase = LeanTweenType.easeInOutCubic;

        [Header("Actions")]
        public static Action<List<Item>> MergeStarted;
        public static Action<Item> ItemPickedUp;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else Destroy(gameObject);
            
            InputManager.ItemClicked += OnItemClicked;
            LevelManager.LevelSpawned += OnLevelSpawned;

            PowerupManager.ItemBackToGame += OnItemBackToGame;
            StoreSpot();
        }

        private void OnDestroy()
        {
            InputManager.ItemClicked -= OnItemClicked;
            LevelManager.LevelSpawned -= OnLevelSpawned;
            PowerupManager.ItemBackToGame -= OnItemBackToGame;
        }

        private void OnItemBackToGame(Item releasedItem)
        {
            if(!_itemMergeDataDictionary.TryGetValue(releasedItem.ItemNameKey, out var item))
                return;
            
            // remove the item from the Dictionary
            item.Remove(releasedItem);
            
            // Check if we have more items with the same
            // If not, remove the dictionary entry
            if(_itemMergeDataDictionary[releasedItem.ItemNameKey].Items.Count <= 0)
                _itemMergeDataDictionary.Remove(releasedItem.ItemNameKey);
            
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
                
                // Force the layout script to update after we've toggled active states
                if (itemSpotParent.TryGetComponent<ItemSpotLayout>(out var layout))
                {
                    layout.UpdateLayout();
                }
            }
            ResetSpotsAndState();
        }

        private void ResetSpotsAndState()
        {
            _itemMergeDataDictionary.Clear();
            IsBusy = false;
            if (_spots != null)
            {
                foreach (var spot in _spots)
                {
                    if (spot)
                    {
                        if (spot.Item)
                        {
                            Destroy(spot.Item.gameObject);
                        }
                        spot.Clear();
                    }
                }
            }
        }

        private void OnItemClicked(Item item)
        {
            if (IsBusy)
            {
                Debug.Log("ItemSpotManager is busy");
                return;
            }

            if (!IsFreeSpotAvailable())
            {
                Debug.Log("No free spots available!");
                return;
            }

            IsBusy = true;
            ItemPickedUp?.Invoke(item);
            HandleItemClick(item);
        }

        private void HandleItemClick(Item item)
        {
            if (_itemMergeDataDictionary.ContainsKey(item.ItemNameKey))
                HandleItemMergeDataFound(item);
            else
                MoveItemToFirstFreeSpot(item);
        }

        private void HandleItemMergeDataFound(Item item)
        {
            var idealSpot = GetIdealSpotFor(item);
            _itemMergeDataDictionary[item.ItemNameKey].Add(item);
            TryMoveItemToIdealSpot(item, idealSpot);
        }

        private ItemSpot GetIdealSpotFor(Item item)
        {
            List<Item> items = _itemMergeDataDictionary[item.ItemNameKey].Items;

            // Find the rightmost sibling index among all similar items — using ZLinq, zero allocation
            int maxSiblingIndex = items.AsValueEnumerable().Select(t => t.Spot.transform.GetSiblingIndex()).DefaultIfEmpty(-1).Max();

            // Clamp to prevent IndexOutOfRangeException when the last slot is occupied
            int idealSpotIndex = Mathf.Clamp(maxSiblingIndex + 1, 0, _activeSpotCount - 1);
            return _spots[idealSpotIndex];
        }

        private void TryMoveItemToIdealSpot(Item item, ItemSpot idealSpot)
        {
            if (!idealSpot.IsEmpty())
            {
                HandleIdealSpotFull(item, idealSpot);
                return;
            }

            MoveItemToSpot(item, idealSpot, () => HandleItemReachedSpot(item));
        }

        private void MoveItemToSpot(Item item, ItemSpot targetSpot, Action completeCallback)
        {
            item.IsMovingToSpot = true;
            targetSpot.Populate(item);

            LeanTween.moveLocal(item.gameObject, itemLocalPositionOnSpot, animationDuration)
                .setEase(animationEase);

            LeanTween.scale(item.gameObject, itemLocalScaleOnSpot, animationDuration)
                .setEase(animationEase);

            LeanTween.rotateLocal(item.gameObject, Vector3.zero, animationDuration)
                .setOnComplete(completeCallback);

            item.DisableShadow();
            item.DisablePhysics();
        }

        private void HandleItemReachedSpot(Item item, bool checkForMerge = true)
        {
            item.IsMovingToSpot = false;
            item.Spot.BumpDown();

            if (!checkForMerge) return;

            if (_itemMergeDataDictionary[item.ItemNameKey].CanMergeItems())
                MergeItems(_itemMergeDataDictionary[item.ItemNameKey]);
            else
                CheckForGameOver();
        }

        private void MergeItems(ItemMergeData itemMergeData)
        {
            List<Item> items = itemMergeData.Items;

            _itemMergeDataDictionary.Remove(itemMergeData.ItemName);

            foreach (var item in items)
                item.Spot.Clear();

            if (_itemMergeDataDictionary.Count <= 0)
                IsBusy = false;
            else
                MoveAllItemsToTheLeft(HandleAllItemsMovedToTheLeft);

            MergeStarted?.Invoke(items);
        }

        // Fixed: previously, the completeCallback delegate was mutated inside the loop,
        // causing each item's animation to fire ALL previously accumulated handlers.
        // Now each item gets its own isolated callback; only the last item triggers the outer callback.
        private void MoveAllItemsToTheLeft(Action completeCallback)
        {
            // Collect all pending moves before executing any, to avoid modifying the spots mid-loop
            var moves = new List<(Item item, ItemSpot target)>();

            for (int i = 3; i < _activeSpotCount; i++)
            {
                ItemSpot spot = _spots[i];
                if (!spot.gameObject.activeInHierarchy || spot.IsEmpty()) continue;

                Item item = spot.Item;
                ItemSpot targetSpot = _spots[i - 3];

                if (!targetSpot.IsEmpty())
                {
                    Debug.LogWarning($"{targetSpot.Item.ItemNameKey} is not empty! Index: {targetSpot.transform.GetSiblingIndex()}");
                    IsBusy = false;
                    return;
                }

                spot.Clear();
                moves.Add((item, targetSpot));
            }

            if (moves.Count == 0)
            {
                completeCallback?.Invoke();
                return;
            }

            for (int i = 0; i < moves.Count; i++)
            {
                // Capture loop variable for closure
                var (movedItem, targetSpot) = moves[i];
                bool isLast = i == moves.Count - 1;

                Action callback = isLast
                    ? () => { HandleItemReachedSpot(movedItem, false); completeCallback?.Invoke(); }
                    : () => HandleItemReachedSpot(movedItem, false);

                MoveItemToSpot(movedItem, targetSpot, callback);
            }
        }

        private void HandleAllItemsMovedToTheLeft() => IsBusy = false;

        private void HandleIdealSpotFull(Item item, ItemSpot idealSpot) =>
            MoveAllItemsToTheRightFrom(idealSpot, item);

        private void MoveAllItemsToTheRightFrom(ItemSpot idealSpot, Item itemToPlace)
        {
            int spotIndex = idealSpot.transform.GetSiblingIndex();

            for (int i = _activeSpotCount - 2; i >= spotIndex; i--)
            {
                ItemSpot spot = _spots[i];

                if (!spot.gameObject.activeInHierarchy || spot.IsEmpty()) continue;

                Item item = spot.Item;
                spot.Clear();

                ItemSpot targetSpot = _spots[i + 1];

                if (!targetSpot.IsEmpty())
                {
                    Debug.LogError("Target spot not empty — should not happen");
                    IsBusy = false;
                    return;
                }

                MoveItemToSpot(item, targetSpot, () => HandleItemReachedSpot(item, false));
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

            CreateItemMergeData(item);
            MoveItemToSpot(item, targetSpot, () => HandleFirstItemReachSpot(item));
        }

        private void HandleFirstItemReachSpot(Item item)
        {
            item.IsMovingToSpot = false;
            item.Spot.BumpDown();
            CheckForGameOver();
        }

        private void CheckForGameOver()
        {
            if (!GetFreeSpot())
                GameManager.Instance.SetGameState(EGameState.GAMEOVER);
            else
                IsBusy = false;
        }

        private void CreateItemMergeData(Item item) =>
            _itemMergeDataDictionary.Add(item.ItemNameKey, new ItemMergeData(item));

        // Plain for-loop — avoids the delegate allocation that FirstOrDefault creates each call
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
            List<ItemSpot> occupiedSpots = _spots.AsValueEnumerable().Take(_activeSpotCount).Where(t => !t.IsEmpty()).ToList();
            return occupiedSpots.Count <= 0 ? null : occupiedSpots[UnityEngine.Random.Range(0, occupiedSpots.Count)];
        }

        public List<Item> GetBestHintItems(int count = 3)
        {
            var allItems = LevelManager.Instance.Items;
            if (allItems == null) return new List<Item>();

            EItemName? targetType = null;

            // Tier 1: Combo Nudge (Items already partially collected)
            foreach (var kvp in _itemMergeDataDictionary.AsValueEnumerable().Where(kvp => kvp.Value.Items.Count > 0).Where(kvp => allItems.AsValueEnumerable().Any(i => i != null && i.ItemNameKey == kvp.Key && i.Spot == null && !i.IsMovingToSpot)))
            {
                targetType = kvp.Key;
                break;
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
                            if (allItems.AsValueEnumerable().Any(i => i != null && i.ItemNameKey == goal.itemPrefab.ItemNameKey && i.Spot == null && !i.IsMovingToSpot))
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
                var fallback = allItems.AsValueEnumerable().FirstOrDefault(i => i != null && i.Spot == null && !i.IsMovingToSpot);
                if (fallback)
                {
                    targetType = fallback.ItemNameKey;
                }
            }

            // Collect up to 'count' items of the chosen type, grouped by proximity
            if (targetType.HasValue)
            {
                var availableItems = allItems.AsValueEnumerable()
                    .Where(i => i && i.ItemNameKey == targetType.Value && !i.Spot && !i.IsMovingToSpot)
                    .ToList();

                if (availableItems.Count <= count)
                    return availableItems;

                // Find the cluster of 'count' items with the minimum total distance to their center
                List<Item> bestCluster = null;
                float minDistanceSum = float.MaxValue;

                foreach (var seed in availableItems)
                {
                    // Order by distance to the seed item
                    var cluster = availableItems.AsValueEnumerable()
                        .OrderBy(i => Vector3.Distance(seed.transform.position, i.transform.position))
                        .Take(count)
                        .ToList();

                    // Calculate total distance spread in this cluster
                    float distanceSum = cluster.AsValueEnumerable().Sum(item => Vector3.Distance(seed.transform.position, item.transform.position));

                    if (distanceSum < minDistanceSum)
                    {
                        minDistanceSum = distanceSum;
                        bestCluster = cluster;
                    }
                }

                return bestCluster ?? availableItems.AsValueEnumerable().Take(count).ToList();
            }

            return new List<Item>();
        }
    }
}