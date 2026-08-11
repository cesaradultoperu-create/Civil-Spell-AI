using System;
using System.Collections.Generic;
using System.IO;

namespace CivilSpellAI.Spell
{
    /// <summary>
    /// Terms in this glossary are left untouched by the spell checker. Add one
    /// term per line to Resources\\technical-glossary.txt to extend it.
    /// </summary>
    public sealed class TechnicalGlossary
    {
        private readonly HashSet<string> terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<string> Terms
        {
            get { return terms; }
        }

        public static TechnicalGlossary LoadDefault()
        {
            TechnicalGlossary glossary = new TechnicalGlossary();

            glossary.AddRange(new[]
            {
                "Civil 3D", "AutoCAD", "Cogo Point", "Pipe Network",
                "Pressure Network", "Feature Line", "Sample Line",
                "Profile View", "Alignment", "Corridor", "Surface",
                "TIN", "EG", "FG", "PVI", "DWG", "DWT", "Xref",
                "MText", "DBText", "LandXML", "ObjectId"
            });

            string assemblyDirectory = Path.GetDirectoryName(typeof(TechnicalGlossary).Assembly.Location);

            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                glossary.AddFromFile(Path.Combine(
                    assemblyDirectory,
                    "Resources",
                    "technical-glossary.txt"));
            }

            return glossary;
        }

        public void Add(string term)
        {
            if (!string.IsNullOrWhiteSpace(term))
                terms.Add(term.Trim());
        }

        public void AddRange(IEnumerable<string> newTerms)
        {
            if (newTerms == null)
                return;

            foreach (string term in newTerms)
                Add(term);
        }

        public void AddFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            foreach (string line in File.ReadAllLines(filePath))
            {
                string term = line.Trim();

                if (term.Length == 0 || term.StartsWith("#"))
                    continue;

                Add(term);
            }
        }
    }
}
