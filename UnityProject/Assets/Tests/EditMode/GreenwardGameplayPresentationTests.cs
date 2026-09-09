using NUnit.Framework;
using UnityEngine;

namespace TDAnnihilation.Tests
{
    public sealed class GreenwardGameplayPresentationTests
    {
        [Test]
        public void GroundHeightHasSmallScaleImperfection()
        {
            float origin = GreenwardWorldLayout.HeightAt(2f, 2f);
            float nearby = GreenwardWorldLayout.HeightAt(3.25f, 2.75f);
            Assert.That(Mathf.Abs(origin - nearby), Is.GreaterThan(0.01f));
            Assert.That(Mathf.Abs(origin - nearby), Is.LessThan(0.75f));
        }

        [Test]
        public void EliteEnemiesAreLargerAndTougherThanRaiders()
        {
            Assert.That(TDEnemyArchetype.Elite.Scale, Is.GreaterThan(TDEnemyArchetype.Raider.Scale));
            Assert.That(TDEnemyArchetype.Elite.HealthMultiplier, Is.GreaterThan(1f));
            Assert.That(TDEnemyArchetype.Raider.Scale, Is.LessThan(1f));
        }

        [Test]
        public void CameraPanInputProducesHorizontalMovement()
        {
            Vector3 movement = TDStrategicCamera.CalculatePan(new Vector2(1f, -0.5f), 10f, 0.25f);
            Assert.That(movement.y, Is.EqualTo(0f));
            Assert.That(movement.sqrMagnitude, Is.GreaterThan(0f));
        }
    }
}
