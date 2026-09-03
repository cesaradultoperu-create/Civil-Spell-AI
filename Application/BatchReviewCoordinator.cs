using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Application
{
    public sealed class BatchReviewProgress
    {
        public BatchReviewProgress(int completedCount, int totalCount)
        {
            if (totalCount < 0)
                throw new ArgumentOutOfRangeException("totalCount");

            if (completedCount < 0 || completedCount > totalCount)
                throw new ArgumentOutOfRangeException("completedCount");

            CompletedCount = completedCount;
            TotalCount = totalCount;
        }

        public int CompletedCount { get; private set; }

        public int TotalCount { get; private set; }
    }

    public sealed class BatchReviewCoordinator
    {
        private const int MaximumRetainedFailures = 100;
        private readonly IReviewCoordinator coordinator;
        private readonly int maximumConcurrency;

        public BatchReviewCoordinator(
            IReviewCoordinator coordinator,
            int maximumConcurrency)
        {
            if (coordinator == null)
                throw new ArgumentNullException("coordinator");

            if (maximumConcurrency < 1 || maximumConcurrency > 16)
                throw new ArgumentOutOfRangeException("maximumConcurrency");

            this.coordinator = coordinator;
            this.maximumConcurrency = maximumConcurrency;
        }

        public async Task<BatchReviewResult> PrepareAsync(
            IList<CorrectionRequest> requests,
            CancellationToken cancellationToken)
        {
            return await PrepareAsync(requests, cancellationToken, null)
                .ConfigureAwait(false);
        }

        public async Task<BatchReviewResult> PrepareAsync(
            IList<CorrectionRequest> requests,
            CancellationToken cancellationToken,
            IProgress<BatchReviewProgress> progress)
        {
            if (requests == null)
                throw new ArgumentNullException("requests");

            if (progress != null)
                progress.Report(new BatchReviewProgress(0, requests.Count));

            BatchWorkState state = new BatchWorkState();
            int workerCount = Math.Min(maximumConcurrency, requests.Count);
            Task[] workers = new Task[workerCount];

            for (int worker = 0; worker < workerCount; worker++)
            {
                workers[worker] = PrepareWorkerAsync(
                    requests,
                    state,
                    cancellationToken,
                    progress);
            }

            await Task.WhenAll(workers).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            List<BatchReviewEntry> entries = state.GetEntries()
                .OrderBy(entry => entry.SourceIndex)
                .ToList();

            return new BatchReviewResult(
                requests.Count,
                entries,
                state.GetFailures(),
                state.TotalFailureCount);
        }

        private async Task PrepareWorkerAsync(
            IList<CorrectionRequest> requests,
            BatchWorkState state,
            CancellationToken cancellationToken,
            IProgress<BatchReviewProgress> progress)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int index = Interlocked.Increment(ref state.NextIndex);

                if (index >= requests.Count)
                    return;

                CorrectionRequest request = requests[index];

                if (request == null)
                {
                    throw new ArgumentException(
                        "Una solicitud del lote es nula.",
                        "requests");
                }

                ReviewSession session = await CancellationBoundary.AwaitAsync(
                    coordinator.PrepareAsync(request, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
                state.AddFailures(session.Failures);

                if (session.Proposals.Count > 0)
                {
                    state.AddEntry(new BatchReviewEntry(
                        index,
                        request,
                        session));
                }
                int progressCount = Interlocked.Increment(
                    ref state.CompletedCount);

                if (progress != null)
                {
                    progress.Report(new BatchReviewProgress(
                        progressCount,
                        requests.Count));
                }
            }
        }

        private sealed class BatchWorkState
        {
            private readonly object entriesSync = new object();
            private readonly List<BatchReviewEntry> entries =
                new List<BatchReviewEntry>();
            private readonly object failuresSync = new object();
            private readonly List<ProviderFailure> failures =
                new List<ProviderFailure>();

            public BatchWorkState()
            {
                NextIndex = -1;
            }

            public int NextIndex;

            public int CompletedCount;

            public int TotalFailureCount { get; private set; }

            public void AddEntry(BatchReviewEntry entry)
            {
                if (entry == null)
                    return;

                lock (entriesSync)
                    entries.Add(entry);
            }

            public IList<BatchReviewEntry> GetEntries()
            {
                lock (entriesSync)
                    return new List<BatchReviewEntry>(entries).AsReadOnly();
            }

            public void AddFailures(IEnumerable<ProviderFailure> newFailures)
            {
                if (newFailures == null)
                    return;

                lock (failuresSync)
                {
                    foreach (ProviderFailure failure in newFailures)
                    {
                        if (failure == null)
                            continue;

                        if (TotalFailureCount < int.MaxValue)
                            TotalFailureCount++;

                        if (failures.Count < MaximumRetainedFailures)
                        {
                            failures.Add(failure);
                        }
                    }
                }
            }

            public IList<ProviderFailure> GetFailures()
            {
                lock (failuresSync)
                    return new List<ProviderFailure>(failures).AsReadOnly();
            }
        }
    }
}
