// Import Unity's core functionality
using UnityEngine;

// Define the namespace for organization
namespace MatchThemAll.Scripts
{
    // ItemSpot represents a designated location where items can be placed
    // This class manages the state and occupancy of individual spots
    public class ItemSpot : MonoBehaviour
    {
        // Inspector section for settings
        [Header("Settings")] 
        // Reference to the item currently occupying this spot (null if empty)
        private Item _item;
        
        // Public method to place an item in this spot
        // Sets the item as a child of this spot's transform and marks the spot as occupied
        // Parameter: item - the Item component to be placed in this spot
        public void Populate(Item item)
        {
            // Store reference to the item occupying this spot
            _item = item;
            // Make the item a child of this spot's transform
            // This affects the item's position, rotation, and scale calculations
            item.transform.SetParent(transform);
            
            item.AssignSpot(this);
        }
        
        public void Clear() => _item = null;

        // Public method to check if this spot is available for use
        // Returns true if no item is currently occupying this spot
        public bool IsEmpty() => _item == null;
    }
}