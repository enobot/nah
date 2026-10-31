using UnityEngine;

// A class multiplies the stats a body already gives, on top of the race. No class kits are approved yet.
// Proposals are in GAME_DESIGN.md. Any asset made from this is a PLACEHOLDER until the owner approves it.
[CreateAssetMenu(menuName = "NAH/Class (placeholder)")]
public sealed class NAHClassDefinition : ScriptableObject
{
    [SerializeField] NAHStatModifiers modifiers = NAHStatModifiers.None;

    public NAHStatModifiers Modifiers => modifiers;
}
