using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

public class UnitController : MonoBehaviour
{
    private GameObject parentObject;
    bool isSelected = false;
    LineRenderer lineRenderer;
    public Material lineMaterial;
    private Material originalMaterial;
    private float lineWidth = 0.1f;

    bool DebugMode = false;

    List<HexTileData> path = new List<HexTileData>();

    public void SetPath(List<HexTileData> newPath)
    {
        path = newPath;
        UpdateLineRenderer(path);
    }

    public HexTileData GetParentHexTileData()
    {
        return parentObject.GetComponent<HexTileData>();
    }

    public void SetParent(GameObject parent)
    {
        parentObject = parent;
    }

    void Start()
    {
        parentObject = transform.parent.gameObject;
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = lineWidth;
        if (originalMaterial == null)
        {
            originalMaterial = GetComponent<Renderer>().material;
        }
    }

    public void Selected()
    {
        Debug.Log("Unit clicked at tile coordinates: " + GetParentHexTileData().GetTileCoordinates());
        isSelected = true;


    }

    public void SetTileData(HexTileData data)
    {
        parentObject = data.gameObject;
    }

    public void Update()
    {
        if (isSelected)
        {
            // Example behavior when selected
            Debug.Log("Unit at " + GetParentHexTileData().GetTileCoordinates() + " is selected.");
            GetComponent<Renderer>().material = lineRenderer.material;
        }
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                HandleMovement();
            }
        }
    }

    public void Deselect()
    {
        isSelected = false;
        GetComponent<Renderer>().material = originalMaterial;
    }
    private float deltaX = 2f; // Horizontal distance between adjacent hex tiles
    private float deltaZ = 1.725f;   // Vertical distance between adjacent hex



    public void MoveToTile(Vector3 targetTileCoords)
    {
        // Simple movement logic (instant move for demonstration)
        // transform.position
        float gridX = (targetTileCoords.z % 2 == 0) ? targetTileCoords.x * deltaX : targetTileCoords.x * deltaX + (deltaX / 2);
        float gridY = targetTileCoords.y;
        float gridZ = targetTileCoords.z * deltaZ;
        transform.position = new Vector3(gridX, gridY, gridZ);
        GetParentHexTileData().SetTileCoordinates(targetTileCoords);
        Debug.Log("Unit moved to tile coordinates: " + targetTileCoords);
    }

    void FixedUpdate()
    {
        if (isSelected)
        {
            if (DebugMode) UserMovement();

        }


    }

    public void resetMaterial()
    {
        GetComponent<Renderer>().material = originalMaterial;
    }

    HexTileData currentTile;
    HexTileData nextTile;
    bool gotPath = false;
    Vector3 targetPosition;

    public void HandleMovement()
    {
        if (path == null || path.Count <= 0)
        {
            nextTile = null;
            if (path != null && path.Count > 0)
            {
                currentTile = path[0];
                nextTile = currentTile;
                gameObject.transform.SetParent(currentTile.GetParentObject().transform, false);
                SetParent(currentTile.GetParentObject());
                currentTile.setCurrentPlaceableObject(gameObject);
                currentTile.setIsOcupied(true);
            }
            gotPath = false;
            UpdateLineRenderer(path);
        }
        else
        {
            currentTile = path[0];
            nextTile = path[1];

            if (!nextTile.GetIsTraversable())
            {
                path.Clear();
                HandleMovement();
                return;
            }
            // targetPosition = nextTile.transform.position + new Vector3(0, 0.5f, 0);
            // transform.position = targetPosition;
            gameObject.transform.SetParent(nextTile.GetParentObject().transform, false);
            SetParent(nextTile.GetParentObject());
            nextTile.setCurrentPlaceableObject(gameObject);
            nextTile.setIsOcupied(true);
            currentTile.setCurrentPlaceableObject(null);
            currentTile.setIsOcupied(false);
            gotPath = true;
            path.RemoveAt(0);

            UpdateLineRenderer(path);

        }
    }

    void UserMovement()
    {
        Vector3 tileCoords = GetParentHexTileData().GetTileCoordinates();
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.wasPressedThisFrame)
            {
                tileCoords += new Vector3(0, 0, 1);
                MoveToTile(tileCoords);
            }
            else if (keyboard.aKey.wasPressedThisFrame)
            {
                tileCoords += new Vector3(-1, 0, 0);
                MoveToTile(tileCoords);
            }
            else if (keyboard.sKey.wasPressedThisFrame)
            {
                tileCoords += new Vector3(0, 0, -1);
                MoveToTile(tileCoords);
            }
            else if (keyboard.dKey.wasPressedThisFrame)
            {
                tileCoords += new Vector3(1, 0, 0);
                MoveToTile(tileCoords);
            }
        }
        else
        {
            // Fallback to legacy Input API if available
            if (Input.GetKeyDown(KeyCode.W))
            {
                tileCoords += new Vector3(0, 0, 1);
                MoveToTile(tileCoords);
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                tileCoords += new Vector3(-1, 0, 0);
                MoveToTile(tileCoords);
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                tileCoords += new Vector3(0, 0, -1);
                MoveToTile(tileCoords);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                tileCoords += new Vector3(1, 0, 0);
                MoveToTile(tileCoords);
            }
        }
    }

    protected void UpdateLineRenderer(List<HexTileData> path)
    {
        if (lineRenderer != null && path != null && path.Count > 0)
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (HexTileData tile in path)
            {
                Vector3 tilePosition = tile.transform.position;
                positions.Add(new Vector3(tilePosition.x, tilePosition.y + 0.5f, tilePosition.z));
            }
            // positions.Add(transform.position + new Vector3(0, 0.5f, 0)); // Add unit's current position at the end
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());
        }
    }



}




