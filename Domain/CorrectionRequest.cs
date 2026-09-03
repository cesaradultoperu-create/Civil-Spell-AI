using System;
using System.Collections.Generic;

namespace CivilSpellAI.Domain
{
    public sealed class CorrectionRequest
    {
        public const int MaximumGlossaryTerms = 10000;
        public const int MaximumGlossaryTermLength = 200;
        private const int MaximumGlossaryCandidates = 50000;

        public CorrectionRequest(
            TextSnapshot snapshot,
            ReviewLanguage language,
            IEnumerable<string> glossaryTerms,
            int maximumAlternatives)
            : this(
                snapshot,
                language,
                glossaryTerms,
                maximumAlternatives,
                "Desconocido")
        {
        }

        public CorrectionRequest(
            TextSnapshot snapshot,
            ReviewLanguage language,
            IEnumerable<string> glossaryTerms,
            int maximumAlternatives,
            string locationName)
        {
            if (snapshot == null)
                throw new ArgumentNullException("snapshot");

            if (maximumAlternatives < 1 || maximumAlternatives > 3)
            {
                throw new ArgumentOutOfRangeException(
                    "maximumAlternatives",
                    "El número de alternativas debe estar entre uno y tres.");
            }

            Snapshot = snapshot;
            Language = language;
            MaximumAlternatives = maximumAlternatives;
            GlossaryTerms = CopyTerms(glossaryTerms);
            LocationName = string.IsNullOrWhiteSpace(locationName)
                ? "Desconocido"
                : locationName.Trim();
        }

        public TextSnapshot Snapshot { get; private set; }

        public ReviewLanguage Language { get; private set; }

        public IList<string> GlossaryTerms { get; private set; }

        public int MaximumAlternatives { get; private set; }

        public string LocationName { get; private set; }

        public string Text
        {
            get { return Snapshot.OriginalText; }
        }

        private static IList<string> CopyTerms(IEnumerable<string> terms)
        {
            List<string> copied = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (terms != null)
            {
                int inspected = 0;

                foreach (string term in terms)
                {
                    inspected++;

                    if (inspected > MaximumGlossaryCandidates)
                        break;

                    if (string.IsNullOrWhiteSpace(term))
                        continue;

                    string trimmed = term.Trim();

                    if (trimmed.Length <= MaximumGlossaryTermLength &&
                        seen.Add(trimmed))
                    {
                        copied.Add(trimmed);

                        if (copied.Count >= MaximumGlossaryTerms)
                            break;
                    }
                }
            }

            return copied.AsReadOnly();
        }
    }
}
