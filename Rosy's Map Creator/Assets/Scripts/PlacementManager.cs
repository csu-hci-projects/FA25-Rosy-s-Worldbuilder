using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;
using Unity.VisualScripting;
using Unity.Mathematics;
using UnityEngine.XR;

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
        HandleDeleteHover(hit);
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (currentPlacementMode == PlacementMode.Place && tilePrefab != null)
            {
                GameObject placed = Instantiate(tilePrefab, previewPos, hexPreview.transform.rotation);
                if (worldState != null)
                {
                    placed.transform.SetParent(worldState.transform, true);
                }
                int gridX = cellPosition.x;
                int gridZ = cellPosition.y; 
                int layerIndex = Mathf.RoundToInt(previewPos.y * 2 - 1);

                TileData tileData = placed.AddComponent<TileData>();
                tileData.SetTileCoordinates(new Vector3(gridX, layerIndex, gridZ));

                placedObjects.Add(placed);
            }
            else if (currentPlacementMode == PlacementMode.Delete)
            {
                if (hit.collider.CompareTag("HexTile") || hit.collider.CompareTag("Buildings") || hit.collider.CompareTag("Decorations") || hit.collider.CompareTag("Units"))
                {
                    Debug.Log("Deleting object: " + hit.collider.gameObject.name);
                    GameObject toDelete = hit.collider.gameObject;
                    placedObjects.Remove(toDelete);
                    Destroy(toDelete);
                }
            }
        }
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

    private void HandleDeleteHover(RaycastHit hit)
    {
        if (currentPlacementMode != PlacementMode.Delete)
        {
            ClearLastHover();
            return;
        }

        if (deleteHoverMaterial == null)
        {
            ClearLastHover();
            return;
        }

        bool canDelete = hit.collider != null && (hit.collider.CompareTag("HexTile") || hit.collider.CompareTag("Buildings") || hit.collider.CompareTag("Decorations") || hit.collider.CompareTag("Units"));
        if (!canDelete)
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
            rend.material = deleteHoverMaterial;
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
}