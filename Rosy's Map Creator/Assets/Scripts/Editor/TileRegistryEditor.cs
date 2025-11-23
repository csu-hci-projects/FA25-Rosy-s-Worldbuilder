#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileRegistry))]
public class TileRegistryEditor : Editor
{
    private TileRegistry _registry;
    private string iconsFolderFilter = "";

    private static readonly (Season season, string suffix)[] SeasonSuffixes = new[]
    {
        (Season.Spring, "_spring"),
        (Season.Summer, "_summer"),
        (Season.Fall,   "_fall"),
        (Season.Winter, "_winter"),
    };

    void OnEnable() { _registry = (TileRegistry)target; }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Registry Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Rebuild Index"))
        {
            _registry.BuildIndex();
            EditorUtility.SetDirty(_registry);
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Add Selected Prefabs"))
        {
            AddSelectedPrefabs();
        }

        EditorGUILayout.Space(6);
        iconsFolderFilter = EditorGUILayout.TextField(new GUIContent("Icons Folder Filter (optional)"), iconsFolderFilter);

        if (GUILayout.Button("Auto-link Icons by Naming"))
        {
            AutoLinkIcons();
        }

        EditorGUILayout.HelpBox(
            "Icon naming: <ID>_spring / _summer / _fall / _winter\n" +
            "If a folder filter is set, only assets under that path are considered.",
            MessageType.Info);
    }

    private void AddSelectedPrefabs()
    {
        var gos = Selection.objects
            .OfType<GameObject>()
            .Where(go => PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
            .ToArray();

        int added = 0;
        foreach (var go in gos)
        {
            var rec = new TileRecord();
            var id = go.name.Trim();
            rec.SetId(id);
            rec.SetDisplayName(ObjectNames.NicifyVariableName(id));
            rec.SetPrefab(go);

            // Add only if not existing
            _registry.AddIfNotExists(rec);
            added++;
        }

        _registry.BuildIndex();
        EditorUtility.SetDirty(_registry);
        Debug.Log($"TileRegistry: processed {added} prefab(s).");
    }

    private void AutoLinkIcons()
    {
        // Iterate all records and try to find sprites matching "<ID>_<season>"
        int linked = 0;
        foreach (var rec in _registry.Records.Where(r => r != null))
        {
            foreach (var (season, suffix) in SeasonSuffixes)
            {
                string spriteName = rec.Id + suffix;
                string[] guids = string.IsNullOrEmpty(iconsFolderFilter)
                    ? AssetDatabase.FindAssets($"{spriteName} t:Sprite")
                    : AssetDatabase.FindAssets($"{spriteName} t:Sprite", new[] { iconsFolderFilter });

                if (guids.Length == 0) continue;

                // Prefer exact name
                Sprite chosen = null;
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (spr != null && spr.name.Equals(spriteName))
                    {
                        chosen = spr; break;
                    }
                    if (chosen == null && spr != null) chosen = spr;
                }

                if (chosen != null)
                {
                    rec.SetIcon(season, chosen);
                    linked++;
                }
            }
        }

        _registry.BuildIndex();
        EditorUtility.SetDirty(_registry);
        Debug.Log($"TileRegistry: auto-linked {linked} seasonal icon(s).");
    }
}
#endif
