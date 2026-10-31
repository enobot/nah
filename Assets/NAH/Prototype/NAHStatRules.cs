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

// Stats the host computes for one fighter. Clients never receive these numbers, only the health they produce.
public struct NAHFighterStats
{
    public float MaxHealth;
    // Fraction of each incoming hit that is removed, from 0 to NAHStatRules.AbsorptionCap.
    public float Absorption;
    // Meant for blocking. Blocking is not built yet, so this has no effect today.
    public float Shielding;
    // Damage of one hit before the target's absorption.
    public float AttackDamage;
}

// Turns a body shape and a weapon into stats, and stats into damage (GAME_DESIGN.md, "Body, weapon size, and stats").
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

    [Header("Smallest weapon (size 0) to biggest weapon (size 1)")]
    [SerializeField, Min(0f)] float minWeaponDamageScale = 0.7f;
    [SerializeField, Min(0f)] float maxWeaponDamageScale = 1.3f;

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
        if (!body.IsValid)
            throw new ArgumentException("Body shape is out of range: " + body, nameof(body));
        if (weapon == null)
            throw new ArgumentNullException(nameof(weapon));

        float bulk = Bulk(body);
        float weaponScale = Mathf.Lerp(minWeaponDamageScale, maxWeaponDamageScale, Mathf.Clamp01(weapon.Size));
        float bodyScale = Mathf.Lerp(minBodyDamageScale, maxBodyDamageScale, bulk);

        return new NAHFighterStats
        {
            MaxHealth = Mathf.Lerp(minHealth, maxHealth, bulk),
            Absorption = Mathf.Clamp(Mathf.Lerp(minAbsorption, maxAbsorption, bulk), 0f, AbsorptionCap),
            Shielding = Mathf.Lerp(minShielding, maxShielding, bulk) + (carriesShield ? shieldItemShielding : 0f),
            AttackDamage = Mathf.Max(0f, weapon.BaseDamage) * weaponScale * bodyScale
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
