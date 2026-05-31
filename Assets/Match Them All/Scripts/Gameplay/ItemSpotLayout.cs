using UnityEngine;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// Equal spacing and alignment configuration script for child Item Spots.
    /// Attach this to the "Item Spots" parent game object to manage child spacing automatically in the editor.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ItemSpotLayout : MonoBehaviour
    {
        private enum LayoutMode
        {
            SpacingFromFirst,
            FitBetweenEnds,
            Centered
        }

        private enum Axis
        {
            X,
            Y,
            Z
        }

        [Header("Layout Settings")]
        [SerializeField] private LayoutMode layoutMode = LayoutMode.SpacingFromFirst;
        [SerializeField] private Axis axis = Axis.Y;
        [SerializeField] private float spacing = -0.14f;

        [Header("Constraint Settings")]
        [Tooltip("Fixes other axes to their current values instead of overriding them.")]
        [SerializeField] private bool lockOtherAxes = true;

        private void OnTransformChildrenChanged()
        {
            UpdateLayout();
        }

        private void OnValidate()
        {
            UpdateLayout();
        }

        [ContextMenu("Update Layout")]
        public void UpdateLayout()
        {
            int childCount = transform.childCount;
            if (childCount <= 1) return;

            // Collect all children
            Transform[] children = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                children[i] = transform.GetChild(i);
            }

            if (layoutMode == LayoutMode.SpacingFromFirst)
            {
                Vector3 firstPos = children[0].localPosition;
                for (int i = 1; i < childCount; i++)
                {
                    Vector3 localPos = children[i].localPosition;
                    float offset = i * spacing;
                    SetCoordinate(ref localPos, GetCoordinate(firstPos) + offset);
                    if (!lockOtherAxes)
                    {
                        SetOtherCoordinates(ref localPos, firstPos);
                    }
                    children[i].localPosition = localPos;
                }
            }
            else if (layoutMode == LayoutMode.FitBetweenEnds)
            {
                Vector3 firstPos = children[0].localPosition;
                Vector3 lastPos = children[childCount - 1].localPosition;
                float startVal = GetCoordinate(firstPos);
                float endVal = GetCoordinate(lastPos);

                for (int i = 0; i < childCount; i++)
                {
                    float t = (float)i / (childCount - 1);
                    Vector3 localPos = children[i].localPosition;
                    SetCoordinate(ref localPos, Mathf.Lerp(startVal, endVal, t));
                    if (!lockOtherAxes)
                    {
                        SetOtherCoordinates(ref localPos, Vector3.Lerp(firstPos, lastPos, t));
                    }
                    children[i].localPosition = localPos;
                }
            }
            else if (layoutMode == LayoutMode.Centered)
            {
                float totalLength = (childCount - 1) * spacing;
                float startVal = -totalLength / 2f;

                for (int i = 0; i < childCount; i++)
                {
                    Vector3 localPos = children[i].localPosition;
                    float offset = startVal + i * spacing;
                    SetCoordinate(ref localPos, offset);
                    if (!lockOtherAxes)
                    {
                        SetCoordinateOnAxis(ref localPos, GetOtherAxisA(), 0f);
                        SetCoordinateOnAxis(ref localPos, GetOtherAxisB(), 0f);
                    }
                    children[i].localPosition = localPos;
                }
            }
        }

        private float GetCoordinate(Vector3 vec)
        {
            return axis switch
            {
                Axis.X => vec.x,
                Axis.Y => vec.y,
                Axis.Z => vec.z,
                _ => 0f
            };
        }

        private void SetCoordinate(ref Vector3 vec, float value)
        {
            if (axis == Axis.X) vec.x = value;
            else if (axis == Axis.Y) vec.y = value;
            else if (axis == Axis.Z) vec.z = value;
        }

        private void SetCoordinateOnAxis(ref Vector3 vec, Axis a, float value)
        {
            if (a == Axis.X) vec.x = value;
            else if (a == Axis.Y) vec.y = value;
            else if (a == Axis.Z) vec.z = value;
        }

        private void SetOtherCoordinates(ref Vector3 vec, Vector3 source)
        {
            if (axis == Axis.X)
            {
                vec.y = source.y;
                vec.z = source.z;
            }
            else if (axis == Axis.Y)
            {
                vec.x = source.x;
                vec.z = source.z;
            }
            else if (axis == Axis.Z)
            {
                vec.x = source.x;
                vec.y = source.y;
            }
        }

        private Axis GetOtherAxisA()
        {
            return axis switch
            {
                Axis.X => Axis.Y,
                Axis.Y => Axis.X,
                Axis.Z => Axis.X,
                _ => Axis.X
            };
        }

        private Axis GetOtherAxisB()
        {
            return axis switch
            {
                Axis.X => Axis.Z,
                Axis.Y => Axis.Z,
                Axis.Z => Axis.Y,
                _ => Axis.Z
            };
        }
    }
}
