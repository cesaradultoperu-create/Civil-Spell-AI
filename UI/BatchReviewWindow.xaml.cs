using System.Collections.Generic;
using System.Windows;
using CivilSpellAI.Domain;

namespace CivilSpellAI.UI
{
    public partial class BatchReviewWindow : Window
    {
        private readonly BatchReviewViewModel viewModel;

        public BatchReviewWindow(BatchReviewResult result)
        {
            InitializeComponent();
            viewModel = new BatchReviewViewModel(result);
            DataContext = viewModel;
        }

        public bool ApplyWasRequested { get; private set; }

        public IList<BatchReviewItemViewModel> SelectedItems
        {
            get { return viewModel.GetSelectedItems(); }
        }

        public bool RememberSelectedDecisions
        {
            get { return viewModel.RememberSelectedDecisions; }
        }

        private void ApplyClick(object sender, RoutedEventArgs eventArgs)
        {
            if (!viewModel.CanApply)
                return;

            ApplyWasRequested = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs eventArgs)
        {
            Close();
        }

        private void SelectVisibleClick(object sender, RoutedEventArgs eventArgs)
        {
            viewModel.SelectAllVisible();
        }

        private void ExcludeVisibleClick(object sender, RoutedEventArgs eventArgs)
        {
            viewModel.ExcludeAllVisible();
        }
    }
}
