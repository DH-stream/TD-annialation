using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TDAnnihilation.Tests
{
    public sealed class TDProgressionTests
    {
        private const string SaveKey = "TDAnnihilation.SkillTreeProgression.v1";

        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteKey(SaveKey);

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(SaveKey);

        [Test]
        public void FirstMapClearAwardsFirstRewardAndUnlocksEndlessOnlyOnce()
        {
            var state = new TDSkillTreeState();

            Assert.That(state.HasClearedMap("greenward"), Is.False);
            Assert.That(state.IsEndlessUnlocked("greenward"), Is.False);
            Assert.That(state.AwardMapClear("greenward", 10, 3), Is.EqualTo(10));
            Assert.That(state.HasClearedMap("greenward"), Is.True);
            Assert.That(state.IsEndlessUnlocked("greenward"), Is.True);
            Assert.That(state.AwardMapClear("greenward", 10, 3), Is.EqualTo(3));
            Assert.That(state.SkullBalance, Is.EqualTo(13));
        }

        [Test]
        public void LockedEndlessAttemptDoesNotResetRunResources()
        {
            var gameObject = new GameObject("Locked endless test");
            gameObject.SetActive(false);
            try
            {
                TDVerticalSliceBootstrap bootstrap = gameObject.AddComponent<TDVerticalSliceBootstrap>();
                TDResourceState resources = gameObject.AddComponent<TDResourceState>();
                var flow = new TDGameFlow(3);
                SetPrivateField(bootstrap, "state", resources);
                SetPrivateField(bootstrap, "flow", flow);
                SetPrivateField(bootstrap, "progression", new TDSkillTreeState());
                resources.gold = 7;
                resources.lives = 4;

                LogAssert.Expect(LogType.Warning, "Endless is locked until this map has been cleared in Normal mode.");
                bootstrap.StartSoloStages(TDRunMode.Endless);

                Assert.That(resources.gold, Is.EqualTo(7));
                Assert.That(resources.lives, Is.EqualTo(4));
                Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.MainMenu));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EndlessRewardsScaleWithTheClearedWaveNumber()
        {
            var state = new TDSkillTreeState();

            Assert.That(state.AwardEndlessWave(1, 1, 1), Is.EqualTo(1));
            Assert.That(state.AwardEndlessWave(2, 1, 1), Is.EqualTo(2));
            Assert.That(state.SkullBalance, Is.EqualTo(3));
        }

        [Test]
        public void EndlessRewardDoesNotOverflowAtHighWaveNumbers()
        {
            var state = new TDSkillTreeState();

            Assert.That(state.AwardEndlessWave(50000, 1, 100000), Is.EqualTo(int.MaxValue));
            Assert.That(state.SkullBalance, Is.EqualTo(int.MaxValue));
        }

        private static void SetPrivateField<T>(object target, string name, T value)
        {
            var field = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected private field " + name);
            field.SetValue(target, value);
        }

        [Test]
        public void NormalRunWinsAtWaveCapAndCompletesEachWaveOnlyOnce()
        {
            var flow = new TDGameFlow(2);
            flow.SelectSoloStages(TDRunMode.Normal);
            flow.StartWave();
            Assert.That(flow.CompleteWave(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.Build));
            Assert.That(flow.CompleteWave(), Is.False);
            flow.StartWave();
            Assert.That(flow.CompleteWave(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.Victory));
            Assert.That(flow.CompleteWave(), Is.False);
        }

        [Test]
        public void ResumeBuildPhaseStartsAtNextWave()
        {
            var flow = new TDGameFlow(3);
            flow.ResumeBuildPhase(1, TDRunMode.Normal);

            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.Build));
            Assert.That(flow.Wave, Is.EqualTo(1));
            flow.StartWave();
            Assert.That(flow.Wave, Is.EqualTo(2));
            Assert.That(flow.RunMode, Is.EqualTo(TDRunMode.Normal));
        }

        [Test]
        public void EndlessRunContinuesPastNormalCapAndCanBeDefeated()
        {
            var flow = new TDGameFlow(1);
            flow.SelectSoloStages(TDRunMode.Endless);
            for (int i = 0; i < 3; i++)
            {
                flow.StartWave();
                Assert.That(flow.CompleteWave(), Is.True);
                Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.Build));
            }
            flow.StartWave();
            flow.Lose();
            Assert.That(flow.Phase, Is.EqualTo(TDGamePhase.Defeat));
            Assert.That(flow.CompleteWave(), Is.False);
        }

        [Test]
        public void PurchaseRequiresSkullsAndPrerequisiteAndNeverSpendsOnRejection()
        {
            var nodes = TDSkillTreeCatalog.CreateDefault();
            var state = new TDSkillTreeState();

            Assert.That(state.TryPurchase("bastion-foundation", nodes), Is.False);
            Assert.That(state.SkullBalance, Is.Zero);
            state.AwardMapClear("greenward", 10, 3);
            Assert.That(state.TryPurchase("bastion-focus", nodes), Is.False);
            Assert.That(state.SkullBalance, Is.EqualTo(10));
            Assert.That(state.TryPurchase("unknown-node", nodes), Is.False);
            Assert.That(state.TryPurchase("bastion-foundation", nodes), Is.True);
            Assert.That(state.SkullBalance, Is.EqualTo(9));
            Assert.That(state.TryPurchase("bastion-foundation", nodes), Is.False);
            Assert.That(state.SkullBalance, Is.EqualTo(9));
            Assert.That(state.TryPurchase("bastion-focus", nodes), Is.True);
            Assert.That(state.SkullBalance, Is.EqualTo(7));
        }

        [Test]
        public void SkillModifiersAggregateOnlyPurchasedNodeEffects()
        {
            var nodes = TDSkillTreeCatalog.CreateDefault();
            var state = new TDSkillTreeState();
            state.AwardMapClear("greenward", 10, 3);
            state.TryPurchase("bastion-foundation", nodes);
            state.TryPurchase("bastion-focus", nodes);

            TDSkillModifiers modifiers = state.GetModifiers(nodes);

            Assert.That(modifiers.ArcherDamageBonus, Is.EqualTo(0.10f).Within(0.0001f));
            Assert.That(modifiers.ArcherRangeBonus, Is.EqualTo(0.10f).Within(0.0001f));
            Assert.That(modifiers.HeroDamageBonus, Is.Zero);
        }

        [Test]
        public void HeroAppliesPurchasedEffectsFromSerializedBaselinesWithoutCompounding()
        {
            var nodes = TDSkillTreeCatalog.CreateDefault();
            var state = new TDSkillTreeState();
            state.AwardMapClear("greenward", 10, 3);
            state.TryPurchase("attack-force", nodes);
            state.TryPurchase("cooldown-tempo", nodes);
            state.TryPurchase("range-sight", nodes);
            state.TryPurchase("elemental-ember", nodes);
            var heroObject = new GameObject("Progression test hero");
            try
            {
                TDHeroController hero = heroObject.AddComponent<TDHeroController>();
                TDSkillModifiers modifiers = state.GetModifiers(nodes);
                hero.ApplySkillModifiers(modifiers);
                hero.ApplySkillModifiers(modifiers);

                Assert.That(hero.EffectiveAttackDamage, Is.EqualTo(26.4f).Within(0.001f));
                Assert.That(hero.AttackCooldownDuration(TDAttackType.Light), Is.EqualTo(0.495f).Within(0.001f));
                Assert.That(hero.EffectiveAttackRange, Is.EqualTo(2.42f).Within(0.001f));
                Assert.That(hero.EffectiveMegaRadius, Is.EqualTo(8.25f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(heroObject);
            }
        }

        [Test]
        public void TowerModifiersAffectArcherAndArcaneCombatStatsFromBaselines()
        {
            var nodes = TDSkillTreeCatalog.CreateDefault();
            var state = new TDSkillTreeState();
            state.AwardMapClear("greenward", 10, 3);
            state.TryPurchase("bastion-foundation", nodes);
            state.TryPurchase("bastion-focus", nodes);
            state.TryPurchase("arcana-spark", nodes);
            state.TryPurchase("arcana-echo", nodes);
            TDSkillModifiers modifiers = state.GetModifiers(nodes);
            var towerObject = new GameObject("Progression test tower");
            try
            {
                TDTowerController tower = towerObject.AddComponent<TDTowerController>();
                tower.ApplySkillModifiers(modifiers, true);
                tower.ApplySkillModifiers(modifiers, true);

                Assert.That(tower.EffectiveRange, Is.EqualTo(8.8f).Within(0.001f));
                Assert.That(tower.EffectiveDamage, Is.EqualTo(17.6f).Within(0.001f));
                tower.ApplySkillModifiers(modifiers, false);
                Assert.That(tower.EffectiveRange, Is.EqualTo(8f).Within(0.001f));
                Assert.That(tower.EffectiveFireInterval, Is.EqualTo(0.99f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(towerObject);
            }
        }

        [Test]
        public void SaveLoadRoundTripPreservesWalletUnlocksAndMapProgress()
        {
            var nodes = TDSkillTreeCatalog.CreateDefault();
            var state = new TDSkillTreeState();
            state.AwardMapClear("greenward", 10, 3);
            state.TryPurchase("bastion-foundation", nodes);
            state.Save();

            TDSkillTreeState loaded = TDSkillTreeState.Load();

            Assert.That(loaded.SkullBalance, Is.EqualTo(9));
            Assert.That(loaded.IsUnlocked("bastion-foundation"), Is.True);
            Assert.That(loaded.HasClearedMap("greenward"), Is.True);
            Assert.That(loaded.IsEndlessUnlocked("greenward"), Is.True);
        }

        [Test]
        public void MissingOrCorruptSaveLoadsSafeDefaults()
        {
            Assert.That(TDSkillTreeState.Load().SkullBalance, Is.Zero);
            PlayerPrefs.SetString(SaveKey, "not json");

            TDSkillTreeState loaded = TDSkillTreeState.Load();

            Assert.That(loaded.SkullBalance, Is.Zero);
            Assert.That(loaded.IsUnlocked("bastion-foundation"), Is.False);
            Assert.That(loaded.HasClearedMap("greenward"), Is.False);
        }
    }
}
