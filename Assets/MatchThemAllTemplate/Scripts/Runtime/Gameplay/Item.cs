using UnityEngine;
using UnityEngine.Rendering;
using NaughtyAttributes;

namespace MatchThemAll.Scripts
{
    /// <summary>A collectable board piece: identity + icon, tray placement overrides, physics and selection look.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Item : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private EItemName itemNameKey;
        [SerializeField] private Sprite icon;

        public EItemName ItemNameKey => itemNameKey;
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

        /// <summary>The tray slot holding this item, or null while it is on the board.</summary>
        public ItemSpot Spot { get; private set; }
        /// <summary>True while the tray is tweening this item into a slot.</summary>
        public bool IsMovingToSpot { get; set; }

        [Header("Physics Safety")]
        [Tooltip("Hard cap on rigidbody speed (m/s). Prevents force-spam (e.g. Fan) from flinging items through boundary colliders.")]
        [SerializeField] private float maxSpeed = 12f;

        [Header("Rendering")]
        [SerializeField] private Renderer _renderer;

        private Rigidbody _rigidbody;
        private Collider[] _colliders;
        private Material _baseMaterial;
        // Built once so Select/Deselect never allocate.
        private Material[] _selectedMaterials;
        private Material[] _deselectedMaterials;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            // Speculative CCD: cheap, catches fast movers against the static boundary walls.
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _colliders = GetComponentsInChildren<Collider>(true);

            // sharedMaterial (not .material) so items keep batching.
            _baseMaterial = _renderer.sharedMaterial;
            _deselectedMaterials = new[] { _baseMaterial };
            _selectedMaterials = new[] { _baseMaterial, null };
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

        public void EnableShadow() => SetShadow(true);
        public void DisableShadow() => SetShadow(false);
        private void SetShadow(bool on) => _renderer.shadowCastingMode = on ? ShadowCastingMode.On : ShadowCastingMode.Off;

        public void EnablePhysics() => SetSimulated(true);
        public void DisablePhysics() => SetSimulated(false);

        /// <summary>Simulated = dynamic rigidbody + colliders on. Off while the item flies to or sits in the tray.</summary>
        private void SetSimulated(bool simulated)
        {
            if (_rigidbody) _rigidbody.isKinematic = !simulated;
            SetCollidersEnabled(simulated);
        }

        private void SetCollidersEnabled(bool on)
        {
            if (_colliders == null) return;
            foreach (var c in _colliders)
                if (c) c.enabled = on;
        }

        /// <summary>Adds the outline as a second material pass (the model must have exactly one submesh).</summary>
        public void Select(Material outlineMaterial)
        {
            _selectedMaterials[1] = outlineMaterial;
            _renderer.sharedMaterials = _selectedMaterials;
        }

        public void Deselect() => _renderer.sharedMaterials = _deselectedMaterials;

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

            SetCollidersEnabled(true);
        }
    }
}
