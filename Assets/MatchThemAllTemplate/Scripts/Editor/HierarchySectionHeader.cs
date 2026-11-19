#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Hierarchy separators: any GameObject whose name starts with "//" is drawn as a section bar
/// (e.g. "// MANAGERS"). Purely visual; the object itself is untouched.
/// </summary>
[InitializeOnLoad]
public static class HierarchySectionHeader
{
    private const string Prefix = "//";
    private static readonly Color Bar = new(0.17f, 0.19f, 0.23f, 1f);
    private static readonly Color Accent = new(0.35f, 0.65f, 1f, 1f);
    private static GUIStyle _label;

    static HierarchySectionHeader() => EditorApplication.hierarchyWindowItemOnGUI += Draw;

    private static void Draw(int instanceId, Rect row)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (EditorUtility.InstanceIDToObject(instanceId) is not GameObject go) return;
        if (!go.name.StartsWith(Prefix, System.StringComparison.Ordinal)) return;

        _label ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };

        EditorGUI.DrawRect(row, Bar);
        EditorGUI.DrawRect(new Rect(row.x, row.y, 3f, row.height), Accent);
        var text = go.name[Prefix.Length..].Trim().ToUpperInvariant();
        _label.Draw(new Rect(row.x + 8f, row.y, row.width - 8f, row.height), text, false, false, false, false);
    }
}
#endif
