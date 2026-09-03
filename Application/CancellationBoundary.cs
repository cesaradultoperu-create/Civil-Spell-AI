using System;
using System.Threading;
using System.Threading.Tasks;

namespace CivilSpellAI.Application
{
    internal static class CancellationBoundary
    {
        public static async Task<T> AwaitAsync<T>(
            Task<T> operation,
            CancellationToken cancellationToken)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");

            cancellationToken.ThrowIfCancellationRequested();

            if (!cancellationToken.CanBeCanceled)
                return await operation.ConfigureAwait(false);

            TaskCompletionSource<bool> cancellation =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(
                delegate { cancellation.TrySetResult(true); }))
            {
                Task completed = await Task
                    .WhenAny(operation, cancellation.Task)
                    .ConfigureAwait(false);

                if (!ReferenceEquals(completed, operation))
                {
                    ObserveFailure(operation);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                T result = await operation.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }
        }

        private static void ObserveFailure(Task operation)
        {
            operation.ContinueWith(
                completed =>
                {
                    AggregateException ignored = completed.Exception;
                    GC.KeepAlive(ignored);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously |
                    TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
    }
}
