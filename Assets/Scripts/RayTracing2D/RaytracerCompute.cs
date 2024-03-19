using UnityEngine;
using TerrainGeneration;

namespace RayTracing
{
    public static class RayTracerCompute
    {
        private const int mapSize = MapGenerator.mapSize;
        private const float sunOrbitRadius = 1.52f * mapSize;
        private const float sunHeight = 5;

        private const float brightnessCoef = 0.4f;
        private const int distToCheckOnRay = 40;

        public static Vector3 sunPosition = new Vector3(sunOrbitRadius, 0, 0);
        private static Vector2 sunPosition2D = new Vector2(sunOrbitRadius, 0);

        private static ComputeShader rayTracerCompute = Object.FindObjectOfType<ComputeShader>();
        public static void SetComputeShader(ComputeShader compute)
        {
            rayTracerCompute = compute;
        }

        private static RenderTexture renderTexture;

        /// <summary>
        /// Set la position du soleil sur le demi cercle de centre le centre de la carte et de rayon sunOrbitRadius
        /// </summary>
        /// <param name="theta"> Angle entre l'axe x et le rayon du centre vers le soleil</param>
        public static void SetSunPosition(float theta)
        {
            float r = sunOrbitRadius;
            float cos_theta = Mathf.Cos(theta), sin_theta = Mathf.Sin(theta);

            sunPosition = new Vector3(r * cos_theta + mapSize / 2, sunHeight, r * sin_theta + mapSize / 2);
            sunPosition2D = new Vector2(sunPosition.x, sunPosition.z);

            Debug.Log("New sun position : " + sunPosition.ToString());
        }

        /// <summary>
        /// Simule les ombres sur la map pour le soleil placé en sunPosition
        /// </summary>
        /// <param name="mapdata"></param>
        public static MapData RayTrace(MapData mapData)
        {
            renderTexture = new RenderTexture(mapSize, mapSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            int kernel = rayTracerCompute.FindKernel("CSMain");
            rayTracerCompute.SetTexture(kernel, "Result", renderTexture);

            int workGroupeSizeX = Mathf.CeilToInt(mapSize / 8);
            int workGroupeSizeY = Mathf.CeilToInt(mapSize / 8);

            rayTracerCompute.Dispatch(kernel, workGroupeSizeX, workGroupeSizeY, 1);

            mapData.texture = new Texture2D(mapSize, mapSize, TextureFormat.RGB24, false);
            // ReadPixels looks at the active RenderTexture.
            RenderTexture.active = renderTexture;
            mapData.texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            mapData.texture.Apply();

            return mapData;
        }
    }
}