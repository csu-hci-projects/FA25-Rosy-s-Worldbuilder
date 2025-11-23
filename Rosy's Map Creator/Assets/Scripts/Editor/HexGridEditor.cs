#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexGrid))]
public class HexGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // draw normal fields first
        DrawDefaultInspector();

        var grid = (HexGrid)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hex Grid Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Layout Grid"))
        {
            grid.LayoutGrid();
        }

        if (GUILayout.Button("Clear Grid"))
        {
            grid.Clear();
        }
    }
}
#endif