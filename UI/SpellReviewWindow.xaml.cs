using System.ComponentModel;
using System.Windows;
using CivilSpellAI.Domain;

namespace CivilSpellAI.UI
{
    public partial class SpellReviewWindow : Window
    {
        private readonly SpellReviewViewModel viewModel;
        private readonly IReviewCoordinator additionalCoordinator;

        public SpellReviewWindow(ReviewSession session)
            : this(session, null)
        {
        }

        public SpellReviewWindow(
            ReviewSession session,
            IReviewCoordinator additionalCoordinator)
        {
            InitializeComponent();
            viewModel = new SpellReviewViewModel(session);
            this.additionalCoordinator = additionalCoordinator;
            DataContext = viewModel;
            Loaded += WindowLoaded;
        }

        public ReviewDecision Decision
        {
            get { return viewModel.Decision ?? ReviewDecision.Cancel(); }
        }

        public ProviderFailure LastProviderFailure
        {
            get { return viewModel.LastProviderFailure; }
        }

        public bool HasBlockedProposals
        {
            get { return viewModel.HasBlockedProposals; }
        }

        private void KeepOriginalClick(object sender, RoutedEventArgs eventArgs)
        {
            viewModel.KeepOriginal();
            Close();
        }

        private void ApplyClick(object sender, RoutedEventArgs eventArgs)
        {
            if (viewModel.ApplySelected())
                Close();
        }

        private void CancelClick(object sender, RoutedEventArgs eventArgs)
        {
            viewModel.Cancel();
            Close();
        }

        private async void WindowLoaded(object sender, RoutedEventArgs eventArgs)
        {
            Loaded -= WindowLoaded;

            if (additionalCoordinator != null)
                await viewModel.LoadAdditionalProposalsAsync(additionalCoordinator);
        }

        private async void RetryClick(object sender, RoutedEventArgs eventArgs)
        {
            AlternativesList.Focus();
            await viewModel.RetryProviderAsync();

            if (!IsVisible)
                return;

            if (viewModel.CanRetry)
                RetryButton.Focus();
            else
                AlternativesList.Focus();
        }

        protected override void OnClosing(CancelEventArgs eventArgs)
        {
            viewModel.CancelPendingProvider();

            if (viewModel.Decision == null)
                viewModel.Cancel();

            base.OnClosing(eventArgs);
        }
    }
}
