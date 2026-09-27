using NUnit.Framework;
using UnityEngine;

namespace TDAnnihilation.Tests
{
    public sealed class TDArcherTowerPrefabTests
    {
        [Test]
        public void ArcherTowerCombinesStaticBaseAndAimableBoltAssembly()
        {
            GameObject prefab = Resources.Load<GameObject>("TDAnnihilation/ArcherTower");
            Assert.That(prefab, Is.Not.Null);

            Transform baseBody = prefab.transform.Find("StaticBody");
            Transform pivot = prefab.transform.Find("ArcherTowerPivot");
            Transform bolt = pivot == null ? null : pivot.Find("ArcherMechanism/ArcherTowerPivot/DrawBolt");

            Assert.That(baseBody, Is.Not.Null);
            Assert.That(baseBody.GetComponent<MeshRenderer>().sharedMaterial.name, Does.Contain("Base"));
            Assert.That(pivot, Is.Not.Null);
            Assert.That(pivot.GetComponent<TDArcherTowerDrawAnimation>(), Is.Not.Null);
            Assert.That(bolt, Is.Not.Null);
            Assert.That(pivot.Find("ShotOrigin"), Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                Bounds baseBounds = instance.transform.Find("StaticBody").GetComponent<Renderer>().bounds;
                Bounds bowBounds = instance.transform.Find("ArcherTowerPivot/ArcherMechanism/ArcherTowerPivot/ArcherBody").GetComponent<Renderer>().bounds;
                float baseWidth = Mathf.Max(baseBounds.size.x, baseBounds.size.z);
                float bowWidth = Mathf.Max(bowBounds.size.x, bowBounds.size.z);
                Assert.That(bowWidth / baseWidth, Is.InRange(0.5f, 0.7f));
                Assert.That(baseBounds.size.y / baseWidth, Is.LessThan(1.65f), "Tower body should stay compact.");
                Assert.That(pivot.localPosition.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(pivot.localPosition.z, Is.EqualTo(0f).Within(0.001f));

                MeshCollider platform = instance.transform.Find("StaticBody").gameObject.AddComponent<MeshCollider>();
                platform.sharedMesh = instance.transform.Find("StaticBody").GetComponent<MeshFilter>().sharedMesh;
                Assert.That(platform.Raycast(new Ray(new Vector3(0.1f, 3f, 0f), Vector3.down), out RaycastHit hit, 4f), Is.True);
                Transform pedestal = pivot.Find("BallistaPedestal");
                Assert.That(pedestal, Is.Not.Null);
                Bounds pedestalBounds = pedestal.GetComponent<Renderer>().bounds;
                Assert.That(pedestalBounds.min.y - hit.point.y, Is.InRange(-0.02f, 0.03f), "Pedestal should rest on the platform.");
                Assert.That(bowBounds.min.y - pedestalBounds.max.y, Is.InRange(-0.02f, 0.03f), "Ballista should rest on the pedestal.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
