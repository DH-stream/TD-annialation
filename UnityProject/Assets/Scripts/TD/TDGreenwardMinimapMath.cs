using UnityEngine;

namespace TDAnnihilation
{
    public static class TDGreenwardMinimapMath
    {
        public static Vector2 WorldToMap(Vector3 world, Bounds bounds, Rect mapRect)
        {
            if (bounds.size.x <= Mathf.Epsilon || bounds.size.z <= Mathf.Epsilon) return mapRect.center;

            float x = Mathf.Clamp01((world.x - bounds.min.x) / bounds.size.x);
            float z = Mathf.Clamp01((world.z - bounds.min.z) / bounds.size.z);
            return new Vector2(
                Mathf.Lerp(mapRect.xMin, mapRect.xMax, x),
                Mathf.Lerp(mapRect.yMax, mapRect.yMin, z));
        }
    }
}
