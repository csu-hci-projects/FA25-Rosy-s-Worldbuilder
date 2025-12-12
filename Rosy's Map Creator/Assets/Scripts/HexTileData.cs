using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HexTileData : MonoBehaviour
{

    public Vector3 tileCoordinates;

    public bool isTraversable = true;
    public bool hasBuilding = false;
    public bool isOccupied = false;

    public void setIsOccupied(bool occupied)
    {
        isOccupied = occupied;
    }
    public bool isWater = false;
    public float movementCost = 1.0f;

    public GameObject parentObject;

    public List<HexTileData> neighbors = new List<HexTileData>();

    public GameObject currentPlaceableObject = null;

    public GameObject GetParentObject()
    {
        return parentObject;
    }

    void Start()
    {
        parentObject = transform.gameObject;
    }

    public void SetTileCoordinates(Vector3 coords)
    {
        tileCoordinates = coords;
    }

    public Vector3 GetTileCoordinates()
    {
        return tileCoordinates;
    }

    public bool GetIsTraversable()
    {
        return isTraversable;
    }
    public GameObject getCurrentPlaceableObject()
    {
        return currentPlaceableObject;
    }

    public void setCurrentPlaceableObject(GameObject obj)
    {
        currentPlaceableObject = obj;
        // obj.transform.SetParent(transform.parent);
    }

    public void AddNeighbor(HexTileData neighbor)
    {
        if (!neighbors.Contains(neighbor))
        {
            neighbors.Add(neighbor);
        }
    }



}
