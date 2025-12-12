using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class WorldStateManager : MonoBehaviour
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

        // NEW: only filled if this object has a HexTileData component
        public HexTileSaveData hexTileData;
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
    public void RebuildAllNeighbors()
    {
        foreach (var kvp in hexTiles)
        {
            UpdateNeighborsForTile(kvp.Value);
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

        // --- NEW: if this object is a hex tile, save its fields ---
        HexTileData hex = t.GetComponent<HexTileData>();
        if (hex != null)
        {
            objData.hexTileData = new HexTileSaveData
            {
                tileCoords = hex.GetTileCoordinates(),
                // adjust these field names to match your HexTileData script:
                isTraversable = hex.isTraversable,
                hasBuilding = hex.hasBuilding,
                isOccupied = hex.isOccupied,
                isWater = hex.isWater,
                movementCost = hex.movementCost,
                currentPlaceableName = hex.currentPlaceableObject != null
                    ? hex.currentPlaceableObject.name.Replace("(Clone)", "").Trim()
                    : null
            };
        }

        data.objects.Add(objData);

        // Recurse into children, BUT if this is a hex tile, skip the placed object
        foreach (Transform c in t)
        {
            if (hex != null && hex.currentPlaceableObject != null &&
                c.gameObject == hex.currentPlaceableObject)
            {
                // don’t save the placeable separately – it’s saved via hexTileData
                continue;
            }

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

        hexTiles.Clear();

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

            // --- NEW: if this is a saved hex tile, restore its data + placed child ---
            if (obj.hexTileData != null)
            {
                Debug.Log($"Restoring HexTileData for '{obj.name}'");
                HexTileData hex = instance.GetComponent<HexTileData>();
                if (hex == null)
                {
                    Debug.LogWarning($"Loaded object '{obj.name}' does not have HexTileData component; adding one.");
                    hex = instance.AddComponent<HexTileData>();
                }
                if (hex != null)
                {

                    hex.SetTileCoordinates(obj.hexTileData.tileCoords);


                    hex.isTraversable = obj.hexTileData.isTraversable;
                    hex.hasBuilding = obj.hexTileData.hasBuilding;
                    hex.isOccupied = obj.hexTileData.isOccupied;
                    hex.isWater = obj.hexTileData.isWater;
                    hex.movementCost = obj.hexTileData.movementCost;


                    if (!string.IsNullOrEmpty(obj.hexTileData.currentPlaceableName) &&
                        prefabLookup.TryGetValue(obj.hexTileData.currentPlaceableName,
                                                 out GameObject placeablePrefab))
                    {
                        GameObject placeableInstance = Instantiate(placeablePrefab, instance.transform);
                        placeableInstance.name = obj.hexTileData.currentPlaceableName;
                        hex.currentPlaceableObject = placeableInstance;
                    }


                    RegisterHexTile(hex);
                    PrintHexTiles();           
                }
            }
            else
            {
                Debug.LogWarning($"Loaded object '{obj.name}' does not have HexTileData component.");
            }


            string fullPath = string.IsNullOrEmpty(obj.parentPath) ? obj.name : obj.parentPath + "/" + obj.name;
            createdByPath[fullPath] = instance.transform;
        }

        Debug.Log($"World state loaded. Objects recreated: {data.objects.Count}");
        RebuildAllNeighbors();
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

        hexTiles.Clear();   // <-- add this line

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

    // Assumes: HexTileData has GetTileCoordinates() and a List<HexTileData> neighbors + AddNeighbor(HexTileData).

    // Dictionary of all tiles in the world
    [SerializeField] public Dictionary<Vector3, HexTileData> hexTiles = new Dictionary<Vector3, HexTileData>();

    // Offsets for your odd/even row hex layout
    private static readonly Vector3[] evenRowOffsets =
    {
    new Vector3(+1, 0, 0),
    new Vector3(0, 0, +1),
    new Vector3(-1, 0, +1),
    new Vector3(-1, 0, 0),
    new Vector3(-1, 0, -1),
    new Vector3(0, 0, -1)
};

    private static readonly Vector3[] oddRowOffsets =
    {
    new Vector3(+1, 0, 0),
    new Vector3(+1, 0, +1),
    new Vector3(0, 0, +1),
    new Vector3(-1, 0, 0),
    new Vector3(0, 0, -1),
    new Vector3(+1, 0, -1)
};

    private bool IsEvenRow(Vector3 coords)
    {
        // Use whatever axis is your "row"; here you're using Z
        return ((int)coords.z % 2) == 0;
    }

    public void RegisterHexTile(HexTileData tile)
    {
        if (tile == null) return;

        Vector3 coords = tile.GetTileCoordinates();
        hexTiles[coords] = tile; // add or replace

        // Keep neighbor links up to date
        UpdateNeighborsForTile(tile);
    }

    public HexTileData GetHexTileAtCoordinates(Vector3 coords)
    {
        hexTiles.TryGetValue(coords, out HexTileData tile);
        return tile;
    }

    public void UnregisterHexTile(HexTileData tile)
    {
        if (tile == null) return;

        Vector3 coords = tile.GetTileCoordinates();

        // Remove this tile from all its neighbors' neighbor lists
        foreach (HexTileData neighbor in GetNeighborsForCoords(coords))
        {
            neighbor.neighbors.Remove(tile);
        }

        hexTiles.Remove(coords);
    }

    // -------------------
    //  Neighbors
    // -------------------

    private List<HexTileData> GetNeighborsForCoords(Vector3 coords)
    {
        List<HexTileData> neighbors = new List<HexTileData>(6);
        Vector3[] offsets = IsEvenRow(coords) ? evenRowOffsets : oddRowOffsets;

        foreach (Vector3 offset in offsets)
        {
            Vector3 neighborCoords = coords + offset;
            if (hexTiles.TryGetValue(neighborCoords, out HexTileData neighborTile))
            {
                neighbors.Add(neighborTile);
            }
        }

        return neighbors;
    }

    public List<HexTileData> UpdateNeighborsForTile(HexTileData tile)
    {
        if (tile == null) return null;

        Vector3 coords = tile.GetTileCoordinates();
        List<HexTileData> neighbors = GetNeighborsForCoords(coords);

        // Clear and rebuild this tile's neighbor list
        tile.neighbors.Clear();

        foreach (HexTileData neighbor in neighbors)
        {
            tile.AddNeighbor(neighbor);

            // Make sure the neighbor also knows about this tile (bidirectional)
            if (!neighbor.neighbors.Contains(tile))
            {
                neighbor.AddNeighbor(tile);
            }
        }

        return neighbors;
    }


    public void PrintHexTiles()
    {
        foreach (var kvp in hexTiles)
        {
            Debug.Log($"Tile \"{kvp.Value.name}\" at: {kvp.Key}");
        }
    }

    public void PrintNeighborsOfTile(HexTileData tile)
    {
        if (tile == null) return;

        List<HexTileData> neighbors = GetNeighborsForCoords(tile.GetTileCoordinates());

        Debug.Log("Neighbors of tile at " + tile.GetTileCoordinates() + ":");
        foreach (HexTileData neighbor in neighbors)
        {
            Debug.Log(" - Neighbor at " + neighbor.GetTileCoordinates());
        }
    }
}

[Serializable]
public class HexTileSaveData
{
    public Vector3 tileCoords;
    public bool isTraversable;
    public bool hasBuilding;
    public bool isOccupied;
    public bool isWater;
    public float movementCost;

    // prefab name of the placed object, without "(Clone)"
    public string currentPlaceableName;
}
