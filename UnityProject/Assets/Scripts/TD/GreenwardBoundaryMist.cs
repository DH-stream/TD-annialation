using UnityEngine;

namespace TDAnnihilation
{
    public sealed class GreenwardBoundaryMist : MonoBehaviour
    {
        [SerializeField] private float driftSpeed = 0.12f;
        private Vector3 origin;

        public static GameObject CreateForEditor(Transform parent)
        {
            Transform existing = parent.Find("Living Boundary Mist");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            GameObject root = new GameObject("Living Boundary Mist");
            root.transform.SetParent(parent);
            GreenwardBoundaryMist mist = root.AddComponent<GreenwardBoundaryMist>();
            mist.BuildVolumes();
            return root;
        }

        private void Awake()
        {
            origin = transform.position;
            if (GetComponentsInChildren<ParticleSystem>(true).Length == 0) BuildVolumes();
        }

        private void Update()
        {
            transform.position = origin + new Vector3(Mathf.Sin(Time.time * driftSpeed) * 0.7f, 0f, Mathf.Cos(Time.time * driftSpeed * 0.83f) * 0.7f);
        }

        private void BuildVolumes()
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Unlit/Color"));
            material.name = "Greenward Boundary Mist";
            material.color = new Color(0.62f, 0.74f, 0.78f, 0.16f);
            CreateVolume("Mist North", new Vector3(0f, 1.6f, 33f), new Vector3(96f, 3.2f, 4f), material);
            CreateVolume("Mist South", new Vector3(0f, 1.6f, -33f), new Vector3(96f, 3.2f, 4f), material);
            CreateVolume("Mist West", new Vector3(-49f, 1.6f, 0f), new Vector3(4f, 3.2f, 64f), material);
            CreateVolume("Mist East", new Vector3(49f, 1.6f, 0f), new Vector3(4f, 3.2f, 64f), material);
        }

        private void CreateVolume(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject volume = new GameObject(name);
            volume.transform.SetParent(transform);
            volume.transform.localPosition = position;
            ParticleSystem particles = volume.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 2.2f);
            main.startColor = new Color(0.74f, 0.84f, 0.86f, 0.22f);
            main.maxParticles = 100;
            var emission = particles.emission;
            emission.rateOverTime = 10f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = scale;
            ParticleSystemRenderer renderer = volume.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = material;
        }
    }
}
