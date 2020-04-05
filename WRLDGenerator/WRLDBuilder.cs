using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.Linq;

[ExecuteInEditMode]
public class WRLDBuilder : MonoBehaviour
{
    [Header("WRLD File")]
    [SerializeField]
    private TextAsset WRLDfile;

    [Space(10)]
    [Header("Prefab")]
    [SerializeField]
    private GameObject terrain;

    private int gridXNum = 0;
    private int gridYNum = 0;

    [SerializeField]
    private GameObject wall;
    private float stageSize = 1f;
    private float baseUnitSize = 0f;

    private float spaceAmount = 0f; // RUNTIME: The spacing we want for our structures
    private float structureScaleSize = 0f; // RUNTIME: Scale size for structures = Scale of this game object divided by the amount of rows we have.

    private List<List<List<KernelProperties>>> kernelStack = new List<List<List<KernelProperties>>>();
    private List<float> kernelStackHeights = new List<float>();

    private void ReadWRLD(TextAsset textAsset)
    {

        // Clear our lists
        kernelStack = new List<List<List<KernelProperties>>>();
        kernelStackHeights = new List<float>();

        // Load the WRLDfile and parse it
        string WORLDFile = WRLDfile.text;
        var WorldText = JSON.Parse(WORLDFile);
        Debug.Log("Generating: " + WorldText["worldName"]);


        // ASSIGN VARIABLES
        // Structure grid
        gridXNum = int.Parse(WorldText["gridXNum"]);
        gridYNum = int.Parse(WorldText["gridYNum"]);

        // Terrain grid
        xSize = int.Parse(WorldText["gridXNum"]); // Make this divisible by the 20 x 20 we have.. maybe 80?
        zSize = int.Parse(WorldText["gridYNum"]);

        // Scales
        // Structure scales
        CalcSize();
        // Terrain scales
        PrimeTerrainSettings();

        // Item values 
        SetupKernelProperties((int)WorldText["stacks"].Count);

        // Set item values
        for (int s = 0; s < (int)WorldText["stacks"].Count; s++)
        {
            for (int y = 0; y < gridYNum; y++)
            {
                for (int x = 0; x < gridYNum; x++)
                {
                    // For each column, add "y" rows to that list
                    kernelStack[s][y][x].heightLevel = float.Parse( (string)WorldText["stacks"][s]["stacksMap"][y][x]["heightLevel"] );
                }
            }
        }

        // TERRAIN MESH
        // Set heights
        CreateTerrainShape(float.Parse((string)WorldText["stacks"][0]["stackHeight"]));
        // Update the UVS
        UpdateMesh();

        // Stack heights
        for (int i = 0; i < (int)WorldText["stacks"].Count; i++)
        {
            float CurStackHeight = float.Parse((string)WorldText["stacks"][i]["stackHeight"]);
            kernelStackHeights.Add(CurStackHeight);
        }

        // Clear previous structures
        ClearAllStructures();

        // Generate structures
        GenerateAllMarkerStacks((int)WorldText["stacks"].Count);
    }

    /// <summary>
    /// Calculate the size to set items based on our scales
    /// </summary>
    private void CalcSize()
    {
        // Get the base unit for the given structure
        baseUnitSize = stageSize / gridYNum;

        // Scale size for structures = Scale of this game object divided by the amount of rows we have.
        structureScaleSize = 1f / (float)gridYNum;

        // Set the spacing we want for our structures
        spaceAmount = baseUnitSize * 1f;
    }

    /// <summary>
    /// Clears any child gameobjects
    /// </summary>
    private void ClearAllStructures ()
    {
        // Delete everything
        var tempList = transform.Cast<Transform>().ToList();
        foreach (Transform child in tempList)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    /// <summary>
    /// Handle user updates
    /// </summary>
    [Space(10)]
    [Header("Generate")]
    public bool BuildStructures;
    public bool ClearStructures;
    private void Update()
    {
        if (BuildStructures)
        {
            BuildStructures = false;
            ClearAllStructures();
            ReadWRLD(WRLDfile);
        }

        if (ClearStructures)
        {
            ClearStructures = false;
            ClearAllStructures();
            ClearMesh();
        }
    }


    /// ———————————
    /// Structure Rendering
    /// ———————————

    /// <summary>
    /// Generate all stacks of markers for pooling
    /// </summary>
    public void GenerateAllMarkerStacks(int numOfStacksToGen)
    {
        for (int i = 0; i < (numOfStacksToGen); i++)
        {
            GenerateMarkers(i);
        }
    }

    /// <summary>
    /// Populates the 2d arraylist of kernel properties with default values
    /// </summary>
    private void SetupKernelProperties(int numOfStacksToGen)
    {

        // Make a new list to hold the stack
        kernelStack.Add(new List<List<KernelProperties>>());

        // Generate row of stacks
        for (int s = 0; s < numOfStacksToGen; s++)
        {
            // Add each stack
            kernelStack.Add(new List<List<KernelProperties>>());

            // Generate grid of kernels
            for (int y = 0; y < gridYNum; y++)
            {
                // For each column, add a list
                kernelStack[s].Add(new List<KernelProperties>());
                for (int x = 0; x < gridYNum; x++)
                {
                    // Set up a default with 0 height
                    KernelProperties defaultProp = new KernelProperties(0);

                    // For each column, add "y" rows to that list
                    kernelStack[s][y].Add(defaultProp);
                }
            }
        }
    }

    // Size bounds for our structures
    private const float structureMinHeight = 0.001f;

    [Space(10)]
    [Header("Settings")]
    [Range(0.1f, 0.25f)]
    public float structureMaxHeight = 0.2f;

    public List<List<List<GameObject>>> markers = new List<List<List<GameObject>>>();
    public void GenerateMarkers(int stackIndex)
    {

        // Add each stack
        markers.Add(new List<List<GameObject>>());

        for (int y = 0; y < gridYNum; y++)
        {
            // Add y item
            markers[stackIndex].Add(new List<GameObject>());

            for (int x = 0; x < gridXNum; x++)
            {

                // 0.001f to 1f based on the slider for current stack
                float currentKernelHeight = Mathf.Lerp(structureMinHeight, structureMaxHeight, kernelStackHeights[stackIndex] / 10f);

                // Pull all items, regard of the stack,
                // with a height level of 0 to the bottom
                float floor = 1f;
                float thresholdBetweenVisibleAndNot = 0.65f;
                if ((kernelStack[stackIndex][x][y].heightLevel <= thresholdBetweenVisibleAndNot))
                {
                    floor = 0f;
                }
                else
                {
                    // Debug.Log(kernelStack[stackIndex][x][y].heightLevel);
                }

                // Height of all the structures underneath
                float heightOfPreviousItems = 0f;
                if (stackIndex > 0)
                {
                    for (int i = 1; i <= stackIndex; i++)
                    {
                        heightOfPreviousItems += Mathf.Lerp(structureMinHeight, structureMaxHeight, kernelStackHeights[i - 1] / 10f);
                    }
                }

                // 1. Instantiate
                GameObject structure = Instantiate(wall, new Vector3(0, 0, 0), Quaternion.identity);
                structure.transform.SetParent(this.gameObject.transform);

                // We need to shift the lerp bounds over by half the distance between two cubes.
                float boundaryShift = (Mathf.Lerp(-0.5f, 0.5f, 1f / (float)gridYNum) - Mathf.Lerp(-0.5f, 0.5f, 2f / (float)gridYNum)) * 0.5f;
                float xLocation = boundaryShift - Mathf.Lerp(-0.5f, 0.5f, (float)y / (float)gridYNum);
                float yLocation = boundaryShift - Mathf.Lerp(-0.5f, 0.5f, (float)x / (float)gridXNum);

                // 2. Set scale, position and rotation, just like CheckKernels()
                structure.transform.localScale = new Vector3(structureScaleSize, structureScaleSize, Mathf.Clamp((floor * currentKernelHeight), structureMinHeight, structureMaxHeight));
                structure.transform.localPosition = new Vector3(xLocation, yLocation, floor * -((currentKernelHeight * 0.5f) + heightOfPreviousItems));
                structure.transform.localEulerAngles = new Vector3(0, 0, 0);

                // 3. Name it
                structure.name = "GeneratedStructure_" + x + "_" + y + "_stack_" + stackIndex;

                // 4. Add it to the list to reference later
                markers[stackIndex][y].Add(structure);
            }
        }
    }


    /// ———————————
    /// Terrain Rendering
    /// ———————————

    // Gameobject
    private GameObject meshHolder = null;

    // Mesh
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private float xMax = 4f;
    private float yMax = 4f;

    // Map resolution
    [SerializeField]
    [Range(0.05f, 0.5f)]
    private float terrainScale = 0.05f;
    private int xSize = 20;
    private int zSize = 20;

    /// <summary>
    /// Set up the scales we need to measure the terrain
    /// </summary>
    private void PrimeTerrainSettings()
    {
        ClearMesh();

        // Instantiate
        meshHolder = Instantiate(terrain, new Vector3(0, 0, 0), Quaternion.identity);
        meshHolder.transform.localScale = new Vector3(terrainScale, terrainScale, terrainScale);
        meshHolder.transform.localPosition = new Vector3((gameObject.transform.position.x * 1.5f), 0.01f, (gameObject.transform.position.z * 1.5f));
        meshHolder.name = "wrld_meshHolder";

        // Generate Mesh
        mesh = new Mesh();
        meshHolder.gameObject.GetComponentInChildren<MeshFilter>().mesh = mesh;
    }

    /// <summary>
    /// Check for terrain gameobject and if it’s there delete it
    /// </summary>
    private void ClearMesh()
    {
        if (meshHolder != null)
        {
            DestroyImmediate(meshHolder);
        }
    }

    // The maximum height for the terrain at 100% darkness
    private float terrainMinHeight = 0.001f;
    private float terrainMaxHeight = 4f;

    [SerializeField]
    [Range(0f, 5f)]
    private float terrainHeight = 0.1f;

    [Range(1f, 20f)]
    public int terrainSteps = 3; // Amount of height steps

    /// <summary>
    /// Create the mesh
    /// </summary>
    private void CreateTerrainShape(float kernelStackHeights)
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        int vertexIndex = 0;

        for (int z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                // Value from the slider, 0f - 1f;
                // mapped to the terrain min and max heights
                float terrainHeightMultiplier = kernelStackHeights / 10f;

                // Set height of vertices based on the slider (terrainHeightMultiplier) * the height value (min - max, 0 - 1, or (kernelStack[0][z][x].heightLevel))
                vertices[vertexIndex] = new Vector3((float)x, 0f + terrainHeight * (terrainHeightMultiplier * (float)QuantizeHeights(Mathf.Lerp(terrainMinHeight, (terrainMaxHeight), kernelStack[0][Mathf.Min(z, zSize - 1)][Mathf.Min(x, xSize - 1)].heightLevel))), (float)z);

                // Advance index
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

    /// <summary>
    /// Locks the heights to a particular amount
    /// </summary>
    /// <param name="height"></param>
    private float QuantizeHeights(float height)
    {
        return (Mathf.Round(height / (float)terrainSteps) * (float)terrainSteps);
    }

    /// <summary>
    /// Updates the mesh
    /// </summary>
    private void UpdateMesh()
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


