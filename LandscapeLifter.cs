using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a mesh with heights based on a height map
/// </summary>
public class LandscapeLifter : MonoBehaviour
{

    Mesh mesh;

	Vector3[] vertices;
	int[] triangles;

	private int xSize = 50;
	private int zSize = 50;

    public Texture2D texture2D;

    void Start()
	{
        // Generate Mesh
        mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

        Center();
    }

    void Update()
    {
        CreateShape();
        UpdateMesh();
    }

    private float scale = 0.004f;


    /// <summary>
    /// Center the mesh
    /// </summary>
    private void Center()
    {
        transform.localScale = new Vector3(scale, scale, scale);
        transform.localPosition = new Vector3((xSize * -0.5f) * scale, 0, (xSize * -0.5f) * scale);
    }

        
    public float heightMax = 12f; // The maximum height for the terrain at 100% darkness

    /// <summary>
    /// Update the mesh height
    /// </summary>
    void CreateShape()
	{
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        int vertexIndex = 0;

        for (int z = 0; z <= zSize; z++)
		{
            for (int x = 0; x <= xSize; x++)
			{
                if (texture2D == null) {
                    vertices[vertexIndex] = new Vector3(x, 0, z);
                }
                else { 
                    vertices[vertexIndex] = new Vector3(x, (heightMax - texture2D.GetPixel(x,z).grayscale * heightMax), z);
                }
                vertexIndex++;
            }
        }

        triangles = new int[xSize * zSize * 6];

        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            { 
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;

                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }

            vert++;
        }
    }


    float xMax = 4f;
    float yMax = 4f;

    /// <summary>
    /// Update our mesh
    /// </summary>
    void UpdateMesh()
	{
		mesh.Clear();

        // Calculate UVs
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / (float)xMax, vertices[i].y / (float)yMax);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
