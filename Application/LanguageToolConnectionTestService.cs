using System;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Application
{
    public sealed class LanguageToolConnectionTestFailure
    {
        private LanguageToolConnectionTestFailure(
            string statusText,
            string userMessage)
        {
            StatusText = statusText;
            UserMessage = userMessage;
        }

        public string StatusText { get; private set; }

        public string UserMessage { get; private set; }

        public static LanguageToolConnectionTestFailure FromException(
            Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            DiagnosticCode diagnosticCode =
                DiagnosticClassifier.FromException(exception);
            string code = DiagnosticCatalog.GetCode(diagnosticCode);
            string statusText;
            string detail;

            switch (diagnosticCode)
            {
                case DiagnosticCode.ConfigurationMissing:
                case DiagnosticCode.ConfigurationFailure:
                    statusText = "Falta configurar LanguageTool local";
                    detail = "Habilite LanguageTool local e intente nuevamente.";
                    break;
                case DiagnosticCode.NetworkUnavailable:
                    statusText = "LanguageTool local no está disponible";
                    detail = "Inicie el servidor local en 127.0.0.1:8081 e intente nuevamente.";
                    break;
                case DiagnosticCode.Timeout:
                    statusText = "La prueba agotó el tiempo de espera";
                    detail = "El servidor local tardó demasiado en responder.";
                    break;
                case DiagnosticCode.InvalidResponse:
                    statusText = "LanguageTool devolvió una respuesta no válida";
                    detail = "El servidor respondió, pero la respuesta no cumplió el formato esperado.";
                    break;
                default:
                    statusText = "No fue posible completar la prueba local";
                    detail = "Ocurrió un error inesperado durante la prueba de LanguageTool.";
                    break;
            }

            return new LanguageToolConnectionTestFailure(
                statusText + " (" + code + ").",
                detail + Environment.NewLine + Environment.NewLine +
                    "Código de soporte: " + code + ".");
        }
    }

    public sealed class LanguageToolConnectionTestService
    {
        public const string FixedTestText = "La estructura esta instalada.";
        private readonly ITextCorrectionProvider provider;

        public LanguageToolConnectionTestService(
            ITextCorrectionProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException("provider");

            this.provider = provider;
        }

        public async Task TestAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CorrectionRequest request = new CorrectionRequest(
                new TextSnapshot(
                    "civilspell-languagetool-test",
                    "connection-test",
                    "SyntheticText",
                    FixedTestText),
                ReviewLanguage.Spanish,
                new string[0],
                1,
                "Prueba local");

            await CancellationBoundary.AwaitAsync(
                provider.ProposeAsync(request, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
