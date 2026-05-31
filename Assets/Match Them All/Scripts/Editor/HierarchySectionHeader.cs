#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class HierarchySectionHeader
{
    static HierarchySectionHeader()
    {
        // Use the legacy hierarchy window callback which works across Unity versions
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
    }

    // Legacy callback: receives the instance ID of the object being drawn
    private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        DrawHighlight(gameObject, selectionRect);
    }

    private static void DrawHighlight(GameObject gameObject, Rect selectionRect)
    {
        if (gameObject != null && gameObject.name.StartsWith("//", System.StringComparison.Ordinal))
        {
            // Dark background for the header
            EditorGUI.DrawRect(selectionRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            // Display the name without slashes, uppercase, centered
            EditorGUI.LabelField(selectionRect, gameObject.name.Replace("/", "").ToUpperInvariant(), new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
        }
    }
}
#endif