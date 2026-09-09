using System.Collections.Generic;
using UnityEngine;

namespace TDAnnihilation
{
    public static class GreenwardGroundBuilder
    {
        public static void Build(Transform parent, IReadOnlyList<Vector3> route)
        {
            if (parent == null || route == null || route.Count < 2) return;
            BuildRibbon(parent, route, "Gravel Road", 4.1f, 0.08f, GreenwardMaterialLibrary.Gravel);
            BuildWheelTracks(parent, route);
            ScatterRoadStones(parent, route);
            BuildGrass(parent);
        }

        private static void BuildWheelTracks(Transform parent, IReadOnlyList<Vector3> route)
        {
            for (int i = 0; i < route.Count - 1; i++)
            {
                Vector3 a = route[i];
                Vector3 b = route[i + 1];
                float length = Vector3.Distance(a, b);
                int marks = Mathf.Max(1, Mathf.FloorToInt(length / 3.2f));
                for (int mark = 0; mark < marks; mark++)
                {
                    if ((i + mark) % 3 == 0) continue;
                    float start = (mark + 0.18f) / marks;
                    float end = Mathf.Min(1f, start + 0.28f / marks);
                    Vector3[] dash = { Vector3.Lerp(a, b, start), Vector3.Lerp(a, b, end) };
                    BuildRibbon(parent, dash, "Worn Wheel Track", 0.18f, 0.095f,
                        GreenwardMaterialLibrary.CompactedGravel, ((i + mark) & 1) == 0 ? -0.78f : 0.78f);
                }
            }
        }

        private static void BuildRibbon(Transform parent, IReadOnlyList<Vector3> route, string name, float width, float lift, Material material, float lateralOffset = 0f)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            float travelled = 0f;
            for (int i = 0; i < route.Count; i++)
            {
                Vector3 forward = i == route.Count - 1 ? route[i] - route[i - 1] : route[i + 1] - route[i];
                forward.y = 0f;
                Vector3 right = Vector3.Cross(Vector3.up, forward.normalized);
                float edgeNoise = Mathf.Sin(i * 2.37f) * Mathf.Min(0.22f, width * 0.18f);
                Vector3 center = route[i] + right * lateralOffset + Vector3.up * lift;
                if (i > 0) travelled += Vector3.Distance(route[i - 1], route[i]);
                vertices.Add(center - right * (width * 0.5f + edgeNoise));
                vertices.Add(center + right * (width * 0.5f - edgeNoise));
                uv.Add(new Vector2(0f, travelled * 0.22f));
                uv.Add(new Vector2(1f, travelled * 0.22f));
                if (i == 0) continue;
                int start = vertices.Count - 4;
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start + 3);
            }
            Mesh mesh = new Mesh { name = name + " Mesh" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            GameObject road = new GameObject(name);
            road.transform.SetParent(parent);
            road.AddComponent<MeshFilter>().sharedMesh = mesh;
            road.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void ScatterRoadStones(Transform parent, IReadOnlyList<Vector3> route)
        {
            if (route == null || route.Count < 2) return;
            var random = new System.Random(20260909);
            for (int i = 0; i < 115; i++)
            {
                int segment = random.Next(route.Count - 1);
                float t = (float)random.NextDouble();
                Vector3 center = Vector3.Lerp(route[segment], route[segment + 1], t);
                Vector3 direction = route[segment + 1] - route[segment];
                Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);
                center += right * ClampRoadsideOffset(Mathf.Lerp(-1.8f, 1.8f, (float)random.NextDouble()));
                float scale = Mathf.Lerp(0.12f, 0.32f, (float)random.NextDouble());
                GameObject stone = CreatePrimitive("Roadside Gravel", PrimitiveType.Sphere, center + Vector3.up * 0.11f,
                    new Vector3(scale * 1.7f, scale * 0.45f, scale), GreenwardMaterialLibrary.Stone, parent);
                Object.Destroy(stone.GetComponent<Collider>());
            }
        }

        public static float ClampRoadsideOffset(float offset)
        {
            return Mathf.Sign(offset == 0f ? 1f : offset) * Mathf.Max(2.6f, Mathf.Abs(offset));
        }

        private static void BuildGrass(Transform parent)
        {
            Mesh cluster = CreateGrassCluster();
            var random = new System.Random(4411);
            for (int batch = 0; batch < 12; batch++)
            {
                CombineInstance[] instances = new CombineInstance[180];
                int filled = 0;
                while (filled < instances.Length)
                {
                    float x = Mathf.Lerp(-46f, 46f, (float)random.NextDouble());
                    float z = Mathf.Lerp(-29f, 29f, (float)random.NextDouble());
                    Vector2 point = new Vector2(x, z);
                    if (GreenwardWorldLayout.IsRoad(point, 2.8f) || Mathf.Abs(x + 15f) < 3.2f || x < -34f || IsVillageFootprint(point)) continue;
                    float scale = Mathf.Lerp(0.65f, 1.35f, (float)random.NextDouble());
                    instances[filled++].transform = Matrix4x4.TRS(
                        new Vector3(x, GreenwardWorldLayout.HeightAt(x, z) + 0.05f, z),
                        Quaternion.Euler(0f, (float)random.NextDouble() * 180f, Mathf.Lerp(-7f, 7f, (float)random.NextDouble())),
                        new Vector3(scale, scale, scale));
                }
                for (int i = 0; i < instances.Length; i++) instances[i].mesh = cluster;
                Mesh combined = new Mesh { name = "Grass Meadow Batch" };
                combined.CombineMeshes(instances, true, true, false);
                GameObject grass = new GameObject("Grass Meadow Batch");
                grass.transform.SetParent(parent);
                grass.AddComponent<MeshFilter>().sharedMesh = combined;
                grass.AddComponent<MeshRenderer>().sharedMaterial = GreenwardMaterialLibrary.Leaves;
            }
        }

        private static bool IsVillageFootprint(Vector2 point)
        {
            Vector3[] buildings =
            {
                new Vector3(-7f, 11f, 3.5f), new Vector3(3f, 10f, 3.1f),
                new Vector3(10f, 7f, 3.3f), new Vector3(-2f, -10f, 3f),
                new Vector3(10f, -11f, 3.7f), new Vector3(2f, -1f, 4f),
                new Vector3(-7f, -7f, 3.5f), new Vector3(15f, 12f, 3.2f)
            };
            for (int i = 0; i < buildings.Length; i++)
            {
                Vector2 center = new Vector2(buildings[i].x, buildings[i].y);
                if ((point - center).sqrMagnitude < buildings[i].z * buildings[i].z) return true;
            }
            return false;
        }

        private static Mesh CreateGrassCluster()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int blade = 0; blade < 5; blade++)
            {
                float angle = blade * 137.5f * Mathf.Deg2Rad;
                Vector3 center = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.12f;
                Vector3 side = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 0.055f;
                Vector3 tip = center + new Vector3(Mathf.Sin(angle) * 0.08f, 0.58f + blade * 0.045f, Mathf.Cos(angle) * 0.08f);
                Vector3 normal = Vector3.Cross(tip - center, side).normalized;
                int start = vertices.Count;
                vertices.Add(center - side); vertices.Add(center + side); vertices.Add(tip);
                vertices.Add(center - side); vertices.Add(tip); vertices.Add(center + side);
                for (int i = 0; i < 3; i++) normals.Add(normal);
                for (int i = 0; i < 3; i++) normals.Add(-normal);
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(.5f, 1f));
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(.5f, 1f)); uv.Add(new Vector2(1f, 0f));
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start + 3); triangles.Add(start + 4); triangles.Add(start + 5);
            }
            Mesh mesh = new Mesh { name = "Tapered Grass Blade Cluster" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name; item.transform.SetParent(parent); item.transform.position = position; item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }
    }
}
