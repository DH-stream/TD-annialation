using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TDAnnihilation.Tests
{
    public sealed class TDAttackKeyBindingsTests
    {
        private const string PreferencesKey = "TDAnnihilation.AttackKeyBindings";

        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteKey(PreferencesKey);

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(PreferencesKey);

        [Test]
        public void DefaultsMatchCurrentAttackKeys()
        {
            TDAttackKeyBindings bindings = TDAttackKeyBindings.Load();

            Assert.That(bindings.GetKey(TDAttackType.Light), Is.EqualTo(Key.F));
            Assert.That(bindings.GetKey(TDAttackType.Heavy), Is.EqualTo(Key.Q));
            Assert.That(bindings.GetKey(TDAttackType.Mega), Is.EqualTo(Key.E));
        }

        [Test]
        public void RebindingPersistsAcrossLoad()
        {
            TDAttackKeyBindings bindings = TDAttackKeyBindings.Load();

            Assert.That(bindings.TrySetKey(TDAttackType.Light, Key.R), Is.True);
            bindings.Save();

            Assert.That(TDAttackKeyBindings.Load().GetKey(TDAttackType.Light), Is.EqualTo(Key.R));
        }

        [Test]
        public void AttackActionsCannotShareAKey()
        {
            TDAttackKeyBindings bindings = TDAttackKeyBindings.Load();

            Assert.That(bindings.TrySetKey(TDAttackType.Light, Key.Q), Is.False);
            Assert.That(bindings.GetKey(TDAttackType.Light), Is.EqualTo(Key.F));
        }

        [TestCase(Key.None)]
        [TestCase(Key.Escape)]
        public void ReservedOrEmptyKeysAreRejected(Key key)
        {
            Assert.That(TDAttackKeyBindings.Load().TrySetKey(TDAttackType.Light, key), Is.False);
        }

        [Test]
        public void InvalidStoredBindingsFallBackToDefaults()
        {
            PlayerPrefs.SetString(PreferencesKey, "not-a-key-set");

            TDAttackKeyBindings bindings = TDAttackKeyBindings.Load();

            Assert.That(bindings.GetKey(TDAttackType.Light), Is.EqualTo(Key.F));
            Assert.That(bindings.GetKey(TDAttackType.Heavy), Is.EqualTo(Key.Q));
            Assert.That(bindings.GetKey(TDAttackType.Mega), Is.EqualTo(Key.E));
        }
    }
}
