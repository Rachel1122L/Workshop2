using UnityEngine;

public class TerrainToMesh : MonoBehaviour
{
    public Terrain terrain;
    public float simplificationFactor = 8f; // higher = fewer vertices

    void Start()
    {
        if (!terrain) terrain = GetComponent<Terrain>();
        ConvertToMesh();
    }

    void ConvertToMesh()
    {
        TerrainData td = terrain.terrainData;
        int width = td.heightmapResolution;
        int height = td.heightmapResolution;
        float[,] heights = td.GetHeights(0, 0, width, height);

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];

        int triIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float h = heights[x, y] * td.size.y;
                vertices[y * width + x] = new Vector3(x, h, y);
                if (x < width - 1 && y < height - 1)
                {
                    int i = y * width + x;
                    triangles[triIndex++] = i;
                    triangles[triIndex++] = i + width + 1;
                    triangles[triIndex++] = i + width;
                    triangles[triIndex++] = i;
                    triangles[triIndex++] = i + 1;
                    triangles[triIndex++] = i + width + 1;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject terrainMesh = new GameObject("LowPolyTerrain", typeof(MeshFilter), typeof(MeshRenderer));
        terrainMesh.GetComponent<MeshFilter>().mesh = mesh;
        terrainMesh.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/FlatTerrain"));
    }
}
