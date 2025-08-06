// Import Unity core functionality
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Define the namespace for organization
namespace MatchThemAll.Scripts
{
    // WHAT THIS SCRIPT DOES:
    // This script is like a "parking attendant" for a puzzle game.
    // When a player clicks on an item (like a cube, sphere, or capsule),
    // this script finds a good parking spot for it and moves it there.
    // It also tries to group similar items together, like organizing a toy box.
    
    public class ItemSpotManager : MonoBehaviour
    {
        [Header("Elements")] 
        // This is like having a folder that contains all the parking spots
        [SerializeField] private Transform itemSpotParent;
        
        // This is our list of all available parking spots in the game
        // Think of it like a parking lot with numbered spaces
        private ItemSpot[] _spots;
        
        [Header("Settings")]
        // When an item gets placed on a spot, these settings control:
        // - Where exactly it sits on the spot (position)
        [SerializeField] private Vector3 itemLocalPositionOnSpot;
        // - How big it appears (scale/size)
        [SerializeField] private Vector3 itemLocalScaleOnSpot;

        // This is like a "busy" sign - when true, we're in the middle of moving an item
        // and don't want to start moving another one at the same time
        private bool _isBusy;
        
        [Header("Data")]
        // This is like a filing cabinet that keeps track of which items are the same type,
        // For example, "I have 3 red cubes in spots 1, 3, and 5"
        private readonly Dictionary<EItemName, ItemMergeData> _itemMergeDataDictionary = new();
        
        // SETUP PHASE: This runs when the game starts
        private void Awake()
        {
            // Sign up to listen for "item clicked" messages
            // Like subscribing to a newsletter - we'll get notified when someone clicks an item
            InputManager.ItemClicked += OnItemClicked;
            
            // Make a list of all available parking spots so we can use them later
            StoreSpot();
        }

        // CLEANUP PHASE: This runs when the game ends or this script is destroyed
        private void OnDestroy() => 
            // Unsubscribe from the newsletter to avoid problems
            InputManager.ItemClicked -= OnItemClicked;
        
        // MAIN EVENT HANDLER: This is called whenever a player clicks on an item
        // Think of this as the "What should I do now?" decision maker
        private void OnItemClicked(Item item)
        {
            // If we're already busy moving another item, ignore this click
            if (_isBusy)
            {
                Debug.Log("ItemSpotManager is busy");
                return;
            }
            
            // Check if we have any empty parking spots left
            if(!IsFreeSpotAvailable())
            {
                Debug.Log("No free spots available - parking lot is full!");
                return;
            }
            
            // Put up the "busy" sign so we don't get interrupted
            _isBusy = true;

            // Now decide what to do with this item
            HandleItemClick(item);
        }

        // DECISION MAKER: Decides whether this is a new type of item or one we've seen before
        private void HandleItemClick(Item item)
        {
            // Check our filing cabinet - have we seen this type of item before?
            if (_itemMergeDataDictionary.ContainsKey(item.ItemName))
                // We have! Try to put it near the other similar items
                HandleItemMergeDataFound(item);
            else
                // This is a new type - just put it in the first available spot
                MoveItemToFirstFreeSpot(item);
        }

        // SMART PLACEMENT: When we've seen this item type before, try to group them together
        private void HandleItemMergeDataFound(Item item)
        {
            // Find the best spot for this item (near similar items)
            var idealSpot = GetIdealSpotFor(item);
            
            // Add this item to our records of similar items
            _itemMergeDataDictionary[item.ItemName].Add(item);
            
            // Try to move the item to the ideal spot
            TryMoveItemToIdealSpot(item, idealSpot);
        }

        // SPOT CALCULATOR: Figures out where this item should go based on where similar items are
        private ItemSpot GetIdealSpotFor(Item item)
        {
            // Get a list of all the similar items we already have
            List<Item> items = _itemMergeDataDictionary[item.ItemName].Items;
            // Find out which spots they're currently using
            List<ItemSpot> itemSpots = items.Select(t => t.spot).ToList();

            // If we have 2 or more similar items, sort their spots by position
            // This helps us find the best "next" spot in the sequence
            if (itemSpots.Count >= 2)
            {
                // Sort spots from right to left (the highest index to the lowest)
                itemSpots.Sort((a, b) => 
                    b.transform.GetSiblingIndex().CompareTo(a.transform.GetSiblingIndex()));
            }
            
            // The ideal spot is the one right after the last similar item
            // Like adding a book to the end of a series on a bookshelf
            int idealSpotIndex = itemSpots[0].transform.GetSiblingIndex() + 1;
            
            return _spots[idealSpotIndex];
        }

        // SPOT CHECKER: Try to use the ideal spot, but have a backup plan if it's taken
        private void TryMoveItemToIdealSpot(Item item, ItemSpot idealSpot)
        {
            // If someone's already parked in our ideal spot...
            if (!idealSpot.IsEmpty())
            {
                // We need a different plan (this method will handle the backup plan)
                HandleIdealSpotFull(item, idealSpot);
                return;
            }
            
            // Great! The ideal spot is free, so park there
            MoveItemToSpot(item, idealSpot);
        }

        // ITEM MOVER: The actual process of moving an item to a specific spot
        private void MoveItemToSpot(Item item, ItemSpot targetSpot)
        {
            // Tell the spot that this item is now parked there
            targetSpot.Populate(item);
            
            // Position the item exactly where it should sit on the spot
            item.transform.localPosition = itemLocalPositionOnSpot;
            // Make it the right size
            item.transform.localScale = itemLocalScaleOnSpot;
            // Make sure it's facing the right direction
            item.transform.localRotation = Quaternion.identity;
            
            // Clean up the item now that it's parked:
            // Turn off its shadow (for better performance)
            item.DisableShadow();
            // Turn off physics so it won't fall or move around
            item.DisablePhysics();

            // Check if this move affects the game (like triggering game over)
            HandleItemReachedSpot(item);
        }

        private void HandleItemReachedSpot(Item item)
        {
            if (_itemMergeDataDictionary[item.ItemName].CanMergeItems())
                MergeItems(_itemMergeDataDictionary[item.ItemName]);
            else CheckForGameOver();
        }

        private void MergeItems(ItemMergeData itemMergeData)
        {
            List<Item> items = itemMergeData.Items;
            
            // Remove the item merge data from the dictionary
            _itemMergeDataDictionary.Remove(itemMergeData.ItemName);

            for (int i = 0; i < items.Count; i++)
            {
                items[i].spot.Clear();
                Destroy(items[i].gameObject);
            }
            
            // TODO: Remove this line after moving the items to the left
            _isBusy = false;
        }

        // BACKUP PLAN: What to do when our ideal spot is already taken
        // (Currently empty - this is where you'd add logic for handling full spots)
        private void HandleIdealSpotFull(Item item, ItemSpot idealSpot)
        {
            // TODO: Add logic here for when the ideal spot is occupied
            // For example: find the next best spot, or rearrange items
        }

        // SIMPLE PLACEMENT: For new item types, use the first available spot
        private void MoveItemToFirstFreeSpot(Item item)
        {
            // Find any empty spot
            var targetSpot = GetFreeSpot();
            
            // Safety check - make sure we actually found a spot
            if(targetSpot == null)
            {
                Debug.Log("Target spot not found -> should not happen");
                return;
            }
            
            // Since this is a new item type, create a new record for it
            CreateItemMergeData(item);
            
            // Move the item to the spot we found
            targetSpot.Populate(item);
            
            // Set up the item's appearance and position
            item.transform.localPosition = itemLocalPositionOnSpot;
            item.transform.localScale = itemLocalScaleOnSpot;
            item.transform.localRotation = Quaternion.identity;
            
            // Clean up the item
            item.DisableShadow();
            item.DisablePhysics();

            // Check if this affects the game state
            HandleFirstItemReachSpot(item);
        }

        // GAME STATE CHECKER: Called after each item placement
        private void HandleFirstItemReachSpot(Item item) => CheckForGameOver();

        // GAME OVER DETECTOR: Checks if the parking lot is full
        private void CheckForGameOver()
        {
            // If there are no more empty spots...
            if (GetFreeSpot() == null)
            {
                Debug.Log("Game Over - no more spots available!");
            }
            else 
                // There are still spots available, so we're not busy anymore
                _isBusy = false;
        }

        // RECORD KEEPER: Creates a new record for a new type of item
        private void CreateItemMergeData(Item item) => 
            _itemMergeDataDictionary.Add(item.ItemName, new ItemMergeData(item));

        // SPOT FINDER: It looks through all spots to find an empty one
        private ItemSpot GetFreeSpot() =>
            // Go through our list of spots and return the first empty one
            // Returns null if all spots are full
            _spots.FirstOrDefault(t => t.IsEmpty());

        // SETUP HELPER: Creates our list of all available parking spots
        private void StoreSpot()
        {
            // Create an array that's big enough for all the child spots
            _spots = new ItemSpot[itemSpotParent.childCount];
            
            // Go through each child spot and add it to our list
            for (int i = 0; i < itemSpotParent.childCount; i++) 
                _spots[i] = itemSpotParent.GetChild(i).GetComponent<ItemSpot>();
        }

        // AVAILABILITY CHECKER: Quickly checks if any spots are available
        private bool IsFreeSpotAvailable() =>
            // Look through all spots and return true if at least one is empty
            _spots.Any(t => t.IsEmpty());
    }
}