using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public Vector2Int gridSize;
    public float radius = 1f;
    public bool isFlatTopped;

    public HexTileGenerationSettings settings;
    [ContextMenu("Clear Grid")]
    public void Clear()
    {
        List<GameObject> children = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            children.Add(child);
        }

        foreach (GameObject child in children)
        {
            DestroyImmediate(child, true);
        }
    }

    [ContextMenu("Layout Grid")]
    public void LayoutGrid()
    {
        Clear();
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                GameObject tile = new GameObject($"Hex C{x},R{y}");

                tile.transform.SetParent(transform, true);

                HexTile hextile = tile.AddComponent<HexTile>();
                hextile.settings = settings;

                hextile.offsetCoordinate = new Vector2Int(x, y);

                hextile.cubeCoordinate = Utilities.OffsetToCube(hextile.offsetCoordinate);

                float q = hextile.cubeCoordinate.x;
                float r = hextile.cubeCoordinate.y;

                Vector3 worldPos = Vector3.zero;
                if (isFlatTopped)
                {
                    float xPos = radius * 1.5f * q;
                    float zPos = radius * Mathf.Sqrt(3f) * (r + q / 2f);
                    worldPos = new Vector3(xPos, 0f, zPos);
                }
                else
                {
                    float xPos = radius * Mathf.Sqrt(3f) * (q + r / 2f);
                    float zPos = radius * 1.5f * r;
                    worldPos = new Vector3(xPos, 0f, zPos);
                }

                tile.transform.position = worldPos;

                hextile.RollTileType();
                hextile.AddTile();
            }
        }
    }
}
