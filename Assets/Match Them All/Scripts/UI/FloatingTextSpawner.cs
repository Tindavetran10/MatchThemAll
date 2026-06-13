using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    public class FloatingTextSpawner : MonoBehaviour
    {
        public static FloatingTextSpawner Instance;

        [SerializeField] private FloatingText floatingTextPrefab;

        [Tooltip("The RectTransform to spawn floating texts inside. Should be always-active (e.g. Main Canvas root).")]
        [SerializeField] private RectTransform container;

        private Canvas _rootCanvas;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // Cache the root canvas for coordinate conversion
            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas != null && _rootCanvas.isRootCanvas == false)
                _rootCanvas = _rootCanvas.rootCanvas;
        }

        /// <summary>
        /// Spawns floating text at a UI element's position (pass transform.position of any UI element).
        /// </summary>
        public void Spawn(string text, Vector3 uiWorldPosition, Color color)
        {
            if (floatingTextPrefab == null || container == null) return;

            var inst = Instantiate(floatingTextPrefab, container);
            var instRect = inst.GetComponent<RectTransform>();

            // Convert world position to canvas local position
            Camera cam = (_rootCanvas != null && _rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                ? _rootCanvas.worldCamera
                : null; // null = screen space overlay

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container,
                RectTransformUtility.WorldToScreenPoint(cam, uiWorldPosition),
                cam,
                out Vector2 localPoint
            );

            instRect.anchoredPosition = localPoint;
            inst.Setup(text, color);
        }

        /// <summary>
        /// Spawns floating text at a fixed canvas-center position. Useful for testing.
        /// </summary>
        public void SpawnAtCenter(string text, Color color)
        {
            if (floatingTextPrefab == null || container == null) return;

            var inst = Instantiate(floatingTextPrefab, container);
            inst.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            inst.Setup(text, color);
        }
    }
}
