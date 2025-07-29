using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class ItemSpot : MonoBehaviour
    {
        [Header("Settings")] private Item _item;
        
        public void Populate(Item item)
        {
            _item = item;
            item.transform.SetParent(transform);
        }

        public bool IsEmpty() => _item == null;
    }
}