using System.Windows;
using System.Threading;
using CivilSpellAI.Application;
using CivilSpellAI.Domain;
using CivilSpellAI.Infrastructure;
using Microsoft.Win32;

namespace CivilSpellAI.UI
{
    public partial class SpellSettingsWindow : Window
    {
        private readonly SpellSettingsViewModel viewModel;
        private readonly DiagnosticLogManager diagnosticLogManager;
        private CancellationTokenSource connectionTestCancellation;

        public SpellSettingsWindow(
            UserConfigurationStore settingsStore,
            PersonalGlossaryStore glossaryStore)
        {
            InitializeComponent();
            viewModel = new SpellSettingsViewModel(settingsStore, glossaryStore);
            diagnosticLogManager = new DiagnosticLogManager(
                System.IO.Path.Combine(
                    settingsStore.ConfigurationDirectory,
                    "diagnostics"));
            DataContext = viewModel;
            UpdateDiagnosticButtons();
        }

        public bool WasSaved { get; private set; }

        public bool DiagnosticsWereDeleted { get; private set; }

        private void SaveClick(object sender, RoutedEventArgs eventArgs)
        {
            try
            {
                viewModel.Save();
                WasSaved = true;
                Close();
            }
            catch (System.InvalidOperationException exception)
            {
                MessageBox.Show(
                    this,
                    exception.Message,
                    "CivilSpellAI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (System.Exception exception)
                when (IsLocalStorageException(exception))
            {
                ShowLocalStorageError(
                    "guardar la configuración local",
                    exception);
            }
        }

        private void CancelClick(object sender, RoutedEventArgs eventArgs)
        {
            Close();
        }

        private async void TestOpenAiConnectionClick(
            object sender,
            RoutedEventArgs eventArgs)
        {
            if (connectionTestCancellation != null)
                return;

            UserSettings settings;

            try
            {
                settings = viewModel.CreateOpenAiConnectionTestSettings();
            }
            catch (System.InvalidOperationException exception)
            {
                MessageBox.Show(
                    this,
                    exception.Message,
                    "CivilSpellAI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            MessageBoxResult confirmation = MessageBox.Show(
                this,
                "La prueba enviará únicamente la frase fija \"" +
                OpenAiConnectionTestService.FixedTestText +
                "\". No usa texto del dibujo ni guarda la respuesta. " +
                "La solicitud puede generar un coste mínimo. ¿Desea continuar?",
                "CivilSpellAI · Probar OpenAI",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (confirmation != MessageBoxResult.Yes)
                return;

            TestOpenAiButton.IsEnabled = false;
            CancelOpenAiTestButton.IsEnabled = true;
            CancelOpenAiTestButton.Focus();
            ConnectionTestStatusText.Text = "Probando la conexión…";
            CancellationTokenSource cancellation =
                new CancellationTokenSource();
            connectionTestCancellation = cancellation;

            try
            {
                ITextCorrectionProvider provider;
                string configurationMessage;

                if (!OpenAiProviderFactory.TryCreate(
                    settings,
                    out provider,
                    out configurationMessage))
                {
                    throw new CorrectionProviderException(
                        ProviderFailureKind.Configuration,
                        configurationMessage ?? "OpenAI no está disponible.");
                }

                await new OpenAiConnectionTestService(provider)
                    .TestAsync(cancellation.Token);

                if (!IsVisible)
                    return;

                ConnectionTestStatusText.Text = "Conexión correcta.";
                MessageBox.Show(
                    this,
                    "Conexión correcta. No se guardó texto ni respuesta de la prueba.",
                    "CivilSpellAI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (System.OperationCanceledException)
            {
                ConnectionTestStatusText.Text = "Prueba de conexión cancelada.";
            }
            catch (System.Exception exception)
            {
                OpenAiConnectionTestFailure failure =
                    OpenAiConnectionTestFailure.FromException(exception);
                ConnectionTestStatusText.Text = failure.StatusText;

                if (IsVisible)
                {
                    MessageBox.Show(
                        this,
                        failure.UserMessage,
                        "CivilSpellAI",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            finally
            {
                cancellation.Dispose();

                if (object.ReferenceEquals(
                    connectionTestCancellation,
                    cancellation))
                {
                    connectionTestCancellation = null;
                }

                if (IsLoaded)
                {
                    TestOpenAiButton.IsEnabled = true;
                    CancelOpenAiTestButton.IsEnabled = false;
                    TestOpenAiButton.Focus();
                }
            }
        }

        private void CancelOpenAiConnectionTestClick(
            object sender,
            RoutedEventArgs eventArgs)
        {
            CancellationTokenSource cancellation =
                connectionTestCancellation;

            if (cancellation == null || cancellation.IsCancellationRequested)
                return;

            CancelOpenAiTestButton.IsEnabled = false;
            ConnectionTestStatusText.Text = "Cancelando la prueba…";
            cancellation.Cancel();
        }

        protected override void OnClosed(System.EventArgs eventArgs)
        {
            if (connectionTestCancellation != null)
                connectionTestCancellation.Cancel();

            base.OnClosed(eventArgs);
        }

        private void ExportDiagnosticsClick(object sender, RoutedEventArgs eventArgs)
        {
            if (!diagnosticLogManager.HasEvents)
            {
                MessageBox.Show(
                    this,
                    "Todavía no existen eventos diagnósticos para exportar.",
                    "CivilSpellAI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".jsonl",
                FileName = "civilspell-diagnostics-" +
                    System.DateTime.Now.ToString("yyyyMMdd") + ".jsonl",
                Filter = "Eventos JSON Lines (*.jsonl)|*.jsonl|Todos los archivos (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                bool exported = diagnosticLogManager.Export(dialog.FileName);
                MessageBox.Show(
                    this,
                    exported
                        ? "Eventos diagnósticos exportados. Revise el archivo antes de compartirlo."
                        : "Todavía no existen eventos diagnósticos para exportar.",
                    "CivilSpellAI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (System.Exception exception)
                when (IsLocalStorageException(exception))
            {
                ShowLocalStorageError(
                    "exportar los eventos diagnósticos",
                    exception);
            }
        }

        private void DeleteDiagnosticsClick(object sender, RoutedEventArgs eventArgs)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                this,
                "¿Desea borrar permanentemente los eventos diagnósticos locales?",
                "CivilSpellAI",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                bool deleted = diagnosticLogManager.Delete();
                DiagnosticsWereDeleted = deleted;
                UpdateDiagnosticButtons();
                MessageBox.Show(
                    this,
                    deleted
                        ? "Eventos diagnósticos borrados."
                        : "No había eventos diagnósticos para borrar.",
                    "CivilSpellAI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (System.Exception exception)
                when (IsLocalStorageException(exception))
            {
                ShowLocalStorageError(
                    "borrar los eventos diagnósticos",
                    exception);
            }
        }

        private void ExportLearningClick(object sender, RoutedEventArgs eventArgs)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".json",
                FileName = "civilspell-learning-" +
                    System.DateTime.Now.ToString("yyyyMMdd") + ".json",
                Filter = "Memoria JSON (*.json)|*.json|Todos los archivos (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                bool exported = viewModel.ExportLearningRecords(dialog.FileName);
                MessageBox.Show(
                    this,
                    exported
                        ? "Memoria exportada. Contiene texto local: revísela antes de compartirla."
                        : "Todavía no existen recuerdos para exportar.",
                    "CivilSpellAI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (System.Exception exception)
                when (IsLocalStorageException(exception))
            {
                ShowLocalStorageError(
                    "exportar la memoria local",
                    exception);
            }
        }

        private void DeleteLearningClick(object sender, RoutedEventArgs eventArgs)
        {
            if (viewModel.SelectedLearningRecord == null)
                return;

            MessageBoxResult confirmation = MessageBox.Show(
                this,
                "¿Desea borrar permanentemente la preferencia seleccionada?",
                "CivilSpellAI",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                viewModel.DeleteSelectedLearningRecord();
            }
            catch (System.Exception exception)
                when (IsLocalStorageException(exception))
            {
                ShowLocalStorageError(
                    "borrar la preferencia seleccionada",
                    exception);
            }
        }

        private void ClearLearningClick(object sender, RoutedEventArgs eventArgs)
        {
            MessageBoxResult confirmation = MessageBox.Show(
                this,
                "¿Desea borrar permanentemente toda la memoria local?",
                "CivilSpellAI",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                viewModel.ClearLearningRecords();
            }
            catch (System.Exception exception)
                when (IsLocalStorageException(exception))
            {
                ShowLocalStorageError(
                    "borrar la memoria local",
                    exception);
            }
        }

        private void UpdateDiagnosticButtons()
        {
            bool hasEvents = diagnosticLogManager.HasEvents;
            ExportDiagnosticsButton.IsEnabled = hasEvents;
            DeleteDiagnosticsButton.IsEnabled = hasEvents;
        }

        private void ShowLocalStorageError(
            string operation,
            System.Exception exception)
        {
            MessageBox.Show(
                this,
                UserFacingError.Create(
                    "No fue posible " + operation +
                        ". Verifique los permisos y el espacio disponible",
                    exception),
                "CivilSpellAI",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static bool IsLocalStorageException(
            System.Exception exception)
        {
            return exception is System.IO.IOException ||
                exception is System.UnauthorizedAccessException ||
                exception is System.Security.SecurityException ||
                exception is System.NotSupportedException;
        }
    }
}
