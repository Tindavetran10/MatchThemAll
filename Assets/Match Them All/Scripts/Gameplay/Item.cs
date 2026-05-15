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

        public ItemSpot spot { get; private set; }

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

        public void AssignSpot(ItemSpot spot) => this.spot = spot;

        public void DisableShadow() =>
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        public void DisablePhysics()
        {
            _rigidbody.isKinematic = true;
            _collider.enabled = false;
        }

        public void Select(Material outlineMaterial) =>
            _renderer.materials = new[] { _baseMaterial, outlineMaterial };

        public void Deselect() =>
            _renderer.materials = new[] { _baseMaterial };
    }
}