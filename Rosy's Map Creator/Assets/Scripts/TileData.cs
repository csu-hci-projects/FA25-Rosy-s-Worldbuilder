using System;
using Unity.VisualScripting;
using UnityEngine;

public class TileData : MonoBehaviour
{

    public Vector3 tileCoordinates;
    public Vector3Int gridCoordinates;

    public bool traversable = true;
    public bool hasBuilding = false;
    public bool isOcupied = false;
    public bool isWater = false;
    public float movementCost = 1.0f;

    public void SetTileCoordinates(Vector3 coords)
    {
        tileCoordinates = coords;
    }

    public void SetTileCoordinates(Vector3Int coords)
    {
        gridCoordinates = coords;
    }

    public Vector3 GetTileCoordinates()
    {
        return tileCoordinates;
    }

    public Vector3Int GetGridCoordinates()
    {
        return gridCoordinates;
    }


}
