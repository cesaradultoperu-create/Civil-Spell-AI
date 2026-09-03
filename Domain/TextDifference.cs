using System;

namespace CivilSpellAI.Domain
{
    public sealed class TextDifference
    {
        public TextDifference(
            int originalStart,
            string originalText,
            int proposedStart,
            string proposedText)
        {
            if (originalStart < 0)
                throw new ArgumentOutOfRangeException("originalStart");

            if (proposedStart < 0)
                throw new ArgumentOutOfRangeException("proposedStart");

            OriginalStart = originalStart;
            OriginalText = originalText ?? string.Empty;
            ProposedStart = proposedStart;
            ProposedText = proposedText ?? string.Empty;
        }

        public int OriginalStart { get; private set; }

        public string OriginalText { get; private set; }

        public int ProposedStart { get; private set; }

        public string ProposedText { get; private set; }
    }
}
