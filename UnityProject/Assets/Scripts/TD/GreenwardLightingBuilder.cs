using UnityEngine;
using UnityEngine.Rendering;

namespace TDAnnihilation
{
    public static class GreenwardLightingBuilder
    {
        private const string LightingRootName = "Greenward Local Lighting";

        public static void Configure(Camera camera)
        {
            Light sun = SelectDirectionalLight();
            if (sun != null)
            {
                sun.type = LightType.Directional;
                sun.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                sun.color = new Color(1f, 0.84f, 0.68f);
                sun.intensity = 1.08f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.88f;
                sun.shadowBias = 0.06f;
                sun.shadowNormalBias = 0.28f;
                RenderSettings.sun = sun;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.40f, 0.50f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.29f, 0.25f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.09f, 0.08f);
            RenderSettings.ambientIntensity = 0.50f;
            RenderSettings.reflectionIntensity = 0.62f;

            ConfigureAtmosphere();

            GameObject lighting = GameObject.Find(LightingRootName);
            if (lighting == null) lighting = new GameObject(LightingRootName);
            AddPoint(lighting.transform, "Forge Firelight", new Vector3(-7f, 2.1f, -4.8f), new Color(1f, 0.31f, 0.055f), 2.55f, 7f);
            AddPoint(lighting.transform, "Portal Glow", new Vector3(-43f, 3f, -10f), new Color(0.56f, 0.10f, 1f), 3.1f, 8.5f);
            AddPoint(lighting.transform, "Arcane Village Glow", new Vector3(-5f, 2.4f, -2f), new Color(0.18f, 0.32f, 1f), 1.35f, 5.5f);
            AddPoint(lighting.transform, "Cool Sky Fill", new Vector3(8f, 13f, -5f), new Color(0.24f, 0.36f, 0.58f), 0.10f, 28f);

            GameObject probeObject = GameObject.Find("Greenward Reflection Probe");
            if (probeObject == null) probeObject = new GameObject("Greenward Reflection Probe");
            probeObject.transform.position = new Vector3(4f, 7f, 2f);
            ReflectionProbe probe = probeObject.GetComponent<ReflectionProbe>();
            if (probe == null) probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.size = new Vector3(82f, 24f, 52f);
            probe.intensity = 0.55f;

            if (camera != null)
            {
                camera.allowHDR = true;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.backgroundColor = new Color(0.08f, 0.11f, 0.16f);
            }
        }

        private static void ConfigureAtmosphere()
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = RenderSettings.skybox;
                if (sky == null || sky.shader != skyShader || sky.name != "Greenward Procedural Sky")
                {
                    sky = new Material(skyShader) { name = "Greenward Procedural Sky" };
                    RenderSettings.skybox = sky;
                }

                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.16f, 0.24f, 0.38f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.07f, 0.065f, 0.055f));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.72f);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 0.72f);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.035f);
                if (sky.HasProperty("_SunSizeConvergence")) sky.SetFloat("_SunSizeConvergence", 5f);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.22f, 0.29f, 0.34f);
            RenderSettings.fogStartDistance = 38f;
            RenderSettings.fogEndDistance = 92f;
        }

        private static Light SelectDirectionalLight()
        {
            if (RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional) return RenderSettings.sun;
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>();
            System.Array.Sort(lights, (a, b) => string.CompareOrdinal(HierarchyPath(a.transform), HierarchyPath(b.transform)));
            for (int i = 0; i < lights.Length; i++)
                if (lights[i].type == LightType.Directional) return lights[i];
            GameObject sunObject = new GameObject("Greenward Sun");
            return sunObject.AddComponent<Light>();
        }

        private static string HierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static void AddPoint(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            Transform child = parent.Find(name);
            GameObject lightObject = child != null ? child.gameObject : new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.GetComponent<Light>();
            if (light == null) light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }
    }
}
