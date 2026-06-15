using UnityEngine;
using UnityEngine.Pool;

namespace MatchThemAll.Scripts.UI
{
    public class FloatingTextSpawner : MonoBehaviour
    {
        public static FloatingTextSpawner Instance;

        [SerializeField] private FloatingText floatingTextPrefab;

        [Tooltip("The RectTransform to spawn floating texts inside. Should be always-active (e.g. Main Canvas root).")]
        [SerializeField] private RectTransform container;

        private Canvas _rootCanvas;
        private ObjectPool<FloatingText> _pool;

        private void Awake()
        {
            if (!Instance) Instance = this;
            else { Destroy(gameObject); return; }

            // Cache the root canvas for coordinate conversion
            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas && !_rootCanvas.isRootCanvas)
                _rootCanvas = _rootCanvas.rootCanvas;

            _pool = new ObjectPool<FloatingText>(
                createFunc: () => Instantiate(floatingTextPrefab, container),
                actionOnGet: ft => ft.gameObject.SetActive(true),
                actionOnRelease: ft => ft.gameObject.SetActive(false),
                actionOnDestroy: ft => Destroy(ft.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        /// <summary>
        /// Spawns floating text at a UI element's position (pass transform.position of any UI element).
        /// </summary>
        public void Spawn(string text, Vector3 uiWorldPosition, Color color)
        {
            if (!floatingTextPrefab || !container) return;

            var inst = _pool.Get();
            var instRect = inst.GetComponent<RectTransform>();

            // Convert world position to canvas local position
            Camera cam = _rootCanvas && _rootCanvas.renderMode == RenderMode.ScreenSpaceCamera
                ? _rootCanvas.worldCamera
                : null; // null = screen space overlay

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container,
                RectTransformUtility.WorldToScreenPoint(cam, uiWorldPosition),
                cam,
                out Vector2 localPoint
            );

            instRect.anchoredPosition = localPoint;
            inst.SetupFloat(text, color, (ft) => _pool.Release(ft));
        }

        /// <summary>
        /// Spawns floating text at a fixed canvas-center position. Useful for testing.
        /// </summary>
        public void SpawnAtCenter(string text, Color color)
        {
            if (!floatingTextPrefab || !container) return;

            var inst = _pool.Get();
            inst.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            inst.SetupFloat(text, color, (ft) => _pool.Release(ft));
        }
    }
}
