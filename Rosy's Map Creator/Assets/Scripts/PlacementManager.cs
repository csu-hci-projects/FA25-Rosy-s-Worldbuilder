using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;
using Unity.VisualScripting;
using Unity.Mathematics;
using UnityEngine.XR;
using System.IO;
using System.Collections.Generic;

public enum PlacementMode
{
    Place = 0,
    Delete = 1,
    Selection = 2,
    None = 3,
}
public class PlacementManager : MonoBehaviour
{
    [SerializeField] private Grid hexGrid;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private GameObject hexPreview;
    [SerializeField] private Season currentSeason = Season.Spring;
    [SerializeField] private PlacementMode currentPlacementMode = PlacementMode.Place;
    [Header("Delete Mode Highlight")]
    [SerializeField] private Material deleteHoverMaterial;
    [Header("Selection Mode Highlight")]
    [SerializeField] private Material selectionHoverMaterial;
    public bool isUnitSelected = false;

    List<HexTileData> path = new List<HexTileData>();

    private Renderer lastHoverRenderer;
    private Material lastOriginalMaterial;

    [Header("Placement Settings")]
    [Tooltip("How much lower (in fractions of tile height) buildings are placed relative to the tile center.")]
    [SerializeField] private float buildingYOffsetFactor = 0.5f;

    [SerializeField] private GameObject worldState;
    [Tooltip("Registry of all placed tiles/buildings (auto-managed).")]
    [SerializeField] private System.Collections.Generic.List<GameObject> placedObjects = new System.Collections.Generic.List<GameObject>();


    public void setTilePrefab(GameObject prefab)
    {
        tilePrefab = prefab;
        var renderer = prefab.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material = SeasonManager.GetMaterialForSeason();
        }
        var prefabMesh = prefab.GetComponentInChildren<MeshFilter>();
        var previewMesh = hexPreview.GetComponent<MeshFilter>();
        if (prefabMesh != null && previewMesh != null)
        {
            previewMesh.mesh = prefabMesh.sharedMesh;
        }
    }

    public void setPlacementMode(PlacementMode mode)
    {
        currentPlacementMode = mode;
    }

    public void HandleModeChange()
    {
        if (currentPlacementMode == PlacementMode.None || currentPlacementMode == PlacementMode.Selection || currentPlacementMode == PlacementMode.Delete)
        {
            hexPreview.SetActive(false);
        }
        else
        {
            hexPreview.SetActive(true);
        }
    }

    public void HandleSeasonChange()
    {

        Season newSeason = SeasonManager.GetCurrentSeason();

        hexPreview.GetComponent<Renderer>().material = SeasonManager.GetMaterialForSeason();


        if (newSeason != currentSeason)
        {
            currentSeason = newSeason;
            if (tilePrefab != null)
            {
                var prefabRenderer = tilePrefab.GetComponentInChildren<Renderer>();
                if (prefabRenderer != null)
                {
                    prefabRenderer.material = SeasonManager.GetMaterialForSeason();
                }
            }
        }
    }

    public GameObject currentSelection = null;
    public GameObject previousSelection = null;

    void Update()
    {
        HandleModeChange();
        HandleSeasonChange();
        if (hexGrid == null || hexPreview == null)
            return;
        if (EventSystem.current != null)
        {
            var pointer = Pointer.current;
            if (pointer != null && EventSystem.current.IsPointerOverGameObject(pointer.deviceId))
                return;
        }

        var mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 mousePosition = mouse.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 1000f, groundMask))
            return;

        Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red, 0.1f);

        Vector3Int cellPosition = hexGrid.WorldToCell(hit.point);
        Vector3 cellPositionWorld = hexGrid.GetCellCenterWorld(cellPosition);
        Vector3 previewPos = GetPlacementPosition(hit, cellPositionWorld);


        hexPreview.transform.position = previewPos;
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            hexPreview.transform.Rotate(0f, 60f, 0f);
        }
        HandleMouseHover(hit);
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (currentPlacementMode == PlacementMode.Place && tilePrefab != null)
            {
                HandlePlacement(cellPosition, previewPos, hit);
            }
            else if (currentPlacementMode == PlacementMode.Delete)
            {
                if (hit.collider.CompareTag("HexTile") || hit.collider.CompareTag("Buildings") || hit.collider.CompareTag("Decorations") || hit.collider.CompareTag("Units"))
                {
                    Debug.Log("Deleting object: " + hit.collider.gameObject.name);
                    GameObject toDelete = hit.collider.gameObject;
                    placedObjects.Remove(toDelete);
                    worldState.GetComponent<WorldStateManager>()?.UnregisterHexTile(toDelete.GetComponent<HexTileData>());
                    Destroy(toDelete);
                }
            }
            else if (currentPlacementMode == PlacementMode.Selection)
            {
                bool flowControl = HandleSelectionChange(hit);
                if (!flowControl)
                {
                    return;
                }

            }
        }
    }

    private bool HandleSelectionChange(RaycastHit hit)
    {
        previousSelection = currentSelection;
        currentSelection = hit.collider.gameObject;

        if (currentSelection.CompareTag("HexTile") && previousSelection.CompareTag("Units"))
        {
            return FindPathFromSelection();
        }

        if (currentSelection.CompareTag("Units") && previousSelection != currentSelection)
        {
            Debug.Log("Selecting unit: " + currentSelection.name);
            currentSelection.GetComponent<UnitController>()?.Selected();
        }

        if (currentSelection == previousSelection && currentSelection.CompareTag("Units"))
        {
            Debug.Log("Deselecting unit: " + currentSelection.name);
            currentSelection.GetComponent<UnitController>()?.Deselect();
            currentSelection = null;
        }

        return true;
    }

    private bool FindPathFromSelection()
    {
        HexTileData targetTileCoords = currentSelection.GetComponent<HexTileData>();
        HexTileData originTileCoords = previousSelection.GetComponent<UnitController>().GetParentHexTileData();
        if (targetTileCoords == null)
        {
            Debug.Log("Target tile coordinates not found.");
            return false;
        }
        if (originTileCoords == null)
        {
            Debug.Log("Origin tile coordinates not found.");
            return false;
        }
        Debug.Log("Finding path from " + originTileCoords.GetTileCoordinates() + " to " + targetTileCoords.GetTileCoordinates());
        path = Pathfinder.FindPath(originTileCoords, targetTileCoords);
        foreach (HexTileData step in path)
        {
            Debug.Log("Path step: " + step.GetTileCoordinates());
        }
        previousSelection.GetComponent<UnitController>()?.SetPath(path);
        previousSelection.GetComponent<UnitController>()?.Deselect();
        currentSelection = null;
        previousSelection = null;

        return false;
    }

    private void HandlePlacement(Vector3Int cellPosition, Vector3 previewPos, RaycastHit hit)
    {
        bool flowControl = false;
        switch (tilePrefab.tag)
        {
            case "HexTile":
                PlaceTileAtPosition(cellPosition, previewPos);
                playSound();
                break;
            case "Buildings":
                flowControl = PlaceObjectOnTile(previewPos, hit);
                if (!flowControl)
                {
                    return;
                }
                break;
            case "Decorations":
                flowControl = PlaceObjectOnTile(previewPos, hit);
                if (!flowControl)
                {
                    return;
                }
                break;
            case "Units":
                flowControl = PlaceObjectOnTile(previewPos, hit);
                if (!flowControl)
                {
                    return;
                }
                break;
            default:
                Debug.Log("Placing object at position: " + previewPos);
                break;
        }



    }

    private void playSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }
    }

    private void PlaceTileAtPosition(Vector3Int cellPosition, Vector3 previewPos)
    {
        Debug.Log("Placing tile at position: " + previewPos);

        GameObject placed = Instantiate(tilePrefab, previewPos, hexPreview.transform.rotation);

        // Parent under worldState if assigned
        if (worldState != null)
        {
            placed.transform.SetParent(worldState.transform, true);
        }

        // Grid coords (assuming x = column, y = row → stored as x,z in tile coords)
        int gridX = cellPosition.x;
        int gridZ = cellPosition.y;
        Debug.Log("Placing at grid coordinates: (" + gridX + ", " + gridZ + ")");

        // Layer index – if you just want 0,1,2... per height step, you can simplify this
        int layerIndex = Mathf.RoundToInt(previewPos.y * 2 - 1);
        // or: int layerIndex = Mathf.RoundToInt(previewPos.y);

        // Make sure we only have ONE HexTileData on the object
        HexTileData tileData = placed.GetComponent<HexTileData>();
        if (tileData == null)
        {
            tileData = placed.AddComponent<HexTileData>();
        }

        // Store logical grid coords in the tile
        tileData.SetTileCoordinates(new Vector3(gridX, layerIndex, gridZ));

        // Cache WorldStateManager and guard against null worldState
        if (worldState != null)
        {
            WorldStateManager wsm = worldState.GetComponent<WorldStateManager>();
            if (wsm != null)
            {
                wsm.RegisterHexTile(tileData);
                wsm.PrintHexTiles();
                wsm.PrintNeighborsOfTile(tileData);
            }
        }

        placedObjects.Add(placed);
    }

    private bool PlaceObjectOnTile(Vector3 previewPos, RaycastHit hit)
    {
        if (hit.collider.CompareTag("HexTile"))
        {
            Debug.Log("Placing object on tile: " + hit.collider.gameObject.name);
            if (hit.collider.GetComponent<HexTileData>().getCurrentPlaceableObject() == null
            && hit.collider.GetComponent<HexTileData>().isOccupied == false
            && hit.collider.GetComponent<HexTileData>().isTraversable)
            {
                GameObject placedObject = Instantiate(tilePrefab, previewPos, hexPreview.transform.rotation);
                placedObject.transform.SetParent(hit.collider.transform, true);
                placedObject.transform.localPosition = Vector3.zero;
                hit.collider.GetComponent<HexTileData>().setCurrentPlaceableObject(placedObject);
                hit.collider.GetComponent<HexTileData>().isOccupied = true;
                placedObject.GetComponent<UnitController>()?.SetTileData(hit.collider.GetComponent<HexTileData>());
                placedObjects.Add(placedObject);
                playSound();
                return false;
            }
            else
            {
                Debug.Log("Cannot place unit: Tile is already occupied.");
                return false;
            }
        }

        return true;
    }

    private Vector3 GetPlacementPosition(RaycastHit hit, Vector3 cellCenter)
    {
        Vector3 pos = cellCenter;

        if (tilePrefab == null)
            return pos;
        var prefabRenderer = tilePrefab.GetComponentInChildren<Renderer>();
        if (prefabRenderer == null)
            return pos;

        float tileHeight = prefabRenderer.bounds.size.y;

        bool hitTileOrBuilding = hit.collider.CompareTag("HexTile") || hit.collider.CompareTag("Buildings");
        bool placingBuilding = tilePrefab.CompareTag("Buildings") || tilePrefab.CompareTag("Decorations") || tilePrefab.CompareTag("Units");

        if (hitTileOrBuilding)
        {
            float top = hit.collider.bounds.max.y;
            pos.y = top + tileHeight * 0.5f;
        }
        if (placingBuilding)
        {
            pos.y -= tileHeight * buildingYOffsetFactor;
        }
        return pos;
    }

    private void HandleMouseHover(RaycastHit hit)
    {

        if (currentPlacementMode != PlacementMode.Delete && currentPlacementMode != PlacementMode.Selection)
        {
            ClearLastHover();
            return;
        }


        Material hoverMaterial = null;
        if (currentPlacementMode == PlacementMode.Delete)
        {
            hoverMaterial = deleteHoverMaterial;
        }
        else if (currentPlacementMode == PlacementMode.Selection)
        {
            hoverMaterial = selectionHoverMaterial;
        }

        if (hoverMaterial == null)
        {
            ClearLastHover();
            return;
        }

        bool canHover = hit.collider != null && (hit.collider.CompareTag("HexTile") || hit.collider.CompareTag("Buildings") || hit.collider.CompareTag("Decorations") || hit.collider.CompareTag("Units"));
        if (!canHover)
        {
            ClearLastHover();
            return;
        }

        var rend = hit.collider.GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            ClearLastHover();
            return;
        }

        if (rend != lastHoverRenderer)
        {
            ClearLastHover();
            lastHoverRenderer = rend;
            lastOriginalMaterial = rend.material;
            rend.material = hoverMaterial;
        }
    }

    private void ClearLastHover()
    {
        if (lastHoverRenderer != null && lastOriginalMaterial != null)
        {
            lastHoverRenderer.material = lastOriginalMaterial;
        }
        lastHoverRenderer = null;
        lastOriginalMaterial = null;
    }

    public void changeMode(Int32 mode)
    {
        print("Changing mode to: " + mode);
        currentPlacementMode = (PlacementMode)mode;
    }

    void OnDrawGizmos()
    {
        if (path != null)
        {
            foreach (var step in path)
            {
                Gizmos.DrawCube(step.transform.position + new Vector3(0, .5f, .5f), Vector3.one * 0.5f);
            }
        }
    }

}