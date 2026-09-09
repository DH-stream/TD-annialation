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
                sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                sun.color = new Color(1f, 0.88f, 0.72f);
                sun.intensity = 1.55f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.68f;
                sun.shadowBias = 0.08f;
                sun.shadowNormalBias = 0.35f;
                RenderSettings.sun = sun;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.64f, 0.72f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.43f, 0.49f, 0.39f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.25f, 0.19f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.reflectionIntensity = 0.78f;

            GameObject lighting = GameObject.Find(LightingRootName);
            if (lighting == null) lighting = new GameObject(LightingRootName);
            AddPoint(lighting.transform, "Forge Firelight", new Vector3(-7f, 2.1f, -4.8f), new Color(1f, 0.38f, 0.08f), 3.2f, 8f);
            AddPoint(lighting.transform, "Portal Glow", new Vector3(-43f, 3f, -10f), new Color(0.62f, 0.12f, 1f), 4.2f, 10f);
            AddPoint(lighting.transform, "Arcane Village Glow", new Vector3(-5f, 2.4f, -2f), new Color(0.24f, 0.38f, 1f), 2.2f, 6f);
            AddPoint(lighting.transform, "Cool Sky Fill", new Vector3(8f, 13f, -5f), new Color(0.34f, 0.48f, 0.72f), 0.55f, 48f);

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
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.38f, 0.55f, 0.68f);
            }
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
