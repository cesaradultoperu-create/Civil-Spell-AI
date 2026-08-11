using System.Collections.Generic;

namespace CivilSpellAI.Spell
{
    public sealed class CorrectionResult
    {
        public CorrectionResult(string originalText, string correctedText, TextLanguage language, IList<CorrectionChange> changes)
        {
            OriginalText = originalText;
            CorrectedText = correctedText;
            Language = language;
            Changes = new List<CorrectionChange>(changes).AsReadOnly();
        }

        public string OriginalText { get; private set; }
        public string CorrectedText { get; private set; }
        public TextLanguage Language { get; private set; }
        public IList<CorrectionChange> Changes { get; private set; }

        public bool HasChanges
        {
            get { return Changes.Count > 0; }
        }
    }
}
