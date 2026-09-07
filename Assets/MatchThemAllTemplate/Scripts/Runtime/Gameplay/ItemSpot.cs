using UnityEngine;

namespace MatchThemAll.Scripts
{
    public class ItemSpot : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _itemParent;

        // PUBLIC ACCESS: Allows other scripts to see what item is in this spot
        // Like looking through a window to see what's inside a parking space
        [field: Header("Settings")]
        public Item Item { get; private set; }

        // SPOT OCCUPIER: Places an item in this parking spot
        public void Populate(Item item)
        {
            // Remember which item is parked here
            Item = item;
            // Make the item a child of this spot (like putting it in a folder)
            item.transform.SetParent(_itemParent);
            // Tell the item which spot it's now living in
            item.AssignSpot(this);
        }
        
        // SPOT CLEANER: Removes the item reference from this spot
        // Like erasing the name from a parking space when someone leaves
        // Note: This doesn't destroy the item, just marks the spot as available
        public void Clear()
        {
            if (Item) 
                Item.UnassignSpot();
            Item = null;
        }
        
        public void BumpDown() => _animator.Play("Bump", 0, 0);

        // AVAILABILITY CHECKER: Returns true if no item is using this spot
        public bool IsEmpty() => !Item;
    }
}