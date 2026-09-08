using System.Collections.Generic;

namespace CivilSpellAI.Spell
{
    /// <summary>
    /// Provides offline word validation without coupling SpellEngine to files
    /// or to a particular dictionary implementation.
    /// </summary>
    public interface ISpellingDictionary
    {
        bool IsLoaded { get; }
        bool Check(string word);
        IReadOnlyList<string> Suggest(string word);
    }
}
