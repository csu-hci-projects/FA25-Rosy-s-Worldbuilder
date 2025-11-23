using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HexTile : MonoBehaviour
{
    public HexTileGenerationSettings settings;
    public HexTileGenerationSettings.TileType tileType;

    public GameObject tile;
    public Vector2Int offsetCoordinate;
    public Vector3Int cubeCoordinate;
    public List<HexTile> neighbours;

    private bool isDirty = false;

    public void RollTileType()
    {
        tileType = (HexTileGenerationSettings.TileType)Random.Range(0, 3);
    }

    public void AddTile()
    {
        if (settings == null)
        {
            Debug.LogError($"HexTile.AddTile: 'settings' is null on '{gameObject.name}'. Assign a HexTileGenerationSettings to the HexGrid or HexTile.");
            return;
        }

        GameObject prefab = settings.GetTile(tileType);
        if (prefab == null)
        {
            Debug.LogError($"HexTile.AddTile: settings.GetTile returned null for tileType {tileType} on '{gameObject.name}'. Check your generation settings.");
            return;
        }

        tile = GameObject.Instantiate(prefab, transform);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localRotation = Quaternion.identity;

        var existingCollider = gameObject.GetComponent<MeshCollider>();
        if (existingCollider == null)
        {
            var childMeshFilter = GetComponentInChildren<MeshFilter>();
            if (childMeshFilter != null && childMeshFilter.sharedMesh != null)
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = childMeshFilter.sharedMesh;
            }
            else
            {
                Debug.LogWarning($"HexTile.AddTile: No MeshFilter with a mesh found under '{gameObject.name}', skipping MeshCollider creation.");
            }
        }
    }

    private void OnValidate()
    {
        if (tile == null) { return; }
        isDirty = true;
    }

    private void Update()
    {
        if (isDirty)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(tile);
            }
            else
            {
                GameObject.DestroyImmediate(tile);
            }

            AddTile();
            isDirty = false;
        }
    }

    public void OnDrawGizmosSelected()
    {
        foreach (HexTile neighbour in neighbours)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, neighbour.transform.position);
        }
    }
}
