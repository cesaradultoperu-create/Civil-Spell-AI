using System.Collections.Generic;
using System.Threading;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Application;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Domain;
using CivilSpellAI.Infrastructure;
using CivilSpellAI.Spell;
using CivilSpellAI.UI;
using System.Windows;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CivilSpellAI.Commands
{
    public sealed class AiSpellAllCommand
    {
        [CommandMethod("AISPELLALL")]
        public void RunAiSpellAll()
        {
            ITextDocumentContext document =
                new AutodeskTextDocumentProvider().GetActiveDocument();

            if (document == null)
                return;

            UserConfigurationStore settingsStore = new UserConfigurationStore();

            using (DiagnosticOperation diagnostic = DiagnosticOperationFactory.Create(
                DiagnosticCommand.AiSpellAll,
                settingsStore))
            {
                try
                {
                    Run(document, settingsStore, diagnostic);
                }
                catch (System.Exception exception)
                {
                    diagnostic.Complete(
                        DiagnosticClassifier.FromException(exception),
                        DiagnosticSeverity.Error,
                        0);
                    document.WriteMessage(
                        "\n{0}",
                        UserFacingError.Create(
                            "CivilSpellAI no pudo revisar el dibujo",
                            exception));
                }
            }
        }

        private static void Run(
            ITextDocumentContext document,
            UserConfigurationStore settingsStore,
            DiagnosticOperation diagnostic)
        {
            IList<TextSelection> selectedTexts = document.ScanAllTexts();

            if (selectedTexts.Count == 0)
            {
                diagnostic.Complete(
                    DiagnosticCode.SelectionInvalid,
                    DiagnosticSeverity.Information,
                    0);
                document.WriteMessage(
                    "\nNo se encontraron objetos DBText o MText en el dibujo.");
                return;
            }

            document.WriteMessage(
                "\nCivilSpellAI está revisando {0} texto(s)…",
                selectedTexts.Count);

            UserSettings settings = settingsStore.Load();
            PersonalGlossaryStore personalGlossary =
                new PersonalGlossaryStore(settingsStore.ConfigurationDirectory);
            OrganizationalGlossaryStore organizationalGlossary =
                new OrganizationalGlossaryStore();
            TechnicalGlossary glossary = TechnicalGlossary.LoadDefault();
            glossary.AddRange(organizationalGlossary.Load());
            glossary.AddRange(personalGlossary.Load());
            SpellEngine engine = new SpellEngine(glossary);
            List<ITextCorrectionProvider> providers =
                new List<ITextCorrectionProvider>(
                    LocalReviewProviderFactory.Create(
                        engine,
                        new LocalLearningStore(
                            settingsStore.ConfigurationDirectory)));

            ITextCorrectionProvider openAiProvider;
            string openAiMessage;
            bool openAiActive = OpenAiProviderFactory.TryCreate(
                settings,
                out openAiProvider,
                out openAiMessage);

            if (openAiActive)
            {
                long totalCharacters = 0;

                foreach (TextSelection selected in selectedTexts)
                    totalCharacters += selected.Text.Length;

                MessageBoxResult remoteDecision = MessageBox.Show(
                    string.Format(
                        "OpenAI está habilitado para este lote.\n\n" +
                        "Sí: enviar el contenido de {0} texto(s), con aproximadamente {1:N0} caracteres en solicitudes independientes. Esto puede generar costes.\n" +
                        "No: continuar únicamente con reglas locales.\n" +
                        "Cancelar: salir sin analizar ni escribir.",
                        selectedTexts.Count,
                        totalCharacters),
                    "CivilSpellAI · Alcance de revisión remota",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Information);

                if (remoteDecision == MessageBoxResult.Cancel)
                {
                    diagnostic.Complete(
                        DiagnosticCode.OperationCancelled,
                        DiagnosticSeverity.Information,
                        selectedTexts.Count);
                    document.WriteMessage(
                        "\nRevisión global cancelada antes de enviar textos.");
                    return;
                }

                if (remoteDecision == MessageBoxResult.Yes)
                {
                    providers.Add(openAiProvider);
                }
                else
                {
                    openAiActive = false;
                    document.WriteMessage(
                        "\nLa revisión continuará únicamente con reglas locales.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(openAiMessage))
            {
                if (settings.CanUseOpenAi)
                {
                    diagnostic.Record(
                        DiagnosticCode.ConfigurationMissing,
                        DiagnosticSeverity.Warning,
                        0);
                }

                document.WriteMessage("\n{0}", openAiMessage);
            }

            if (settings.SimulatedAiEnabled)
            {
                providers.Add(new FakeAiCorrectionProvider(settings.GetScenario()));
            }

            ReviewCoordinator reviewCoordinator = new ReviewCoordinator(
                providers,
                new TechnicalTokenValidator());
            BatchReviewCoordinator batchCoordinator =
                new BatchReviewCoordinator(reviewCoordinator, openAiActive ? 2 : 4);
            List<CorrectionRequest> requests = new List<CorrectionRequest>();

            foreach (TextSelection selected in selectedTexts)
            {
                requests.Add(new CorrectionRequest(
                    selected.Snapshot,
                    ReviewLanguage.Unknown,
                    glossary.Terms,
                    3,
                    selected.LocationName));
            }

            BatchPreparationWindow preparation = new BatchPreparationWindow(
                batchCoordinator,
                requests);
            AcadApplication.ShowModalWindow(preparation);

            if (preparation.Failure != null)
            {
                throw new System.InvalidOperationException(
                    "No se pudo preparar la revisión global.",
                    preparation.Failure);
            }

            if (preparation.WasCancelled || preparation.Result == null)
            {
                diagnostic.Complete(
                    DiagnosticCode.OperationCancelled,
                    DiagnosticSeverity.Information,
                    selectedTexts.Count);
                document.WriteMessage(
                    "\nPreparación cancelada. No se modificó ningún texto.");
                return;
            }

            BatchReviewResult result = preparation.Result;

            if (result.FailureCount > 0)
            {
                diagnostic.Record(
                    DiagnosticClassifier.FromProviderFailure(
                        result.Failures[0].Kind),
                    DiagnosticSeverity.Warning,
                    result.FailureCount);
            }

            int blockedProposalCount = 0;

            foreach (BatchReviewEntry entry in result.Entries)
            {
                foreach (ValidatedCorrectionProposal proposal in entry.Session.Proposals)
                {
                    if (!proposal.CanApply)
                        blockedProposalCount++;
                }
            }

            if (blockedProposalCount > 0)
            {
                diagnostic.Record(
                    DiagnosticCode.ValidationBlocked,
                    DiagnosticSeverity.Warning,
                    blockedProposalCount);
            }

            if (result.Entries.Count == 0)
            {
                diagnostic.Complete(
                    DiagnosticCode.CommandCompleted,
                    DiagnosticSeverity.Information,
                    selectedTexts.Count);
                document.WriteMessage(
                    "\nNo se encontraron correcciones para los textos compatibles.");
                return;
            }

            BatchReviewWindow window = new BatchReviewWindow(result);
            AcadApplication.ShowModalWindow(window);

            if (!window.ApplyWasRequested)
            {
                diagnostic.Complete(
                    DiagnosticCode.OperationCancelled,
                    DiagnosticSeverity.Information,
                    selectedTexts.Count);
                document.WriteMessage(
                    "\nRevisión global cancelada. No se modificó ningún texto.");
                return;
            }

            List<AtomicTextWriteOperation> writes =
                new List<AtomicTextWriteOperation>();

            foreach (BatchReviewItemViewModel item in window.SelectedItems)
            {
                TextSelection selected =
                    selectedTexts[item.Entry.SourceIndex];
                writes.Add(new AtomicTextWriteOperation(
                    selected.TargetId,
                    selected.Snapshot,
                    item.ProposedText));
            }

            AtomicTextWriteResult writeResult = document.ApplyBatch(writes);

            if (writeResult.Status == AtomicTextWriteStatus.Applied &&
                window.RememberSelectedDecisions)
            {
                TryRecordPreferences(
                    document,
                    diagnostic,
                    settingsStore,
                    window.SelectedItems);
            }
            diagnostic.Complete(
                DiagnosticClassifier.FromWriteStatus(writeResult.Status),
                writeResult.Status == AtomicTextWriteStatus.Applied ||
                    writeResult.Status == AtomicTextWriteStatus.NoChange
                    ? DiagnosticSeverity.Information
                    : DiagnosticSeverity.Warning,
                selectedTexts.Count);
            WriteResult(document, writeResult);
        }

        private static void TryRecordPreferences(
            ITextDocumentContext document,
            DiagnosticOperation diagnostic,
            UserConfigurationStore settingsStore,
            IList<BatchReviewItemViewModel> selectedItems)
        {
            try
            {
                LocalLearningStore learningStore =
                    new LocalLearningStore(settingsStore.ConfigurationDirectory);

                foreach (BatchReviewItemViewModel item in selectedItems)
                {
                    learningStore.Record(
                        item.Entry.Request,
                        ReviewDecision.Apply(item.Proposal, true));
                }

                document.WriteMessage(
                    "\nLas decisiones seleccionadas se guardaron en la memoria local.");
            }
            catch (System.IO.IOException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    selectedItems.Count);
                document.WriteMessage(
                    "\nEl lote se aplicó, pero no pudo guardarse toda la memoria local.");
            }
            catch (System.UnauthorizedAccessException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    selectedItems.Count);
                document.WriteMessage(
                    "\nEl lote se aplicó, pero no hay permiso para guardar la memoria local.");
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    selectedItems.Count);
                document.WriteMessage(
                    "\nEl lote se aplicó, pero la memoria local no pudo actualizarse.");
            }
            catch (System.Security.SecurityException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    selectedItems.Count);
                document.WriteMessage(
                    "\nEl lote se aplicó, pero la política de seguridad impidió actualizar la memoria local.");
            }
            catch (System.NotSupportedException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    selectedItems.Count);
                document.WriteMessage(
                    "\nEl lote se aplicó, pero la ubicación de la memoria local no es compatible.");
            }
        }

        private static void WriteResult(
            ITextDocumentContext document,
            AtomicTextWriteResult result)
        {
            switch (result.Status)
            {
                case AtomicTextWriteStatus.Applied:
                    document.WriteMessage(
                        "\nSe corrigieron {0} texto(s). Use U una vez para revertir todo el lote.",
                        result.AppliedCount);
                    break;
                case AtomicTextWriteStatus.Conflict:
                    document.WriteMessage(
                        "\nEl texto con handle {0} cambió durante la revisión. No se modificó ningún objeto.",
                        result.FailedHandle);
                    break;
                case AtomicTextWriteStatus.DocumentMismatch:
                    document.WriteMessage(
                        "\nEl documento ya no coincide con la revisión. No se modificó ningún objeto.");
                    break;
                case AtomicTextWriteStatus.NoChange:
                    document.WriteMessage("\nNo se seleccionaron cambios para aplicar.");
                    break;
                default:
                    document.WriteMessage(
                        "\nEl objeto con handle {0} ya no está disponible. No se modificó ningún objeto.",
                        result.FailedHandle);
                    break;
            }
        }
    }
}
