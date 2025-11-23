using System.Collections.Generic;
using UnityEngine;

public class MapChunkSpawner : MonoBehaviour
{
    public Vector2Int mapSize = new Vector2Int();
    public int chunkSize = 7;
    public List<GameObject> chunks;

    private void OnEnable()
    {
        LayoutGrid();
    }


    public void LayoutGrid()
    {
        Clear();
        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                GameObject chunk = GameObject.Instantiate(GetRandomChunk());
                chunk.name = $"Chunk C{x},R{y}";
                chunk.transform.position = Utilities.GetPositionForHexFromCoordinate(chunkSize * x, chunkSize * y, chunkSize);
                chunk.transform.SetParent(transform, true);
            }
        }
    }
    public void Clear()
    {
        var children = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            children.Add(transform.GetChild(i).gameObject);
        }
        foreach (var child in children)
        {
            DestroyImmediate(child);
        }
    }

    private GameObject GetRandomChunk()
    {
        if (chunks == null || chunks.Count == 0)
            return null;
        int index = Random.Range(0, chunks.Count);
        return chunks[index];
    }
}
