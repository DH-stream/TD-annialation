using UnityEngine;

namespace TDAnnihilation
{
    public sealed class GreenwardWaterMotion : MonoBehaviour
    {
        [SerializeField] private Vector2 flowDirection = new Vector2(0f, -1f);
        [SerializeField] private float flowSpeed = 0.18f;
        [SerializeField] private float waveHeight = 0.035f;
        private Renderer waterRenderer;
        private Material waterMaterial;
        private Vector3 startPosition;

        private void Awake()
        {
            waterRenderer = GetComponent<Renderer>();
            if (waterRenderer != null) waterMaterial = waterRenderer.material;
            startPosition = transform.position;
        }

        private void Update()
        {
            if (waterMaterial != null && waterMaterial.HasProperty("_BaseMap"))
                waterMaterial.SetTextureOffset("_BaseMap", flowDirection.normalized * (Time.time * flowSpeed));
            transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * 2.4f) * waveHeight;
        }
    }
}
