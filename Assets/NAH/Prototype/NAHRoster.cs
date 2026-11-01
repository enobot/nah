using System;
using System.Globalization;
using UnityEngine;

// The races and classes a player can choose, in a fixed order. A client sends the index of its choice,
// and the host checks the index against these lists. Every entry today is a PLACEHOLDER.
[CreateAssetMenu(menuName = "NAH/Roster (placeholder)")]
public sealed class NAHRoster : ScriptableObject
{
    [SerializeField] NAHRaceDefinition[] races = Array.Empty<NAHRaceDefinition>();
    [SerializeField] NAHClassDefinition[] classes = Array.Empty<NAHClassDefinition>();

    public int RaceCount => races.Length;
    public int ClassCount => classes.Length;

    // Null for -1 ("none") or any index outside the list.
    public NAHRaceDefinition Race(int index)
    {
        return index >= 0 && index < races.Length ? races[index] : null;
    }

    public NAHClassDefinition Class(int index)
    {
        return index >= 0 && index < classes.Length ? classes[index] : null;
    }

    // Accepts a display name (any capitalization) or an index. Returns -1 when nothing matches.
    public int FindRace(string nameOrIndex)
    {
        return Find(nameOrIndex, races.Length, i => races[i] != null ? races[i].DisplayName : null);
    }

    public int FindClass(string nameOrIndex)
    {
        return Find(nameOrIndex, classes.Length, i => classes[i] != null ? classes[i].DisplayName : null);
    }

    static int Find(string nameOrIndex, int count, Func<int, string> nameAt)
    {
        if (string.IsNullOrWhiteSpace(nameOrIndex))
            return -1;
        string wanted = nameOrIndex.Trim();
        if (int.TryParse(wanted, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            return index >= 0 && index < count ? index : -1;
        for (int i = 0; i < count; i++)
        {
            if (string.Equals(nameAt(i), wanted, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

#if UNITY_EDITOR
    // Used only by the editor's placeholder roster builder.
    public void SetPlaceholder(NAHRaceDefinition[] newRaces, NAHClassDefinition[] newClasses)
    {
        races = newRaces;
        classes = newClasses;
    }
#endif
}
