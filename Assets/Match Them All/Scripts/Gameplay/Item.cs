using UnityEngine;

namespace MatchThemAll.Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class Item : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EItemName itemNameKey;
        public EItemName ItemNameKey => itemNameKey;

        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;

        public ItemSpot Spot { get; private set; }
        public bool IsMovingToSpot { get; set; } = false;

        [Header("Elements")]
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Collider _collider;

        // Cached in Awake — avoids GetComponent call at runtime inside DisablePhysics()
        private Rigidbody _rigidbody;
        private Material _baseMaterial;

        private void Awake()
        {
            _baseMaterial = _renderer.material;
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void AssignSpot(ItemSpot spot) => Spot = spot;
        
        public void UnassignSpot() => Spot = null;
        
        public void EnableShadow() =>
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        public void DisableShadow() =>
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        public void EnablePhysics()
        {
            if (_rigidbody != null) 
                _rigidbody.isKinematic = false;
            if (_collider != null) 
                _collider.enabled = true;
        }
        
        public void DisablePhysics()
        {
            if (_rigidbody != null) 
                _rigidbody.isKinematic = true;
            if (_collider != null) 
                _collider.enabled = false;
        }

        public void Select(Material outlineMaterial) =>
            _renderer.materials = new[] { _baseMaterial, outlineMaterial };

        public void Deselect() =>
            _renderer.materials = new[] { _baseMaterial };

        public void ApplyRandomForce(float magnitude)
        {
            GetComponent<Rigidbody>().AddForce(Random.insideUnitSphere * magnitude, ForceMode.VelocityChange);
        }
    }
}