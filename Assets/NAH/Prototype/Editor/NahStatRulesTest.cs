using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Checks the shape of the stat rules, not the placeholder numbers themselves.
public sealed class NahStatRulesTest
{
    NAHStatRules rules;
    NAHWeaponDefinition weapon;

    [SetUp]
    public void SetUp()
    {
        rules = ScriptableObject.CreateInstance<NAHStatRules>();
        weapon = ScriptableObject.CreateInstance<NAHWeaponDefinition>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(rules);
        Object.DestroyImmediate(weapon);
    }

    [Test]
    public void BiggerFatterRounderBodyHasMoreHealthAbsorptionAndShielding()
    {
        var small = rules.Compute(new NAHBodyShape(0f, 0f, 0f), weapon, false);
        var average = rules.Compute(NAHBodyShape.Average, weapon, false);
        var big = rules.Compute(new NAHBodyShape(1f, 1f, 1f), weapon, false);

        Assert.Less(small.MaxHealth, average.MaxHealth);
        Assert.Less(average.MaxHealth, big.MaxHealth);
        Assert.Less(small.Absorption, big.Absorption);
        Assert.Less(small.Shielding, big.Shielding);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void EachBodySliderRaisesHealthOnItsOwn(int slider)
    {
        var baseline = rules.Compute(NAHBodyShape.Average, weapon, false);
        var raised = NAHBodyShape.Average;
        if (slider == 0)
            raised.size = 1f;
        else if (slider == 1)
            raised.fatness = 1f;
        else
            raised.roundness = 1f;

        Assert.Greater(rules.Compute(raised, weapon, false).MaxHealth, baseline.MaxHealth);
    }

    [Test]
    public void BiggerWeaponDealsMoreDamage()
    {
        SetWeaponSize(0f);
        float smallWeapon = rules.Compute(NAHBodyShape.Average, weapon, false).AttackDamage;
        SetWeaponSize(1f);
        float bigWeapon = rules.Compute(NAHBodyShape.Average, weapon, false).AttackDamage;

        Assert.Greater(bigWeapon, smallWeapon);
    }

    [Test]
    public void ShieldAddsShieldingAndNothingElse()
    {
        var without = rules.Compute(NAHBodyShape.Average, weapon, false);
        var with = rules.Compute(NAHBodyShape.Average, weapon, true);

        Assert.Greater(with.Shielding, without.Shielding);
        Assert.AreEqual(without.MaxHealth, with.MaxHealth);
        Assert.AreEqual(without.Absorption, with.Absorption);
        Assert.AreEqual(without.AttackDamage, with.AttackDamage);
    }

    [Test]
    public void AbsorptionReducesDamageButNeverRemovesAllOfIt()
    {
        var target = new NAHFighterStats { Absorption = 0.25f };
        Assert.AreEqual(30f, rules.DamageTaken(40f, target), 0.0001f);

        target.Absorption = 5f;
        Assert.Greater(rules.DamageTaken(40f, target), 0f, "Absorption above the cap blocked the whole hit.");

        Assert.AreEqual(0f, rules.DamageTaken(float.NaN, target));
        Assert.AreEqual(0f, rules.DamageTaken(-10f, target));
    }

    [Test]
    public void AverageBodyWithThePlaceholderWeaponKeepsTheOldPrototypeNumbers()
    {
        // The prototype used 100 health and 34 damage before stats existed. The placeholder defaults keep those for an average body.
        var average = rules.Compute(NAHBodyShape.Average, weapon, false);
        Assert.AreEqual(100f, average.MaxHealth, 0.001f);
        Assert.AreEqual(34f, average.AttackDamage, 0.001f);
    }

    [Test]
    public void OutOfRangeBodiesAreRejected()
    {
        Assert.IsTrue(new NAHBodyShape(0f, 0f, 0f).IsValid);
        Assert.IsTrue(new NAHBodyShape(1f, 1f, 1f).IsValid);
        Assert.IsFalse(new NAHBodyShape(float.NaN, 0.5f, 0.5f).IsValid);
        Assert.IsFalse(new NAHBodyShape(0.5f, float.PositiveInfinity, 0.5f).IsValid);
        Assert.IsFalse(new NAHBodyShape(0.5f, 0.5f, -0.01f).IsValid);
        Assert.IsFalse(new NAHBodyShape(1.01f, 0.5f, 0.5f).IsValid);
        Assert.Throws<ArgumentException>(() => rules.Compute(new NAHBodyShape(2f, 0f, 0f), weapon, false));
    }

    void SetWeaponSize(float size)
    {
        var serialized = new SerializedObject(weapon);
        serialized.FindProperty("size").floatValue = size;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
