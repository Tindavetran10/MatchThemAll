using PrimeTween;
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
        [Tooltip("Higher = snappier. The squash/release tween lasts 3 / speed seconds.")]
        [SerializeField] private float speed = 18f;
        [Tooltip("Sound played when the button is pressed. Set to None for silent buttons.")]
        [SerializeField] private SoundEvent clickSound = SoundEvent.ButtonClick;

        private Vector3 _baseScale = Vector3.one;
        private float _target = 1f;
        private Selectable _selectable;
        private Tween _tween;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _selectable = GetComponent<Selectable>();
        }

        private void OnDisable()
        {
            _tween.Stop();
            _target = 1f;
            transform.localScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable && !_selectable.IsInteractable()) return;
            ScaleTo(pressedScale);
            SoundManager.Sfx(clickSound);
        }

        public void OnPointerUp(PointerEventData eventData) => ScaleTo(1f);
        public void OnPointerExit(PointerEventData eventData) => ScaleTo(1f);

        private void ScaleTo(float target)
        {
            if (Mathf.Approximately(_target, target)) return;
            _target = target;
            _tween.Stop();
            _tween = Tween.Scale(transform, _baseScale * target, 3f / speed, Ease.OutCubic, useUnscaledTime: true);
        }
    }
}
