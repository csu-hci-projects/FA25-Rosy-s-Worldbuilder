using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    private static TileManager instance;
    private Dictionary<Vector3Int, HexTile> tiles;

    private void Awake()
    {
        instance = this;
        tiles = new Dictionary<Vector3Int, HexTile>();
        HexTile[] hexTiles = gameObject.GetComponentsInChildren<HexTile>();
        foreach (HexTile tile in hexTiles)
        {
            RegisterTile(tile);
        }
        foreach (HexTile hexTile in hexTiles)
        {
            List<HexTile> neighbours = getNeighbours(hexTile);
            hexTile.neighbours = neighbours;
        }
    }

    private List<HexTile> getNeighbours(HexTile tile)
    {
        List<HexTile> neighbours = new List<HexTile>();
        Vector3Int[] neighbourCoords = new Vector3Int[]
        {
            new Vector3Int(1, 0, -1), new Vector3Int(1, -1, 0), new Vector3Int(0, -1, 1),
            new Vector3Int(-1, 0, 1), new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, -1)
        };

        foreach (Vector3Int neighbourCoord in neighbourCoords)
        {
            Vector3Int tileCoord = tile.cubeCoordinate;
            if (tiles.TryGetValue(tileCoord + neighbourCoord, out HexTile neighbour))
            {
                neighbours.Add(neighbour);
            }
        }

        return neighbours;
    }


    public void RegisterTile(HexTile tile)
    {
        tiles[tile.cubeCoordinate] = tile;
    }
}
