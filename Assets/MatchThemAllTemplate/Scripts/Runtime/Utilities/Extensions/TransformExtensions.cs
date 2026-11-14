using UnityEngine;

namespace MatchThemAll.Scripts.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>Destroys every child. They are detached at once, so childCount is 0 straight after the call.</summary>
        public static void Clear(this Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.Destroy(parent.GetChild(i).gameObject);
            parent.DetachChildren();
        }
    }
}
