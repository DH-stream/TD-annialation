using NUnit.Framework;
using TDAnnihilation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class TDHeroEquipmentTests
{
    [TestCase(TDHeroEquipment.Unarmed, "UnarmedAttack", "PunchRight")]
    [TestCase(TDHeroEquipment.Melee, "MeleeAttack", "Stable Sword Outward Slash")]
    [TestCase(TDHeroEquipment.Staff, "StaffAttack", "Standing 1H Magic Attack 01")]
    public void EquipmentHasAnimationAndKeepsAttacksResponsive(TDHeroEquipment equipment, string stateName, string clipName)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Main Char/MainCharaniim.controller");
        Assert.That(controller, Is.Not.Null);
        var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/TDAnnihilation/GreenwardHero.prefab");
        Assert.That(heroPrefab.GetComponent<Animator>().runtimeAnimatorController, Is.EqualTo(controller));
        var state = System.Array.Find(controller.layers[0].stateMachine.states, child => child.state.name == stateName).state;
        Assert.That(state, Is.Not.Null);
        Assert.That(state.motion.name, Is.EqualTo(clipName));

        var heroObject = new GameObject("Equipment test hero");
        try
        {
            var hero = heroObject.AddComponent<TDHeroController>();
            hero.Equip(equipment);
            Assert.That(hero.Equipment, Is.EqualTo(equipment));
            Assert.That(hero.TryAttack(TDAttackType.Light), Is.True);
            Assert.That(hero.TryAttack(TDAttackType.Light), Is.False);
            Assert.That(hero.AttackCooldownRemaining(TDAttackType.Light), Is.GreaterThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(heroObject);
        }
    }

    [Test]
    public void InvalidEquipmentDoesNotReplaceCurrentEquipment()
    {
        var heroObject = new GameObject("Equipment validation test hero");
        try
        {
            var hero = heroObject.AddComponent<TDHeroController>();
            hero.Equip(TDHeroEquipment.Melee);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => hero.Equip((TDHeroEquipment)99));
            Assert.That(hero.Equipment, Is.EqualTo(TDHeroEquipment.Melee));
        }
        finally
        {
            Object.DestroyImmediate(heroObject);
        }
    }
}
