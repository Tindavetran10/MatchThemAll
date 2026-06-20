using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class MatchSystem : MonoBehaviour
    {
        private void Awake()
        {
            EventBus.Subscribe<ItemReachedSpotEvent>(OnItemReachedSpot);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ItemReachedSpotEvent>(OnItemReachedSpot);
        }

        private void OnItemReachedSpot(ItemReachedSpotEvent evt)
        {
            var item = evt.Item;
            
            // Get all items of the same type on the board that have finished moving
            var mergeableItems = ItemSpotManager.Instance.GetItemsOnBoard(item.ItemNameKey)
                .Where(i => !i.IsMovingToSpot).ToList();

            if (mergeableItems.Count >= 3)
            {
                // We have a match! We only merge the first 3 to prevent index out of bounds in animations.
                var itemsToMerge = mergeableItems.Take(3).ToList();
                EventBus.Publish(new MergeStartedEvent { MergedItems = itemsToMerge });
            }
            else
            {
                // Check for Game Over condition
                if (ItemSpotManager.Instance.IsBoardFullAndNoItemsMoving())
                {
                    EventBus.Publish(new SpotFilledEvent());
                }
            }
        }
    }
}
