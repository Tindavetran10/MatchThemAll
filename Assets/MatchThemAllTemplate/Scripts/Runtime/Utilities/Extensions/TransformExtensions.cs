using UnityEngine;

namespace MatchThemAll.Scripts.Extensions
{
    public static class TransformExtensions
    {
        public static void Clear(this Transform transform)
        {
            while (transform.childCount > 0)
            {
                var child = transform.GetChild(0);
                child.parent = null;
                Object.Destroy(child.gameObject);
            }
        }
        
        public static Transform GetLast(this Transform transform) => 
            transform.childCount <= 0 ? null : transform.GetChild(transform.childCount - 1);
    }
}