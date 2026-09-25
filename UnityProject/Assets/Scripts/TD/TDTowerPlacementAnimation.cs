using UnityEngine;

namespace TDAnnihilation
{
    public sealed class TDTowerPlacementAnimation : MonoBehaviour
    {
        private const float Duration = 0.35f;
        private Vector3 fullScale;
        private float elapsed;
        private bool playing;

        public static float EvaluateHeight(float elapsedTime, float duration)
        {
            if (duration <= 0f) return 1f;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public void Play()
        {
            fullScale = transform.localScale;
            elapsed = 0f;
            playing = true;
            transform.localScale = new Vector3(fullScale.x, fullScale.y * 0.12f, fullScale.z);
            SpawnDust(transform.position);
        }

        private void Update()
        {
            if (!playing) return;
            elapsed += Time.deltaTime;
            float height = EvaluateHeight(elapsed, Duration);
            transform.localScale = new Vector3(fullScale.x, fullScale.y * height, fullScale.z);
            if (elapsed < Duration) return;
            transform.localScale = fullScale;
            playing = false;
        }

        private static void SpawnDust(Vector3 position)
        {
            GameObject dust = new GameObject("Tower Placement Dust");
            dust.transform.position = position + Vector3.up * 0.18f;
            ParticleSystem particles = dust.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.22f;
            main.startLifetime = 0.45f;
            main.startSpeed = 2.3f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.72f, 0.52f, 0.28f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.22f;
            shape.length = 0.08f;
            ParticleSystemRenderer renderer = dust.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Unlit/Color");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(shader) { color = new Color(0.72f, 0.52f, 0.28f, 1f) };
            particles.Play();
        }
    }
}
