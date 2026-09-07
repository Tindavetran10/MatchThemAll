using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MatchThemAll.Scripts.UI
{
    /// <summary>
    /// Draws the connecting path line between saga-map nodes. A single Canvas Graphic that
    /// renders a polyline through its points (one draw, no per-segment GameObjects).
    /// raycastTarget = false so it never blocks node taps.
    /// </summary>
    public class LevelMapPath : Graphic
    {
        [Tooltip("Thickness of the connecting line, in the Canvas's local units.")]
        [SerializeField] private float thickness = 16f;

        private readonly List<Vector2> _points = new();

        /// <summary>Replace the path points and rebuild. Points are in this Graphic's local space.</summary>
        public void SetPoints(IEnumerable<Vector2> points)
        {
            _points.Clear();
            if (points != null) _points.AddRange(points);
            SetVerticesDirty();
        }

        public void ClearPoints()
        {
            _points.Clear();
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false; // the path is decorative; never intercept clicks
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 2 || color.a <= 0f) return;

            // Build a quad pair per segment: two vertices on each side of the line (thickness offset along the normal).
            for (int i = 0; i < _points.Count - 1; i++)
            {
                Vector2 a = _points[i];
                Vector2 b = _points[i + 1];
                Vector2 dir = (b - a);
                float len = dir.magnitude;
                if (len < 0.001f) continue;
                Vector2 normal = new Vector2(-dir.y, dir.x) / len * (thickness * 0.5f);

                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(a.x - normal.x, a.y - normal.y), color, Vector2.zero);
                vh.AddVert(new Vector3(a.x + normal.x, a.y + normal.y), color, Vector2.zero);
                vh.AddVert(new Vector3(b.x + normal.x, b.y + normal.y), color, Vector2.zero);
                vh.AddVert(new Vector3(b.x - normal.x, b.y - normal.y), color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
