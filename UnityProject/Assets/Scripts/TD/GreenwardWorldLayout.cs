using System.Collections.Generic;
using UnityEngine;

namespace TDAnnihilation
{
    public enum GreenwardSurfaceRegion
    {
        Meadow,
        Road,
        Riverbank,
        Corruption,
        Castle
    }

    public static class GreenwardWorldLayout
    {
        public const float Width = 96f;
        public const float Depth = 64f;

        public static List<Vector3> CreateRoute()
        {
            float[,] points = { {-43,-10}, {-34,-8}, {-27,-2}, {-20,5}, {-11,7}, {-3,3}, {6,-2}, {16,-5}, {25,-1}, {34,4}, {43,4} };
            var route = new List<Vector3>(points.GetLength(0));
            for (int i = 0; i < points.GetLength(0); i++)
            {
                float x = points[i, 0], z = points[i, 1];
                route.Add(new Vector3(x, HeightAt(x, z) + 0.28f, z));
            }
            return route;
        }

        public static float HeightAt(float x, float z)
        {
            float eastRise = Mathf.SmoothStep(0f, 4.5f, Mathf.InverseLerp(12f, 45f, x));
            float valley = -0.8f * Mathf.Exp(-Mathf.Pow((x + 7f) / 13f, 2f));
            float undulation = Mathf.Sin(x * 0.105f) * 0.42f + Mathf.Cos(z * 0.14f) * 0.32f;
            float broadBumps = (Mathf.PerlinNoise((x + 91f) * 0.075f, (z + 47f) * 0.075f) - 0.5f) * 0.72f;
            float fineBumps = (Mathf.PerlinNoise((x + 19f) * 0.23f, (z + 113f) * 0.23f) - 0.5f) * 0.18f;
            return eastRise + valley + undulation + broadBumps + fineBumps;
        }

        public static float RouteLength(IReadOnlyList<Vector3> route)
        {
            float length = 0f;
            for (int i = 1; i < route.Count; i++) length += Vector3.Distance(route[i - 1], route[i]);
            return length;
        }

        public static bool IsRoad(Vector2 point, float halfWidth)
        {
            List<Vector3> route = CreateRoute();
            float thresholdSquared = halfWidth * halfWidth;
            for (int i = 1; i < route.Count; i++)
            {
                Vector2 a = new Vector2(route[i - 1].x, route[i - 1].z);
                Vector2 b = new Vector2(route[i].x, route[i].z);
                Vector2 segment = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
                if ((point - (a + segment * t)).sqrMagnitude <= thresholdSquared) return true;
            }
            return false;
        }

        public static GreenwardSurfaceRegion SurfaceRegionAt(Vector2 point)
        {
            if (IsRoad(point, 2.2f)) return GreenwardSurfaceRegion.Road;
            if (point.x < -30f) return GreenwardSurfaceRegion.Corruption;
            if (Mathf.Abs(point.x + 15f) < 3.5f) return GreenwardSurfaceRegion.Riverbank;
            if (point.x > 28f) return GreenwardSurfaceRegion.Castle;
            return GreenwardSurfaceRegion.Meadow;
        }
    }
}
