using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CivilSpellAI.Infrastructure
{
    public sealed class PersonalGlossaryStore
    {
        public const int MaximumTerms = 1000;
        public const int MaximumTermLength = 200;
        public const long MaximumFileBytes = 512L * 1024L;
        private const int MaximumCandidateTerms = 10000;
        private readonly string filePath;

        public PersonalGlossaryStore(string configurationDirectory)
        {
            if (string.IsNullOrWhiteSpace(configurationDirectory))
            {
                throw new ArgumentException(
                    "Se requiere el directorio de configuración.",
                    "configurationDirectory");
            }

            filePath = Path.Combine(configurationDirectory, "personal-glossary.txt");
        }

        public string FilePath
        {
            get { return filePath; }
        }

        public IList<string> Load()
        {
            string[] candidates = { filePath, filePath + ".tmp" };

            foreach (string candidate in candidates)
            {
                if (!File.Exists(candidate))
                    continue;

                try
                {
                    if (new FileInfo(candidate).Length > MaximumFileBytes)
                        continue;

                    return Normalize(File.ReadLines(candidate), false);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (System.Security.SecurityException)
                {
                }
                catch (NotSupportedException)
                {
                }
            }

            return new List<string>().AsReadOnly();
        }

        public void Save(IEnumerable<string> terms)
        {
            IList<string> normalized = ValidateAndNormalize(terms);
            string directory = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directory);
            string temporaryPath = filePath + ".tmp";
            File.WriteAllLines(temporaryPath, normalized);
            UserConfigurationStore.ReplaceFile(temporaryPath, filePath);
        }

        internal static IList<string> ValidateAndNormalize(
            IEnumerable<string> terms)
        {
            return Normalize(terms, true);
        }

        private static IList<string> Normalize(
            IEnumerable<string> terms,
            bool rejectExcess)
        {
            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int inspected = 0;

            foreach (string value in terms ?? new string[0])
            {
                inspected++;

                if (inspected > MaximumCandidateTerms)
                {
                    if (rejectExcess)
                    {
                        throw new InvalidOperationException(
                            "El glosario contiene demasiadas líneas para procesarlas de forma segura.");
                    }

                    break;
                }

                string term = (value ?? string.Empty).Trim();

                if (term.Length == 0)
                    continue;

                if (term.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                {
                    if (rejectExcess)
                    {
                        throw new InvalidOperationException(
                            "Cada término del glosario debe ocupar una sola línea.");
                    }

                    continue;
                }

                if (term.Length > MaximumTermLength)
                {
                    if (rejectExcess)
                    {
                        throw new InvalidOperationException(string.Format(
                            "Cada término del glosario admite como máximo {0} caracteres.",
                            MaximumTermLength));
                    }

                    continue;
                }

                unique.Add(term);

                if (unique.Count > MaximumTerms)
                {
                    if (rejectExcess)
                    {
                        throw new InvalidOperationException(string.Format(
                            "El glosario personal admite como máximo {0} términos distintos.",
                            MaximumTerms));
                    }

                    break;
                }
            }

            List<string> sorted = unique.OrderBy(
                term => term,
                StringComparer.OrdinalIgnoreCase)
                .Take(MaximumTerms)
                .ToList();
            return sorted.AsReadOnly();
        }
    }
}
