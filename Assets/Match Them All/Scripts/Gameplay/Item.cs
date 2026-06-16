using UnityEngine;
using UnityEngine.Rendering;

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
        public bool IsMovingToSpot { get; set; }

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
            _renderer.shadowCastingMode = ShadowCastingMode.On;

        public void DisableShadow() =>
            _renderer.shadowCastingMode = ShadowCastingMode.Off;

        public void EnablePhysics()
        {
            if (_rigidbody) 
                _rigidbody.isKinematic = false;
            if (_collider) 
                _collider.enabled = true;
        }
        
        public void DisablePhysics()
        {
            if (_rigidbody) 
                _rigidbody.isKinematic = true;
            if (_collider) 
                _collider.enabled = false;
        }

        public void Select(Material outlineMaterial) =>
            _renderer.materials = new[] { _baseMaterial, outlineMaterial };

        public void Deselect() =>
            _renderer.materials = new[] { _baseMaterial };

        public void ApplyRandomForce(float magnitude) => 
            GetComponent<Rigidbody>().AddForce(Random.insideUnitSphere * magnitude, ForceMode.VelocityChange);

        public void ResetState()
        {
            UnassignSpot();
            IsMovingToSpot = false;
            
            Deselect();
            EnableShadow();
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            
            if (_rigidbody)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            
            if (_collider)
            {
                _collider.enabled = true;
            }
        }
    }
}