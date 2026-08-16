using UnityEngine;
using UnityEngine.Rendering;
using NaughtyAttributes;

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

        [Header("Dock Placement Overrides")]
        [Tooltip("If enabled, overrides the default rotation when the item is placed in a dock/spot.")]
        [SerializeField] private bool useCustomDockRotation;
        [ShowIf(nameof(useCustomDockRotation))]
        [SerializeField] private Vector3 customDockRotation;

        [Tooltip("If enabled, adds an offset to the default position when the item is placed in a dock/spot.")]
        [SerializeField] private bool useCustomDockPositionOffset;
        [ShowIf(nameof(useCustomDockPositionOffset))]
        [SerializeField] private Vector3 customDockPositionOffset;

        public bool UseCustomDockRotation => useCustomDockRotation;
        public Vector3 CustomDockRotation => customDockRotation;
        public bool UseCustomDockPositionOffset => useCustomDockPositionOffset;
        public Vector3 CustomDockPositionOffset => customDockPositionOffset;

        public ItemSpot Spot { get; private set; }
        public bool IsMovingToSpot { get; set; }

        [Header("Physics Safety")]
        [Tooltip("Hard cap on rigidbody speed (m/s). Prevents force-spam (e.g. Fan) from flinging items through boundary colliders.")]
        [SerializeField] private float maxSpeed = 12f;

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
            // Speculative CCD: cheap, catches fast movers against the static boundary walls.
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            _deselectedMaterials = new[] { _baseMaterial };
            _selectedMaterials = new Material[2];
            _selectedMaterials[0] = _baseMaterial;

            _colliders = GetComponentsInChildren<Collider>(true);
        }

        // Failsafe clamp: no force source (Fan spam, gravity, bounce chains) can push an item past maxSpeed,
        // so per-step travel stays well below boundary collider thickness. Also rescues escapees.
        private void FixedUpdate()
        {
            if (_rigidbody == null || _rigidbody.isKinematic || IsMovingToSpot) return;

            var v = _rigidbody.linearVelocity;
            if (v.sqrMagnitude > maxSpeed * maxSpeed)
                _rigidbody.linearVelocity = v.normalized * maxSpeed;

            KeepInPlayArea();
        }

        // ponytail: play area derived from the top-down camera + margin; swap for explicit level bounds if levels ever diverge from the camera framing.
        private static Bounds _playArea;
        private static bool _playAreaKnown;
        private static Bounds PlayArea
        {
            get
            {
                if (_playAreaKnown) return _playArea;
                var cam = Camera.main;
                float h = cam != null && cam.orthographic ? cam.orthographicSize * 2f : 24f;
                float w = h * (cam != null ? cam.aspect : 0.5f);
                _playArea = new Bounds(Vector3.zero, new Vector3(w + 4f, 20f, h + 4f));
                _playAreaKnown = true;
                return _playArea;
            }
        }

        private void KeepInPlayArea()
        {
            var p = transform.position;
            if (PlayArea.Contains(p)) return;
            var clamped = PlayArea.ClosestPoint(p);
            clamped.y = Mathf.Max(clamped.y, 0.5f); // sit just above the board plane
            transform.position = clamped;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            Debug.LogWarning($"[Item] {name} escaped the play area — teleported back to {clamped}.", this);
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
                foreach (var itemCollider in _colliders)
                {
                    if (itemCollider) itemCollider.enabled = true;
                }
            }
        }
        
        public void DisablePhysics()
        {
            if (_rigidbody) 
                _rigidbody.isKinematic = true;
            if (_colliders != null)
            {
                foreach (var itemCollider in _colliders)
                {
                    if (itemCollider) itemCollider.enabled = false;
                }
            }
        }

        public void Select(Material outlineMaterial)
        {
            _selectedMaterials[1] = outlineMaterial;
            _renderer.sharedMaterials = _selectedMaterials;
        }

        public void Deselect() =>
            _renderer.sharedMaterials = _deselectedMaterials;

        public void ApplyRandomForce(float magnitude)
        {
            // Direct velocity set (same math as AddForce VelocityChange) but pre-clamped,
            // so no integration step ever sees a speed above maxSpeed.
            var v = _rigidbody.linearVelocity + Random.insideUnitSphere * magnitude;
            _rigidbody.linearVelocity = Vector3.ClampMagnitude(v, maxSpeed);
        }

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

            if (_colliders == null) return;
            foreach (var itemCollider in _colliders)
            {
                if (itemCollider) itemCollider.enabled = true;
            }
        }
    }
}