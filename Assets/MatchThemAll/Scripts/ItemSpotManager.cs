using UnityEngine;

namespace MatchThemAll.Scripts
{
    // ItemSpotManager acts as an OBSERVER in the Observer Pattern
    // It watches for item click events and responds accordingly
    public class ItemSpotManager : MonoBehaviour
    {
        // Header attribute creates a section in the Unity Inspector for organization
        [Header("Elements")] 
        // SerializeField makes a private field visible in Unity Inspector
        // Reference to the parent transform that will hold all item spots
        [SerializeField] private Transform itemSpots;
        
        // Another Inspector section for configuration values
        [Header("Settings")]
        // The local position an item should have when placed on a spot
        [SerializeField] private Vector3 itemLocalPositionOnSpot;
        // The local scale an item should have when placed on a spot
        [SerializeField] private Vector3 itemLocalScaleOnSpot;
        
        // Awake is called when the script instance is being loaded
        // OBSERVER PATTERN: Subscribe this observer to the InputManager's ItemClicked event
        // This establishes the observer relationship - when InputManager detects a click,
        // this class will be automatically notified via the OnItemClicked method
        private void Awake() => InputManager.ItemClicked += OnItemClicked;

        // OnDestroy is called when the MonoBehaviour is destroyed
        // OBSERVER PATTERN: Unsubscribe from the event to prevent memory leaks
        // This is crucial for proper cleanup in the Observer Pattern
        // Without this, the event might try to call methods on destroyed objects
        private void OnDestroy() => InputManager.ItemClicked -= OnItemClicked;
        
        // OBSERVER PATTERN: This is the UPDATE/NOTIFY method that gets called when the subject changes
        // Event handler method called automatically when an item is clicked
        // Parameter 'item' is the Item component that was clicked (passed from the subject)
        private void OnItemClicked(Item item)
        {
            // Debug message to confirm the observer received the notification
            Debug.Log("Item clicked");
            
            // OBSERVER RESPONSE: The following code is this observer's specific reaction to the event
            // Other observers might respond differently (play sounds, update score, show effects, etc.)
            
            // Make the clicked item a child of the itemSpots transform
            // This organizes the item in the hierarchy and affects its transform calculations
            item.transform.SetParent(itemSpots);
            
            // Set the item's position relative to its new parent (itemSpots)
            item.transform.localPosition = itemLocalPositionOnSpot;
            // Set the item's scale relative to its new parent
            item.transform.localScale = itemLocalScaleOnSpot;
            
            // Call the item's method to disable shadow rendering
            item.DisableShadow();

            // Call the item's method to disable physics and collision detection
            item.DisablePhysics();
        }
    }
}