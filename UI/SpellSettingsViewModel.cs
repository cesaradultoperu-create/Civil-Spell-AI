using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CivilSpellAI.Infrastructure;

namespace CivilSpellAI.UI
{
    public sealed class LearningRecordItemViewModel : INotifyPropertyChanged
    {
        private bool isEnabled;

        public LearningRecordItemViewModel(LearningRecord record)
        {
            if (record == null)
                throw new ArgumentNullException("record");

            Id = record.Id;
            SourceText = record.SourceText;
            SelectedText = record.SelectedText;
            LanguageDisplay = record.Language.ToString();
            AcceptanceCount = record.AcceptanceCount;
            LastUsedUtc = record.LastUsedUtc;
            isEnabled = record.IsEnabled;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Id { get; private set; }

        public string SourceText { get; private set; }

        public string SelectedText { get; private set; }

        public string LanguageDisplay { get; private set; }

        public int AcceptanceCount { get; private set; }

        public string LastUsedUtc { get; private set; }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value)
                    return;

                isEnabled = value;
                PropertyChangedEventHandler handler = PropertyChanged;

                if (handler != null)
                    handler(this, new PropertyChangedEventArgs("IsEnabled"));
            }
        }

        public bool Matches(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            string candidate = filter.Trim();
            return Contains(SourceText, candidate) ||
                Contains(SelectedText, candidate) ||
                Contains(LanguageDisplay, candidate);
        }

        private static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public sealed class SpellSettingsViewModel : INotifyPropertyChanged
    {
        private readonly UserConfigurationStore settingsStore;
        private readonly PersonalGlossaryStore glossaryStore;
        private readonly LocalLearningStore learningStore;
        private readonly IOpenAiCredentialProvider credentials;
        private string learningFilterText;
        private LearningRecordItemViewModel selectedLearningRecord;

        public SpellSettingsViewModel(
            UserConfigurationStore settingsStore,
            PersonalGlossaryStore glossaryStore)
            : this(
                settingsStore,
                glossaryStore,
                new EnvironmentOpenAiCredentialProvider())
        {
        }

        public SpellSettingsViewModel(
            UserConfigurationStore settingsStore,
            PersonalGlossaryStore glossaryStore,
            IOpenAiCredentialProvider credentials)
        {
            if (settingsStore == null)
                throw new ArgumentNullException("settingsStore");

            if (glossaryStore == null)
                throw new ArgumentNullException("glossaryStore");

            if (credentials == null)
                throw new ArgumentNullException("credentials");

            this.settingsStore = settingsStore;
            this.glossaryStore = glossaryStore;
            this.credentials = credentials;
            learningStore = new LocalLearningStore(
                settingsStore.ConfigurationDirectory);
            UserSettings settings = settingsStore.Load();
            SimulatedAiEnabled = settings.SimulatedAiEnabled;
            SelectedScenario = settings.GetScenario().ToString();
            OpenAiEnabled = settings.OpenAiEnabled;
            OpenAiTextOnlyConsentGranted =
                settings.OpenAiTextOnlyConsentGranted;
            OpenAiModel = settings.OpenAiModel;
            DiagnosticsEnabled = settings.DiagnosticsEnabled;
            OpenAiCredentialStatus = GetOpenAiCredentialStatus(
                credentials.IsConfigured);
            ScenarioNames = Enum.GetNames(typeof(FakeAiScenario)).ToList().AsReadOnly();
            OpenAiModels = OpenAiCorrectionProvider.SupportedModels.ToList().AsReadOnly();
            PersonalTermsText = string.Join(Environment.NewLine, glossaryStore.Load());
            LearningRecords = new ObservableCollection<LearningRecordItemViewModel>();
            VisibleLearningRecords = new ObservableCollection<LearningRecordItemViewModel>();

            foreach (LearningRecord record in learningStore.GetRecords())
            {
                LearningRecordItemViewModel item =
                    new LearningRecordItemViewModel(record);
                LearningRecords.Add(item);
                VisibleLearningRecords.Add(item);
            }

            IList<string> organizationalTerms =
                new OrganizationalGlossaryStore().Load();
            OrganizationalGlossaryStatus = organizationalTerms.Count == 0
                ? "Glosario organizacional: no instalado."
                : string.Format(
                    "Glosario organizacional: {0} término(s) de solo lectura.",
                    organizationalTerms.Count);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool SimulatedAiEnabled { get; set; }

        public string SelectedScenario { get; set; }

        public bool OpenAiEnabled { get; set; }

        public bool OpenAiTextOnlyConsentGranted { get; set; }

        public string OpenAiModel { get; set; }

        public IList<string> OpenAiModels { get; private set; }

        public string OpenAiCredentialStatus { get; private set; }

        public bool DiagnosticsEnabled { get; set; }

        public IList<string> ScenarioNames { get; private set; }

        public string PersonalTermsText { get; set; }

        public ObservableCollection<LearningRecordItemViewModel> LearningRecords
        {
            get;
            private set;
        }

        public ObservableCollection<LearningRecordItemViewModel> VisibleLearningRecords
        {
            get;
            private set;
        }

        public string OrganizationalGlossaryStatus { get; private set; }

        public string LearningFilterText
        {
            get { return learningFilterText; }
            set
            {
                string normalized = value ?? string.Empty;

                if (string.Equals(
                    learningFilterText,
                    normalized,
                    StringComparison.Ordinal))
                    return;

                learningFilterText = normalized;
                RaisePropertyChanged("LearningFilterText");
                RefreshLearningFilter();
            }
        }

        public LearningRecordItemViewModel SelectedLearningRecord
        {
            get { return selectedLearningRecord; }
            set
            {
                if (ReferenceEquals(selectedLearningRecord, value))
                    return;

                selectedLearningRecord = value;
                RaisePropertyChanged("SelectedLearningRecord");
                RaisePropertyChanged("HasSelectedLearningRecord");
            }
        }

        public bool HasSelectedLearningRecord
        {
            get { return SelectedLearningRecord != null; }
        }

        public bool HasLearningRecords
        {
            get { return LearningRecords.Count > 0; }
        }

        public string LearningSummary
        {
            get
            {
                return string.Format(
                    "{0} recuerdo(s); {1} visible(s).",
                    LearningRecords.Count,
                    VisibleLearningRecords.Count);
            }
        }

        public void Save()
        {
            if (OpenAiEnabled && !OpenAiTextOnlyConsentGranted)
            {
                throw new InvalidOperationException(
                    "Para habilitar OpenAI debe aceptar el envio exclusivo del contenido de los textos.");
            }

            UserSettings settings = new UserSettings
            {
                SimulatedAiEnabled = SimulatedAiEnabled,
                SimulatedAiScenario = SelectedScenario,
                OpenAiEnabled = OpenAiEnabled,
                OpenAiTextOnlyConsentGranted = OpenAiTextOnlyConsentGranted,
                OpenAiConsentVersion = OpenAiTextOnlyConsentGranted
                    ? UserSettings.CurrentConsentVersion
                    : 0,
                OpenAiModel = OpenAiModel,
                OpenAiTimeoutSeconds = 45,
                DiagnosticsEnabled = DiagnosticsEnabled
            };
            IList<string> personalTerms =
                PersonalGlossaryStore.ValidateAndNormalize(
                    SplitTerms(PersonalTermsText));
            settingsStore.Save(settings);
            glossaryStore.Save(personalTerms);
            learningStore.UpdateEnabledStates(
                LearningRecords.ToDictionary(
                    item => item.Id,
                    item => item.IsEnabled,
                    StringComparer.Ordinal));
        }

        public UserSettings CreateOpenAiConnectionTestSettings()
        {
            if (!OpenAiTextOnlyConsentGranted)
            {
                throw new InvalidOperationException(
                    "Autorice el envío del texto fijo de prueba antes de probar la conexión.");
            }

            if (!RefreshOpenAiCredentialStatus())
            {
                throw new InvalidOperationException(
                    "Configure la variable de entorno de usuario OPENAI_API_KEY antes de probar la conexión.");
            }

            UserSettings settings = new UserSettings
            {
                OpenAiEnabled = true,
                OpenAiTextOnlyConsentGranted = true,
                OpenAiConsentVersion = UserSettings.CurrentConsentVersion,
                OpenAiModel = OpenAiModel,
                OpenAiTimeoutSeconds = 30
            };
            settings.Normalize();
            return settings;
        }

        private bool RefreshOpenAiCredentialStatus()
        {
            bool isConfigured = credentials.IsConfigured;
            string status = GetOpenAiCredentialStatus(isConfigured);

            if (!string.Equals(
                OpenAiCredentialStatus,
                status,
                StringComparison.Ordinal))
            {
                OpenAiCredentialStatus = status;
                RaisePropertyChanged("OpenAiCredentialStatus");
            }

            return isConfigured;
        }

        private static string GetOpenAiCredentialStatus(bool isConfigured)
        {
            return isConfigured
                ? "Clave OPENAI_API_KEY detectada."
                : "Clave OPENAI_API_KEY no detectada.";
        }

        public bool DeleteSelectedLearningRecord()
        {
            LearningRecordItemViewModel selected = SelectedLearningRecord;

            if (selected == null || !learningStore.Delete(selected.Id))
                return false;

            LearningRecords.Remove(selected);
            SelectedLearningRecord = null;
            RefreshLearningFilter();
            RaisePropertyChanged("HasLearningRecords");
            return true;
        }

        public void ClearLearningRecords()
        {
            learningStore.Clear();
            LearningRecords.Clear();
            VisibleLearningRecords.Clear();
            SelectedLearningRecord = null;
            RaisePropertyChanged("LearningSummary");
            RaisePropertyChanged("HasLearningRecords");
        }

        public bool ExportLearningRecords(string destinationPath)
        {
            return learningStore.Export(destinationPath);
        }

        private void RefreshLearningFilter()
        {
            LearningRecordItemViewModel previous = SelectedLearningRecord;
            VisibleLearningRecords.Clear();

            foreach (LearningRecordItemViewModel item in LearningRecords)
            {
                if (item.Matches(LearningFilterText))
                    VisibleLearningRecords.Add(item);
            }

            if (previous == null || !VisibleLearningRecords.Contains(previous))
                SelectedLearningRecord = VisibleLearningRecords.FirstOrDefault();

            RaisePropertyChanged("LearningSummary");
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static IEnumerable<string> SplitTerms(string value)
        {
            return (value ?? string.Empty).Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
