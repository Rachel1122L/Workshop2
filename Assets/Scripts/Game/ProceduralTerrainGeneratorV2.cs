using UnityEngine;
[ExecuteInEditMode]
public class ProceduralTerrainGeneratorV2 : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainWidth = 512;
    public int terrainLength = 512;
    public int terrainHeight = 50; // max vertical height

    [Header("Noise Settings")]
    public float scale = 80f;
    public float offsetX = 0f;
    public float offsetY = 0f;

    [Header("Island Settings")]
    [Range(0f, 1f)] public float islandSize = 0.5f; // smaller = more ocean
    [Range(0f, 1f)] public float edgeFalloff = 0.5f; // how fast it fades to sea level

    [Header("Seed Settings")]
    [Tooltip("Leave 0 to auto-randomize each play")]
    public int seed = 0;
    public bool randomizeOnPlay = true;

    [Header("Noise Variation")]
    public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Terrain Layers (Textures)")]
    public TerrainLayer waterLayer;
    public TerrainLayer sandLayer;
    public TerrainLayer grassLayer;
    public TerrainLayer rockLayer;
    public TerrainLayer snowLayer;

    [Header("Height Thresholds for Layers")]
    [Range(0f, 1f)] public float sandHeight = 0.3f;
    [Range(0f, 1f)] public float grassHeight = 0.45f;
    [Range(0f, 1f)] public float rockHeight = 0.6f;
    [Range(0f, 1f)] public float snowHeight = 0.8f;

    private Terrain terrain;
    private int currentSeed;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found!");
            return;
        }

        // Auto-randomize seed if enabled and seed is 0
        if (randomizeOnPlay && seed == 0)
        {
            currentSeed = System.DateTime.Now.GetHashCode();
        }
        else
        {
            currentSeed = seed;
        }

        GenerateTerrain();
        Debug.Log("Generated Terrain with Seed: " + currentSeed);
    }

    void OnValidate()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();

        if (terrain != null && Application.isPlaying == false)
        {
            // Use the seed directly in editor
            currentSeed = seed;
            GenerateTerrain();
        }
    }

    public void GenerateTerrain()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();

        // Apply dimensions
        TerrainData data = terrain.terrainData;
        data.heightmapResolution = 513;
        data.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        float[,] heights = GenerateHeights(data.heightmapResolution);
        data.SetHeights(0, 0, heights);

        // Apply textures based on height
        ApplyTerrainTextures(data, heights);
    }

    float[,] GenerateHeights(int resolution)
    {
        float[,] heights = new float[resolution, resolution];
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float maxDistance = resolution * islandSize;

        // Initialize random with seed
        Random.InitState(currentSeed);

        // Generate octave offsets
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetXOctave = Random.Range(-10000f, 10000f);
            float offsetYOctave = Random.Range(-10000f, 10000f);
            octaveOffsets[i] = new Vector2(offsetXOctave, offsetYOctave);
        }

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseValue = 0f;
                float maxAmplitude = 0f;

                // Multi-octave Perlin noise
                for (int o = 0; o < octaves; o++)
                {
                    float xCoord = (float)x / resolution * scale * frequency + offsetX + octaveOffsets[o].x;
                    float yCoord = (float)y / resolution * scale * frequency + offsetY + octaveOffsets[o].y;

                    noiseValue += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;
                    maxAmplitude += amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseValue /= maxAmplitude;

                // Distance from center for island shape
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float mask = Mathf.Clamp01(1f - Mathf.Pow(distance / maxDistance, edgeFalloff));

                // Combine noise + island mask
                //heights[x, y] = noiseValue * mask;

                // Combine noise + island mask
                float rawHeight = noiseValue * mask;

                // Quantize (snap) height to fixed levels
                float step = 0.5f; // height intervals (0, 0.5, 1)
                float quantized = Mathf.Round(rawHeight / step) * step;

                heights[x, y] = quantized;

            }
        }

        return heights;
    }

    void ApplyTerrainTextures(TerrainData data, float[,] heights)
    {
        int resolution = data.alphamapResolution;
        float[,,] alphamaps = new float[resolution, resolution, 5]; // 5 layers

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Map alphamap coordinates to heightmap
                float heightX = (x / (float)resolution) * (heights.GetLength(0) - 1);
                float heightY = (y / (float)resolution) * (heights.GetLength(1) - 1);

                int hx = Mathf.Clamp((int)heightX, 0, heights.GetLength(0) - 1);
                int hy = Mathf.Clamp((int)heightY, 0, heights.GetLength(1) - 1);

                float height = heights[hy, hx];

                // Determine texture based on height
                float water = height < sandHeight ? 1f : 0f;
                float sand = (height >= sandHeight && height < grassHeight) ? 1f : 0f;
                float grass = (height >= grassHeight && height < rockHeight) ? 1f : 0f;
                float rock = (height >= rockHeight && height < snowHeight) ? 1f : 0f;
                float snow = height >= snowHeight ? 1f : 0f;

                // Smooth transitions between layers
                if (height >= sandHeight - 0.05f && height < sandHeight + 0.05f)
                {
                    float blend = (height - (sandHeight - 0.05f)) / 0.1f;
                    water = 1f - blend;
                    sand = blend;
                }
                if (height >= grassHeight - 0.05f && height < grassHeight + 0.05f)
                {
                    float blend = (height - (grassHeight - 0.05f)) / 0.1f;
                    sand = 1f - blend;
                    grass = blend;
                }
                if (height >= rockHeight - 0.05f && height < rockHeight + 0.05f)
                {
                    float blend = (height - (rockHeight - 0.05f)) / 0.1f;
                    grass = 1f - blend;
                    rock = blend;
                }
                if (height >= snowHeight - 0.05f && height < snowHeight + 0.05f)
                {
                    float blend = (height - (snowHeight - 0.05f)) / 0.1f;
                    rock = 1f - blend;
                    snow = blend;
                }

                // Normalize and assign
                float total = water + sand + grass + rock + snow;
                alphamaps[y, x, 0] = water / total;
                alphamaps[y, x, 1] = sand / total;
                alphamaps[y, x, 2] = grass / total;
                alphamaps[y, x, 3] = rock / total;
                alphamaps[y, x, 4] = snow / total;
            }
        }

        data.SetAlphamaps(0, 0, alphamaps);
    }
}