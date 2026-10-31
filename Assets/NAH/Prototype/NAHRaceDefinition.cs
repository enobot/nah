using UnityEngine;

// A race multiplies the stats a body already gives. No races are approved yet.
// Proposals are in GAME_DESIGN.md. Any asset made from this is a PLACEHOLDER until the owner approves it.
[CreateAssetMenu(menuName = "NAH/Race (placeholder)")]
public sealed class NAHRaceDefinition : ScriptableObject
{
    [SerializeField] NAHStatModifiers modifiers = NAHStatModifiers.None;

    public NAHStatModifiers Modifiers => modifiers;
}
