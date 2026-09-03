using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Application;
using CivilSpellAI.Domain;

namespace CivilSpellAI.UI
{
    public sealed class ProposalReviewItemViewModel
    {
        private readonly ValidatedCorrectionProposal validatedProposal;

        public ProposalReviewItemViewModel(
            ValidatedCorrectionProposal validatedProposal,
            int number)
        {
            if (validatedProposal == null)
                throw new ArgumentNullException("validatedProposal");

            this.validatedProposal = validatedProposal;
            Title = string.Format(
                "Alternativa {0} · {1}",
                number,
                GetSourceName(validatedProposal.Proposal.Source));
            ProposedText = validatedProposal.Proposal.ProposedText;
            Explanation = validatedProposal.Proposal.Explanation;
            DiffText = BuildDiffText(validatedProposal.Proposal.Changes);
            ValidationText = BuildValidation(validatedProposal);
        }

        public string Title { get; private set; }

        public string ProposedText { get; private set; }

        public string Explanation { get; private set; }

        public string DiffText { get; private set; }

        public string ValidationText { get; private set; }

        public bool CanApply
        {
            get { return validatedProposal.CanApply; }
        }

        public CorrectionProposal Proposal
        {
            get { return validatedProposal.Proposal; }
        }

        internal static string BuildDiffText(IEnumerable<TextDifference> changes)
        {
            StringBuilder builder = new StringBuilder();

            foreach (TextDifference change in changes)
            {
                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append('“');
                builder.Append(DisplayPart(change.OriginalText));
                builder.Append("”  →  “");
                builder.Append(DisplayPart(change.ProposedText));
                builder.Append('”');
            }

            return builder.Length == 0 ? "Sin cambios." : builder.ToString();
        }

        private static string BuildValidation(
            ValidatedCorrectionProposal validatedProposal)
        {
            if (validatedProposal.Validation.Issues.Count == 0)
                return "Validación técnica aprobada.";

            StringBuilder builder = new StringBuilder("Propuesta bloqueada:");

            foreach (ValidationIssue issue in validatedProposal.Validation.Issues)
            {
                builder.AppendLine();
                builder.Append("• ");
                builder.Append(issue.Message);
            }

            return builder.ToString();
        }

        private static string DisplayPart(string value)
        {
            return string.IsNullOrEmpty(value) ? "∅" : value;
        }

        private static string GetSourceName(ProposalSource source)
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
    }

    public sealed class SpellReviewViewModel : INotifyPropertyChanged
    {
        private ProposalReviewItemViewModel selectedProposal;
        private CancellationTokenSource providerCancellation;
        private IReviewCoordinator additionalCoordinator;
        private bool isProviderLoading;
        private bool canRetry;
        private string providerStatusDisplay;
        private bool isManualEditEnabled;
        private string manualText;
        private ProposalValidationResult manualValidation;
        private bool rememberPreference;

        public SpellReviewViewModel(ReviewSession session)
        {
            if (session == null)
                throw new ArgumentNullException("session");

            Session = session;
            OriginalText = session.Request.Text;
            manualText = OriginalText;
            LanguageDisplay = GetLanguageDisplayName(GetDetectedLanguage(session));

            Proposals = new ObservableCollection<ProposalReviewItemViewModel>();
            int number = 1;

            foreach (ValidatedCorrectionProposal proposal in session.Proposals)
                Proposals.Add(new ProposalReviewItemViewModel(proposal, number++));

            providerStatusDisplay = "Proveedor de IA desactivado.";

            foreach (ProposalReviewItemViewModel proposal in Proposals)
            {
                if (proposal.CanApply)
                {
                    selectedProposal = proposal;
                    break;
                }
            }

            if (selectedProposal == null && Proposals.Count > 0)
                selectedProposal = Proposals[0];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ReviewSession Session { get; private set; }

        public string OriginalText { get; private set; }

        public string LanguageDisplay { get; private set; }

        public ObservableCollection<ProposalReviewItemViewModel> Proposals { get; private set; }

        public bool HasBlockedProposals
        {
            get
            {
                foreach (ProposalReviewItemViewModel proposal in Proposals)
                {
                    if (!proposal.CanApply)
                        return true;
                }

                return false;
            }
        }

        public ReviewDecision Decision { get; private set; }

        public ProviderFailure LastProviderFailure { get; private set; }

        public ProposalReviewItemViewModel SelectedProposal
        {
            get { return selectedProposal; }
            set
            {
                if (ReferenceEquals(selectedProposal, value))
                    return;

                selectedProposal = value;
                RaisePropertyChanged("SelectedProposal");
                RaisePropertyChanged("ResultText");
                RaisePropertyChanged("CurrentDiffText");
                RaisePropertyChanged("CurrentValidationText");
                RaisePropertyChanged("CanApply");
            }
        }

        public bool IsManualEditEnabled
        {
            get { return isManualEditEnabled; }
            set
            {
                if (isManualEditEnabled == value)
                    return;

                isManualEditEnabled = value;

                if (value)
                {
                    manualText = SelectedProposal == null
                        ? OriginalText
                        : SelectedProposal.ProposedText;
                    ValidateManualText();
                }
                else
                {
                    manualValidation = null;
                }

                RaisePropertyChanged("IsManualEditEnabled");
                RaisePropertyChanged("IsResultReadOnly");
                RaisePropertyChanged("ResultText");
                RaisePropertyChanged("CurrentDiffText");
                RaisePropertyChanged("CurrentValidationText");
                RaisePropertyChanged("CanApply");
            }
        }

        public bool IsResultReadOnly
        {
            get { return !IsManualEditEnabled; }
        }

        public string ResultText
        {
            get
            {
                if (IsManualEditEnabled)
                    return manualText;

                return SelectedProposal == null
                    ? OriginalText
                    : SelectedProposal.ProposedText;
            }
            set
            {
                if (!IsManualEditEnabled)
                    return;

                string normalized = value ?? string.Empty;

                if (string.Equals(manualText, normalized, StringComparison.Ordinal))
                    return;

                manualText = normalized;
                ValidateManualText();
                RaisePropertyChanged("ResultText");
                RaisePropertyChanged("CurrentDiffText");
                RaisePropertyChanged("CurrentValidationText");
                RaisePropertyChanged("CanApply");
            }
        }

        public string CurrentDiffText
        {
            get
            {
                if (IsManualEditEnabled)
                {
                    return manualValidation == null
                        ? "Sin cambios."
                        : ProposalReviewItemViewModel.BuildDiffText(
                            manualValidation.Changes);
                }

                return SelectedProposal == null
                    ? "Sin cambios."
                    : SelectedProposal.DiffText;
            }
        }

        public string CurrentValidationText
        {
            get
            {
                if (IsManualEditEnabled)
                    return BuildManualValidationText(manualValidation);

                return SelectedProposal == null
                    ? "No existe una alternativa seleccionada."
                    : SelectedProposal.ValidationText;
            }
        }

        public bool CanApply
        {
            get
            {
                if (IsManualEditEnabled)
                    return manualValidation != null && manualValidation.CanApply;

                return SelectedProposal != null && SelectedProposal.CanApply;
            }
        }

        public bool RememberPreference
        {
            get { return rememberPreference; }
            set
            {
                if (rememberPreference == value)
                    return;

                rememberPreference = value;
                RaisePropertyChanged("RememberPreference");
            }
        }

        public bool IsProviderLoading
        {
            get { return isProviderLoading; }
            private set
            {
                if (isProviderLoading == value)
                    return;

                isProviderLoading = value;
                RaisePropertyChanged("IsProviderLoading");
            }
        }

        public bool CanRetry
        {
            get { return canRetry; }
            private set
            {
                if (canRetry == value)
                    return;

                canRetry = value;
                RaisePropertyChanged("CanRetry");
            }
        }

        public string ProviderStatusDisplay
        {
            get { return providerStatusDisplay; }
            private set
            {
                if (string.Equals(providerStatusDisplay, value, StringComparison.Ordinal))
                    return;

                providerStatusDisplay = value;
                RaisePropertyChanged("ProviderStatusDisplay");
            }
        }

        public async Task LoadAdditionalProposalsAsync(
            IReviewCoordinator coordinator)
        {
            if (coordinator == null)
                throw new ArgumentNullException("coordinator");

            if (Decision != null)
                return;

            additionalCoordinator = coordinator;
            CancelPendingProvider();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            providerCancellation = cancellation;
            IsProviderLoading = true;
            CanRetry = false;
            ProviderStatusDisplay = "Proveedor de IA: cargando alternativas…";

            try
            {
                ReviewSession additional = await CancellationBoundary.AwaitAsync(
                    coordinator.PrepareAsync(
                        Session.Request,
                        cancellation.Token),
                    cancellation.Token);

                if (!IsCurrentRequest(cancellation) || Decision != null)
                    return;

                int added = MergeProposals(additional.Proposals);

                if (additional.Failures.Count > 0)
                {
                    ProviderFailure failure = additional.Failures[0];
                    LastProviderFailure = failure;
                    ProviderStatusDisplay = BuildProviderFailureStatus(failure);
                    CanRetry = true;
                }
                else if (added > 0)
                {
                    LastProviderFailure = null;
                    ProviderStatusDisplay = string.Format(
                        "Proveedor de IA: {0} alternativa(s) añadida(s).",
                        added);
                }
                else
                {
                    LastProviderFailure = null;
                    ProviderStatusDisplay =
                        "Proveedor de IA: no produjo alternativas nuevas.";
                }
            }
            catch (OperationCanceledException)
            {
                if (IsCurrentRequest(cancellation) && Decision == null)
                    ProviderStatusDisplay = "Solicitud de IA cancelada.";
            }
            catch (Exception)
            {
                if (IsCurrentRequest(cancellation) && Decision == null)
                {
                    LastProviderFailure = new ProviderFailure(
                        "Proveedor de IA",
                        ProviderFailureKind.Unexpected,
                        "El proveedor no pudo completar la revisión.");
                    ProviderStatusDisplay =
                        "Proveedor de IA: error inesperado (GEN-001).";
                    CanRetry = true;
                }
            }
            finally
            {
                if (ReferenceEquals(providerCancellation, cancellation))
                {
                    providerCancellation = null;
                    IsProviderLoading = false;
                }

                cancellation.Dispose();
            }
        }

        public Task RetryProviderAsync()
        {
            if (additionalCoordinator == null || IsProviderLoading || Decision != null)
                return Task.FromResult(false);

            return LoadAdditionalProposalsAsync(additionalCoordinator);
        }

        public void CancelPendingProvider()
        {
            CancellationTokenSource cancellation = providerCancellation;

            if (cancellation != null && !cancellation.IsCancellationRequested)
                cancellation.Cancel();
        }

        public void KeepOriginal()
        {
            CancelPendingProvider();
            CanRetry = false;
            Decision = ReviewDecision.KeepOriginal(OriginalText);
        }

        public bool ApplySelected()
        {
            if (!CanApply)
                return false;

            CancelPendingProvider();
            CanRetry = false;
            Decision = IsManualEditEnabled
                ? ReviewDecision.Manual(manualText, RememberPreference)
                : ReviewDecision.Apply(
                    SelectedProposal.Proposal,
                    RememberPreference);
            return true;
        }

        public void Cancel()
        {
            CancelPendingProvider();
            CanRetry = false;
            Decision = ReviewDecision.Cancel();
        }

        private bool IsCurrentRequest(CancellationTokenSource cancellation)
        {
            return ReferenceEquals(providerCancellation, cancellation) &&
                !cancellation.IsCancellationRequested;
        }

        private void ValidateManualText()
        {
            CorrectionProposal proposal = new CorrectionProposal(
                manualText ?? string.Empty,
                ProposalSource.ManualEdit,
                Session.Request.Language,
                "Edición manual del usuario.",
                null,
                null);
            manualValidation = new TechnicalTokenValidator().Validate(
                Session.Request,
                proposal);
        }

        private static string BuildManualValidationText(
            ProposalValidationResult validation)
        {
            if (validation == null)
                return "Active la edición manual para validar el resultado.";

            if (validation.Issues.Count == 0)
                return "Edición manual aprobada por la validación técnica.";

            StringBuilder builder = new StringBuilder("Edición manual bloqueada:");

            foreach (ValidationIssue issue in validation.Issues)
            {
                builder.AppendLine();
                builder.Append("• ");
                builder.Append(issue.Message);
            }

            return builder.ToString();
        }

        private static string BuildProviderFailureStatus(
            ProviderFailure failure)
        {
            DiagnosticCode diagnosticCode =
                DiagnosticClassifier.FromProviderFailure(failure.Kind);
            string description;

            switch (diagnosticCode)
            {
                case DiagnosticCode.ConfigurationMissing:
                case DiagnosticCode.ConfigurationFailure:
                    description = "configuración incompleta";
                    break;
                case DiagnosticCode.AuthenticationRejected:
                    description = "credencial rechazada";
                    break;
                case DiagnosticCode.NetworkUnavailable:
                    description = "servicio no disponible";
                    break;
                case DiagnosticCode.Timeout:
                    description = "tiempo de espera agotado";
                    break;
                case DiagnosticCode.InvalidResponse:
                    description = "respuesta no válida";
                    break;
                default:
                    description = "error inesperado";
                    break;
            }

            return string.Format(
                "{0}: {1} ({2}).",
                failure.ProviderName,
                description,
                DiagnosticCatalog.GetCode(diagnosticCode));
        }

        private int MergeProposals(
            IEnumerable<ValidatedCorrectionProposal> additionalProposals)
        {
            HashSet<string> known = new HashSet<string>(StringComparer.Ordinal);

            foreach (ProposalReviewItemViewModel item in Proposals)
                known.Add(item.ProposedText);

            int added = 0;

            foreach (ValidatedCorrectionProposal proposal in additionalProposals)
            {
                if (known.Add(proposal.Proposal.ProposedText))
                {
                    ProposalReviewItemViewModel item = new ProposalReviewItemViewModel(
                        proposal,
                        Math.Min(
                            Proposals.Count + 1,
                            Session.Request.MaximumAlternatives));

                    if (Proposals.Count < Session.Request.MaximumAlternatives)
                    {
                        Proposals.Add(item);
                    }
                    else if (item.CanApply)
                    {
                        int blockedIndex = -1;

                        for (int index = Proposals.Count - 1; index >= 0; index--)
                        {
                            if (!Proposals[index].CanApply)
                            {
                                blockedIndex = index;
                                break;
                            }
                        }

                        if (blockedIndex < 0)
                            continue;

                        ProposalReviewItemViewModel removed =
                            Proposals[blockedIndex];
                        Proposals[blockedIndex] = item;

                        if (ReferenceEquals(SelectedProposal, removed))
                            SelectedProposal = item;
                    }
                    else
                    {
                        continue;
                    }

                    RaisePropertyChanged("HasBlockedProposals");

                    if ((SelectedProposal == null ||
                         !SelectedProposal.CanApply) &&
                        item.CanApply)
                    {
                        SelectedProposal = item;
                    }

                    added++;
                }
            }

            return added;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static ReviewLanguage GetDetectedLanguage(ReviewSession session)
        {
            foreach (ValidatedCorrectionProposal proposal in session.Proposals)
            {
                if (proposal.Proposal.Language != ReviewLanguage.Unknown)
                    return proposal.Proposal.Language;
            }

            return session.Request.Language;
        }

        private static string GetLanguageDisplayName(ReviewLanguage language)
        {
            switch (language)
            {
                case ReviewLanguage.Spanish:
                    return "Español";
                case ReviewLanguage.English:
                    return "Inglés";
                case ReviewLanguage.Mixed:
                    return "Mixto";
                default:
                    return "No identificado";
            }
        }
    }
}
