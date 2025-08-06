using System.Collections.Generic;

namespace MatchThemAll.Scripts
{
    public struct ItemMergeData
    {
        public EItemName ItemName;
        public readonly List<Item> Items;
        
        public ItemMergeData(Item firstItem)
        {
            ItemName = firstItem.ItemName;
            Items = new List<Item> { firstItem };
        }
        
        public void Add(Item item) => Items.Add(item);
        
        public bool CanMergeItems() => Items.Count >= 3;
    }
}