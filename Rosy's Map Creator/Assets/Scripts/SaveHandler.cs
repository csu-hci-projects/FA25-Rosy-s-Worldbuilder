using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveHandler : MonoBehaviour
{
    [Header("World Root Reference")]
    [SerializeField] private GameObject worldState;

    [Header("Spawnable Prefabs (match by name)")]
    [Tooltip("List of prefabs that can be re-instantiated on load. Names must match saved object names (without '(Clone)').")]
    [SerializeField] private GameObject[] spawnablePrefabs;

    [Header("Season Materials (assign in Inspector)")]
    [SerializeField] private Material springMaterial;
    [SerializeField] private Material summerMaterial;
    [SerializeField] private Material fallMaterial;
    [SerializeField] private Material winterMaterial;
    private Dictionary<string, GameObject> prefabLookup;

    [Serializable]
    public class WorldObjectData
    {
        public string name;
        public string tag;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string parentPath;
        public string season;
    }

    [Serializable]
    public class WorldSaveData
    {
        public List<WorldObjectData> objects = new List<WorldObjectData>();
        public string savedAt;
        public int version = 2;
    }

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "worldsave.json");

    private void Awake()
    {
        BuildPrefabLookup();
    }

    private void BuildPrefabLookup()
    {
        prefabLookup = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        if (spawnablePrefabs == null) return;
        foreach (var p in spawnablePrefabs)
        {
            if (p == null) continue;
            var key = p.name.Replace("(Clone)", "").Trim();
            if (!prefabLookup.ContainsKey(key))
            {
                prefabLookup.Add(key, p);
            }
        }
    }

    [ContextMenu("Save World State")]
    public void SaveWorldState()
    {
        if (worldState == null)
        {
            Debug.LogError("SaveHandler: worldState reference not assigned.");
            return;
        }

        var data = new WorldSaveData
        {
            savedAt = DateTime.UtcNow.ToString("o")
        };

        foreach (Transform child in worldState.transform)
        {
            AddRecursive(child, data, worldState.transform);
        }

        string json = JsonUtility.ToJson(data, true);
        try
        {
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"World state saved to {SaveFilePath}\nObjects: {data.objects.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write save file: {ex.Message}");
        }
    }

    private void AddRecursive(Transform t, WorldSaveData data, Transform root)
    {
        var objData = new WorldObjectData
        {
            name = t.gameObject.name.Replace("(Clone)", "").Trim(),
            tag = t.gameObject.tag,
            position = t.position,
            rotation = t.rotation,
            scale = t.localScale,
            parentPath = GetRelativePath(t.parent, root),
            season = DetectSeason(t)
        };
        data.objects.Add(objData);
        foreach (Transform c in t)
        {
            AddRecursive(c, data, root);
        }
    }

    private string GetRelativePath(Transform current, Transform root)
    {
        if (current == null || current == root)
            return string.Empty;
        var stack = new Stack<string>();
        while (current != null && current != root)
        {
            stack.Push(current.name);
            current = current.parent;
        }
        return string.Join("/", stack.ToArray());
    }

    [ContextMenu("Load World State")]
    public void LoadWorldState()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.LogWarning("SaveHandler: No save file found to load.");
            return;
        }

        string json = File.ReadAllText(SaveFilePath);
        var data = JsonUtility.FromJson<WorldSaveData>(json);
        if (data == null)
        {
            Debug.LogError("SaveHandler: Failed to deserialize save data.");
            return;
        }

        ClearWorldState();

        var createdByPath = new Dictionary<string, Transform>();

        foreach (var obj in data.objects)
        {
            GameObject prefab;
            if (!prefabLookup.TryGetValue(obj.name, out prefab))
            {
                Debug.LogWarning($"SaveHandler: No prefab found for '{obj.name}', creating empty GameObject as placeholder.");
                prefab = null;
            }

            GameObject instance = prefab != null ? Instantiate(prefab) : new GameObject(obj.name);
            instance.name = obj.name;

            // Determine parent
            Transform parent = worldState.transform;
            if (!string.IsNullOrEmpty(obj.parentPath))
            {
                if (!createdByPath.TryGetValue(obj.parentPath, out parent))
                {
                    parent = CreateHierarchyForPath(obj.parentPath, createdByPath);
                }
            }

            instance.transform.SetParent(parent, true);
            instance.transform.position = obj.position;
            instance.transform.rotation = obj.rotation;
            instance.transform.localScale = obj.scale;

            if (!string.IsNullOrEmpty(obj.season))
            {
                ApplySeasonMaterial(instance.transform, obj.season);
            }

            string fullPath = string.IsNullOrEmpty(obj.parentPath) ? obj.name : obj.parentPath + "/" + obj.name;
            createdByPath[fullPath] = instance.transform;
        }

        Debug.Log($"World state loaded. Objects recreated: {data.objects.Count}");
    }

    private Transform CreateHierarchyForPath(string path, Dictionary<string, Transform> cache)
    {
        string[] parts = path.Split('/');
        string running = string.Empty;
        Transform currentParent = worldState.transform;
        foreach (var part in parts)
        {
            running = string.IsNullOrEmpty(running) ? part : running + "/" + part;
            if (!cache.TryGetValue(running, out var t))
            {
                var go = new GameObject(part);
                go.transform.SetParent(currentParent, true);
                t = go.transform;
                cache[running] = t;
            }
            currentParent = t;
        }
        return currentParent;
    }

    private string DetectSeason(Transform t)
    {
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend == null) return string.Empty;
        var mat = rend.sharedMaterial;
        if (mat == null) return string.Empty;
        Debug.Log("Material name: " + rend.material.name);
        if (rend.material.name.Contains("spring")) return "Spring";
        if (rend.material.name.Contains("summer")) return "Summer";
        if (rend.material.name.Contains("fall")) return "Fall";
        if (rend.material.name.Contains("winter")) return "Winter";
        return string.Empty;
    }

    private void ApplySeasonMaterial(Transform t, string season)
    {
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend == null) return;
        Material target = null;
        switch (season)
        {
            case "Spring": target = springMaterial; break;
            case "Summer": target = summerMaterial; break;
            case "Fall": target = fallMaterial; break;
            case "Winter": target = winterMaterial; break;
        }
        if (target != null)
        {
            rend.sharedMaterial = target;
        }
    }
    [ContextMenu("Clear World State")]
    public void ClearWorldState()
    {
        if (worldState == null)
        {
            Debug.LogError("SaveHandler: worldState reference not assigned.");
            return;
        }
        var children = new List<GameObject>();
        foreach (Transform child in worldState.transform)
        {
            children.Add(child.gameObject);
        }
        foreach (var c in children)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(c);
            else
                Destroy(c);
#else
            Destroy(c);
#endif
        }
    }
}
