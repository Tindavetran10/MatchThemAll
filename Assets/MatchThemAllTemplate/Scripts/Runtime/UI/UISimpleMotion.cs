using UnityEngine;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Lightweight decorative motion for menu art (rotating shines, floating titles, pulsing buttons).
    /// Uses unscaled time so menus animate even when Time.timeScale is 0.
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

        public void Setup(Mode m, float amt, float spd, float ph = 0f)
        {
            mode = m; amount = amt; speed = spd; phase = ph;
        }

        private void OnEnable()
        {
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            if (mode == Mode.Float) transform.localPosition = _basePos;
            if (mode == Mode.Pulse) transform.localScale = _baseScale;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            switch (mode)
            {
                case Mode.Rotate:
                    transform.Rotate(0f, 0f, -amount * Time.unscaledDeltaTime);
                    break;
                case Mode.Float:
                    transform.localPosition = _basePos + Vector3.up * (Mathf.Sin((t * speed + phase) * Mathf.PI * 2f) * amount);
                    break;
                case Mode.Pulse:
                    float s = 1f + Mathf.Sin((t * speed + phase) * Mathf.PI * 2f) * amount;
                    transform.localScale = _baseScale * s;
                    break;
            }
        }
    }
}
