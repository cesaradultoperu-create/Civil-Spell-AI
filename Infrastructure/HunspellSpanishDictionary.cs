using System;
using System.Collections.Generic;
using System.IO;
using CivilSpellAI.Spell;
using WeCantSpell.Hunspell;

namespace CivilSpellAI.Infrastructure
{
    /// <summary>
    /// Loads the open es_PE Hunspell dictionary distributed with the add-in.
    /// If its files cannot be read, local rule-based correction remains active.
    /// </summary>
    public sealed class HunspellSpanishDictionary : ISpellingDictionary
    {
        private static readonly Lazy<HunspellSpanishDictionary> DefaultDictionary =
            new Lazy<HunspellSpanishDictionary>(LoadFromAssemblyDirectory, true);

        private readonly WordList words;

        private HunspellSpanishDictionary(WordList words)
        {
            this.words = words;
        }

        public bool IsLoaded
        {
            get { return words != null; }
        }

        public static HunspellSpanishDictionary LoadDefault()
        {
            return DefaultDictionary.Value;
        }

        private static HunspellSpanishDictionary LoadFromAssemblyDirectory()
        {
            string assemblyDirectory = Path.GetDirectoryName(
                typeof(HunspellSpanishDictionary).Assembly.Location);

            if (string.IsNullOrWhiteSpace(assemblyDirectory))
                return new HunspellSpanishDictionary(null);

            return Load(
                Path.Combine(assemblyDirectory, "Resources", "Dictionaries", "es_PE.dic"),
                Path.Combine(assemblyDirectory, "Resources", "Dictionaries", "es_PE.aff"));
        }

        public static HunspellSpanishDictionary Load(
            string dictionaryPath,
            string affixPath)
        {
            if (string.IsNullOrWhiteSpace(dictionaryPath) ||
                string.IsNullOrWhiteSpace(affixPath) ||
                !File.Exists(dictionaryPath) ||
                !File.Exists(affixPath))
            {
                return new HunspellSpanishDictionary(null);
            }

            try
            {
                return new HunspellSpanishDictionary(
                    WordList.CreateFromFiles(dictionaryPath, affixPath));
            }
            catch (IOException)
            {
                return new HunspellSpanishDictionary(null);
            }
            catch (UnauthorizedAccessException)
            {
                return new HunspellSpanishDictionary(null);
            }
            catch (ArgumentException)
            {
                return new HunspellSpanishDictionary(null);
            }
            catch (FormatException)
            {
                return new HunspellSpanishDictionary(null);
            }
            catch (NotSupportedException)
            {
                return new HunspellSpanishDictionary(null);
            }
            catch (System.Security.SecurityException)
            {
                return new HunspellSpanishDictionary(null);
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
