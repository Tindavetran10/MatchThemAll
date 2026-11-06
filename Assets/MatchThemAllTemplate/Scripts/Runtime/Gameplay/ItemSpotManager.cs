using System;
using System.Collections.Generic;
using ZLinq;
using UnityEngine;
using PrimeTween;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Owns the tray. The tray is treated as one packed row: every change (an item arrives, a merge
    /// removes three, a power-up takes one back) rebuilds the row in memory and then reconciles the
    /// slots with it, tweening only the items whose slot changed.
    /// </summary>
    public class ItemSpotManager : MonoBehaviour
    {
        public static ItemSpotManager Instance { get; private set; }

        /// <summary>Raised when the player sends an item from the board to the tray.</summary>
        public static Action<Item> ItemPickedUp;

        [Header("Tray")]
        [SerializeField] private Transform itemSpotParent;
        [SerializeField] private Vector3 itemLocalPositionOnSpot;
        [SerializeField] private Vector3 itemLocalScaleOnSpot;

        [Header("Slide Tween")]
        [SerializeField] private float animationDuration = 0.15f;
        [SerializeField] private Ease animationEase = Ease.InOutCubic;

        private ItemSpot[] _spots;
        private int _activeSpotCount = 7;

        // Reused scratch buffer for row rebuilds (no per-tap allocation).
        private readonly List<Item> _row = new();
        // Items sent from the board that have not finished their first flight yet. Only these
        // trigger a match check on arrival; items merely sliding along the tray do not.
        private readonly HashSet<Item> _newcomers = new();

        #region Lifecycle

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (!TryGetComponent<MatchSystem>(out _)) gameObject.AddComponent<MatchSystem>();

            _spots = itemSpotParent.GetComponentsInChildren<ItemSpot>(true);

            EventBus.Subscribe<ItemClickedEvent>(OnItemClickedEvent);
            EventBus.Subscribe<MergeStartedEvent>(OnMergeStarted);
            LevelManager.LevelSpawned += OnLevelSpawned;
        }

        private void Start()
        {
            if (PowerupManager.Instance) PowerupManager.Instance.OnItemBackToGame += OnItemBackToGame;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ItemClickedEvent>(OnItemClickedEvent);
            EventBus.Unsubscribe<MergeStartedEvent>(OnMergeStarted);
            LevelManager.LevelSpawned -= OnLevelSpawned;
            if (PowerupManager.Instance) PowerupManager.Instance.OnItemBackToGame -= OnItemBackToGame;
        }

        #endregion

        #region Event handlers

        private void OnLevelSpawned(Level level)
        {
            _activeSpotCount = Mathf.Min(level.SpotCount, _spots.Length);
            for (int i = 0; i < _spots.Length; i++)
                _spots[i].gameObject.SetActive(i < _activeSpotCount);

            if (itemSpotParent.TryGetComponent<ItemSpotLayout>(out var layout)) layout.UpdateLayout();

            EmptyTray();
        }

        private void OnItemClickedEvent(ItemClickedEvent evt)
        {
            if (!GetFreeSpot()) return; // tray full: the tap is ignored

            ItemPickedUp?.Invoke(evt.ClickedItem);
            AddToRow(evt.ClickedItem);
        }

        private void OnMergeStarted(MergeStartedEvent evt)
        {
            // The merged items stay parented to their slots while MergeManager animates them;
            // we only free the slots and close the gap.
            foreach (var item in evt.MergedItems)
            {
                _newcomers.Remove(item);
                if (item.Spot) item.Spot.Clear();
            }
            Repack();
        }

        private void OnItemBackToGame(Item item)
        {
            _newcomers.Remove(item);
            if (item.Spot) item.Spot.Clear();
            Repack();
        }

        #endregion

        #region Row logic

        /// <summary>Inserts the item right after the last item of the same kind (or at the end) and repacks.</summary>
        private void AddToRow(Item item)
        {
            ReadRow();

            int insertAt = _row.Count;
            for (int i = _row.Count - 1; i >= 0; i--)
            {
                if (_row[i].ItemNameKey != item.ItemNameKey) continue;
                insertAt = i + 1;
                break;
            }

            _row.Insert(insertAt, item);
            _newcomers.Add(item);
            ApplyRow();
        }

        private void Repack()
        {
            ReadRow();
            ApplyRow();
        }

        /// <summary>Copies the current occupants, left to right, into <see cref="_row"/>.</summary>
        private void ReadRow()
        {
            _row.Clear();
            for (int i = 0; i < _activeSpotCount; i++)
                if (_spots[i].Item) _row.Add(_spots[i].Item);
        }

        /// <summary>Makes slot i hold _row[i]; anything that changed slot (or is new) slides there.</summary>
        private void ApplyRow()
        {
            for (int i = 0; i < _activeSpotCount; i++)
            {
                var target = _spots[i];
                var wanted = i < _row.Count ? _row[i] : null;
                if (target.Item == wanted) continue;

                if (!wanted) { target.Clear(); continue; }

                var oldSpot = wanted.Spot;
                target.Populate(wanted);
                if (oldSpot && oldSpot.Item == wanted) oldSpot.Clear();
                SlideIntoSpot(wanted);
            }
        }

        private void SlideIntoSpot(Item item)
        {
            item.IsMovingToSpot = true;
            item.DisablePhysics();
            item.DisableShadow();
            Tween.StopAll(item.transform); // also cancels hint pulses and any earlier slide

            var t = item.transform;
            Vector3 fromPos = t.localPosition, fromScale = t.localScale;
            Quaternion fromRot = t.localRotation;
            Vector3 toPos = itemLocalPositionOnSpot + (item.UseCustomDockPositionOffset ? item.CustomDockPositionOffset : Vector3.zero);
            Quaternion toRot = Quaternion.Euler(item.UseCustomDockRotation ? item.CustomDockRotation : Vector3.zero);
            Vector3 toScale = itemLocalScaleOnSpot;

            Tween.Custom(t, 0f, 1f, animationDuration, (tr, p) =>
                {
                    tr.localPosition = Vector3.LerpUnclamped(fromPos, toPos, p);
                    tr.localRotation = Quaternion.SlerpUnclamped(fromRot, toRot, p);
                    tr.localScale = Vector3.LerpUnclamped(fromScale, toScale, p);
                }, animationEase)
                .OnComplete(item, Arrived);
        }

        private void Arrived(Item item)
        {
            item.IsMovingToSpot = false;
            if (item.Spot) item.Spot.BumpDown();

            if (_newcomers.Remove(item))
                EventBus.Publish(new ItemReachedSpotEvent(item));
        }

        private void EmptyTray()
        {
            _newcomers.Clear();
            foreach (var spot in _spots)
            {
                var item = spot.Item;
                spot.Clear();
                if (!item) continue;
                Tween.StopAll(item.transform);
                ItemPoolManager.Instance.ReleaseItem(item);
            }
        }

        #endregion

        #region Queries

        private ItemSpot GetFreeSpot() => _spots.AsValueEnumerable().Take(_activeSpotCount).FirstOrDefault(t => t.IsEmpty());

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
                if (!_spots[i].IsEmpty() && _spots[i].Item.ItemNameKey == itemName)
                    return true;
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
            if (GetFreeSpot()) return false;
            for (int i = 0; i < _activeSpotCount; i++)
            {
                if (_spots[i].Item && _spots[i].Item.IsMovingToSpot) return false;
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
                if (allItems.AsValueEnumerable()
                    .Any(i => i &&
                              i.gameObject.activeInHierarchy &&
                              i.ItemNameKey == type &&
                              !i.Spot &&
                              !i.IsMovingToSpot))
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
                            if (allItems.AsValueEnumerable()
                                .Any(i => i &&
                                          i.gameObject.activeInHierarchy &&
                                          i.ItemNameKey == goal.itemPrefab.ItemNameKey &&
                                          !i.Spot &&
                                          !i.IsMovingToSpot))
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
                var fallback = allItems.AsValueEnumerable()
                    .FirstOrDefault(i =>
                        i && i.gameObject.activeInHierarchy && !i.Spot && !i.IsMovingToSpot);
                if (fallback)
                    targetType = fallback.ItemNameKey;
            }

            // Collect up to 'count' items of the chosen type, grouped by proximity
            if (targetType.HasValue)
            {
                var availableItems = allItems.AsValueEnumerable()
                    .Where(i => i &&
                                i.gameObject.activeInHierarchy &&
                                i.ItemNameKey == targetType.Value &&
                                !i.Spot &&
                                !i.IsMovingToSpot)
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

        #endregion
    }
}
