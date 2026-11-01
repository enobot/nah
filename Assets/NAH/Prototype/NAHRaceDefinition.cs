using UnityEngine;

// A race multiplies the stats a body already gives. The races in Roster/ are PLACEHOLDERS:
// the owner will choose the real races, names, and numbers (GAME_DESIGN.md, "Proposed races").
[CreateAssetMenu(menuName = "NAH/Race (placeholder)")]
public sealed class NAHRaceDefinition : ScriptableObject
{
    [SerializeField] string displayName = "Unnamed race";
    [Tooltip("Short background for the character screen.")]
    [SerializeField, TextArea(2, 5)] string introduction = "";
    [SerializeField] NAHStatModifiers modifiers = NAHStatModifiers.None;

    public string DisplayName => displayName;
    public string Introduction => introduction;
    public NAHStatModifiers Modifiers => modifiers;

#if UNITY_EDITOR
    // Used only by the editor's placeholder roster builder.
    public void SetPlaceholder(string newName, string newIntroduction, NAHStatModifiers newModifiers)
    {
        displayName = newName;
        introduction = newIntroduction;
        modifiers = newModifiers;
    }
#endif
}
