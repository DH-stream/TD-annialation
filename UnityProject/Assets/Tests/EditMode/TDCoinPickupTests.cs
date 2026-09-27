using NUnit.Framework;
using UnityEngine;

namespace TDAnnihilation.Tests
{
    public sealed class TDCoinPickupTests
    {
        [Test]
        public void NearbyDropsUseStackWithoutChangingRewardOrPushingHero()
        {
            var owner = new GameObject("Coin test resource");
            var state = owner.AddComponent<TDResourceState>();
            state.gold = 3;
            TDCoinPickup pickup = null;
            TDCoinPickup distant = null;
            try
            {
                pickup = TDCoinPickup.Spawn(state, 5, Vector3.zero);
                Assert.That(pickup.transform.Find("CoinVisual"), Is.Not.Null);

                TDCoinPickup merged = TDCoinPickup.Spawn(state, 12, Vector3.right);
                Assert.That(merged, Is.SameAs(pickup));
                Assert.That(pickup.Value, Is.EqualTo(17));
                Assert.That(pickup.transform.Find("CoinStackVisual"), Is.Not.Null);
                Assert.That(pickup.GetComponentsInChildren<Collider>(true), Is.Empty);

                distant = TDCoinPickup.Spawn(state, 10, Vector3.right * 4f);
                Assert.That(distant, Is.Not.SameAs(pickup));
                Assert.That(distant.transform.Find("CoinVisual"), Is.Not.Null);

                pickup.Collect();
                pickup.Collect();
                distant.Collect();
                Assert.That(state.gold, Is.EqualTo(30));
            }
            finally
            {
                if (pickup != null) Object.DestroyImmediate(pickup.gameObject);
                if (distant != null) Object.DestroyImmediate(distant.gameObject);
                Object.DestroyImmediate(owner);
            }
        }
    }
}
