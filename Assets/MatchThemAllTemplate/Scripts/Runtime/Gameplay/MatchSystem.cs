using UnityEngine;
using ZLinq;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Decides merges and the full-tray game over. Runs when a newly tapped item lands
    /// (<see cref="ItemReachedSpotEvent"/>) and when the tray settles after items were pushed aside
    /// (<see cref="CheckSettledTray"/>), so the result never depends on which slide finishes first.
    /// </summary>
    public class MatchSystem : MonoBehaviour
    {
        private void Awake() => EventBus.Subscribe<ItemReachedSpotEvent>(OnItemReachedSpot);

        private void OnDestroy() => EventBus.Unsubscribe<ItemReachedSpotEvent>(OnItemReachedSpot);

        private static void OnItemReachedSpot(ItemReachedSpotEvent evt)
        {
            if (!TryMerge(evt.Item.ItemNameKey)) CheckTrayFull();
        }

        /// <summary>Called by ItemSpotManager once nothing in the tray is moving any more.</summary>
        public static void CheckSettledTray()
        {
            foreach (var kind in ItemSpotManager.Instance.GetOccupiedItemTypes())
                if (TryMerge(kind)) return;
            CheckTrayFull();
        }

        // Three settled items of one kind merge.
        private static bool TryMerge(EItemName kind)
        {
            var itemsToMerge = ItemSpotManager.Instance.GetItemsOnBoard(kind).AsValueEnumerable()
                .Where(i => !i.IsMovingToSpot)
                .Take(3)
                .ToList();
            if (itemsToMerge.Count < 3) return false;

            EventBus.Publish(new MergeStartedEvent { MergedItems = itemsToMerge });
            return true;
        }

        private static void CheckTrayFull()
        {
            if (ItemSpotManager.Instance.IsBoardFullAndNoItemsMoving())
                EventBus.Publish(new SpotFilledEvent());
        }
    }
}
