#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapChunkSpawner))]
public class MapChunkSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var spawner = (MapChunkSpawner)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Map Chunk Tools", EditorStyles.boldLabel);
        if (GUILayout.Button("Roll"))
        {
            spawner.LayoutGrid();
        }
    }
}
#endif
