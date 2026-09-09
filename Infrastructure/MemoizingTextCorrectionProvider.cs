using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Infrastructure
{
    /// <summary>
    /// Reuses an in-flight or completed provider result for equivalent text
    /// during one command execution. The cache is intentionally in memory only.
    /// </summary>
    public sealed class MemoizingTextCorrectionProvider : ITextCorrectionProvider
    {
        private readonly ITextCorrectionProvider inner;
        private readonly object sync = new object();
        private readonly Dictionary<RequestKey, Task<IReadOnlyList<CorrectionProposal>>>
            requests = new Dictionary<RequestKey, Task<IReadOnlyList<CorrectionProposal>>>();

        public MemoizingTextCorrectionProvider(ITextCorrectionProvider inner)
        {
            if (inner == null)
                throw new ArgumentNullException("inner");

            this.inner = inner;
        }

        public string Name
        {
            get { return inner.Name; }
        }

        public async Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
            CorrectionRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            cancellationToken.ThrowIfCancellationRequested();
            RequestKey key = new RequestKey(
                request.Text,
                request.Language,
                request.MaximumAlternatives);
            Task<IReadOnlyList<CorrectionProposal>> sharedTask;

            lock (sync)
            {
                if (!requests.TryGetValue(key, out sharedTask))
                {
                    sharedTask = inner.ProposeAsync(request, cancellationToken);

                    if (sharedTask == null)
                    {
                        throw new InvalidOperationException(
                            "El proveedor devolvio una operacion nula.");
                    }

                    requests.Add(key, sharedTask);
                }
            }

            IReadOnlyList<CorrectionProposal> result =
                await sharedTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        private sealed class RequestKey : IEquatable<RequestKey>
        {
            public RequestKey(
                string text,
                ReviewLanguage language,
                int maximumAlternatives)
            {
                Text = text ?? string.Empty;
                Language = language;
                MaximumAlternatives = maximumAlternatives;
            }

            private string Text { get; set; }

            private ReviewLanguage Language { get; set; }

            private int MaximumAlternatives { get; set; }

            public bool Equals(RequestKey other)
            {
                return other != null &&
                    Language == other.Language &&
                    MaximumAlternatives == other.MaximumAlternatives &&
                    string.Equals(Text, other.Text, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as RequestKey);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)Language;
                    hash = (hash * 397) ^ MaximumAlternatives;
                    hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Text);
                    return hash;
                }
            }
        }
    }
}
