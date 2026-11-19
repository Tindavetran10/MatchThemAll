using System.Collections.Generic;
using UnityEngine;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// Lays out the power-up spots under this object (the camera's "Powerup Zone") as one row:
    /// every active child holding a <see cref="Powerup"/> is a slot, ordered left→right by hierarchy order.
    /// Slots keep <see cref="slotPitch"/> spacing while they fit the screen width; past that, all slots
    /// shrink evenly around <see cref="rowCenter"/>. To add a power-up, duplicate a spot (it brings its
    /// own Slot Background) — the row re-flows in the editor and at runtime. Children without a
    /// Powerup (e.g. VFX anchors) are left untouched.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PowerupTrayLayout : MonoBehaviour
    {
        [Tooltip("Slot spacing at full size, in camera-local units.")]
        [SerializeField] private float slotPitch = 1.15f;
        [Tooltip("Camera-local centre of the tray row; slots scale around this point.")]
        [SerializeField] private Vector3 rowCenter = new(0f, -3.93f, 4.9f);
        [Tooltip("Gap kept between the outer slots and the screen edges, in camera-local units.")]
        [SerializeField] private float sideMargin = 0.03f;

        private readonly List<Transform> _slots = new();
        private Camera _camera;
        private float _laidOutAspect = -1f;

        private void Start() => Layout();

        private void OnTransformChildrenChanged() => Layout();

        private void Update()
        {
            // Editor: re-flow on any change (spots added, removed, reordered, toggled). Runtime: only on aspect change.
            if (!Application.isPlaying || (_camera && !Mathf.Approximately(_camera.aspect, _laidOutAspect)))
                Layout();
        }

        [ContextMenu("Layout")]
        public void Layout()
        {
            if (!_camera) _camera = GetComponentInParent<Camera>();

            _slots.Clear();
            foreach (Transform child in transform)
                if (child.gameObject.activeSelf && child.GetComponentInChildren<Powerup>(true))
                    _slots.Add(child);

            int count = _slots.Count;
            if (count == 0) return;

            // ponytail: assumes the orthographic game camera; a perspective camera would need the frustum width at rowCenter.z.
            float width = _camera ? _camera.orthographicSize * _camera.aspect * 2f - sideMargin * 2f : count * slotPitch;
            if (_camera) _laidOutAspect = _camera.aspect;

            float pitch = Mathf.Min(slotPitch, width / count);
            float scale = pitch / slotPitch;
            for (int i = 0; i < count; i++)
            {
                // Spot contents are authored around the spot origin, so scaling the spot about rowCenter
                // means offsetting it by rowCenter * (1 - scale).
                var position = new Vector3(rowCenter.x + (i - (count - 1) * 0.5f) * pitch,
                    rowCenter.y * (1f - scale), rowCenter.z * (1f - scale));
                var slot = _slots[i];
                if (slot.localPosition != position) slot.localPosition = position;
                if (slot.localScale != Vector3.one * scale) slot.localScale = Vector3.one * scale;
            }
        }
    }
}
