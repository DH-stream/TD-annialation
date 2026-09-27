using UnityEngine;

namespace TDAnnihilation
{
    public sealed class GreenwardAmbientMotion : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        public float speed = 22f;
        public float sway;
        public bool cloth;
        public bool pulse;

        private Quaternion origin;
        private Vector3 originalScale;
        private Mesh clothMesh;
        private Vector3[] clothVertices;
        private Renderer visual;
        private MaterialPropertyBlock properties;
        private Color baseColor;

        private void Start()
        {
            origin = transform.localRotation;
            originalScale = transform.localScale;
            if (cloth) BuildCloth();
            if (!pulse) return;
            visual = GetComponent<Renderer>();
            if (visual == null) return;
            properties = new MaterialPropertyBlock();
            baseColor = visual.sharedMaterial.GetColor("_BaseColor");
        }

        private void Update()
        {
            if (cloth)
            {
                AnimateCloth();
                return;
            }
            if (pulse)
            {
                float wave = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
                float breadth = Mathf.Lerp(0.86f, 1.16f, wave);
                transform.localScale = new Vector3(originalScale.x * breadth, originalScale.y, originalScale.z * breadth);
                if (visual != null)
                {
                    if (properties == null) properties = new MaterialPropertyBlock();
                    properties.SetColor("_BaseColor", baseColor * Mathf.Lerp(0.75f, 1.35f, wave));
                    visual.SetPropertyBlock(properties);
                }
                return;
            }
            if (sway > 0f) transform.localRotation = origin * Quaternion.AngleAxis(Mathf.Sin(Time.time * speed * Mathf.Deg2Rad) * sway, axis);
            else transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
        }

        private void BuildCloth()
        {
            const int columns = 5, rows = 9;
            float height = originalScale.y, width = originalScale.z;
            transform.localScale = Vector3.one;
            clothVertices = new Vector3[columns * rows];
            int[] triangles = new int[(columns - 1) * (rows - 1) * 6];
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
                clothVertices[row * columns + column] = new Vector3(0f, height * (0.5f - row / (float)(rows - 1)), width * (column / (float)(columns - 1) - 0.5f));
            int index = 0;
            for (int row = 0; row < rows - 1; row++)
            for (int column = 0; column < columns - 1; column++)
            {
                int top = row * columns + column;
                triangles[index++] = top;
                triangles[index++] = top + 1;
                triangles[index++] = top + columns;
                triangles[index++] = top + 1;
                triangles[index++] = top + columns + 1;
                triangles[index++] = top + columns;
            }
            clothMesh = new Mesh { name = "Wind Worn Royal Banner" };
            clothMesh.vertices = clothVertices;
            clothMesh.triangles = triangles;
            clothMesh.RecalculateNormals();
            GetComponent<MeshFilter>().mesh = clothMesh;
            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            visual = GetComponent<Renderer>();
            Material material = new Material(visual.sharedMaterial);
            material.SetFloat("_Cull", 0f);
            visual.material = material;
        }

        private void AnimateCloth()
        {
            const int columns = 5, rows = 9;
            float time = Time.time * speed * Mathf.Deg2Rad;
            for (int row = 0; row < rows; row++)
            {
                float loose = row / (float)(rows - 1);
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    Vector3 vertex = clothVertices[index];
                    vertex.x = loose * (0.18f + 0.10f * Mathf.Sin(time * 1.6f + loose * 2f + column * 0.65f));
                    vertex.z = originalScale.z * (column / (float)(columns - 1) - 0.5f) + loose * 0.06f * Mathf.Sin(time * 2f + loose * 3f);
                    clothVertices[index] = vertex;
                }
            }
            clothMesh.vertices = clothVertices;
            clothMesh.RecalculateNormals();
            clothMesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (clothMesh != null) Destroy(clothMesh);
            if (cloth && visual != null) Destroy(visual.material);
        }
    }
}
