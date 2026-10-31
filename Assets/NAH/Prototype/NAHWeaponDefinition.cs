using UnityEngine;
using UnityEngine.Serialization;

// Weapon categories confirmed in GAME_DESIGN.md, "Weapon styles".
public enum NAHWeaponCategory
{
    Unarmed,
    Sword,
    Greatsword,
    Dagger,
    Spear,
    Hammer,
    Axe,
    Staff
}

// One weapon's gameplay data. Weight and length are gameplay values, not the size of its mesh,
// so a bigger-looking cosmetic weapon cannot hit harder or reach farther by itself.
// The values are PLACEHOLDERS until the owner chooses them.
[CreateAssetMenu(menuName = "NAH/Weapon (placeholder)")]
public sealed class NAHWeaponDefinition : ScriptableObject
{
    [SerializeField] NAHWeaponCategory category = NAHWeaponCategory.Sword;
    [Tooltip("0 light to 1 heavy. Heavier hits harder and swings slower.")]
    [SerializeField, Range(0f, 1f), FormerlySerializedAs("size")] float weight = 0.5f;
    [Tooltip("0 short to 1 long. Longer reaches farther and swings slower.")]
    [SerializeField, Range(0f, 1f)] float length = 0.5f;
    [SerializeField, Min(0f)] float baseDamage = 34f;

    public NAHWeaponCategory Category => category;
    public float Weight => weight;
    public float Length => length;
    public float BaseDamage => baseDamage;
}
