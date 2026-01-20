using System.Collections.Generic;

namespace MatchThemAll.Scripts
{
    public struct ItemMergeData
    {
        public EItemName ItemName;
        public readonly List<Item> Items;
        
        public ItemMergeData(Item firstItem)
        {
            ItemName = firstItem.ItemNameKey;
            Items = new List<Item> { firstItem };
        }
        
        public void Add(Item item) => Items.Add(item);
        
        // MERGE CHECKER: Determines if we have enough items of the same type to merge them
        // In this game, when you collect 3 or more identical items, they disappear (merge)
        // Like having 3 matching cards in a card game - you can play them together
        public bool CanMergeItems() => Items.Count >= 3;
    }
}