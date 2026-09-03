using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CivilSpellAI.Infrastructure
{
    public sealed class OrganizationalGlossaryStore
    {
        public const int MaximumTerms = 5000;
        public const int MaximumTermLength = 200;
        public const long MaximumFileBytes = 1024L * 1024L;
        private readonly string filePath;

        public OrganizationalGlossaryStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "CivilSpellAI",
                "organizational-glossary.txt"))
        {
        }

        public OrganizationalGlossaryStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Se requiere la ruta del glosario.", "filePath");

            this.filePath = filePath;
        }

        public string FilePath
        {
            get { return filePath; }
        }

        public IList<string> Load()
        {
            if (!File.Exists(filePath))
                return new List<string>().AsReadOnly();

            try
            {
                if (new FileInfo(filePath).Length > MaximumFileBytes)
                    return new List<string>().AsReadOnly();

                return File.ReadLines(filePath)
                    .Select(line => (line ?? string.Empty).Trim())
                    .Where(line => line.Length > 0 &&
                        line.Length <= MaximumTermLength &&
                        !line.StartsWith("#"))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(line => line, StringComparer.OrdinalIgnoreCase)
                    .Take(MaximumTerms)
                    .ToList()
                    .AsReadOnly();
            }
            catch (IOException)
            {
                return new List<string>().AsReadOnly();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>().AsReadOnly();
            }
            catch (System.Security.SecurityException)
            {
                return new List<string>().AsReadOnly();
            }
            catch (NotSupportedException)
            {
                return new List<string>().AsReadOnly();
            }
        }
    }
}
