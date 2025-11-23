using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class NewMonoBehaviourScript : MonoBehaviour
{
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    public Material material;
    private List<Face> faces;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        mesh = new Mesh();
        mesh.name = "Hex";

        meshFilter.mesh = mesh;
        meshRenderer.material = material;
    }

    private void OnEnable()
    {
        DrawMesh();
    }

    public void OnValidate()
    {
        if (Application.isPlaying && mesh != null)
        {
            DrawMesh();
        }
    }

    public void DrawMesh()
    {
        DrawFaces();
        CombineFaces();
    }
    public float innerSize = 0.5f;
    public float outerSize = 1f;
    public float height = 1f;
    private void DrawFaces()
    {
        faces = new List<Face>();

        for (int point = 0; point < 6; point++)
        {
            faces.Add(CreateFace(innerSize, outerSize, height / 2f, height / 2f, point));
        }
    }

    private Face CreateFace(float innerRad, float outerRad, float heightA, float heightB, int point, bool reverese = false)
    {
        Vector3 pointA = GetPoint(innerRad, heightB, point);
        Vector3 pointB = GetPoint(innerRad, heightB, (point < 5) ? point + 1 : 0);
        Vector3 pointC = GetPoint(outerRad, heightA, (point < 5) ? point + 1 : 0);
        Vector3 pointD = GetPoint(outerRad, heightA, point);

        List<Vector3> vertices = new List<Vector3> { pointA, pointB, pointC, pointD };
        List<int> triangles = new List<int> { 0, 1, 2, 2, 3, 0 };
        List<Vector2> uvs = new List<Vector2>() { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        if (reverese)
        {
            triangles.Reverse();
        }
        return new Face(vertices, triangles, uvs);
    }

    protected Vector3 GetPoint(float radius, float height, int index)
    {
        float angle_deg = 60 * index;
        float angle_rad = Mathf.Deg2Rad * angle_deg;
        return new Vector3(radius * Mathf.Cos(angle_rad), height, radius * Mathf.Sin(angle_rad));
    }

    private void CombineFaces()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < faces.Count; i++)
        {
            vertices.AddRange(faces[i].vertices);
            uvs.AddRange(faces[i].uvs);

            int offset = (4 * i);
            foreach (var t in faces[i].triangles)
            {
                triangles.Add(t + offset);
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }


}


public struct Face
{
    public List<Vector3> vertices { get; private set; }
    public List<int> triangles { get; private set; }
    public List<Vector2> uvs { get; private set; }
    public Face(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs;
    }
}