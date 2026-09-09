using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Infrastructure
{
    /// <summary>
    /// Calls a LanguageTool server running on this computer. Remote hosts are
    /// rejected so selecting local grammar can never send drawing text online.
    /// </summary>
    public sealed class LanguageToolLoopbackTransport : ILanguageToolTransport
    {
        public static readonly Uri DefaultEndpoint =
            new Uri("http://127.0.0.1:8081/v2/check");
        private static readonly HttpClient SharedClient = CreateSharedClient();
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private const int MaximumResponseBytes = 1000000;
        private readonly Uri endpoint;

        public LanguageToolLoopbackTransport()
            : this(DefaultEndpoint)
        {
        }

        public LanguageToolLoopbackTransport(Uri endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");

            if (!endpoint.IsAbsoluteUri || !endpoint.IsLoopback ||
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "LanguageTool local solo admite una dirección HTTP de esta computadora.",
                    "endpoint");
            }

            this.endpoint = endpoint;
        }

        internal static HttpClientHandler CreateLoopbackOnlyHandler()
        {
            return new HttpClientHandler
            {
                // A loopback response must never redirect drawing text to a
                // different host. Proxies and cookies are unnecessary for the
                // local LanguageTool service and are disabled as well.
                AllowAutoRedirect = false,
                UseProxy = false,
                UseCookies = false
            };
        }

        public async Task<string> CheckAsync(
            string text,
            string languageCode,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("timeout");

            using (CancellationTokenSource timeoutCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                timeoutCancellation.CancelAfter(timeout);
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("text", text),
                    new KeyValuePair<string, string>("language", languageCode ?? "es")
                });

                try
                {
                    using (HttpResponseMessage response = await SharedClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        timeoutCancellation.Token).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new CorrectionProviderException(
                                ProviderFailureKind.Unavailable,
                                "LanguageTool local no está disponible.");
                        }

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
                        "LanguageTool local agotó el tiempo de espera.");
                }
                catch (HttpRequestException)
                {
                    throw new CorrectionProviderException(
                        ProviderFailureKind.Unavailable,
                        "LanguageTool local no está iniciado.");
                }
                catch (IOException)
                {
                    throw new CorrectionProviderException(
                        ProviderFailureKind.Unavailable,
                        "La conexión local con LanguageTool se interrumpió.");
                }
            }
        }

        internal static async Task<string> ReadBoundedResponseAsync(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            if (content.Headers.ContentLength.HasValue &&
                content.Headers.ContentLength.Value > MaximumResponseBytes)
            {
                throw ResponseTooLarge();
            }

            using (Stream source = await content.ReadAsStreamAsync().ConfigureAwait(false))
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
                        "LanguageTool local devolvió texto no válido.");
                }
            }
        }

        private static CorrectionProviderException ResponseTooLarge()
        {
            return new CorrectionProviderException(
                ProviderFailureKind.InvalidResponse,
                "LanguageTool local devolvió una respuesta demasiado extensa.");
        }

        private static HttpClient CreateSharedClient()
        {
            return new HttpClient(CreateLoopbackOnlyHandler(), true)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }
    }
}
