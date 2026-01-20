// Import Unity's core functionality
using UnityEngine;

// Define the namespace to organize code and avoid naming conflicts
namespace MatchThemAll.Scripts
{
    // RequireComponent attribute ensures this GameObject always has a Rigidbody
    // Unity will automatically add this component if it's missing
    [RequireComponent(typeof(Rigidbody))]
    // Item class represents the data/object being observed in the Observer Pattern
    // Now includes visual feedback capabilities for selection states
    public class Item : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EItemName itemNameKey;
        public EItemName ItemNameKey => itemNameKey;

        public ItemSpot spot { get; private set; }

        // Inspector section for component references
        [Header("Elements")]
        // Reference to the Renderer component for visual effects and material changes
        [SerializeField] private Renderer _renderer;
        // Reference to the Collider component for physics interactions
        [SerializeField] private Collider _collider;
        
        // Store the original material to restore it when deselecting
        private Material _baseMaterial;

        // Awake is called when the script instance is being loaded (before Start)
        // Store the original material for later restoration
        private void Awake() => _baseMaterial = _renderer.material;
        
        public void AssignSpot(ItemSpot spot) => this.spot = spot;

        // Public method to disable the item's shadow rendering
        // This method is called by observers when they respond to item click events
        public void DisableShadow() => 
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Public method to disable physics interactions for this item
        // This method is called by observers as part of their response to item click events
        public void DisablePhysics()
        {
            // Set the Rigidbody to kinematic mode (no longer affected by physics forces)
            GetComponent<Rigidbody>().isKinematic = true;
            // Disable the Collider so it no longer participates in collision detection
            _collider.enabled = false;
        }

        // Public method to apply selection visual feedback
        // Adds an outline material to the existing material array
        // Parameter: outlineMaterial - the material used to create the selection outline effect
        public void Select(Material outlineMaterial) => 
            _renderer.materials = new[] {_baseMaterial, outlineMaterial};

        // Public method to remove selection visual feedback
        // Restores the renderer to only use the base material
        public void Deselect() => 
            _renderer.materials = new[] {_baseMaterial};
    }
}