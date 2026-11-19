using System.Collections.Generic;
using MatchThemAll.Scripts.Power_Ups;
using MatchThemAll.Scripts.SaveSystem;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Sits on a currency pill (CoinPill / GemPill). On <see cref="CurrencyGrantedEvent"/> for its currency,
    /// bursts copies of the pill's icon out of the event's screen position, flies them into the icon, and
    /// counts the text up as each one lands. The pill's display (CoinDisplay / GemDisplay) is paused while
    /// coins fly and re-enabled at the end, so the counter always settles on the saved value.
    /// </summary>
    public class CurrencyFlyEffect : MonoBehaviour
    {
        [SerializeField] private ECurrency currency;
        [Tooltip("Coins fly into this icon; its sprite is also the flying coin.")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI counterText;
        [Tooltip("CoinDisplay / GemDisplay on the counter text. Paused while coins fly.")]
        [SerializeField] private Behaviour counterDisplay;

        [Header("Burst")]
        [SerializeField] private int maxCoins = 10;
        [SerializeField] private float coinSize = 80f;
        [SerializeField] private float burstRadius = 180f;
        [SerializeField] private float burstDuration = 0.35f;
        [SerializeField] private float flyDuration = 0.55f;
        [SerializeField] private float stagger = 0.06f;
        [Tooltip("Sorting order of the overlay the coins fly on (above panels and popups).")]
        [SerializeField] private int sortingOrder = 500;

        private readonly Stack<RectTransform> _pool = new();
        private readonly List<RectTransform> _active = new();
        private readonly List<Sequence> _sequences = new();
        private RectTransform _layer;
        private Canvas _rootCanvas;
        private int _shown;

        private void OnEnable() => EventBus.Subscribe<CurrencyGrantedEvent>(OnGranted);

        private void OnDisable()
        {
            EventBus.Unsubscribe<CurrencyGrantedEvent>(OnGranted);
            foreach (var seq in _sequences) if (seq.isAlive) seq.Stop();
            _sequences.Clear();
            foreach (var coin in _active) Release(coin);
            _active.Clear();
            if (counterDisplay) counterDisplay.enabled = true;
        }

        private void OnGranted(CurrencyGrantedEvent e)
        {
            if (e.Currency != currency || e.Amount <= 0 || !icon || !EnsureLayer()) return;

            // Hold the counter at the value before this grant (or keep counting from where a running burst is).
            if (_active.Count == 0) _shown = SaveManager.GetCurrency(currency) - e.Amount;
            if (counterDisplay) counterDisplay.enabled = false;
            if (counterText) counterText.text = _shown.ToString("N0");

            Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_layer, e.ScreenPosition, cam, out Vector3 from);
            float unit = _layer.lossyScale.x;

            int count = Mathf.Min(e.Amount, maxCoins);
            for (int i = 0; i < count; i++)
            {
                int share = e.Amount / count + (i < e.Amount % count ? 1 : 0);
                RectTransform coin = Get();
                coin.position = from;
                coin.localScale = Vector3.zero;
                _active.Add(coin);

                Vector3 burstTo = from + (Vector3)(Random.insideUnitCircle * (burstRadius * unit));
                float delay = e.Delay + i * stagger;
                // startDelay, not ChainDelay: the first coin's delay is often 0 and PrimeTween warns on a 0 s delay.
                _sequences.Add(Sequence.Create()
                    .Chain(Tween.Scale(coin, 1f, burstDuration, Ease.OutBack, startDelay: delay))
                    .Group(Tween.Position(coin, burstTo, burstDuration, Ease.OutCubic, startDelay: delay))
                    .Chain(Tween.Position(coin, icon.transform.position, flyDuration, Ease.InCubic))
                    .Group(Tween.Scale(coin, 0.6f, flyDuration, Ease.InCubic))
                    .ChainCallback(this, self => self.Arrive(coin, share)));
            }
        }

        private void Arrive(RectTransform coin, int share)
        {
            _active.Remove(coin);
            Release(coin);

            _shown += share;
            if (counterText) counterText.text = _shown.ToString("N0");
            Tween.StopAll(icon.transform);
            icon.transform.localScale = Vector3.one;
            Tween.PunchScale(icon.transform, Vector3.one * 0.25f, 0.2f);
            SoundManager.Sfx(SoundEvent.CoinReward); // one clink per coin that lands

            // Last coin: hand the counter back to its display, which shows the saved value.
            if (_active.Count > 0) return;
            _sequences.Clear();
            if (counterDisplay) counterDisplay.enabled = true;
        }

        // ── Pool ──────────────────────────────────────────────────────────────

        private bool EnsureLayer()
        {
            if (_layer) return true;
            _rootCanvas = icon.canvas ? icon.canvas.rootCanvas : null;
            if (!_rootCanvas) return false;

            var go = new GameObject("CurrencyFlyLayer", typeof(RectTransform), typeof(Canvas));
            _layer = (RectTransform)go.transform;
            _layer.SetParent(_rootCanvas.transform, false);
            _layer.anchorMin = Vector2.zero;
            _layer.anchorMax = Vector2.one;
            _layer.offsetMin = _layer.offsetMax = Vector2.zero;
            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            return true;
        }

        private RectTransform Get()
        {
            if (_pool.Count > 0)
            {
                var pooled = _pool.Pop();
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            var go = new GameObject("FlyingCoin", typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>();
            img.sprite = icon.sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.SetParent(_layer, false);
            rt.sizeDelta = Vector2.one * coinSize;
            return rt;
        }

        private void Release(RectTransform coin)
        {
            if (!coin) return;
            coin.gameObject.SetActive(false);
            _pool.Push(coin);
        }
    }
}
