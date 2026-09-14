using System;
using System.Collections.Generic;
using System.IO;
using CivilSpellAI.Spell;
using WeCantSpell.Hunspell;

namespace CivilSpellAI.Infrastructure
{
    /// <summary>
    /// Loads the open en_US Hunspell dictionary distributed with the add-in.
    /// If its files cannot be read, local rule-based correction remains active.
    /// </summary>
    public sealed class HunspellEnglishDictionary : ISpellingDictionary
    {
        private static readonly Lazy<HunspellEnglishDictionary> DefaultDictionary =
            new Lazy<HunspellEnglishDictionary>(LoadFromAssemblyDirectory, true);

        private readonly WordList words;

        private HunspellEnglishDictionary(WordList words)
        {
            this.words = words;
        }

        public bool IsLoaded
        {
            get { return words != null; }
        }

        public static HunspellEnglishDictionary LoadDefault()
        {
            return DefaultDictionary.Value;
        }

        private static HunspellEnglishDictionary LoadFromAssemblyDirectory()
        {
            string assemblyDirectory = Path.GetDirectoryName(
                typeof(HunspellEnglishDictionary).Assembly.Location);

            if (string.IsNullOrWhiteSpace(assemblyDirectory))
                return new HunspellEnglishDictionary(null);

            return Load(
                Path.Combine(assemblyDirectory, "Resources", "Dictionaries", "en_US.dic"),
                Path.Combine(assemblyDirectory, "Resources", "Dictionaries", "en_US.aff"));
        }

        public static HunspellEnglishDictionary Load(
            string dictionaryPath,
            string affixPath)
        {
            if (string.IsNullOrWhiteSpace(dictionaryPath) ||
                string.IsNullOrWhiteSpace(affixPath) ||
                !File.Exists(dictionaryPath) ||
                !File.Exists(affixPath))
            {
                return new HunspellEnglishDictionary(null);
            }

            try
            {
                return new HunspellEnglishDictionary(
                    WordList.CreateFromFiles(dictionaryPath, affixPath));
            }
            catch (IOException)
            {
                return new HunspellEnglishDictionary(null);
            }
            catch (UnauthorizedAccessException)
            {
                return new HunspellEnglishDictionary(null);
            }
            catch (ArgumentException)
            {
                return new HunspellEnglishDictionary(null);
            }
            catch (FormatException)
            {
                return new HunspellEnglishDictionary(null);
            }
            catch (NotSupportedException)
            {
                return new HunspellEnglishDictionary(null);
            }
            catch (System.Security.SecurityException)
            {
                return new HunspellEnglishDictionary(null);
            }
        }

        public bool Check(string word)
        {
            return !IsLoaded || words.Check(word);
        }

        public IReadOnlyList<string> Suggest(string word)
        {
            if (!IsLoaded || string.IsNullOrWhiteSpace(word))
                return new string[0];

            return new List<string>(words.Suggest(word)).AsReadOnly();
        }
    }
}
