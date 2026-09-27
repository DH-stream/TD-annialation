using UnityEngine;

namespace TDAnnihilation
{
    public sealed class GreenwardFire : MonoBehaviour
    {
        public float height = 1.4f;
        public float radius = 0.3f;
        private Material flameMaterial;
        private Texture2D flameTexture;

        private void Start()
        {
            GameObject flame = new GameObject("Wind Driven Flame");
            flame.transform.SetParent(transform, false);
            ParticleSystem particles = flame.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.05f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(height * 0.75f, height * 1.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.8f, radius * 1.7f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 70f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = radius * 0.5f;
            shape.angle = 8f;
            var velocity = particles.velocityOverLifetime;
            velocity.x = new ParticleSystem.MinMaxCurve(0.3f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            velocity.enabled = true;
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.22f;
            noise.frequency = 0.7f;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.42f, 0.08f), 0.55f), new GradientColorKey(new Color(0.7f, 0.08f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.12f), new GradientAlphaKey(0.7f, 0.55f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
            flameMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Unlit/Color")) { name = "Greenward Flame" };
            flameTexture = new Texture2D(32, 64, TextureFormat.RGBA32, false) { name = "Soft Flame Silhouette", filterMode = FilterMode.Bilinear };
            Color[] pixels = new Color[32 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 32; x++)
            {
                float rise = y / 63f;
                float width = Mathf.Lerp(0.45f, 0.08f, rise);
                float edge = Mathf.Clamp01(1f - Mathf.Abs(x / 31f - 0.5f) / width);
                float alpha = edge * edge * Mathf.Sin(rise * Mathf.PI);
                pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha);
            }
            flameTexture.SetPixels(pixels);
            flameTexture.Apply(false, true);
            flameMaterial.SetTexture("_BaseMap", flameTexture);
            flameMaterial.SetColor("_BaseColor", Color.white);
            flameMaterial.SetFloat("_Surface", 1f);
            flameMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            flameMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            flameMaterial.SetFloat("_ZWrite", 0f);
            flameMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            flameMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            flame.GetComponent<ParticleSystemRenderer>().sharedMaterial = flameMaterial;
            GameObject glow = new GameObject("Firelight");
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * 0.35f;
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.14f);
            light.intensity = 1.4f;
            light.range = Mathf.Max(3f, height * 2.5f);
            light.shadows = LightShadows.None;
        }

        private void OnDestroy()
        {
            if (flameMaterial != null) Destroy(flameMaterial);
            if (flameTexture != null) Destroy(flameTexture);
        }
    }
}
