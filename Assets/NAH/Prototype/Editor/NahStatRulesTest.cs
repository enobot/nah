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
    public void HeavierWeaponHitsHarderAndSwingsSlower()
    {
        SetWeapon("weight", 0f);
        var light = rules.Compute(NAHBodyShape.Average, weapon, false);
        SetWeapon("weight", 1f);
        var heavy = rules.Compute(NAHBodyShape.Average, weapon, false);

        Assert.Greater(heavy.AttackDamage, light.AttackDamage);
        Assert.Greater(heavy.SwingInterval, light.SwingInterval);
        Assert.AreEqual(light.Reach, heavy.Reach, 0.0001f, "Weight alone should not change reach.");
    }

    [Test]
    public void LongerWeaponReachesFartherAndSwingsSlower()
    {
        SetWeapon("length", 0f);
        var shortWeapon = rules.Compute(NAHBodyShape.Average, weapon, false);
        SetWeapon("length", 1f);
        var longWeapon = rules.Compute(NAHBodyShape.Average, weapon, false);

        Assert.Greater(longWeapon.Reach, shortWeapon.Reach);
        Assert.Greater(longWeapon.SwingInterval, shortWeapon.SwingInterval);
        Assert.AreEqual(shortWeapon.AttackDamage, longWeapon.AttackDamage, 0.0001f, "Length alone should not change damage.");
    }

    [Test]
    public void BiggerBodyMovesSlowerHitsHarderAndIsABiggerTarget()
    {
        var small = rules.Compute(new NAHBodyShape(0f, 0f, 0f), weapon, false);
        var big = rules.Compute(new NAHBodyShape(1f, 1f, 1f), weapon, false);

        Assert.Less(big.MoveSpeedScale, small.MoveSpeedScale);
        Assert.Greater(big.AttackDamage, small.AttackDamage);
        Assert.Greater(big.BodyHeight, small.BodyHeight);
        Assert.Greater(big.BodyWidth, small.BodyWidth);
    }

    [Test]
    public void TallerBodyIsTallerAndFatterBodyIsWider()
    {
        var tall = rules.Compute(new NAHBodyShape(1f, 0.5f, 0.5f), weapon, false);
        var wide = rules.Compute(new NAHBodyShape(0.5f, 1f, 1f), weapon, false);
        var average = rules.Compute(NAHBodyShape.Average, weapon, false);

        Assert.Greater(tall.BodyHeight, average.BodyHeight);
        Assert.AreEqual(average.BodyWidth, tall.BodyWidth, 0.0001f, "Height alone should not widen the hitbox.");
        Assert.Greater(wide.BodyWidth, average.BodyWidth);
        Assert.AreEqual(average.BodyHeight, wide.BodyHeight, 0.0001f, "Fatness and roundness alone should not make the hitbox taller.");
    }

    [Test]
    public void RaceAndClassAreRatiosOnTopOfTheBody()
    {
        var plain = rules.Compute(NAHBodyShape.Average, weapon, false);
        var race = NAHStatModifiers.None;
        race.health = 2f;
        race.moveSpeed = 1.2f;
        var fighterClass = NAHStatModifiers.None;
        fighterClass.damage = 1.5f;
        fighterClass.swingSpeed = 2f;
        fighterClass.armor = 0.5f;

        var both = rules.Compute(NAHBodyShape.Average, weapon, race, fighterClass, false);

        Assert.AreEqual(plain.MaxHealth * 2f, both.MaxHealth, 0.001f);
        Assert.AreEqual(plain.MoveSpeedScale * 1.2f, both.MoveSpeedScale, 0.001f);
        Assert.AreEqual(plain.AttackDamage * 1.5f, both.AttackDamage, 0.001f);
        Assert.AreEqual(plain.SwingInterval / 2f, both.SwingInterval, 0.001f);
        Assert.AreEqual(plain.Absorption * 0.5f, both.Absorption, 0.001f);
        Assert.AreEqual(plain.BodyHeight, both.BodyHeight, 0.0001f, "Race and class ratios should not resize the hitbox.");
    }

    [Test]
    public void BrokenRaceOrClassDataIsRejected()
    {
        var zero = NAHStatModifiers.None;
        zero.health = 0f;
        var notANumber = NAHStatModifiers.None;
        notANumber.swingSpeed = float.NaN;

        Assert.Throws<ArgumentException>(() => rules.Compute(NAHBodyShape.Average, weapon, zero, NAHStatModifiers.None, false));
        Assert.Throws<ArgumentException>(() => rules.Compute(NAHBodyShape.Average, weapon, NAHStatModifiers.None, notANumber, false));
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
        // The approved heavy walk, the old 0.45 s swing, the old 1.5 m reach, and the old capsule.
        Assert.AreEqual(1f, average.MoveSpeedScale, 0.001f);
        Assert.AreEqual(0.45f, average.SwingInterval, 0.001f);
        Assert.AreEqual(1.5f, average.Reach, 0.001f);
        Assert.AreEqual(1f, average.BodyWidth, 0.001f);
        Assert.AreEqual(1f, average.BodyHeight, 0.001f);
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

    void SetWeapon(string property, float value)
    {
        var serialized = new SerializedObject(weapon);
        serialized.FindProperty(property).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
