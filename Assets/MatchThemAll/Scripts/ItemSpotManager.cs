// Import Unity core functionality

using System.Linq;
using UnityEngine;

// Define the namespace for organization
namespace MatchThemAll.Scripts
{
    // ItemSpotManager acts as an OBSERVER in the Observer Pattern
    // It watches for item click events and manages item placement in available spots
    // Now includes sophisticated spot management and availability checking
    public class ItemSpotManager : MonoBehaviour
    {
        // Header attribute creates a section in the Unity Inspector for organization
        [Header("Elements")] 
        // SerializeField makes a private field visible in Unity Inspector
        // Reference to the parent transform that contains all ItemSpot children
        [SerializeField] private Transform itemSpotParent;
        
        // Array to store references to all available ItemSpot components
        // Cached for performance to avoid repeated GetComponent calls
        private ItemSpot[] _spots;
        
        // Another Inspector section for configuration values
        [Header("Settings")]
        // The local position an item should have when placed on a spot
        [SerializeField] private Vector3 itemLocalPositionOnSpot;
        
        // The local scale an item should have when placed on a spot
        [SerializeField] private Vector3 itemLocalScaleOnSpot;
        
        // Awake is called when the script instance is being loaded
        // OBSERVER PATTERN: Subscribe this observer to the InputManager's ItemClicked event
        // Also initialize the spot's array for efficient spot management
        private void Awake()
        {
            // Subscribe to the ItemClicked event to receive notifications
            InputManager.ItemClicked += OnItemClicked;
            // Cache all ItemSpot components for efficient access
            StoreSpot();
        }

        // OnDestroy is called when the MonoBehaviour is destroyed
        // OBSERVER PATTERN: Unsubscribe from the event to prevent memory leaks
        // This is crucial for proper cleanup in the Observer Pattern
        private void OnDestroy() => InputManager.ItemClicked -= OnItemClicked;
        
        // OBSERVER PATTERN: This is the UPDATE/NOTIFY method that gets called when the subject changes
        // Event handler method called automatically when an item is clicked
        // Parameter 'item' is the Item component that was clicked (passed from the subject)
        private void OnItemClicked(Item item)
        {
            // Check if there are any available spots before processing the item
            if(!IsFreeSpotAvailable())
            {
                Debug.Log("No free spots available");
                return;
            }

            // Process the item click if spots are available
            HandleItemClick(item);
        }

        // Private method to process an item click when spots are available
        // Delegates the actual placement logic to more specific methods
        private void HandleItemClick(Item item)
        {
            // Move the item to the first available spot
            MoveItemToFirstFreeSpot(item);
        }

        // Private method to move an item to the first available spot
        // Handles the complete process of item placement and transformation
        private void MoveItemToFirstFreeSpot(Item item)
        {
            // Find the first available spot -- no item
            ItemSpot targetSpot = GetFreeSpot();
            
            // Safety check - this shouldn't happen if IsFreeSpotAvailable() was true
            if(targetSpot == null)
            {
                Debug.Log("Target spot not found -> should not happen");
                return;
            }
            
            // Assign the item to the target spot (makes item a child of the spot)
            targetSpot.Populate(item);
            
            // OBSERVER RESPONSE: Configure the item's transform properties
            // Set the item's position relative to its new parent spot
            item.transform.localPosition = itemLocalPositionOnSpot;
            // Set the item's scale relative to its new parent spot
            item.transform.localScale = itemLocalScaleOnSpot;
            // Reset rotation to ensure consistent orientation
            item.transform.localRotation = Quaternion.identity;
            
            // Call the item's methods to finalize the placement
            // Disable shadow rendering for performance or visual reasons
            item.DisableShadow();
            // Disable physics and collision detection since the item is now placed
            item.DisablePhysics();
        }
        
        // Private method to find and return the first available ItemSpot
        // Returns null if all spots are occupied
        private ItemSpot GetFreeSpot() =>
            // Iterate through all spots to find the first empty one
            // Return null if no free spots were found
            _spots.FirstOrDefault(t => t.IsEmpty());

        // Private method to cache all ItemSpot components for efficient access
        // Called once during Awake to avoid repeated GetComponent calls
        private void StoreSpot()
        {
            // Create an array with size matching the number of child objects
            _spots = new ItemSpot[itemSpotParent.childCount];
            
            // Get an ItemSpot component from each child and store in the array
            for (int i = 0; i < itemSpotParent.childCount; i++) 
                _spots[i] = itemSpotParent.GetChild(i).GetComponent<ItemSpot>();
        }

        // Private method to check if any spots are available for item placement
        // Returns true if at least one spot is empty, false if all are occupied
        private bool IsFreeSpotAvailable() =>
            // Check each spot for availability
            // Return false if no free spots were found
            _spots.Any(t => t.IsEmpty());
    }
}