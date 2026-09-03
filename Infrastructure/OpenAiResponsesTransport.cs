using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Infrastructure
{
    public sealed class OpenAiResponsesTransport : IOpenAiTransport
    {
        private static readonly Uri ResponsesEndpoint =
            new Uri("https://api.openai.com/v1/responses");

        private static readonly HttpClient SharedClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        private static readonly Encoding StrictUtf8 =
            new UTF8Encoding(false, true);
        private const int MaximumResponseBytes = 804096;
        private readonly IOpenAiCredentialProvider credentials;

        public OpenAiResponsesTransport(IOpenAiCredentialProvider credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException("credentials");

            this.credentials = credentials;
        }

        public async Task<string> SendAsync(
            string requestJson,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            string apiKey = credentials.GetApiKey();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new CorrectionProviderException(
                    ProviderFailureKind.Configuration,
                    "No se encontro la variable de entorno OPENAI_API_KEY.");
            }

            using (CancellationTokenSource timeoutCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Post,
                ResponsesEndpoint))
            {
                timeoutCancellation.CancelAfter(timeout);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    requestJson,
                    Encoding.UTF8,
                    "application/json");

                try
                {
                    using (HttpResponseMessage response = await SharedClient
                        .SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            timeoutCancellation.Token)
                        .ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                            throw CreateHttpFailure(response.StatusCode);

                        return await ReadBoundedResponseAsync(
                            response.Content,
                            timeoutCancellation.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    throw new CorrectionProviderException(
                        ProviderFailureKind.Timeout,
                        "OpenAI supero el tiempo de espera configurado.");
                }
                catch (HttpRequestException)
                {
                    throw new CorrectionProviderException(
                        ProviderFailureKind.Network,
                        "No fue posible conectar con OpenAI.");
                }
                catch (IOException)
                {
                    throw new CorrectionProviderException(
                        ProviderFailureKind.Network,
                        "La conexión con OpenAI se interrumpió durante la respuesta.");
                }
            }
        }

        internal static async Task<string> ReadBoundedResponseAsync(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            long? declaredLength = content.Headers.ContentLength;

            if (declaredLength.HasValue &&
                declaredLength.Value > MaximumResponseBytes)
            {
                throw ResponseTooLarge();
            }

            using (Stream source = await content
                .ReadAsStreamAsync()
                .ConfigureAwait(false))
            using (MemoryStream buffer = new MemoryStream())
            {
                byte[] chunk = new byte[8192];

                while (true)
                {
                    int read = await source.ReadAsync(
                        chunk,
                        0,
                        chunk.Length,
                        cancellationToken).ConfigureAwait(false);

                    if (read == 0)
                        break;

                    if (buffer.Length + read > MaximumResponseBytes)
                        throw ResponseTooLarge();

                    buffer.Write(chunk, 0, read);
                }

                try
                {
                    return StrictUtf8.GetString(buffer.ToArray());
                }
                catch (DecoderFallbackException)
                {
                    throw new CorrectionProviderException(
                        ProviderFailureKind.InvalidResponse,
                        "OpenAI devolvio una respuesta con codificación no válida.");
                }
            }
        }

        private static CorrectionProviderException ResponseTooLarge()
        {
            return new CorrectionProviderException(
                ProviderFailureKind.InvalidResponse,
                "OpenAI devolvio una respuesta demasiado extensa.");
        }

        private static CorrectionProviderException CreateHttpFailure(
            HttpStatusCode statusCode)
        {
            int code = (int)statusCode;

            if (statusCode == HttpStatusCode.Unauthorized ||
                statusCode == HttpStatusCode.Forbidden)
            {
                return new CorrectionProviderException(
                    ProviderFailureKind.Authentication,
                    "OpenAI rechazo la clave API. Verifique OPENAI_API_KEY.");
            }

            if (code == 408 || code == 429 || code >= 500)
            {
                return new CorrectionProviderException(
                    ProviderFailureKind.Unavailable,
                    "OpenAI no esta disponible temporalmente (HTTP " + code + ").");
            }

            return new CorrectionProviderException(
                ProviderFailureKind.InvalidResponse,
                "OpenAI rechazo la solicitud (HTTP " + code + ").");
        }
    }
}
