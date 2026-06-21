using UnityEngine;
using ZLinq;

namespace MatchThemAll.Scripts
{
    public class MatchSystem : MonoBehaviour
    {
        private void Awake() => EventBus.Subscribe<ItemReachedSpotEvent>(OnItemReachedSpot);

        private void OnDestroy() => EventBus.Unsubscribe<ItemReachedSpotEvent>(OnItemReachedSpot);

        private static void OnItemReachedSpot(ItemReachedSpotEvent evt)
        {
            var item = evt.Item;
            
            // Get up to 3 items of the same type on the board that have finished moving
            var itemsToMerge = ItemSpotManager.Instance.GetItemsOnBoard(item.ItemNameKey).AsValueEnumerable()
                .Where(i => !i.IsMovingToSpot)
                .Take(3)
                .ToList();

            if (itemsToMerge.Count == 3)
            {
                EventBus.Publish(new MergeStartedEvent { MergedItems = itemsToMerge });
            }
            else
            {
                // Check for Game Over condition
                if (ItemSpotManager.Instance.IsBoardFullAndNoItemsMoving()) 
                    EventBus.Publish(new SpotFilledEvent());
            }
        }
    }
}
