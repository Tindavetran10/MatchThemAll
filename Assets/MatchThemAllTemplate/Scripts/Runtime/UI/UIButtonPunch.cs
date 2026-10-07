using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Cartoon-style press feedback: squashes the button while pressed and springs back on release.
    /// Uses unscaled time so it still works while the game is paused.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonPunch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float speed = 18f;

        private Vector3 _baseScale = Vector3.one;
        private float _target = 1f;
        private float _current = 1f;
        private Selectable _selectable;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _selectable = GetComponent<Selectable>();
        }

        private void OnDisable()
        {
            _target = _current = 1f;
            transform.localScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable && !_selectable.IsInteractable()) return;
            _target = pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => _target = 1f;
        public void OnPointerExit(PointerEventData eventData) => _target = 1f;

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.Lerp(_current, _target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            if (Mathf.Abs(_current - _target) < 0.001f) _current = _target;
            transform.localScale = _baseScale * _current;
        }
    }
}
