using System.Collections.Generic;

namespace MatchThemAll.Scripts
{
    public readonly struct ItemMergeData
    {
        public readonly EItemName ItemName;
        public readonly List<Item> Items;
        
        public ItemMergeData(Item firstItem)
        {
            ItemName = firstItem.ItemNameKey;
            Items = new List<Item> { firstItem };
        }
        
        public void Add(Item item) => Items.Add(item);
        
        public void Remove(Item item) => Items.Remove(item);
        
        // MERGE CHECKER: Determines if we have enough items of the same type to merge them
        // In this game, when you collect 3 or more identical items, they disappear (merge)
        // Like having 3 matching cards in a card game - you can play them together
        public bool CanMergeItems()
        {
            if (Items.Count < 3) return false;
            foreach (var item in Items)
            {
                if (item.IsMovingToSpot) return false;
            }
            return true;
        }
    }
}