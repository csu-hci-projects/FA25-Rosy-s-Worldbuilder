using System;
using System.Collections.Generic;
using UnityEngine;



public enum AssetCategory
{
    Tile,
    Unit,
    Building,
    Decoration
}

public enum Faction
{
    None,
    Neutral,
    Blue,
    Green,
    Red,
    Yellow
}

[Serializable]
public class GameAssetRecord
{
    [SerializeField] private int numericId;             // primary numeric ID
    [SerializeField] private string id;                 // optional: path-style id, e.g. "tiles/base/grass_01"
    [SerializeField] private GameObject prefab;
    [SerializeField] private AssetCategory category;
    [SerializeField] private Faction faction;
    [SerializeField] private string displayName;

    [Header("Seasonal UI Icons")]
    [SerializeField] private Sprite springIcon;
    [SerializeField] private Sprite summerIcon;
    [SerializeField] private Sprite fallIcon;
    [SerializeField] private Sprite winterIcon;

    // -------- Public API --------

    /// <summary>Numeric ID to use in code / saves.</summary>
    public int NumericId => numericId;

    /// <summary>Optional legacy/string ID (path-ish). Not required once you’re on numeric IDs.</summary>
    public string Id => id;

    public GameObject Prefab => prefab;
    public AssetCategory Category => category;
    public Faction Faction => faction;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? (prefab != null ? prefab.name : (!string.IsNullOrEmpty(id) ? id : numericId.ToString()))
            : displayName;

    public string GetFormattedName()
    {
        //Remove underscores and capitalize after underscore
        string formattedName = DisplayName.Replace("_", " ");
        formattedName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formattedName);
        return formattedName;
    }

    public Sprite GetIcon(Season season)
    {
        return season switch
        {
            Season.Spring => springIcon,
            Season.Summer => summerIcon,
            Season.Fall => fallIcon,
            Season.Winter => winterIcon,
            _ => null
        };
    }

    public GameObject GetPrefab()
    {
        return prefab;
    }

#if UNITY_EDITOR
    // Editor setters used by your registry builder

    public void SetNumericId(int value) => numericId = value;
    public void SetId(string value) => id = value;
    public void SetPrefab(GameObject go) => prefab = go;
    public void SetCategory(AssetCategory c) => category = c;
    public void SetFaction(Faction f) => faction = f;
    public void SetDisplayName(string value) => displayName = value;

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
#endif
}

[CreateAssetMenu(fileName = "GameAssetRegistry", menuName = "Game/Asset Registry", order = 1)]
public class GameAssetRegistry : ScriptableObject
{
    [SerializeField] private List<GameAssetRecord> records = new();

    [Header("Season Materials (for auto icon generation)")]
    public Material springMaterial;
    public Material summerMaterial;
    public Material fallMaterial;
    public Material winterMaterial;

    private Dictionary<int, GameAssetRecord> _byNumericId;
    private Dictionary<string, GameAssetRecord> _byId;

    private void OnEnable()
    {
        BuildIndex();
    }

    public IReadOnlyList<GameAssetRecord> Records => records;

    public void BuildIndex()
    {
        _byNumericId = new Dictionary<int, GameAssetRecord>();
        _byId = new Dictionary<string, GameAssetRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in records)
        {
            if (r == null) continue;

            // Index by numeric ID
            if (r.NumericId != 0)
            {
                if (_byNumericId.ContainsKey(r.NumericId))
                {
                    Debug.LogWarning($"GameAssetRegistry: Duplicate NumericId '{r.NumericId}' found. Keeping first.");
                }
                else
                {
                    _byNumericId.Add(r.NumericId, r);
                }
            }

            // Optional: index by string Id if present
            if (!string.IsNullOrWhiteSpace(r.Id))
            {
                if (_byId.ContainsKey(r.Id))
                {
                    Debug.LogWarning($"GameAssetRegistry: Duplicate Id '{r.Id}' found. Keeping first.");
                }
                else
                {
                    _byId.Add(r.Id, r);
                }
            }
        }
    }

    // ---- Numeric ID API (preferred) ----

    public bool TryGet(int numericId, out GameAssetRecord rec)
    {
        if (numericId == 0)
        {
            rec = null;
            return false;
        }

        if (_byNumericId == null) BuildIndex();
        return _byNumericId.TryGetValue(numericId, out rec);
    }

    public GameAssetRecord GetOrNull(int numericId)
    {
        var found = TryGet(numericId, out var rec);
        return found ? rec : null;
    }

    // ---- String ID API (legacy / optional) ----

    public bool TryGet(string id, out GameAssetRecord rec)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            rec = null;
            return false;
        }

        if (_byId == null) BuildIndex();
        return _byId.TryGetValue(id, out rec);
    }

    public GameAssetRecord GetOrNull(string id)
    {
        return TryGet(id, out var rec) ? rec : null;
    }

#if UNITY_EDITOR
    public void ClearAll()
    {
        records.Clear();
        BuildIndex();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void AddOrReplace(GameAssetRecord incoming)
    {
        if (incoming == null) return;

        // Prefer numeric key when present
        if (incoming.NumericId != 0)
        {
            int idx = records.FindIndex(r => r != null && r.NumericId == incoming.NumericId);
            if (idx >= 0) records[idx] = incoming;
            else records.Add(incoming);
        }
        else if (!string.IsNullOrWhiteSpace(incoming.Id))
        {
            int idx = records.FindIndex(r =>
                r != null && string.Equals(r.Id, incoming.Id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) records[idx] = incoming;
            else records.Add(incoming);
        }
        else
        {
            // If no ID set yet, just append; editor code should fill it in.
            records.Add(incoming);
        }

        BuildIndex();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
