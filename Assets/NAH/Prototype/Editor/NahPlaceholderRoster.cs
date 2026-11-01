using UnityEditor;
using UnityEngine;

// Creates the PLACEHOLDER roster: races with plain species names and classes with generated names, using the
// ratios proposed in GAME_DESIGN.md. It runs once. Later runs reuse the assets, so the owner's renames and tuning are kept.
// Ratio order: health, armor, damage, move speed, swing speed.
public static class NahPlaceholderRoster
{
    const string ParentFolder = "Assets/NAH/Prototype";
    const string Folder = ParentFolder + "/Roster";
    const string RosterPath = Folder + "/NAHPlaceholderRoster.asset";

    readonly struct Entry
    {
        public readonly string Name;
        public readonly NAHStatModifiers Ratios;
        public readonly string Introduction;

        public Entry(string name, float health, float armor, float damage, float moveSpeed, float swingSpeed, string introduction)
        {
            Name = name;
            Ratios = new NAHStatModifiers { health = health, armor = armor, damage = damage, moveSpeed = moveSpeed, swingSpeed = swingSpeed };
            Introduction = introduction;
        }
    }

    static readonly Entry[] Races =
    {
        new Entry("Human", 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, "Keeping your word to strangers. A human champion carries a treaty seal that someone broke, and wants to know who."),
        new Entry("Elf", 0.85f, 0.85f, 1.05f, 1.20f, 1.10f, "A promise kept across centuries. An elf champion came to finish a promise made before they were born. Fast and nimble, but big hits hurt."),
        new Entry("Dwarf", 1.10f, 1.20f, 1.05f, 0.90f, 0.80f, "Honor lives in what you make. A dwarf champion came to win back a maker's mark stolen from their family's forge."),
        new Entry("Bear", 1.15f, 1.20f, 1.10f, 0.90f, 0.75f, "Guarding the weak through winter. A bear champion left a den unguarded to answer the summons and wants to get back."),
        new Entry("Wolf", 0.95f, 0.85f, 1.05f, 1.20f, 1.00f, "Loyalty to the pack. A wolf champion fights so that no packmate is left behind in the Nexus."),
        new Entry("Goblin", 0.85f, 0.90f, 0.95f, 1.15f, 1.15f, "Paying every debt. A goblin champion owes something to the Scrap-King of Wickmarket and means to pay it in the arena."),
        new Entry("Troll", 1.20f, 1.15f, 1.15f, 0.85f, 0.75f, "Patience. A troll waits a hundred years to answer an insult; the Nexus interrupted the wait."),
        new Entry("Fairy", 0.75f, 0.80f, 1.00f, 1.30f, 1.25f, "Never weighing down the ones you carry. A fairy champion slipped into the Nexus on purpose, to look for a way out from inside."),
        new Entry("Flower Folk", 1.00f, 0.95f, 1.00f, 1.05f, 1.00f, "Family first. The Five Gardens argue about who speaks for them; a flower champion must choose which queen they answer to."),
        new Entry("Dog", 0.95f, 0.90f, 1.00f, 1.20f, 1.00f, "Loyalty to a person, not a crown. A dog champion is searching the Nexus for the companion who was summoned with them."),
        new Entry("Duck", 0.85f, 0.95f, 0.95f, 1.10f, 1.15f, "Never taken seriously, winning anyway. A duck champion insists the Nexus summoned them by mistake and plans to prove it wrong."),
        new Entry("Chicken", 0.75f, 0.85f, 0.95f, 1.25f, 1.30f, "Courage far bigger than the body. A chicken champion has never backed down, not even from things that could eat it."),
    };

    static readonly Entry[] Classes =
    {
        new Entry("Oathwarden", 1.15f, 1.25f, 0.90f, 0.95f, 0.80f, "Stands in front so others do not have to. A sword to answer, a shield to hold the line."),
        new Entry("Dawnhammer", 1.10f, 1.15f, 0.95f, 0.90f, 0.90f, "A sworn protector who mends allies between blows. Slow to fall, slower to give up."),
        new Entry("Skypiercer", 0.90f, 0.90f, 1.10f, 1.10f, 1.05f, "Strikes from afar with a bow. Quick feet, light armor: keep your distance or pay for it."),
        new Entry("Veilstalker", 0.85f, 0.85f, 1.15f, 1.15f, 1.05f, "Strikes from behind with twin daggers and is gone before the answer comes."),
        new Entry("Wrathborn", 1.10f, 0.85f, 1.25f, 1.00f, 0.85f, "Trades armor for fury. The more they bleed, the harder they hit."),
        new Entry("Aetherweaver", 0.85f, 0.80f, 1.20f, 1.00f, 1.20f, "Shapes fire and frost from a staff. Fragile up close, dangerous at range."),
        new Entry("Lifesinger", 1.00f, 1.00f, 0.80f, 1.05f, 1.05f, "Keeps the team standing. Weak alone, priceless beside a friend."),
        new Entry("Wildspeaker", 0.90f, 0.90f, 0.85f, 1.00f, 1.00f, "Fights through bound creatures. Guard the caller and the pack guards you."),
    };

    public static NAHRoster LoadOrCreate()
    {
        var existing = AssetDatabase.LoadAssetAtPath<NAHRoster>(RosterPath);
        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder(ParentFolder, "Roster");

        var races = new NAHRaceDefinition[Races.Length];
        for (int i = 0; i < Races.Length; i++)
        {
            var race = ScriptableObject.CreateInstance<NAHRaceDefinition>();
            race.SetPlaceholder(Races[i].Name, Races[i].Introduction, Races[i].Ratios);
            AssetDatabase.CreateAsset(race, Folder + "/Race_" + Races[i].Name.Replace(" ", "") + ".asset");
            races[i] = race;
        }

        var classes = new NAHClassDefinition[Classes.Length];
        for (int i = 0; i < Classes.Length; i++)
        {
            var fighterClass = ScriptableObject.CreateInstance<NAHClassDefinition>();
            fighterClass.SetPlaceholder(Classes[i].Name, Classes[i].Introduction, Classes[i].Ratios);
            AssetDatabase.CreateAsset(fighterClass, Folder + "/Class_" + Classes[i].Name + ".asset");
            classes[i] = fighterClass;
        }

        var roster = ScriptableObject.CreateInstance<NAHRoster>();
        roster.SetPlaceholder(races, classes);
        AssetDatabase.CreateAsset(roster, RosterPath);
        AssetDatabase.SaveAssets();
        return roster;
    }
}
