using System;
using System.Collections.Generic;

namespace CivilSpellAI.Domain
{
    public sealed class BatchReviewEntry
    {
        public BatchReviewEntry(
            int sourceIndex,
            CorrectionRequest request,
            ReviewSession session)
        {
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException("sourceIndex");

            if (request == null)
                throw new ArgumentNullException("request");

            if (session == null)
                throw new ArgumentNullException("session");

            if (!object.ReferenceEquals(session.Request, request))
            {
                throw new ArgumentException(
                    "La sesión debe corresponder a la solicitud del lote.",
                    "session");
            }

            SourceIndex = sourceIndex;
            Request = request;
            Session = session;
        }

        public int SourceIndex { get; private set; }

        public CorrectionRequest Request { get; private set; }

        public ReviewSession Session { get; private set; }
    }

    public sealed class BatchReviewResult
    {
        public BatchReviewResult(
            int scannedCount,
            IEnumerable<BatchReviewEntry> entries,
            IEnumerable<ProviderFailure> failures)
            : this(scannedCount, entries, failures, -1)
        {
        }

        public BatchReviewResult(
            int scannedCount,
            IEnumerable<BatchReviewEntry> entries,
            IEnumerable<ProviderFailure> failures,
            int totalFailureCount)
        {
            if (scannedCount < 0)
                throw new ArgumentOutOfRangeException("scannedCount");

            List<ProviderFailure> copiedFailures = CopyNonNull(failures);

            if (totalFailureCount < 0)
                totalFailureCount = copiedFailures.Count;

            if (totalFailureCount < copiedFailures.Count)
                throw new ArgumentOutOfRangeException("totalFailureCount");

            if (totalFailureCount > 0 && copiedFailures.Count == 0)
            {
                throw new ArgumentException(
                    "Un lote con fallos debe conservar al menos un detalle seguro.",
                    "failures");
            }

            ScannedCount = scannedCount;
            Entries = CopyNonNull(entries).AsReadOnly();
            HashSet<int> sourceIndexes = new HashSet<int>();

            foreach (BatchReviewEntry entry in Entries)
            {
                if (entry.SourceIndex >= scannedCount ||
                    !sourceIndexes.Add(entry.SourceIndex))
                {
                    throw new ArgumentException(
                        "Las filas del lote deben tener índices únicos dentro del escaneo.",
                        "entries");
                }
            }

            Failures = copiedFailures.AsReadOnly();
            FailureCount = totalFailureCount;
        }

        public int ScannedCount { get; private set; }

        public IList<BatchReviewEntry> Entries { get; private set; }

        public IList<ProviderFailure> Failures { get; private set; }

        public int FailureCount { get; private set; }

        private static List<T> CopyNonNull<T>(IEnumerable<T> values)
            where T : class
        {
            List<T> copied = new List<T>();

            foreach (T value in values ?? new T[0])
            {
                if (value != null)
                    copied.Add(value);
            }

            return copied;
        }
    }
}
