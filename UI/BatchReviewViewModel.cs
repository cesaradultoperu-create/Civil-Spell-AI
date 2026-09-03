using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CivilSpellAI.Domain;

namespace CivilSpellAI.UI
{
    public sealed class BatchReviewItemViewModel : INotifyPropertyChanged
    {
        private readonly Action selectionChanged;
        private bool isSelected;
        private ProposalReviewItemViewModel selectedAlternative;

        public BatchReviewItemViewModel(
            BatchReviewEntry entry,
            Action selectionChanged)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            this.selectionChanged = selectionChanged;
            Entry = entry;
            AvailableAlternatives = new ObservableCollection<ProposalReviewItemViewModel>();
            int number = 1;

            foreach (ValidatedCorrectionProposal proposal in entry.Session.Proposals)
            {
                AvailableAlternatives.Add(
                    new ProposalReviewItemViewModel(proposal, number++));
            }

            ValidatedCorrectionProposal selected = SelectBestProposal(
                entry.Session.Proposals);
            selectedAlternative = AvailableAlternatives.FirstOrDefault(item =>
                selected != null && ReferenceEquals(item.Proposal, selected.Proposal));
            OriginalText = entry.Request.Text;
            LocationDisplay = entry.Request.LocationName;
            HandleDisplay = string.Format(
                "{0} · Handle {1}",
                entry.Request.Snapshot.EntityType,
                entry.Request.Snapshot.ObjectHandle);

            isSelected = selected != null && selected.CanApply;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BatchReviewEntry Entry { get; private set; }

        public ObservableCollection<ProposalReviewItemViewModel> AvailableAlternatives
        {
            get;
            private set;
        }

        public ProposalReviewItemViewModel SelectedAlternative
        {
            get { return selectedAlternative; }
            set
            {
                if (ReferenceEquals(selectedAlternative, value))
                    return;

                selectedAlternative = value;

                if (!CanApply)
                    isSelected = false;

                RaisePropertyChanged("SelectedAlternative");
                RaisePropertyChanged("Proposal");
                RaisePropertyChanged("ProposedText");
                RaisePropertyChanged("DiffText");
                RaisePropertyChanged("ValidationText");
                RaisePropertyChanged("SourceDisplay");
                RaisePropertyChanged("StatusDisplay");
                RaisePropertyChanged("CanApply");
                RaisePropertyChanged("IsSelected");

                if (selectionChanged != null)
                    selectionChanged();
            }
        }

        public CorrectionProposal Proposal
        {
            get { return SelectedAlternative == null ? null : SelectedAlternative.Proposal; }
        }

        public string HandleDisplay { get; private set; }

        public string OriginalText { get; private set; }

        public string LocationDisplay { get; private set; }

        public string ProposedText
        {
            get
            {
                return SelectedAlternative == null
                    ? OriginalText
                    : SelectedAlternative.ProposedText;
            }
        }

        public string DiffText
        {
            get
            {
                return SelectedAlternative == null
                    ? "Sin corrección aplicable."
                    : SelectedAlternative.DiffText;
            }
        }

        public string ValidationText
        {
            get
            {
                return SelectedAlternative == null
                    ? "Todas las propuestas quedaron bloqueadas."
                    : SelectedAlternative.ValidationText;
            }
        }

        public string SourceDisplay
        {
            get
            {
                return SelectedAlternative == null
                    ? "Bloqueada"
                    : GetSourceDisplay(SelectedAlternative.Proposal.Source);
            }
        }

        public string StatusDisplay
        {
            get
            {
                if (!CanApply)
                    return "Bloqueada";

                return IsSelected ? "Seleccionada" : "Excluida";
            }
        }

        public bool CanApply
        {
            get { return SelectedAlternative != null && SelectedAlternative.CanApply; }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                bool normalized = value && CanApply;

                if (isSelected == normalized)
                    return;

                isSelected = normalized;
                RaisePropertyChanged("IsSelected");
                RaisePropertyChanged("StatusDisplay");

                if (selectionChanged != null)
                    selectionChanged();
            }
        }

        public bool Matches(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
                return true;

            string filter = filterText.Trim();
            return Contains(HandleDisplay, filter) ||
                Contains(LocationDisplay, filter) ||
                Contains(OriginalText, filter) ||
                Contains(ProposedText, filter) ||
                Contains(ValidationText, filter) ||
                Contains(SourceDisplay, filter) ||
                Contains(StatusDisplay, filter);
        }

        private static ValidatedCorrectionProposal SelectBestProposal(
            IEnumerable<ValidatedCorrectionProposal> proposals)
        {
            ValidatedCorrectionProposal best = proposals
                .Where(proposal => proposal.CanApply)
                .OrderByDescending(proposal => proposal.Validation.Changes.Count)
                .ThenBy(proposal => proposal.Proposal.Source == ProposalSource.LocalRules ? 0 : 1)
                .FirstOrDefault();

            return best ?? proposals.FirstOrDefault();
        }

        private static string GetSourceDisplay(ProposalSource source)
        {
            switch (source)
            {
                case ProposalSource.LocalRules:
                    return "Reglas locales";
                case ProposalSource.ArtificialIntelligence:
                    return "IA";
                case ProposalSource.LearnedPreference:
                    return "Preferencia aprendida";
                case ProposalSource.ManualEdit:
                    return "Edición manual";
                default:
                    return source.ToString();
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public sealed class BatchReviewViewModel : INotifyPropertyChanged
    {
        private BatchReviewItemViewModel focusedItem;
        private string filterText;
        private bool showSelectedOnly;
        private string selectedLocation;
        private bool rememberSelectedDecisions;
        private const string AllLocations = "Todos los espacios";

        public BatchReviewViewModel(BatchReviewResult result)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            Items = new ObservableCollection<BatchReviewItemViewModel>();
            VisibleItems = new ObservableCollection<BatchReviewItemViewModel>();

            foreach (BatchReviewEntry entry in result.Entries)
            {
                BatchReviewItemViewModel item =
                    new BatchReviewItemViewModel(entry, OnSelectionChanged);
                Items.Add(item);
                VisibleItems.Add(item);
            }

            FocusedItem = Items.FirstOrDefault();
            List<string> locations = Items
                .Select(item => item.LocationDisplay)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
            locations.Insert(0, AllLocations);
            LocationNames = locations.AsReadOnly();
            selectedLocation = AllLocations;
            Summary = string.Format(
                "{0} texto(s) revisado(s); {1} con propuestas; {2} fallo(s) de proveedor.",
                result.ScannedCount,
                Items.Count,
                result.FailureCount);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BatchReviewItemViewModel> Items { get; private set; }

        public ObservableCollection<BatchReviewItemViewModel> VisibleItems { get; private set; }

        public string Summary { get; private set; }

        public IList<string> LocationNames { get; private set; }

        public string SelectedLocation
        {
            get { return selectedLocation; }
            set
            {
                string normalized = string.IsNullOrWhiteSpace(value)
                    ? AllLocations
                    : value;

                if (string.Equals(
                    selectedLocation,
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
                    return;

                selectedLocation = normalized;
                RaisePropertyChanged("SelectedLocation");
                RefreshFilter();
            }
        }

        public string FilterText
        {
            get { return filterText; }
            set
            {
                string normalized = value ?? string.Empty;

                if (string.Equals(filterText, normalized, StringComparison.Ordinal))
                    return;

                filterText = normalized;
                RaisePropertyChanged("FilterText");
                RefreshFilter();
            }
        }

        public bool ShowSelectedOnly
        {
            get { return showSelectedOnly; }
            set
            {
                if (showSelectedOnly == value)
                    return;

                showSelectedOnly = value;
                RaisePropertyChanged("ShowSelectedOnly");
                RefreshFilter();
            }
        }

        public string VisibleSummary
        {
            get
            {
                return string.Format(
                    "Mostrando {0} de {1} fila(s).",
                    VisibleItems.Count,
                    Items.Count);
            }
        }

        public BatchReviewItemViewModel FocusedItem
        {
            get { return focusedItem; }
            set
            {
                if (ReferenceEquals(focusedItem, value))
                    return;

                focusedItem = value;
                RaisePropertyChanged("FocusedItem");
            }
        }

        public int SelectedCount
        {
            get { return Items.Count(item => item.IsSelected && item.CanApply); }
        }

        public bool CanApply
        {
            get { return SelectedCount > 0; }
        }

        public string ApplyButtonText
        {
            get { return string.Format("_Aplicar seleccionados ({0})", SelectedCount); }
        }

        public bool RememberSelectedDecisions
        {
            get { return rememberSelectedDecisions; }
            set
            {
                if (rememberSelectedDecisions == value)
                    return;

                rememberSelectedDecisions = value;
                RaisePropertyChanged("RememberSelectedDecisions");
            }
        }

        public IList<BatchReviewItemViewModel> GetSelectedItems()
        {
            return Items
                .Where(item => item.IsSelected && item.CanApply)
                .ToList()
                .AsReadOnly();
        }

        public void SelectAllVisible()
        {
            foreach (BatchReviewItemViewModel item in VisibleItems.ToList())
            {
                if (item.CanApply)
                    item.IsSelected = true;
            }
        }

        public void ExcludeAllVisible()
        {
            foreach (BatchReviewItemViewModel item in VisibleItems.ToList())
                item.IsSelected = false;
        }

        private void OnSelectionChanged()
        {
            if (ShowSelectedOnly)
                RefreshFilter();

            RaisePropertyChanged("SelectedCount");
            RaisePropertyChanged("CanApply");
            RaisePropertyChanged("ApplyButtonText");
        }

        private void RefreshFilter()
        {
            BatchReviewItemViewModel previousFocus = FocusedItem;
            VisibleItems.Clear();

            foreach (BatchReviewItemViewModel item in Items)
            {
                if (item.Matches(FilterText) &&
                    (string.Equals(
                        SelectedLocation,
                        AllLocations,
                        StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(
                        item.LocationDisplay,
                        SelectedLocation,
                        StringComparison.OrdinalIgnoreCase)) &&
                    (!ShowSelectedOnly || item.IsSelected))
                {
                    VisibleItems.Add(item);
                }
            }

            if (previousFocus == null || !VisibleItems.Contains(previousFocus))
                FocusedItem = VisibleItems.FirstOrDefault();

            RaisePropertyChanged("VisibleSummary");
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
