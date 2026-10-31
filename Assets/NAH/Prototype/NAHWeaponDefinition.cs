using UnityEngine;

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

// One weapon's gameplay data. Its size is a gameplay value, not the size of its mesh,
// so a bigger-looking cosmetic weapon cannot raise damage by itself.
// The values are PLACEHOLDERS until the owner chooses them.
[CreateAssetMenu(menuName = "NAH/Weapon (placeholder)")]
public sealed class NAHWeaponDefinition : ScriptableObject
{
    [SerializeField] NAHWeaponCategory category = NAHWeaponCategory.Sword;
    [SerializeField, Range(0f, 1f)] float size = 0.5f;
    [SerializeField, Min(0f)] float baseDamage = 34f;

    public NAHWeaponCategory Category => category;
    public float Size => size;
    public float BaseDamage => baseDamage;
}
