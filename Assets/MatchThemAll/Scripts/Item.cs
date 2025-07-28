using UnityEngine;

namespace MatchThemAll.Scripts
{
    // RequireComponent attribute ensures this GameObject always has a Rigidbody and SphereCollider
    // Unity will automatically add these components if they're missing
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    // Item class represents the data/object being observed in the Observer Pattern
    // It doesn't participate directly in the pattern but is the subject of the notifications
    public class Item : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private Renderer _renderer;
        private Material _baseMaterial;

        private void Awake() => _baseMaterial = _renderer.material;


        // Public method to disable the item's shadow rendering
        // Currently empties but provides interface for shadow management
        // This method is called by observers when they respond to item click events
        public void DisableShadow()
        {
            
        }
        
        // Public method to disable physics interactions for this item
        // This method is called by observers as part of their response to item click events
        public void DisablePhysics()
        {
            // Set the Rigidbody to kinematic mode (no longer affected by physics forces)
            GetComponent<Rigidbody>().isKinematic = true;
            // Disable the SphereCollider so it no longer participates in collision detection
            GetComponent<SphereCollider>().enabled = false;
        }

        public void Select(Material outlineMaterial) => 
            _renderer.materials = new[] {_baseMaterial, outlineMaterial};

        public void Deselect() => 
            _renderer.materials = new[] {_baseMaterial};
    }
}