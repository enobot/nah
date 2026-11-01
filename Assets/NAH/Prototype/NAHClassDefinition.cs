using UnityEngine;

// A class multiplies the stats a body already gives, on top of the race. The classes in Roster/ are PLACEHOLDERS:
// the owner will choose the real classes, names, and numbers (GAME_DESIGN.md, "Proposed class kits").
[CreateAssetMenu(menuName = "NAH/Class (placeholder)")]
public sealed class NAHClassDefinition : ScriptableObject
{
    [SerializeField] string displayName = "Unnamed class";
    [Tooltip("Short introduction for the side panel of the character screen.")]
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
