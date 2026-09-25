using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GPUGrassRenderer : MonoBehaviour
{
    [Header("Inställningar för GPU-Gräs")]
    [Tooltip("Dra in Meshen för gräset här (t.ex. UNS_Grass_LOD0 Mesh)")]
    public Mesh grassMesh;

    [Tooltip("Dra in gräs-materialet här")]
    public Material grassMaterial;

    [Tooltip("Totalt antal grässtrån på banan")]
    public int totalInstances = 5000;

    [Tooltip("Min och max storlek på grässtråna")]
    public Vector2 scaleRange = new Vector2(0.2f, 0.4f);

    private List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
    private bool isInitialized = false;

    private void OnEnable()
    {
        if (!isInitialized && grassMesh != null && grassMaterial != null)
        {
            GenerateGrass();
        }
    }

    private void Update()
    {
        if (!isInitialized || grassMesh == null || grassMaterial == null) return;

        // Rita ut alla tusentals grässtrån direkt via grafikkortet i varje frame
        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, batch);
        }
    }

    [ContextMenu("1. Generera Gräs (GPU)")]
    public void GenerateGrass()
    {
        batches.Clear();

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("Lägg till en Mesh Collider på marken!");
            return;
        }

        Bounds bounds = col.bounds;
        List<Matrix4x4> currentBatch = new List<Matrix4x4>();

        for (int i = 0; i < totalInstances; i++)
        {
            float rx = Random.Range(bounds.min.x, bounds.max.x);
            float rz = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 rayStart = new Vector3(rx, bounds.max.y + 20f, rz);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f))
            {
                if (hit.collider == col)
                {
                    Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    float scale = Random.Range(scaleRange.x, scaleRange.y);
                    Vector3 scaleVec = new Vector3(scale, scale, scale);

                    Matrix4x4 mat = Matrix4x4.TRS(hit.point, rotation, scaleVec);
                    currentBatch.Add(mat);

                    // Unity klarar max 1023 matriser per GPU-batch
                    if (currentBatch.Count == 1023)
                    {
                        batches.Add(currentBatch.ToArray());
                        currentBatch.Clear();
                    }
                }
            }
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch.ToArray());
        }

        isInitialized = true;
        Debug.Log($"Släppte ut {totalInstances} GPU-instanser utan att skapa ett enda GameObject!");
    }

    [ContextMenu("2. Rensa Gräs")]
    public void ClearGrass()
    {
        batches.Clear();
        isInitialized = false;
    }
}