using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;
using CivilSpellAI.Spell;

namespace CivilSpellAI.Application
{
    public sealed class RuleBasedCorrectionProvider : ITextCorrectionProvider
    {
        private readonly SpellEngine engine;
        private readonly ITextDiffer differ;

        public RuleBasedCorrectionProvider()
            : this(new SpellEngine(), new TextDiffer())
        {
        }

        public RuleBasedCorrectionProvider(SpellEngine engine, ITextDiffer differ)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");

            if (differ == null)
                throw new ArgumentNullException("differ");

            this.engine = engine;
            this.differ = differ;
        }

        public string Name
        {
            get { return "Reglas locales"; }
        }

        public Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
            CorrectionRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            cancellationToken.ThrowIfCancellationRequested();
            CorrectionResult result = engine.Analyze(request.Text);
            List<CorrectionProposal> proposals = new List<CorrectionProposal>();

            if (result.HasChanges)
            {
                proposals.Add(new CorrectionProposal(
                    result.CorrectedText,
                    ProposalSource.LocalRules,
                    MapLanguage(result.Language),
                    string.Format(
                        "Corrección local conservadora: {0} cambio(s).",
                        result.Changes.Count),
                    differ.Calculate(result.OriginalText, result.CorrectedText),
                    new string[0]));
            }

            IReadOnlyList<CorrectionProposal> readOnly = proposals.AsReadOnly();
            return Task.FromResult(readOnly);
        }

        private static ReviewLanguage MapLanguage(TextLanguage language)
        {
            switch (language)
            {
                case TextLanguage.Spanish:
                    return ReviewLanguage.Spanish;
                case TextLanguage.English:
                    return ReviewLanguage.English;
                case TextLanguage.Mixed:
                    return ReviewLanguage.Mixed;
                default:
                    return ReviewLanguage.Unknown;
            }
        }
    }
}
