using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MatchThemAll.Scripts.SaveSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Scrolling level saga map.
    /// • Level 1 at the BOTTOM of the content (y = 0, the content's pivot).
    /// • Highest level at the TOP.
    /// • Content height grows naturally with the number of levels.
    /// • Drag finger DOWN → content moves down → higher levels come into view.
    /// • movementType = Clamped – zero elastic snap-back.
    /// </summary>
    public class LevelMapManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelMapNode nodePrefab;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private LevelMapPath pathRenderer;
        [SerializeField] private Button backButton;

        [Header("Layout (device-robust — computed from viewport at runtime)")]
        [Tooltip("Node square size, as a fraction of the viewport width (e.g. 0.22 = 22% of screen width).")]
        [SerializeField, Range(0.1f, 0.4f)] private float nodeSizeFraction = 0.22f;
        [Tooltip("Vertical gap between nodes, as a multiple of node size (e.g. 1.6 = comfortable spacing).")]
        [SerializeField, Range(0.5f, 3f)] private float verticalSpacingMultiplier = 1.6f;
        [Tooltip("Half the horizontal zig-zag swing, as a fraction of viewport width. Clamped so nodes stay on-screen.")]
        [SerializeField, Range(0f, 0.35f)] private float zigzagFraction = 0.26f;
        [Tooltip("Scroll slack ABOVE the top node, as a fraction of viewport height (breathing room so the top node isn't cut off).")]
        [SerializeField, Range(0f, 1f)] private float topPaddingFraction = 0.5f;
        [Tooltip("Gap between level 1 and the bottom edge, as a fraction of viewport height. Keep small so level 1 sits near the bottom.")]
        [SerializeField, Range(0f, 1f)] private float bottomPaddingFraction = 0.15f;

        // Resolved in GenerateMap from the live viewport (device-dependent → consistent look everywhere).
        private float _nodeSize;
        private float _verticalSpacing;
        private float _zigzagWidth;
        private float _topPadding;
        private float _bottomPadding;

        private readonly List<LevelDataSO> _levels = new();
        private readonly List<LevelMapNode> _nodes  = new();

        // Remembered vertical scroll position across scene reloads (0 = bottom, 1 = top).
        // Static so it survives navigating MainMenu → LevelSelect → MainMenu. Null = never set.
        private static float? _rememberedNormalizedY;

        private async void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            EnsureViewportRaycastImage();

            // Clamped = no elastic bounce/snap-back, ever.
            if (scrollRect != null)
            {
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.elasticity   = 0f;
                scrollRect.onValueChanged.AddListener(OnScrollChanged);
            }

            try { await LoadLevelsAsync(); }
            catch (Exception e) { Debug.LogException(e); }

            // Hand off to a coroutine so we can yield until layout is ready.
            StartCoroutine(BuildAfterLayout());
        }

        private void OnScrollChanged(Vector2 pos)
        {
            // Remember the vertical scroll so re-entering the map restores the player's spot.
            _rememberedNormalizedY = pos.y;
        }

        private IEnumerator BuildAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            GenerateMap();          // uses viewport.rect.height (now correct)
            // Restore the player's last scroll position if we have one; otherwise snap to the furthest level.
            if (_rememberedNormalizedY.HasValue)
                RestoreScroll(_rememberedNormalizedY.Value);
            else
                SnapToFurthest();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }

        public void OnBackClicked() => SceneLoader.Load(SceneLoader.MainMenu);

        // ── Viewport background ──────────────────────────────────────────────

        private void EnsureViewportRaycastImage()
        {
            if (scrollRect?.viewport == null) return;
            var img = scrollRect.viewport.GetComponent<Image>()
                   ?? scrollRect.viewport.gameObject.AddComponent<Image>();
            img.color         = new Color(0, 0, 0, 0);
            img.raycastTarget = true;
        }

        // ── Data loading ─────────────────────────────────────────────────────

        private async Task LoadLevelsAsync()
        {
            var handle = Addressables.LoadAssetsAsync<LevelDataSO>("LevelData");
            var loaded = await handle.Task;
            _levels.Clear();
            if (loaded != null)
            {
                _levels.AddRange(loaded);
                _levels.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            }
        }

        // ── Map generation ───────────────────────────────────────────────────

        private void GenerateMap()
        {
            if (nodePrefab == null || contentRoot == null)
            {
                Debug.LogError("[LevelMapManager] nodePrefab/contentRoot not assigned.", this);
                return;
            }
            if (_levels.Count == 0)
            {
                Debug.LogWarning("[LevelMapManager] No levels found (label: LevelData).", this);
                return;
            }

            ClearExisting();

            // Resolve layout from the LIVE viewport so it looks the same on any portrait device.
            // (CanvasScaler matches height → vertical units constant; width clamped via fractions below.)
            ResolveLayout();

            // Enforce bottom-center anchoring and pivot so height expansion only goes UP.
            contentRoot.anchorMin = new Vector2(0.5f, 0f);
            contentRoot.anchorMax = new Vector2(0.5f, 0f);
            contentRoot.pivot = new Vector2(0.5f, 0f);
            contentRoot.anchoredPosition = new Vector2(contentRoot.anchoredPosition.x, 0f);

            var ids      = BuildOrderedIds();
            int progress = ResolveCurrentProgressIndex(ids);
            var pathPoints = new List<Vector2>(_levels.Count);

            for (int i = 0; i < _levels.Count; i++)
            {
                Vector2 pos = NodeLocalPosition(i);

                LevelMapNode node = Instantiate(nodePrefab, contentRoot);
                node.gameObject.name = $"Node_{i + 1:00}_{_levels[i].Id}";

                if (node.transform is RectTransform rt)
                {
                    // Enforce bottom-center anchors so position is measured from the bottom of the Content
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    // Pivot can remain (0.5, 0.5) so the node is centered on the specified Y coordinate
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    // Size the node from the live viewport so it scales consistently across devices.
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _nodeSize);
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   _nodeSize);
                    rt.anchoredPosition = pos;
                    rt.gameObject.SetActive(true);
                }

                node.Configure(_levels[i],
                    orderedIndex: i,
                    currentProgressIndex: progress,
                    bestStars: SaveManager.GetLevelStars(_levels[i].Id));

                _nodes.Add(node);
                pathPoints.Add(pos);
            }

            SizeContent();

            if (pathRenderer != null)
            {
                // Align path exactly with Content so node positions (local to Content) match path coordinates
                var pathRt = pathRenderer.GetComponent<RectTransform>();
                if (pathRt != null)
                {
                    pathRt.anchorMin = new Vector2(0.5f, 0f);
                    pathRt.anchorMax = new Vector2(0.5f, 0f);
                    pathRt.pivot = new Vector2(0.5f, 0f);
                    pathRt.anchoredPosition = Vector2.zero;
                    pathRt.sizeDelta = contentRoot.sizeDelta;
                }
                pathRenderer.SetPoints(pathPoints);
            }
        }

        /// <summary>
        /// Derives every layout dimension from the live viewport rect, so the map looks identical
        /// (relative to the screen) on any portrait phone/tablet. Must run AFTER the canvas has a real
        /// size (i.e. inside GenerateMap, which runs from the BuildAfterLayout coroutine post-yield).
        /// </summary>
        private void ResolveLayout()
        {
            float vw = scrollRect != null && scrollRect.viewport != null ? scrollRect.viewport.rect.width  : 1080f;
            float vh = scrollRect != null && scrollRect.viewport != null ? scrollRect.viewport.rect.height : 1920f;
            vh = Mathf.Max(1f, vh); vw = Mathf.Max(1f, vw);

            _nodeSize        = vw * nodeSizeFraction;
            _verticalSpacing = _nodeSize * verticalSpacingMultiplier;
            _zigzagWidth     = vw * zigzagFraction;
            _topPadding      = vh * topPaddingFraction;
            _bottomPadding   = vh * bottomPaddingFraction;
        }

        /// <summary>
        /// index 0 → Level 1 → sits at y=_bottomPadding.
        /// index N-1 → Highest level → sits near the top.
        /// Content pivot=(0.5,0) so positive y goes upward.
        /// </summary>
        private Vector2 NodeLocalPosition(int index)
        {
            float x = _levels.Count > 1
                ? Mathf.Sin((index / (float)(_levels.Count - 1)) * Mathf.PI * 2f) * _zigzagWidth
                : 0f;
            // index 0 → y = _bottomPadding. Each subsequent level adds _verticalSpacing.
            float y = _bottomPadding + index * _verticalSpacing;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Content height = _bottomPadding + (levels − 1) spacings + _topPadding so the highest node has
        /// breathing room above it. Grows automatically as more levels are added.
        /// </summary>
        private void SizeContent()
        {
            float height = _bottomPadding + (_levels.Count - 1) * _verticalSpacing + _topPadding;
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        // ── Scroll position (one-shot, on build) ─────────────────────────────

        private float ComputedContentHeight()
            => _bottomPadding + (_levels.Count - 1) * _verticalSpacing + _topPadding;

        private void SnapToFurthest()
        {
            if (scrollRect == null || _levels.Count == 0) return;

            float contentH   = ComputedContentHeight();     // use the formula, not rect.height (can be stale post-size)
            float viewportH  = scrollRect.viewport.rect.height;
            float scrollable = Mathf.Max(0f, contentH - viewportH);

            if (scrollable < 1f)
            {
                SetScrollNormalized(0f); // content fits — show level 1 at the bottom
                return;
            }

            int   idx   = ResolveCurrentProgressIndex(BuildOrderedIds());
            float nodeY = _bottomPadding + idx * _verticalSpacing;

            // Place the furthest node ~2/3 down the viewport (lower third) — so level 1 reads as
            // "near the bottom" on a fresh save, and the next levels scroll up into view above it.
            float nodeScreenOffsetFromTop = viewportH * 2f / 3f;
            float contentBottom = nodeY - viewportH + nodeScreenOffsetFromTop;
            float normalized    = Mathf.Clamp01(contentBottom / scrollable);

            SetScrollNormalized(normalized);
        }

        /// <summary>Restores a previously-remembered scroll position (0=bottom, 1=top).</summary>
        private void RestoreScroll(float normalizedY) => SetScrollNormalized(normalizedY);

        /// <summary>Sets the vertical normalized position and forces it to stick (beats ScrollRect's re-clamp on re-entry).</summary>
        private void SetScrollNormalized(float normalizedY)
        {
            if (scrollRect == null) return;
            scrollRect.normalizedPosition = new Vector2(0f, normalizedY);
            scrollRect.velocity = Vector2.zero;
            Canvas.ForceUpdateCanvases();   // commit content size so LateUpdate's clamp agrees
            scrollRect.normalizedPosition = new Vector2(0f, normalizedY);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private int ResolveCurrentProgressIndex(IReadOnlyList<string> ids)
        {
            string furthest = SaveManager.GetFurthestLevelId();
            if (string.IsNullOrEmpty(furthest)) return 0;
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == furthest) return i;
            return 0;
        }

        private IReadOnlyList<string> BuildOrderedIds()
        {
            var ids = new List<string>(_levels.Count);
            foreach (var lvl in _levels) ids.Add(lvl.Id);
            return ids;
        }

        private void ClearExisting()
        {
            foreach (var n in _nodes)
                if (n != null) Destroy(n.gameObject);
            _nodes.Clear();
            if (pathRenderer != null) pathRenderer.ClearPoints();
        }
    }
}
