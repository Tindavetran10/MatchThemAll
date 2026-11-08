using System;
using PrimeTween;
using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Lightweight decorative motion for menu art (rotating shines, floating titles, pulsing buttons).
    /// Driven by an infinite PrimeTween started in OnEnable. Uses unscaled time so menus animate even when Time.timeScale is 0.
    /// </summary>
    public class UISimpleMotion : MonoBehaviour
    {
        public enum Mode { Rotate, Float, Pulse }

        [SerializeField] private Mode mode = Mode.Float;
        [Tooltip("Rotate: degrees/sec. Float: pixels of travel. Pulse: scale amount (e.g. 0.05).")]
        [SerializeField] private float amount = 12f;
        [Tooltip("Cycles per second for Float/Pulse.")]
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private float phase;

        private Vector3 _basePos;
        private Vector3 _baseScale;
        private Tween _tween;

        public void Setup(Mode m, float amt, float spd, float ph = 0f)
        {
            mode = m; amount = amt; speed = spd; phase = ph;
        }

        private void OnEnable()
        {
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;

            switch (mode)
            {
                case Mode.Rotate:
                    if (amount == 0f) return;
                    var start = transform.localEulerAngles;
                    _tween = Tween.LocalEulerAngles(transform, start, start + new Vector3(0f, 0f, -360f * Mathf.Sign(amount)),
                        360f / Mathf.Abs(amount), Ease.Linear, cycles: -1, useUnscaledTime: true);
                    break;
                case Mode.Float:
                    if (speed <= 0f) return;
                    _tween = Tween.LocalPositionY(transform, _basePos.y - amount, _basePos.y + amount,
                        0.5f / speed, Ease.InOutSine, cycles: -1, CycleMode.Yoyo, useUnscaledTime: true);
                    SetPhase();
                    break;
                case Mode.Pulse:
                    if (speed <= 0f) return;
                    _tween = Tween.Scale(transform, _baseScale * (1f - amount), _baseScale * (1f + amount),
                        0.5f / speed, Ease.InOutSine, cycles: -1, CycleMode.Yoyo, useUnscaledTime: true);
                    SetPhase();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // An InOutSine yoyo between -amount and +amount traces -cos; a quarter period ahead it matches
        // the original sin((t * speed + phase) * 2π) curve, so existing phase values keep their timing.
        private void SetPhase() => _tween.elapsedTimeTotal = Mathf.Repeat(phase + 0.25f, 1f) / speed;

        private void OnDisable()
        {
            _tween.Stop();
            switch (mode)
            {
                case Mode.Float:
                    transform.localPosition = _basePos;
                    break;
                case Mode.Pulse:
                    transform.localScale = _baseScale;
                    break;
                case Mode.Rotate:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
