using System;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Application
{
    public sealed class OpenAiConnectionTestFailure
    {
        private OpenAiConnectionTestFailure(
            string statusText,
            string userMessage)
        {
            StatusText = statusText;
            UserMessage = userMessage;
        }

        public string StatusText { get; private set; }

        public string UserMessage { get; private set; }

        public static OpenAiConnectionTestFailure FromException(
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
                    statusText = "Falta configurar OpenAI";
                    detail = "Configure la variable de entorno de usuario OPENAI_API_KEY e intente nuevamente.";
                    break;
                case DiagnosticCode.AuthenticationRejected:
                    statusText = "La credencial fue rechazada";
                    detail = "OpenAI rechazó la credencial. Verifique OPENAI_API_KEY sin compartir su valor.";
                    break;
                case DiagnosticCode.NetworkUnavailable:
                    statusText = "OpenAI no está disponible";
                    detail = "Compruebe la conexión de red e intente nuevamente más tarde.";
                    break;
                case DiagnosticCode.Timeout:
                    statusText = "La prueba agotó el tiempo de espera";
                    detail = "La solicitud tardó demasiado. Compruebe la red e intente nuevamente.";
                    break;
                case DiagnosticCode.InvalidResponse:
                    statusText = "OpenAI devolvió una respuesta no válida";
                    detail = "OpenAI respondió, pero la respuesta no cumplió el formato esperado.";
                    break;
                default:
                    statusText = "No fue posible completar la prueba";
                    detail = "Ocurrió un error inesperado durante la prueba de conexión.";
                    break;
            }

            return new OpenAiConnectionTestFailure(
                statusText + " (" + code + ").",
                detail + Environment.NewLine + Environment.NewLine +
                    "Código de soporte: " + code + ".");
        }
    }

    public sealed class OpenAiConnectionTestService
    {
        public const string FixedTestText = "Prueba de conexión de CivilSpellAI.";
        private readonly ITextCorrectionProvider provider;

        public OpenAiConnectionTestService(ITextCorrectionProvider provider)
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
                    "civilspell-connection-test",
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

            // A provider may finish after ignoring a cancellation request. Do
            // not turn that late response into a successful connection test.
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
