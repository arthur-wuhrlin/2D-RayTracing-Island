using UnityEngine;
using NoiseMath;
using RayTracing;

namespace TerrainGeneration
{
    public class MapGenerator : MonoBehaviour
    {
        public enum DrawMode { NoiseMap, ColourMap, FalloffMap };
        [SerializeField] private DrawMode drawMode;

        [SerializeField] private bool useFalloff;
        [SerializeField] private float islandEffect;

        enum DisplayMethod { Basic, CPURayTracing, GPURayTracing };
        [SerializeField] private DisplayMethod useRayTracing;
        [SerializeField] private float sunAngle;

        [SerializeField] private float noiseScale;
        [SerializeField] private Noise.NoiseFractalType noiseFractalType;
        [SerializeField] [Range(0f, 1f)] private float frequency;
        [SerializeField] private int seed;
        [SerializeField] [Range(0, 10)] private short octaves;
        [SerializeField] [Range(0f, 2f)] private float gain;
        [SerializeField] [Range(0f, 2f)] private float lacunarity;

        [SerializeField] private ComputeShader rayTraceCompute;

        private Noise noise;

        public bool autoUpdate;

        public const int mapSize = 500;
        [SerializeField] private TerrainType[] regions;
        private float[,] falloffMap;


        public void Awake()
        {
            RayTracer.SetSunPosition(sunAngle);

            RayTracerCompute.SetSunPosition(sunAngle);
            RayTracerCompute.SetComputeShader(rayTraceCompute);
        }

        public void DrawMapInEditor()
        {
            MapData mapData = GenerateMapData(Vector2Int.zero);
            MapDisplay display = FindObjectOfType<MapDisplay>();

            if (drawMode == DrawMode.NoiseMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
            }
            else if (drawMode == DrawMode.ColourMap)
            {
                // Detection of usage of GPU
                if (mapData.texture != null)
                    display.DrawTexture(mapData.texture);
                else
                    display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapSize, mapSize));
            }
            else if (drawMode == DrawMode.FalloffMap)
            {
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloff(mapSize, islandEffect)));
            }
        }

        private MapData GenerateMapData(Vector2Int center)
        {
            float[,] noiseMap = NoiseMapGeneration.GenerateNoiseMap(mapSize, center, noise, noiseScale);
            Color[] colourMap = new Color[mapSize * mapSize];

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    if (useFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }

                    float currentHeight = noiseMap[x, y];

                    for (int i = 0; i < regions.Length; i++)
                    {
                        if (currentHeight >= regions[i].height)
                        {
                            colourMap[y * mapSize + x] = regions[i].colour;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            MapData mapData = new MapData(noiseMap, colourMap);

            switch(useRayTracing)
            {
                case DisplayMethod.Basic:
                    break;
                case DisplayMethod.CPURayTracing:
                    RayTracer.RayTrace(mapData);
                    break;
                case DisplayMethod.GPURayTracing:
                    mapData = RayTracerCompute.RayTrace(mapData);
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            return mapData;
        }

        public void ApplyNoiseParameters()
        {
            noise = new Noise(seed, frequency, noiseFractalType);
            if(noiseFractalType == Noise.NoiseFractalType.ValueFractal)
            {
                noise.SetFractalParameters(octaves, gain , lacunarity);
            }
        }

        public void GetFalloffMap()
        {
            falloffMap = FalloffGenerator.GenerateFalloff(mapSize, islandEffect);
        }

        private void OnValidate()
        {
            if (lacunarity < 1)
            {
                lacunarity = 1;
            }
            if (octaves < 0)
            {
                octaves = 0;
            }
        }
    }


    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color colour;
    }

    public struct MapData
    {
        public float[,] heightMap;
        public Color[] colourMap;
        public Texture2D texture;

        public MapData (float[,] heightMap, Color[] colourMap)
        {
            this.heightMap = heightMap;
            this.colourMap = colourMap;
            this.texture = null;
        }
    }
}