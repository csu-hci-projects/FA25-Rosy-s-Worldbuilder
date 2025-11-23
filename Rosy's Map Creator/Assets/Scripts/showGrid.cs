using UnityEngine;
using UnityEngine.UI;

public class showGrid : MonoBehaviour
{
    public Grid grid;
    public bool showGridLines = true;
    public bool showCellCenters = true;
    public bool showCellCoordinates = true;
    public Color gridLineColor = Color.white;
    public float gridLineWidth = 0.1f;
    private LineRenderer lineRenderer;

    public int gridMinX = -5;
    public int gridMinY = -5;
    public int gridMaxX = 5;
    public int gridMaxY = 5;

    void Start()
    {
        grid = GetComponent<Grid>();
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = gridLineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.startColor = gridLineColor;
        lineRenderer.endColor = gridLineColor;
        if (showGridLines)
        {
            for (int x = gridMinX; x <= gridMaxX; x++)
            {
                for (int y = gridMinY; y <= gridMaxY; y++)
                {
                    Vector3Int cellPosition = new Vector3Int(x, y, 0);
                    Vector3 cellWorldPosition = grid.GetCellCenterWorld(cellPosition);

                    TextMesh textMesh = new GameObject("CellText").AddComponent<TextMesh>();
                    textMesh.text = $"({x},{y})";
                    textMesh.transform.position = cellWorldPosition;
                    textMesh.characterSize = 0.2f;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.color = Color.black;
                    textMesh.transform.SetParent(this.transform);

                    Vector3[] corners = new Vector3[6];
                    for (int i = 0; i < 6; i++)
                    {
                        corners[i] = cellWorldPosition + Vector3.Scale(grid.cellSize, HexCorner(i));
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        lineRenderer.positionCount += 2;
                        lineRenderer.SetPosition(lineRenderer.positionCount - 2, corners[i]);
                        lineRenderer.SetPosition(lineRenderer.positionCount - 1, corners[(i + 1) % 6]);
                    }
                }
            }

        }
    }
    private Vector3 HexCorner(int i)
    {
        float angle_deg = 60 * i - 30;
        float angle_rad = Mathf.Deg2Rad * angle_deg;
        float radius = 0.5f;
        return new Vector3(radius * Mathf.Cos(angle_rad), 0, radius * Mathf.Sin(angle_rad));
    }

    void Update()
    {

    }
}
