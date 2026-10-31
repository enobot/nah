using System;
using UnityEngine;

// Body shape sliders, each from 0 to 1, where 0.5 is an average body.
// These are gameplay values: they set stats. The visible mesh does not.
public struct NAHBodyShape
{
    public float size;
    public float fatness;
    public float roundness;

    public NAHBodyShape(float size, float fatness, float roundness)
    {
        this.size = size;
        this.fatness = fatness;
        this.roundness = roundness;
    }

    public static NAHBodyShape Average => new NAHBodyShape(0.5f, 0.5f, 0.5f);

    // A client can send anything. Only finite values from 0 to 1 are accepted.
    public bool IsValid => InRange(size) && InRange(fatness) && InRange(roundness);

    static bool InRange(float value)
    {
        return float.IsFinite(value) && value >= 0f && value <= 1f;
    }

    public override string ToString()
    {
        return "size " + size.ToString("0.00") + ", fatness " + fatness.ToString("0.00") + ", roundness " + roundness.ToString("0.00");
    }
}

// Ratios a race or class applies on top of the body and weapon. 1 means no change, 1.2 means 20 percent more.
// swingSpeed above 1 makes swings faster.
[Serializable]
public struct NAHStatModifiers
{
    public float health;
    public float armor;
    public float damage;
    public float moveSpeed;
    public float swingSpeed;

    public static NAHStatModifiers None => new NAHStatModifiers
    {
        health = 1f,
        armor = 1f,
        damage = 1f,
        moveSpeed = 1f,
        swingSpeed = 1f
    };

    public bool IsValid => Positive(health) && Positive(armor) && Positive(damage) && Positive(moveSpeed) && Positive(swingSpeed);

    static bool Positive(float value)
    {
        return float.IsFinite(value) && value > 0f;
    }
}

// Stats the host computes for one fighter. Clients receive only the health and body size these produce.
public struct NAHFighterStats
{
    public float MaxHealth;
    // Fraction of each incoming hit that armor removes, from 0 to NAHStatRules.AbsorptionCap.
    public float Absorption;
    // Meant for blocking. Blocking is not built yet, so this has no effect today.
    public float Shielding;
    // Damage of one hit before the target's absorption.
    public float AttackDamage;
    // Multiplies walk and sprint speed. 1 is the approved heavy walk.
    public float MoveSpeedScale;
    // Seconds between swings.
    public float SwingInterval;
    // How far in front of the attacker a swing reaches, in meters.
    public float Reach;
    // Hitbox and placeholder capsule size, where 1 is the average body.
    public float BodyWidth;
    public float BodyHeight;
}

// Turns a body, a weapon, a race, and a class into stats, and stats into damage
// (GAME_DESIGN.md, "Body, weapon size, and stats"). Bigger bodies are tougher, hit harder, move slower, and are bigger targets.
// Heavier weapons hit harder and swing slower. Longer weapons reach farther and swing slower.
// Every number below is a PLACEHOLDER until the owner chooses the formulas. Tune them in the asset, not in code.
[CreateAssetMenu(menuName = "NAH/Stat Rules (placeholder)")]
public sealed class NAHStatRules : ScriptableObject
{
    // Absorption never reaches 100 percent, so every hit still does some damage.
    public const float AbsorptionCap = 0.9f;

    [Header("How much each body slider counts toward bulk")]
    [SerializeField, Min(0f)] float sizeWeight = 1f;
    [SerializeField, Min(0f)] float fatnessWeight = 1f;
    [SerializeField, Min(0f)] float roundnessWeight = 1f;

    [Header("Smallest body (bulk 0) to biggest body (bulk 1)")]
    [SerializeField, Min(1f)] float minHealth = 60f;
    [SerializeField, Min(1f)] float maxHealth = 140f;
    [SerializeField, Range(0f, AbsorptionCap)] float minAbsorption = 0f;
    [SerializeField, Range(0f, AbsorptionCap)] float maxAbsorption = 0.3f;
    [SerializeField, Min(0f)] float minShielding = 0f;
    [SerializeField, Min(0f)] float maxShielding = 30f;
    [SerializeField, Min(0f)] float minBodyDamageScale = 0.85f;
    [SerializeField, Min(0f)] float maxBodyDamageScale = 1.15f;
    [SerializeField, Min(0.05f)] float smallBodySpeedScale = 1.25f;
    [SerializeField, Min(0.05f)] float bigBodySpeedScale = 0.75f;

    [Header("Hitbox size: height follows size, width follows fatness and roundness")]
    [SerializeField, Min(0.3f)] float minBodyHeightScale = 0.7f;
    [SerializeField, Min(0.3f)] float maxBodyHeightScale = 1.3f;
    [SerializeField, Min(0.3f)] float minBodyWidthScale = 0.7f;
    [SerializeField, Min(0.3f)] float maxBodyWidthScale = 1.3f;
    [SerializeField, Min(0.1f)] float minBodyReachScale = 0.9f;
    [SerializeField, Min(0.1f)] float maxBodyReachScale = 1.1f;

    [Header("Lightest weapon (weight 0) to heaviest (weight 1)")]
    [SerializeField, Min(0f)] float minWeaponDamageScale = 0.7f;
    [SerializeField, Min(0f)] float maxWeaponDamageScale = 1.3f;
    [SerializeField, Min(0.05f)] float lightSwingScale = 0.7f;
    [SerializeField, Min(0.05f)] float heavySwingScale = 1.3f;

    [Header("Shortest weapon (length 0) to longest (length 1)")]
    [SerializeField, Min(0.1f)] float minWeaponReach = 1f;
    [SerializeField, Min(0.1f)] float maxWeaponReach = 2f;
    [SerializeField, Min(0.05f)] float shortSwingScale = 0.85f;
    [SerializeField, Min(0.05f)] float longSwingScale = 1.15f;

    [Header("Swing time of an average weapon")]
    [SerializeField, Min(0.05f)] float baseSwingSeconds = 0.45f;

    [Header("Equipment")]
    [SerializeField, Min(0f)] float shieldItemShielding = 25f;

    // One number for how big a body is overall: 0 for the smallest, thinnest, least round body, 1 for the biggest.
    public float Bulk(NAHBodyShape body)
    {
        float total = sizeWeight + fatnessWeight + roundnessWeight;
        if (total <= 0f)
            return 0.5f;
        return Mathf.Clamp01((body.size * sizeWeight + body.fatness * fatnessWeight + body.roundness * roundnessWeight) / total);
    }

    public NAHFighterStats Compute(NAHBodyShape body, NAHWeaponDefinition weapon, bool carriesShield)
    {
        return Compute(body, weapon, NAHStatModifiers.None, NAHStatModifiers.None, carriesShield);
    }

    public NAHFighterStats Compute(NAHBodyShape body, NAHWeaponDefinition weapon, NAHStatModifiers race, NAHStatModifiers fighterClass, bool carriesShield)
    {
        if (!body.IsValid)
            throw new ArgumentException("Body shape is out of range: " + body, nameof(body));
        if (weapon == null)
            throw new ArgumentNullException(nameof(weapon));
        if (!race.IsValid)
            throw new ArgumentException("Race modifiers must be finite and above zero.", nameof(race));
        if (!fighterClass.IsValid)
            throw new ArgumentException("Class modifiers must be finite and above zero.", nameof(fighterClass));

        float bulk = Bulk(body);
        float weight = Mathf.Clamp01(weapon.Weight);
        float length = Mathf.Clamp01(weapon.Length);

        float damage = Mathf.Max(0f, weapon.BaseDamage)
            * Mathf.Lerp(minWeaponDamageScale, maxWeaponDamageScale, weight)
            * Mathf.Lerp(minBodyDamageScale, maxBodyDamageScale, bulk);
        float swing = baseSwingSeconds
            * Mathf.Lerp(lightSwingScale, heavySwingScale, weight)
            * Mathf.Lerp(shortSwingScale, longSwingScale, length);

        return new NAHFighterStats
        {
            MaxHealth = Mathf.Lerp(minHealth, maxHealth, bulk) * race.health * fighterClass.health,
            Absorption = Mathf.Clamp(Mathf.Lerp(minAbsorption, maxAbsorption, bulk) * race.armor * fighterClass.armor, 0f, AbsorptionCap),
            Shielding = Mathf.Lerp(minShielding, maxShielding, bulk) + (carriesShield ? shieldItemShielding : 0f),
            AttackDamage = damage * race.damage * fighterClass.damage,
            MoveSpeedScale = Mathf.Lerp(smallBodySpeedScale, bigBodySpeedScale, bulk) * race.moveSpeed * fighterClass.moveSpeed,
            SwingInterval = swing / (race.swingSpeed * fighterClass.swingSpeed),
            Reach = Mathf.Lerp(minWeaponReach, maxWeaponReach, length) * Mathf.Lerp(minBodyReachScale, maxBodyReachScale, body.size),
            BodyWidth = Mathf.Lerp(minBodyWidthScale, maxBodyWidthScale, (body.fatness + body.roundness) * 0.5f),
            BodyHeight = Mathf.Lerp(minBodyHeightScale, maxBodyHeightScale, body.size)
        };
    }

    // How much health one hit removes from the target.
    public float DamageTaken(float attackDamage, NAHFighterStats target)
    {
        if (!float.IsFinite(attackDamage) || attackDamage <= 0f)
            return 0f;
        float absorption = float.IsFinite(target.Absorption) ? Mathf.Clamp(target.Absorption, 0f, AbsorptionCap) : 0f;
        return attackDamage * (1f - absorption);
    }
}
