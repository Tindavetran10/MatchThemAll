// Import Unity core functionality
using System;
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
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.15f;
        [SerializeField] private LeanTweenType animationEase = LeanTweenType.easeInOutCubic;
        
        [Header("Actions")]
        public static Action<List<Item>> mergeStarted;
        
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
            if (_itemMergeDataDictionary.ContainsKey(item.ItemNameKey))
                // We have! Try to put it near the other similar items
                HandleItemMergeDataFound(item);
            else
                // This is a new type - just put it in the first available spot
                MoveItemToFirstFreeSpot(item);
        }

        // SMART PLACEMENT: When we've seen this item type before, try to group them together
        private void HandleItemMergeDataFound(Item item)
        {
            // Find the best spot for this item (right next to the similar items)
            var idealSpot = GetIdealSpotFor(item);
            
            // Add this item to our records of similar items
            _itemMergeDataDictionary[item.ItemNameKey].Add(item);
            
            // Try to move the item to the ideal spot
            TryMoveItemToIdealSpot(item, idealSpot);
        }

        // SPOT CALCULATOR: Figures out where this item should go based on where similar items are
        private ItemSpot GetIdealSpotFor(Item item)
        {
            // Get a list of all the similar items we already have
            List<Item> items = _itemMergeDataDictionary[item.ItemNameKey].Items;
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
            var idealSpotIndex = itemSpots[0].transform.GetSiblingIndex() + 1;
            
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
            MoveItemToSpot(item, idealSpot, () => HandleItemReachedSpot(item));
        }

        // ITEM MOVER: Now with an option to skip merge checking,
        // The new parameter 'checkForMerge' lets us control when to check for merging
        // Sometimes we move items around and don't want to trigger merging yet
        private void MoveItemToSpot(Item item, ItemSpot targetSpot, Action completeCallback)
        {
            // Tell the spot that this item is now parked there
            targetSpot.Populate(item);
            
            // Position the item exactly where it should sit on the spot
            //item.transform.localPosition = itemLocalPositionOnSpot;
            LeanTween.moveLocal(item.gameObject, itemLocalPositionOnSpot, animationDuration)
                .setEase(animationEase);
            
            // Make it the right size
            //item.transform.localScale = itemLocalScaleOnSpot;
            LeanTween.scale(item.gameObject, itemLocalScaleOnSpot, animationDuration)
                .setEase(animationEase);
            
            // Make sure it's facing the right direction
            //item.transform.localRotation = Quaternion.identity;
            LeanTween.rotateLocal(item.gameObject, Vector3.zero, animationDuration)
                .setOnComplete(completeCallback);
            
            // Clean up the item now that it's parked:
            // Turn off its shadow (for better performance)
            item.DisableShadow();
            // Turn off physics so it won't fall or move around
            item.DisablePhysics();
        }

        // MERGE DETECTOR: Checks if we should merge items after placing one
        // This replaces the old HandleFirstItemReachSpot method
        private void HandleItemReachedSpot(Item item, bool checkForMerge = true)
        {
            item.spot.BumpDown();
            
            // If we're told not to check for merging, skip this entirely
            // (useful when we're just rearranging items)
            if(!checkForMerge) 
                return;
    
            // Check if we have enough identical items to merge them (3 or more)
            if (_itemMergeDataDictionary[item.ItemNameKey].CanMergeItems())
                // We do! Time to merge (remove) these items
                MergeItems(_itemMergeDataDictionary[item.ItemNameKey]);
            else 
                // Not enough items to merge yet, check if the game is over
                CheckForGameOver();
        }

        // ITEM MERGER: Removes 3+ identical items from the game
        // Like completing a set in a puzzle - the pieces disappear when matched
        private void MergeItems(ItemMergeData itemMergeData)
        {
            // Get the list of all identical items that need to be merged
            List<Item> items = itemMergeData.Items;
    
            // Remove this group from our tracking system
            // (we won't need to track these items anymore since they're being destroyed)
            _itemMergeDataDictionary.Remove(itemMergeData.ItemName);

            // Go through each item in the merge group
            foreach (var item in items)
            {
                // Tell the spot that it's now empty (clear the parking space)
                item.spot.Clear();
            }

            if(_itemMergeDataDictionary.Count <= 0)
                _isBusy = false;
            else
                MoveAllItemsToTheLeft(HandleAllItemsMovedToTheLeft);
            
            mergeStarted?.Invoke(items);
        }

        private void MoveAllItemsToTheLeft(Action completeCallback)
        {
            bool callBackTriggered = false;
            for (int i = 3; i < _spots.Length; i++)
            {
                ItemSpot spot = _spots[i];
                
                if(spot.IsEmpty())
                    continue;
                
                Item item = spot.Item;
                ItemSpot targetSpot = _spots[i - 3];

                if (!targetSpot.IsEmpty())
                {
                    Debug.LogWarning($"{targetSpot.Item.ItemNameKey} is not empty! {targetSpot.transform.GetSiblingIndex()}");
                    _isBusy = false;
                    return;   
                }
                
                spot.Clear();
                
                completeCallback += () => HandleItemReachedSpot(item, false);
                MoveItemToSpot(item, targetSpot, completeCallback);
                
                callBackTriggered = true;
            }
            
            if(!callBackTriggered) 
                completeCallback?.Invoke();
        }

        private void HandleAllItemsMovedToTheLeft()
        {
            _isBusy = false;
        }

        // SPACE MAKER: What to do when our ideal spot is occupied
        // Now actually implemented - it makes room by shifting items to the right
        private void HandleIdealSpotFull(Item item, ItemSpot idealSpot)
        {
            // SOLUTION: Push all items to the right to make space
            // Like asking people in a movie theater row to scoot over so you can sit
            MoveAllItemsToTheRightFrom(idealSpot, item);
        }

        // ITEM SHIFTER: Moves all items to the right to create space for a new item
        // This is like making room in a crowded parking lot by asking everyone to move over
        private void MoveAllItemsToTheRightFrom(ItemSpot idealSpot, Item itemToPlace)
        {
            // Find out which spot number we want to place our item in
            int spotIndex = idealSpot.transform.GetSiblingIndex();

            // Start from the rightmost occupied spot and work backwards (right to left)
            // We go backwards to avoid overwriting items as we move them
            for (int i = _spots.Length - 2; i >= spotIndex; i--)
            {
                ItemSpot spot = _spots[i];
        
                // If this spot is empty, skip it (nothing to move)
                if(_spots[i].IsEmpty())
                    continue;
        
                // Get the item currently in this spot
                Item item = spot.Item;
        
                // Clear this spot (mark it as available)
                spot.Clear();
        
                // The target spot is the next one to the right
                ItemSpot targetSpot = _spots[i + 1];

                // Safety check: Make sure the target spot is actually empty
                // (This should always be true if our logic is correct)
                if (!targetSpot.IsEmpty())
                {
                    Debug.LogError("This should not happen - Target spot not empty");
                    _isBusy = false;
                    return;
                }
        
                // Move the item to its new spot (one position to the right)
                // We set checkForMerge to false because we're just rearranging, not adding new items
                MoveItemToSpot(item, targetSpot, () => HandleItemReachedSpot(item, false));
            }
    
            // Now that we've made space, place the new item in the ideal spot
            MoveItemToSpot(itemToPlace, idealSpot, () => HandleItemReachedSpot(itemToPlace));
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
            
            MoveItemToSpot(item, targetSpot, () => HandleFirstItemReachSpot(item));
        }

        // GAME STATE CHECKER: Called after each item placement
        private void HandleFirstItemReachSpot(Item item)
        {
            item.spot.BumpDown();
            CheckForGameOver();
        }

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
            _itemMergeDataDictionary.Add(item.ItemNameKey, new ItemMergeData(item));

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