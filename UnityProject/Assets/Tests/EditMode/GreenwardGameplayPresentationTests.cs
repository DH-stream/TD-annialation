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

        [Test]
        public void GameFlowTraversesMenuBuildWaveVictoryAndBack()
        {
            var flow = new TDGameFlow(1);
            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.MainMenu));

            flow.SelectSoloStages();
            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.Build));

            flow.StartWave();
            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.Wave));

            flow.CompleteWave();
            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.Victory));

            flow.ReturnToMenu();
            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.MainMenu));
        }

        [Test]
        public void HeroControllerRequiresPhysicalCollision()
        {
            var hero = new GameObject("Collision test hero");
            try
            {
                hero.AddComponent<TDHeroController>();
                Assert.That(hero.GetComponent<CharacterController>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(hero);
            }
        }

        [Test]
        public void GameplayCharactersPreserveWorldScale()
        {
            Assert.That(TDPresentationScale.Hero, Is.LessThanOrEqualTo(0.9f));
            Assert.That(TDEnemyArchetype.Raider.Scale, Is.LessThan(TDPresentationScale.Hero));
        }

        [Test]
        public void RoadsideDecorationsRemainOutsideEnemyClearance()
        {
            Assert.That(GreenwardGroundBuilder.ClampRoadsideOffset(0.4f), Is.GreaterThanOrEqualTo(2.6f));
            Assert.That(GreenwardGroundBuilder.ClampRoadsideOffset(-1.2f), Is.LessThanOrEqualTo(-2.6f));
        }
    }
}
