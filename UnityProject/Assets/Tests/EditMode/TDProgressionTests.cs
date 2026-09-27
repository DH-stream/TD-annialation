using NUnit.Framework;
using UnityEngine;

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
        public void EndlessRewardsScaleWithTheClearedWaveNumber()
        {
            var state = new TDSkillTreeState();

            Assert.That(state.AwardEndlessWave(1, 1, 1), Is.EqualTo(1));
            Assert.That(state.AwardEndlessWave(2, 1, 1), Is.EqualTo(2));
            Assert.That(state.SkullBalance, Is.EqualTo(3));
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
