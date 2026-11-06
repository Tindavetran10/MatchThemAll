using System.Collections.Generic;
using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Lines up the active children (the tray's item spots) along one local axis, in edit mode too.
    /// Put it on the "Item Spots" parent; ItemSpotManager calls <see cref="UpdateLayout"/> after it
    /// hides spots for levels with fewer slots.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ItemSpotLayout : MonoBehaviour
    {
        // Serialized as ints: append new values, never reorder.
        private enum LayoutMode { SpacingFromFirst, FitBetweenEnds, Centered }
        private enum Axis { X, Y, Z }

        [Header("Layout Settings")]
        [Tooltip("SpacingFromFirst: keep the first child, step by Spacing. FitBetweenEnds: keep first and last, spread the rest evenly. Centered: step by Spacing around the parent's origin.")]
        [SerializeField] private LayoutMode layoutMode = LayoutMode.SpacingFromFirst;
        [SerializeField] private Axis axis = Axis.Y;
        [SerializeField] private float spacing = -0.14f;

        [Header("Constraint Settings")]
        [Tooltip("Fixes other axes to their current values instead of overriding them.")]
        [SerializeField] private bool lockOtherAxes = true;

        [Header("Filter Settings")]
        [Tooltip("Optional list of children to ignore during layout.")]
        [SerializeField] private List<Transform> ignoredChildren = new();

        private readonly List<Transform> _laidOut = new();

        private void OnTransformChildrenChanged() => UpdateLayout();
        private void OnValidate() => UpdateLayout();

        [ContextMenu("Update Layout")]
        public void UpdateLayout()
        {
            CollectChildren();
            int n = _laidOut.Count;
            if (n < 2) return;

            int a = (int)axis;
            Vector3 first = _laidOut[0].localPosition;
            Vector3 last = _laidOut[n - 1].localPosition;

            for (int i = 0; i < n; i++)
            {
                Vector3 p = _laidOut[i].localPosition;
                switch (layoutMode)
                {
                    case LayoutMode.SpacingFromFirst:
                        if (i == 0) continue; // the first child is the anchor
                        p[a] = first[a] + i * spacing;
                        if (!lockOtherAxes) CopyOtherAxes(ref p, first, a);
                        break;

                    case LayoutMode.FitBetweenEnds:
                        float t = i / (float)(n - 1);
                        Vector3 along = Vector3.Lerp(first, last, t);
                        p[a] = along[a];
                        if (!lockOtherAxes) CopyOtherAxes(ref p, along, a);
                        break;

                    case LayoutMode.Centered:
                        p[a] = (i - (n - 1) * 0.5f) * spacing;
                        if (!lockOtherAxes) CopyOtherAxes(ref p, Vector3.zero, a);
                        break;
                }
                _laidOut[i].localPosition = p;
            }
        }

        private void CollectChildren()
        {
            _laidOut.Clear();
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;
                if (ignoredChildren != null && ignoredChildren.Contains(child)) continue;
                _laidOut.Add(child);
            }
        }

        private static void CopyOtherAxes(ref Vector3 target, Vector3 source, int keepAxis)
        {
            for (int k = 0; k < 3; k++)
                if (k != keepAxis) target[k] = source[k];
        }
    }
}
