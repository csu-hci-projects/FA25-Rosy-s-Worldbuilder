using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class TileRecord
{
    [SerializeField] private string id;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private string displayName;

    [SerializeField] private Sprite springIcon;
    [SerializeField] private Sprite summerIcon;
    [SerializeField] private Sprite fallIcon;
    [SerializeField] private Sprite winterIcon;

    public string Id => id;
    public GameObject Prefab => tilePrefab;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;

    public Sprite GetIcon(Season s)
    {
        return s switch
        {
            Season.Spring => springIcon,
            Season.Summer => summerIcon,
            Season.Fall => fallIcon,
            Season.Winter => winterIcon,
            _ => null
        };
    }

    public void SetId(string value) => id = value;
    public void SetDisplayName(string value) => displayName = value;
    public void SetPrefab(GameObject go) => tilePrefab = go;

    public void SetIcon(Season s, Sprite sprite)
    {
        switch (s)
        {
            case Season.Spring: springIcon = sprite; break;
            case Season.Summer: summerIcon = sprite; break;
            case Season.Fall: fallIcon = sprite; break;
            case Season.Winter: winterIcon = sprite; break;
        }
    }
}

[CreateAssetMenu(fileName = "TileRegistry", menuName = "Game/Tile Registry", order = 1)]
public class TileRegistry : ScriptableObject
{
    [SerializeField] private List<TileRecord> records = new();
    private Dictionary<string, TileRecord> _byId;

    void OnEnable() => BuildIndex();

    public void BuildIndex()
    {
        _byId = new Dictionary<string, TileRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in records)
        {
            if (r == null || string.IsNullOrWhiteSpace(r.Id)) continue;
            if (_byId.ContainsKey(r.Id))
                Debug.LogWarning($"Duplicate TileRecord ID '{r.Id}' found. Keeping first.");
            else
                _byId.Add(r.Id, r);
        }
    }

    public IReadOnlyList<TileRecord> Records => records;
    public IEnumerable<string> AllIds()
    {
        foreach (var r in records) if (r != null && !string.IsNullOrWhiteSpace(r.Id)) yield return r.Id;
    }

    public bool TryGet(string id, out TileRecord rec)
    {
        if (_byId == null || _byId.Count != records.Count) BuildIndex();
        rec = null;
        return id != null && _byId.TryGetValue(id, out rec);
    }

    public TileRecord GetOrNull(string id) => TryGet(id, out var r) ? r : null;

#if UNITY_EDITOR
    public void AddOrReplace(TileRecord incoming)
    {
        if (incoming == null || string.IsNullOrWhiteSpace(incoming.Id)) return;
        int idx = records.FindIndex(r => r != null && string.Equals(r.Id, incoming.Id, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) records[idx] = incoming;
        else records.Add(incoming);
        BuildIndex();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void AddIfNotExists(TileRecord incoming)
    {
        if (incoming == null || string.IsNullOrWhiteSpace(incoming.Id)) return;
        if (records.Exists(r => r != null && string.Equals(r.Id, incoming.Id, StringComparison.OrdinalIgnoreCase))) return;
        records.Add(incoming);
        BuildIndex();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
