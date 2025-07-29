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
        [SerializeField] private Transform itemSpotParent;
        private ItemSpot[] _spots;
        
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
        private void Awake()
        {
            InputManager.ItemClicked += OnItemClicked;
            StoreSpot();
        }

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
            if(!IsFreeSpotAvailable())
            {
                Debug.Log("No free spots available");
                return;
            }

            HandleItemClick(item);

            // OBSERVER RESPONSE: The following code is this observer's specific reaction to the event
            // Other observers might respond differently (play sounds, update score, show effects, etc.)
            
            // Make the clicked item a child of the itemSpots transform
            // This organizes the item in the hierarchy and affects its transform calculations
            //item.transform.SetParent(itemSpotParent);
            
            
        }

        private void HandleItemClick(Item item)
        {
            MoveItemToFirstFreeSpot(item);
        }

        private void MoveItemToFirstFreeSpot(Item item)
        {
            ItemSpot targetSpot = GetFreeSpot();
            
            if(targetSpot == null)
            {
                Debug.Log("Target spot not found -> should not happen");
                return;
            }
            
            targetSpot.Populate(item);
            
            
            // Set the item's position relative to its new parent (itemSpots)
            item.transform.localPosition = itemLocalPositionOnSpot;
            // Set the item's scale relative to its new parent
            item.transform.localScale = itemLocalScaleOnSpot;
            item.transform.localRotation = Quaternion.identity;
            
            // Call the item's method to disable shadow rendering
            item.DisableShadow();

            // Call the item's method to disable physics and collision detection
            item.DisablePhysics();
        }
        
        private ItemSpot GetFreeSpot()
        {
            for (int i = 0; i < _spots.Length; i++)
            {
                if(_spots[i].IsEmpty())
                    return _spots[i];
            }
            return null;
        }

        private void StoreSpot()
        {
            _spots = new ItemSpot[itemSpotParent.childCount];
            
            for (int i = 0; i < itemSpotParent.childCount; i++) 
                _spots[i] = itemSpotParent.GetChild(i).GetComponent<ItemSpot>();
        }

        private bool IsFreeSpotAvailable()
        {
            for (int i = 0; i < _spots.Length; i++)
            {
                if(_spots[i].IsEmpty())
                    return true;
            }
            return false;
        }
    }
}