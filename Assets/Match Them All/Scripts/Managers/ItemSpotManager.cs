using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class ItemSpotManager : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private Transform itemSpotParent;
        private ItemSpot[] _spots;

        [Header("Settings")]
        [SerializeField] private Vector3 itemLocalPositionOnSpot;
        [SerializeField] private Vector3 itemLocalScaleOnSpot;

        private bool _isBusy;

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
            InputManager.ItemClicked += OnItemClicked;
            StoreSpot();
        }

        private void OnDestroy() => InputManager.ItemClicked -= OnItemClicked;

        private void OnItemClicked(Item item)
        {
            if (_isBusy)
            {
                Debug.Log("ItemSpotManager is busy");
                return;
            }

            if (!IsFreeSpotAvailable())
            {
                Debug.Log("No free spots available!");
                return;
            }

            _isBusy = true;
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
            List<ItemSpot> itemSpots = items.Select(t => t.spot).ToList();

            if (itemSpots.Count >= 2)
            {
                itemSpots.Sort((a, b) =>
                    b.transform.GetSiblingIndex().CompareTo(a.transform.GetSiblingIndex()));
            }

            // Clamp to prevent IndexOutOfRangeException when last slot is occupied
            int idealSpotIndex = Mathf.Clamp(
                itemSpots[0].transform.GetSiblingIndex() + 1,
                0,
                _spots.Length - 1);

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
            item.spot.BumpDown();

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
                item.spot.Clear();

            if (_itemMergeDataDictionary.Count <= 0)
                _isBusy = false;
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

            for (int i = 3; i < _spots.Length; i++)
            {
                ItemSpot spot = _spots[i];
                if (spot.IsEmpty()) continue;

                Item item = spot.Item;
                ItemSpot targetSpot = _spots[i - 3];

                if (!targetSpot.IsEmpty())
                {
                    Debug.LogWarning($"{targetSpot.Item.ItemNameKey} is not empty! Index: {targetSpot.transform.GetSiblingIndex()}");
                    _isBusy = false;
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

        private void HandleAllItemsMovedToTheLeft() => _isBusy = false;

        private void HandleIdealSpotFull(Item item, ItemSpot idealSpot) =>
            MoveAllItemsToTheRightFrom(idealSpot, item);

        private void MoveAllItemsToTheRightFrom(ItemSpot idealSpot, Item itemToPlace)
        {
            int spotIndex = idealSpot.transform.GetSiblingIndex();

            for (int i = _spots.Length - 2; i >= spotIndex; i--)
            {
                ItemSpot spot = _spots[i];

                if (spot.IsEmpty()) continue;

                Item item = spot.Item;
                spot.Clear();

                ItemSpot targetSpot = _spots[i + 1];

                if (!targetSpot.IsEmpty())
                {
                    Debug.LogError("Target spot not empty — should not happen");
                    _isBusy = false;
                    return;
                }

                MoveItemToSpot(item, targetSpot, () => HandleItemReachedSpot(item, false));
            }

            MoveItemToSpot(itemToPlace, idealSpot, () => HandleItemReachedSpot(itemToPlace));
        }

        private void MoveItemToFirstFreeSpot(Item item)
        {
            var targetSpot = GetFreeSpot();

            if (targetSpot == null)
            {
                Debug.Log("Target spot not found — should not happen");
                return;
            }

            CreateItemMergeData(item);
            MoveItemToSpot(item, targetSpot, () => HandleFirstItemReachSpot(item));
        }

        private void HandleFirstItemReachSpot(Item item)
        {
            item.spot.BumpDown();
            CheckForGameOver();
        }

        private void CheckForGameOver()
        {
            if (GetFreeSpot() == null)
                GameManager.instance.SetGameState(EGameState.GAMEOVER);
            else
                _isBusy = false;
        }

        private void CreateItemMergeData(Item item) =>
            _itemMergeDataDictionary.Add(item.ItemNameKey, new ItemMergeData(item));

        private ItemSpot GetFreeSpot() =>
            _spots.FirstOrDefault(t => t.IsEmpty());

        // Delegates to GetFreeSpot to avoid iterating the array twice
        private bool IsFreeSpotAvailable() => GetFreeSpot() != null;

        private void StoreSpot()
        {
            _spots = new ItemSpot[itemSpotParent.childCount];
            for (int i = 0; i < itemSpotParent.childCount; i++)
                _spots[i] = itemSpotParent.GetChild(i).GetComponent<ItemSpot>();
        }
    }
}