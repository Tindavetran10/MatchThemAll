using PrimeTween;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// The lasting "the level is frozen" look while the Freeze power-up holds the clock:
    /// frost creeping in from the screen edges, ice spheres on the board's corners and edges,
    /// and an icy fill inside the clock pill (behind the digits) showing how much freeze is left, plus a snowflake
    /// badge on the stopwatch. Both live inside the timer, so they can't overlap the goal cards below it. Driven only by
    /// <see cref="TimerFrozenEvent"/> / <see cref="TimerUnfrozenEvent"/> and TimerManager.FreezeRemaining01.
    /// The frost lives on its own Screen Space - Camera canvas placed just behind the spheres, so the spheres,
    /// item spots and tray draw over it while it still frosts the item pile; the HUD (overlay canvas) stays on top.
    /// </summary>
    public class FreezeFx : MonoBehaviour
    {
        [Header("Screen frost")]
        [SerializeField] private CanvasGroup frost;

        [Header("Freeze time left (inside the clock pill)")]
        [Tooltip("Group holding the icy fill; sits inside the timer pill, behind the digits.")]
        [SerializeField] private CanvasGroup bar;
        [Tooltip("The icy fill; its right edge shrinks as the freeze runs out.")]
        [SerializeField] private RectTransform barFill;
        [Tooltip("Snowflake badge on the stopwatch, shown while frozen.")]
        [SerializeField] private CanvasGroup badge;

        [Header("Ice spheres")]
        [SerializeField] private GameObject iceSpherePrefab;
        [SerializeField] private ParticleSystem shatterVfx;
        [Tooltip("World height the spheres sit at (above the item pile, below the camera).")]
        [SerializeField] private float boardHeight = 4f;
        [Tooltip("Viewport positions of the spheres. They hug the left/right screen edges (centres just off-screen, so only part shows), " +
                 "from the board's bottom corners (above the item spots and tray) to its top corners (below the HUD).")]
        [SerializeField] private Vector2[] spherePoints =
        {
            new(0f, .30f), new(1f, .30f), new(0f, .80f), new(1f, .80f),
            new(0f, .47f), new(1f, .47f), new(0f, .63f), new(1f, .63f),
        };

        private Transform[] _spheres;
        private Vector3 _sphereScale;
        private bool _frozen;

        private void Awake()
        {
            if (frost) frost.alpha = 0f;
            if (bar) bar.alpha = 0f;
            if (badge) badge.alpha = 0f;
            EventBus.Subscribe<TimerFrozenEvent>(OnFrozen);
            EventBus.Subscribe<TimerUnfrozenEvent>(OnUnfrozen);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TimerFrozenEvent>(OnFrozen);
            EventBus.Unsubscribe<TimerUnfrozenEvent>(OnUnfrozen);
        }

        private void Update()
        {
            if (!_frozen || !barFill || !TimerManager.Instance) return;
            barFill.anchorMax = new Vector2(TimerManager.Instance.FreezeRemaining01, 1f);
        }

        private void OnFrozen(TimerFrozenEvent evt)
        {
            _frozen = true;
            if (frost) Tween.Alpha(frost, 1f, .45f, Ease.OutQuad);
            if (bar) Tween.Alpha(bar, 1f, .25f);
            if (badge) Tween.Alpha(badge, 1f, .25f);
            if (barFill) barFill.anchorMax = Vector2.one;
            ShowSpheres();
        }

        private void OnUnfrozen(TimerUnfrozenEvent evt)
        {
            _frozen = false;
            if (frost) Tween.Alpha(frost, 0f, .6f, Ease.InQuad);
            if (bar) Tween.Alpha(bar, 0f, .25f);
            if (badge) Tween.Alpha(badge, 0f, .25f);
            HideSpheres();
        }

        private void ShowSpheres()
        {
            var cam = Camera.main;
            if (!iceSpherePrefab || !cam) return;
            if (_spheres == null)
            {
                var holder = new GameObject("Ice Spheres").transform;
                _sphereScale = iceSpherePrefab.transform.localScale;
                _spheres = new Transform[spherePoints.Length];
                for (int i = 0; i < _spheres.Length; i++)
                    _spheres[i] = Instantiate(iceSpherePrefab, holder).transform;
            }

            float depth = cam.transform.position.y - boardHeight;
            for (int i = 0; i < _spheres.Length; i++)
            {
                var s = _spheres[i];
                Tween.StopAll(s);
                s.gameObject.SetActive(true);
                s.position = cam.ViewportToWorldPoint(new Vector3(spherePoints[i].x, spherePoints[i].y, depth));
                s.rotation = Random.rotation;
                s.localScale = Vector3.zero;
                Tween.Scale(s, _sphereScale, .4f, Ease.OutBack, startDelay: .06f * i);
            }
        }

        private void HideSpheres()
        {
            if (_spheres == null) return;
            foreach (var s in _spheres)
            {
                if (!s || !s.gameObject.activeSelf) continue;
                Tween.StopAll(s);
                if (shatterVfx && VfxPool.Instance) VfxPool.Instance.Play(shatterVfx, s.position);
                Tween.Scale(s, Vector3.zero, .2f, Ease.InBack).OnComplete(s, t => t.gameObject.SetActive(false));
            }
        }
    }
}
