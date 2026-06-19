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

        // Cached in Awake — avoids GetComponent call at runtime inside DisablePhysics()
        private Rigidbody _rigidbody;
        private Material _baseMaterial;

        // Pre-allocated material arrays — reused on every Select/Deselect call to avoid GC allocations
        private Material[] _selectedMaterials;
        private Material[] _deselectedMaterials;
        private Collider[] _colliders;

        private void Awake()
        {
            _baseMaterial = _renderer.sharedMaterial; // sharedMaterial avoids cloning — preserves GPU batching
            _rigidbody = GetComponent<Rigidbody>();
            
            _deselectedMaterials = new[] { _baseMaterial };
            _selectedMaterials = new Material[2];
            _selectedMaterials[0] = _baseMaterial;
            
            _colliders = GetComponentsInChildren<Collider>(true);
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
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i]) _colliders[i].enabled = true;
                }
            }
        }
        
        public void DisablePhysics()
        {
            if (_rigidbody) 
                _rigidbody.isKinematic = true;
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i]) _colliders[i].enabled = false;
                }
            }
        }

        public void Select(Material outlineMaterial)
        {
            _selectedMaterials[1] = outlineMaterial;
            _renderer.materials = _selectedMaterials;
        }

        public void Deselect() =>
            _renderer.materials = _deselectedMaterials;

        public void ApplyRandomForce(float magnitude) => 
            _rigidbody.AddForce(Random.insideUnitSphere * magnitude, ForceMode.VelocityChange);

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
            
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i]) _colliders[i].enabled = true;
                }
            }
        }
    }
}