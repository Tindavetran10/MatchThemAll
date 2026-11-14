#if UNITY_EDITOR
using MatchThemAll.Scripts.Power_Ups;
using UnityEditor;
using UnityEngine;

namespace MatchThemAll.Scripts.Editor
{
    /// <summary>
    /// Live 3D preview of a power-up's tray slot (slot prefab + tray model or icon, counter, badge),
    /// with orbit / pan / zoom like the Item Manager's dock preview. Used by <see cref="PowerupManagerWindow"/>.
    /// </summary>
    public class PowerupSlotPreview
    {
        private const float DefaultYaw = 0f, DefaultPitch = 10f; // the game camera looks along the slot's +Z

        private PreviewRenderUtility _pru;
        private GameObject _instance;
        private PowerupDataSO _shown;
        private int _shownHash;
        private float _yaw = DefaultYaw, _pitch = DefaultPitch, _dist = 3f, _fitDist = 3f;
        private Vector2 _pan;
        private Vector3 _focus;

        public void ResetCamera()
        {
            _yaw = DefaultYaw;
            _pitch = DefaultPitch;
            _dist = _fitDist;
            _pan = Vector2.zero;
        }

        public void Dispose()
        {
            if (_pru != null) { _pru.Cleanup(); _pru = null; }
            _instance = null;
            _shown = null;
        }

        public void Draw(Rect rect, PowerupDataSO so)
        {
            if (Event.current.type == EventType.Layout) return;
            if (_pru == null)
            {
                _pru = new PreviewRenderUtility();
                _pru.camera.fieldOfView = 30f;
                _pru.camera.clearFlags = CameraClearFlags.SolidColor;
                _pru.camera.backgroundColor = new Color(0.15f, 0.15f, 0.17f, 1f);
                _pru.camera.nearClipPlane = 0.01f;
                _pru.camera.farClipPlane = 100f;
                _pru.lights[0].intensity = 1.3f;
                _pru.lights[0].transform.rotation = Quaternion.Euler(30f, 20f, 0f);
                _pru.lights[1].intensity = 0.6f;
            }

            Rebuild(so);
            HandleInput(rect);

            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            var cam = _pru.camera.transform;
            cam.rotation = rot;
            cam.position = _focus + rot * new Vector3(_pan.x, _pan.y, -_dist);

            _pru.BeginPreview(rect, GUIStyle.none);
            _pru.camera.Render();
            _pru.EndAndDrawPreview(rect);

            if (!so || !so.UIPrefab)
                GUI.Label(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, 20), "No slot prefab assigned", EditorStyles.whiteLabel);
            GUI.Label(new Rect(rect.x, rect.yMax - 18, rect.width - 6, 18), "LMB orbit • MMB pan • Scroll zoom", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });
        }

        /// <summary>Re-instantiates the slot when the selection or any field that changes its look changes.</summary>
        private void Rebuild(PowerupDataSO so)
        {
            int hash = 17;
            if (so)
            {
                hash = hash * 31 + (so.UIPrefab ? so.UIPrefab.GetInstanceID() : 0);
                hash = hash * 31 + (so.TrayModel ? so.TrayModel.GetInstanceID() : 0);
                hash = hash * 31 + (so.Icon ? so.Icon.GetInstanceID() : 0);
                hash = hash * 31 + so.defaultAmount;
                hash = hash * 31 + so.ModelOffset.GetHashCode();
                hash = hash * 31 + so.ModelRotation.GetHashCode();
            }
            if (so == _shown && hash == _shownHash && _instance) return;
            bool selectionChanged = so != _shown || !_instance; // only refit the camera for a new selection, not for pose tweaks
            _shown = so;
            _shownHash = hash;
            if (_instance) Object.DestroyImmediate(_instance);
            if (!so || !so.UIPrefab) return;

            _instance = _pru.InstantiatePrefabInScene(so.UIPrefab);
            var pu = _instance.GetComponentInChildren<Powerup>(true);
            if (pu)
            {
                pu.Configure(so);
                pu.UpdateVisuals(so.defaultAmount, false);
            }

            var renderers = _instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            _focus = b.center;
            _fitDist = Mathf.Max(0.5f, b.extents.magnitude / Mathf.Tan(15f * Mathf.Deg2Rad));
            if (selectionChanged) ResetCamera();
        }

        private void HandleInput(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                _yaw += e.delta.x * 0.5f;
                _pitch = Mathf.Clamp(_pitch + e.delta.y * 0.5f, -80f, 80f);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 2)
            {
                _pan += new Vector2(-e.delta.x, e.delta.y) * _dist * 0.002f;
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                _dist = Mathf.Max(0.2f, _dist * (1f + e.delta.y * 0.05f));
                e.Use();
            }
        }
    }
}
#endif
