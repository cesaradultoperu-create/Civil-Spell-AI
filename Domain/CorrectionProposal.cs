using System;
using System.Collections.Generic;

namespace CivilSpellAI.Domain
{
    public sealed class CorrectionProposal
    {
        public CorrectionProposal(
            string proposedText,
            ProposalSource source,
            ReviewLanguage language,
            string explanation,
            IEnumerable<TextDifference> changes,
            IEnumerable<string> warnings)
        {
            if (proposedText == null)
                throw new ArgumentNullException("proposedText");

            ProposedText = proposedText;
            Source = source;
            Language = language;
            Explanation = explanation ?? string.Empty;
            Changes = Copy(changes);
            Warnings = Copy(warnings);
        }

        public string ProposedText { get; private set; }

        public ProposalSource Source { get; private set; }

        public ReviewLanguage Language { get; private set; }

        public string Explanation { get; private set; }

        public IList<TextDifference> Changes { get; private set; }

        public IList<string> Warnings { get; private set; }

        private static IList<T> Copy<T>(IEnumerable<T> values)
        {
            List<T> copied = new List<T>();

            foreach (T value in values ?? new T[0])
            {
                if (!object.ReferenceEquals(value, null))
                    copied.Add(value);
            }

            return copied.AsReadOnly();
        }
    }
}
