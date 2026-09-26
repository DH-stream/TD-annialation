using NUnit.Framework;
using UnityEngine;

namespace TDAnnihilation.Tests
{
    public sealed class TDGreenwardMinimapMathTests
    {
        private static readonly Bounds WorldBounds = new Bounds(new Vector3(4f, 0f, 8f), new Vector3(20f, 10f, 30f));
        private static readonly Rect MapRect = new Rect(10f, 20f, 100f, 60f);

        [Test]
        public void WorldCornersMapToOppositeScreenCorners()
        {
            Vector2 worldMin = TDGreenwardMinimapMath.WorldToMap(WorldBounds.min, WorldBounds, MapRect);
            Vector2 worldMax = TDGreenwardMinimapMath.WorldToMap(WorldBounds.max, WorldBounds, MapRect);

            Assert.That(worldMin.x, Is.EqualTo(MapRect.xMin).Within(0.001f));
            Assert.That(worldMin.y, Is.EqualTo(MapRect.yMax).Within(0.001f));
            Assert.That(worldMax.x, Is.EqualTo(MapRect.xMax).Within(0.001f));
            Assert.That(worldMax.y, Is.EqualTo(MapRect.yMin).Within(0.001f));
        }

        [Test]
        public void WorldCenterMapsToMapCenter()
        {
            Vector2 point = TDGreenwardMinimapMath.WorldToMap(WorldBounds.center, WorldBounds, MapRect);

            Assert.That(point.x, Is.EqualTo(MapRect.center.x).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(MapRect.center.y).Within(0.001f));
        }

        [Test]
        public void WorldHeightDoesNotAffectMapPosition()
        {
            Vector3 first = new Vector3(5f, -20f, 9f);
            Vector3 second = new Vector3(5f, 70f, 9f);

            Assert.That(TDGreenwardMinimapMath.WorldToMap(first, WorldBounds, MapRect),
                Is.EqualTo(TDGreenwardMinimapMath.WorldToMap(second, WorldBounds, MapRect)));
        }

        [Test]
        public void DegenerateWorldBoundsMapToCenter()
        {
            var bounds = new Bounds(Vector3.one, new Vector3(0f, 10f, 0f));

            Assert.That(TDGreenwardMinimapMath.WorldToMap(Vector3.zero, bounds, MapRect), Is.EqualTo(MapRect.center));
        }
    }
}
