using System;
using MatchThemAll.Scripts;
using UnityEngine;

// This class acts as the SUBJECT (Observable) in the Observer Pattern
public class InputManager : MonoBehaviour
{
    // Static Action event that other scripts can subscribe to when an item is clicked
    // This is the NOTIFICATION MECHANISM in the Observer Pattern
    // Multiple observers can subscribe to this event without the InputManager knowing about them
    // This creates loose coupling - InputManager doesn't depend on specific observer classes
    public static Action<Item> ItemClicked;

    [Header("Settings")] 
    private Item _currentItem;
    
    // Start method is called once when the GameObject is first created
    // Currently empty but available for initialization code
    void Start()
    {
        
    }

    // Update method is called every frame by Unity's game loop
    void Update()
    {
        // Check if the left mouse button (button 0) was pressed down this frame
        if(Input.GetMouseButton(0))
            // If the mouse was clicked, call the method to handle the click
            HandleDrag(); 
        else if (Input.GetMouseButtonUp(0))
            HandleMouseUp();
    }

    // Private method to process mouse click events
    private void HandleDrag()
    {
        // Cast a ray from the main camera through the mouse position into the 3D world
        // The ray extends 100 units from the camera
        // out RaycastHit hit stores information about what the ray hits
        Physics.Raycast(Camera.main!.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100);
        
        // If the ray didn't hit any collider, exit the method early
        if(hit.collider == null)
        {
            _currentItem = null;
            return;
        }

        // Try to get an Item component from the hit collider
        // If the hit object doesn't have an Item component, exit the method
        if(!hit.collider.TryGetComponent(out Item item))
        {
            _currentItem = null;
            return;
        }

        // OBSERVER PATTERN: Notify all observers (subscribers) that an item was clicked
        // The ? operator ensures the event is only invoked if there are subscribers
        // This is where the Subject notifies all Observers about the state change
        // Any class that subscribed to ItemClicked will automatically receive this notification
        _currentItem = item;
    }
    
    private void HandleMouseUp()
    {
        // If the currentItem is null, exit the method early
        if(_currentItem == null)
            return;
        
        ItemClicked?.Invoke(_currentItem);
        _currentItem = null;
    }
}