using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using CivilSpellAI.Application;
using CivilSpellAI.Domain;

namespace CivilSpellAI.UI
{
    public partial class BatchPreparationWindow : Window
    {
        private readonly BatchReviewCoordinator coordinator;
        private readonly IList<CorrectionRequest> requests;
        private readonly CancellationTokenSource cancellation;
        private readonly BatchPreparationViewModel viewModel;
        private bool isRunning;
        private bool allowClose;

        public BatchPreparationWindow(
            BatchReviewCoordinator coordinator,
            IList<CorrectionRequest> requests)
        {
            if (coordinator == null)
                throw new ArgumentNullException("coordinator");

            if (requests == null)
                throw new ArgumentNullException("requests");

            InitializeComponent();
            this.coordinator = coordinator;
            this.requests = requests;
            cancellation = new CancellationTokenSource();
            viewModel = new BatchPreparationViewModel(requests.Count);
            DataContext = viewModel;
            Loaded += WindowLoaded;
        }

        public BatchReviewResult Result { get; private set; }

        public bool WasCancelled { get; private set; }

        public Exception Failure { get; private set; }

        private async void WindowLoaded(object sender, RoutedEventArgs eventArgs)
        {
            Loaded -= WindowLoaded;
            isRunning = true;
            IProgress<BatchReviewProgress> progress =
                new Progress<BatchReviewProgress>(viewModel.Report);

            try
            {
                Result = await coordinator.PrepareAsync(
                    requests,
                    cancellation.Token,
                    progress);
            }
            catch (OperationCanceledException)
            {
                WasCancelled = true;
            }
            catch (Exception exception)
            {
                Failure = exception;
            }
            finally
            {
                isRunning = false;
                allowClose = true;
                Close();
            }
        }

        private void CancelClick(object sender, RoutedEventArgs eventArgs)
        {
            CancelAnalysis();
        }

        protected override void OnClosing(CancelEventArgs eventArgs)
        {
            if (isRunning && !allowClose)
            {
                eventArgs.Cancel = true;
                CancelAnalysis();
            }

            base.OnClosing(eventArgs);
        }

        protected override void OnClosed(EventArgs eventArgs)
        {
            cancellation.Dispose();
            base.OnClosed(eventArgs);
        }

        private void CancelAnalysis()
        {
            if (!isRunning || cancellation.IsCancellationRequested)
                return;

            WasCancelled = true;
            viewModel.BeginCancel();
            cancellation.Cancel();
        }
    }
}
