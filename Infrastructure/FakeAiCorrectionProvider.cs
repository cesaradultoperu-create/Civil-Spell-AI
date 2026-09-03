using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Infrastructure
{
    public sealed class FakeAiCorrectionProvider : ITextCorrectionProvider
    {
        private static readonly KeyValuePair<string, string>[] Rules =
        {
            new KeyValuePair<string, string>(@"\bcarreteraa\b", "carretera"),
            new KeyValuePair<string, string>(@"\bprincipall\b", "principal"),
            new KeyValuePair<string, string>(@"\bestructuraa\b", "estructura"),
            new KeyValuePair<string, string>(@"\bdrenajeee\b", "drenaje"),
            new KeyValuePair<string, string>(@"\bseñalizaciónn\b", "señalización"),
            new KeyValuePair<string, string>(@"\btopografíaaa\b", "topografía")
        };

        private readonly FakeAiScenario scenario;

        public FakeAiCorrectionProvider(FakeAiScenario scenario)
        {
            this.scenario = scenario;
        }

        public string Name
        {
            get { return "IA simulada"; }
        }

        public async Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
            CorrectionRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            int delayMilliseconds = scenario == FakeAiScenario.SlowSuccessful
                ? 5000
                : 300;
            await Task.Delay(delayMilliseconds, cancellationToken)
                .ConfigureAwait(false);

            switch (scenario)
            {
                case FakeAiScenario.Unavailable:
                    throw new CorrectionProviderException(
                        ProviderFailureKind.Unavailable,
                        "El proveedor simulado no está disponible.");
                case FakeAiScenario.Timeout:
                    throw new CorrectionProviderException(
                        ProviderFailureKind.Timeout,
                        "La solicitud simulada superó el tiempo de espera.");
                case FakeAiScenario.InvalidResponse:
                    throw new CorrectionProviderException(
                        ProviderFailureKind.InvalidResponse,
                        "El proveedor simulado devolvió una respuesta no válida.");
                case FakeAiScenario.UnsafeTechnicalChange:
                    return CreateUnsafeProposal(request);
                default:
                    return CreateSafeProposals(request);
            }
        }

        private static IReadOnlyList<CorrectionProposal> CreateSafeProposals(
            CorrectionRequest request)
        {
            string corrected = request.Text;

            foreach (KeyValuePair<string, string> rule in Rules)
            {
                corrected = Regex.Replace(
                    corrected,
                    rule.Key,
                    match => MatchCapitalization(match.Value, rule.Value),
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            List<CorrectionProposal> proposals = new List<CorrectionProposal>();

            if (!string.Equals(corrected, request.Text, StringComparison.Ordinal))
            {
                proposals.Add(CreateProposal(
                    corrected,
                    "Alternativa simulada de corrección conservadora."));

                if (!Regex.IsMatch(corrected, @"[.!?]\s*$"))
                {
                    proposals.Add(CreateProposal(
                        corrected + ".",
                        "Alternativa simulada que además completa la puntuación."));
                }
            }

            return proposals.AsReadOnly();
        }

        private static IReadOnlyList<CorrectionProposal> CreateUnsafeProposal(
            CorrectionRequest request)
        {
            Match number = Regex.Match(request.Text, @"\d+(?:[.,]\d+)*");
            string unsafeText = number.Success
                ? request.Text.Remove(number.Index, number.Length).Insert(number.Index, "999")
                : request.Text + " 999";

            return new List<CorrectionProposal>
            {
                CreateProposal(
                    unsafeText,
                    "Escenario simulado que altera un valor técnico y debe bloquearse.")
            }.AsReadOnly();
        }

        private static CorrectionProposal CreateProposal(
            string text,
            string explanation)
        {
            return new CorrectionProposal(
                text,
                ProposalSource.ArtificialIntelligence,
                ReviewLanguage.Unknown,
                explanation,
                null,
                null);
        }

        private static string MatchCapitalization(string original, string replacement)
        {
            if (original.ToUpperInvariant() == original)
                return replacement.ToUpperInvariant();

            if (char.IsUpper(original[0]))
                return char.ToUpperInvariant(replacement[0]) + replacement.Substring(1);

            return replacement;
        }
    }
}
