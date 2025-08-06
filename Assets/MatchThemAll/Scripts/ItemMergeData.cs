using System.Collections.Generic;

namespace MatchThemAll.Scripts
{
    public struct ItemMergeData
    {
        public string ItemName;
        public readonly List<Item> Items;
        
        public ItemMergeData(Item firstItem)
        {
            ItemName = firstItem.name;
            Items = new List<Item> { firstItem };
        }
        
        public void Add(Item item) => Items.Add(item);
    }
}