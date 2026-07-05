using Match_Them_All.Scripts.Power_Ups;
using MatchThemAll.Scripts;
using UnityEngine;

// This class acts as the SUBJECT (Observable) in the Observer Pattern
// Handles item selection with visual feedback and drag-to-select functionality
public class InputManager : MonoBehaviour
{
    // EventBus handles notifications.
    // Static fields are kept for Tutorial overrides only.
    
    public static bool IsTutorialActive;
    public static Item[] TutorialTargets;

    public static InputManager Instance { get; private set; }

    [Header("Settings")] 
    // Material used to create a visual outline effect when an item is selected
    [SerializeField] private Material outlineMaterial;
    public Material OutlineMaterial => outlineMaterial;
    public bool IsPointerActive => _input != null && _input.Gameplay.Click.IsPressed();
    
    [Header("Optimization")]
    [SerializeField] private LayerMask itemLayerMask;
    [SerializeField] private LayerMask powerupLayerMask;

    
    // Reference to the currently selected item (if any)
    // Helps maintain the selection state during drag operations
    private Item _currentItem;
    private MTAInputSystem_Actions _input;
    
    private Camera _mainCamera;
    
    private void Awake()
    {
        if (!Instance) Instance = this;
        else Destroy(gameObject);
        
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    // Start method is called once when the GameObject is first created
    // Currently empty but available for initialization code
    private void Start()
    {
        _mainCamera = Camera.main;
        _input = new MTAInputSystem_Actions();
        _input.Gameplay.Click.Enable();     // button action
        _input.Gameplay.Pointer.Enable();  

        int tutorialLayer = LayerMask.NameToLayer("Tutorial");
        if (tutorialLayer != -1) {
            itemLayerMask.value |= 1 << tutorialLayer;
        }

        WarmupOutlineShader();
    }

    private void WarmupOutlineShader()
    {
        if (outlineMaterial == null || _mainCamera == null) return;
        
        // Create a temporary dummy object to force the GPU to compile the Outline Shader variant immediately.
        // This prevents a massive CPU/GPU spike the first time the HintManager or user selects an item.
        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(dummy.GetComponent<Collider>());
        
        // Place it just barely inside the camera's frustum, scaled LARGE so it is guaranteed
        // to rasterize pixels and trigger the shader compilation!
        // We can do this safely because SceneLoader fades to black during this exact frame, 
        // completely hiding this dummy object from the player's view.
        dummy.transform.SetParent(_mainCamera.transform);
        dummy.transform.localPosition = new Vector3(0, 0, _mainCamera.nearClipPlane + 0.1f);
        dummy.transform.localScale = Vector3.one * 10f;
        
        var meshRenderer = dummy.GetComponent<MeshRenderer>();
        meshRenderer.material = outlineMaterial;
        
        // Destroy it after 1 second
        Destroy(dummy, 1f);
    }

    // Update method is called every frame by Unity's game loop
    // Handles both drag selection and mouse release events
    private void Update()
    {
        if(GameManager.Instance.IsGame() && LevelManager.Instance && LevelManager.Instance.IsLevelReady)
            HandleControl();
    }

    private void HandleControl()
    {
        if (_input.Gameplay.Click.WasPressedThisFrame())
            HandleMouseDown();
        else if (_input.Gameplay.Click.IsPressed())
            HandleDrag();
        else if (_input.Gameplay.Click.WasReleasedThisFrame()) HandleMouseUp();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        if (_input == null) return;
        _input.Gameplay.Disable(); // must be called before Dispose() to satisfy the finalizer assertion
        _input.Dispose();
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState != EGameState.GAME) 
            DeselectCurrentItem();
    }

    private void HandleMouseDown()
    {
        // This returns a Vector2 in *screen* pixel coordinates.
        var pointerScreenPos = _input.Gameplay.Pointer.ReadValue<Vector2>();
        var ray = _mainCamera.ScreenPointToRay(pointerScreenPos);

        int currentMask = powerupLayerMask;
        if (IsTutorialActive)
        {
            int tutorialLayer = LayerMask.NameToLayer("Tutorial");
            if (tutorialLayer != -1) 
                currentMask = 1 << tutorialLayer;
        }

        Physics.Raycast(ray, out var hit, 100f, currentMask);
        
        if(!hit.collider)
            return;
        
        if (hit.collider.TryGetComponent(out Powerup powerup) && powerup.Data != null)
            EventBus.Publish(new PowerupClickedEvent(powerup.Data));
    }
    
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
        
        int currentMask = itemLayerMask;
        if (IsTutorialActive)
        {
            int tutorialLayer = LayerMask.NameToLayer("Tutorial");
            if (tutorialLayer != -1) 
                currentMask = 1 << tutorialLayer;
        }

        // Only raycast against items (skip UI, floor, etc.)
        if (!Physics.Raycast(ray, out var hit, 100f, currentMask))
        {
            DeselectCurrentItem();
            return;
        }
        
        // If the ray didn't hit any collider, deselect the current item and exit
        if(!hit.collider)
        {
            DeselectCurrentItem();
            return;
        }

        // Check if the hit item with the Collider has a parent 
        if (!hit.collider.transform.parent)
            return;

        // Try to get an Item component from the hit collider's parent
        // Item scripts are expected to be on parent objects, not the colliders themselves
        if(!hit.collider.transform.parent.TryGetComponent(out Item item))
        {
            DeselectCurrentItem();
            return;
        }

        // TUTORIAL OVERRIDE: If tutorial is active, block selecting non-tutorial targets
        if (IsTutorialActive)
        {
            bool isTarget = false;
            if (TutorialTargets != null)
            {
                foreach (var target in TutorialTargets)
                {
                    if (target == item) 
                        isTarget = true;
                }
            }
            
            if (!isTarget)
            {
                DeselectCurrentItem();
                return;
            }
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
        if(_currentItem)
            _currentItem.Deselect();
        
        // Clear the current item reference
        _currentItem = null;
    }
    
    // Private method to handle mouse button release
    // This is where the actual "click" event is triggered for observers
    private void HandleMouseUp()
    {
        // If no item is currently selected, there's nothing to do
        if(!_currentItem)
            return;
        
        // Remove visual selection feedback
        _currentItem.Deselect();
        
        // OBSERVER PATTERN: Notify all observers that an item was clicked via EventBus
        EventBus.Publish(new ItemClickedEvent(_currentItem));
        
        // Clear the current item reference after processing
        _currentItem = null;
    }
}