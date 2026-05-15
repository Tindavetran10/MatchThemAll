using System;
using MatchThemAll.Scripts;
using UnityEngine;

// This class acts as the SUBJECT (Observable) in the Observer Pattern
// Handles item selection with visual feedback and drag-to-select functionality
public class InputManager : MonoBehaviour
{
    // Static Action event that other scripts can subscribe to when an item is clicked
    // This is the NOTIFICATION MECHANISM in the Observer Pattern.
    // Multiple observers can subscribe to this event without the InputManager knowing about them.
    // This creates loose coupling - InputManager doesn't depend on specific observer classes
    public static Action<Item> ItemClicked;
    
    [Header("Settings")] 
    // Material used to create a visual outline effect when an item is selected
    [SerializeField] private Material outlineMaterial;
    
    [Header("Optimization")]
    [SerializeField] private LayerMask itemLayerMask;

    
    // Reference to the currently selected item (if any)
    // Helps maintain the selection state during drag operations
    private Item _currentItem;
    private MTAInputSystem_Actions _input;
    
    private Camera _mainCamera;
    
    // Start method is called once when the GameObject is first created
    // Currently empty but available for initialization code
    private void Start()
    {
        _mainCamera = Camera.main;
        _input = new MTAInputSystem_Actions();
        _input.Gameplay.Click.Enable();     // button action
        _input.Gameplay.Pointer.Enable();  
    }

    // Update method is called every frame by Unity's game loop
    // Handles both drag selection and mouse release events
    private void Update()
    {
        if(GameManager.instance.IsGame())
            HandleControl();
    }

    private void HandleControl()
    {
        if (_input.Gameplay.Click.IsPressed())
            HandleDrag();
        else if (_input.Gameplay.Click.WasReleasedThisFrame()) HandleMouseUp();
    }

    private void OnDestroy() => _input?.Dispose();

    // Private method to handle mouse drag/hover for item selection
    // This provides real-time visual feedback as the user hovers over items
    private void HandleDrag()
    {
        // This returns a Vector2 in *screen* pixel coordinates.
        var pointerScreenPos = _input.Gameplay.Pointer.ReadValue<Vector2>();
        var ray = _mainCamera.ScreenPointToRay(pointerScreenPos);
        
        // Cast a ray from the main camera through the mouse position into the 3D world
        // The ray extends 100 units from the camera
        // out RaycastHit hit stores information about what the ray hits.
        // The Ray can only hit game objects that have a collider
        //Physics.Raycast(Camera.main!.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100);
        
        // Only raycast against items (skip UI, floor, etc.)
        if (!Physics.Raycast(ray, out var hit, 100f, itemLayerMask))
        {
            DeselectCurrentItem();
            return;
        }
        
        // If the ray didn't hit any collider, deselect the current item and exit
        if(hit.collider == null)
        {
            DeselectCurrentItem();
            return;
        }

        // Check if the hit item with the Collider has a parent 
        if (hit.collider.transform.parent == null)
            return;

        // Try to get an Item component from the hit collider's parent
        // Item scripts are expected to be on parent objects, not the colliders themselves
        if(!hit.collider.transform.parent.TryGetComponent(out Item item))
        {
            DeselectCurrentItem();
            return;
        }

        // Ensure only one item is selected at a time by deselecting the previous one
        DeselectCurrentItem();
        
        // Set the new item as selected and apply visual feedback
        _currentItem = item;
        // Call the item's Select method to apply outline material for visual feedback
        _currentItem.Select(outlineMaterial);
    }

    // Private method to deselect the currently selected item
    // Removes visual feedback and clears the current item reference
    private void DeselectCurrentItem()
    {
        // If there's a currently selected item, remove its selection visual
        if(_currentItem != null)
            _currentItem.Deselect();
        
        // Clear the current item reference
        _currentItem = null;
    }
    
    // Private method to handle mouse button release
    // This is where the actual "click" event is triggered for observers
    private void HandleMouseUp()
    {
        // If no item is currently selected, there's nothing to do
        if(_currentItem == null)
            return;
        
        // Remove visual selection feedback
        _currentItem.Deselect();
        
        // OBSERVER PATTERN: Notify all observers that an item was clicked
        // This is where the Subject notifies all Observers about the item selection
        // The event is only fired on mouse release, ensuring a complete click gesture
        ItemClicked?.Invoke(_currentItem);
        
        // Clear the current item reference after processing
        _currentItem = null;
    }
}