using System.Threading;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Application;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Domain;
using CivilSpellAI.Infrastructure;
using CivilSpellAI.Spell;
using CivilSpellAI.UI;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CivilSpellAI.Commands
{
    public class AiSpellCommand
    {
        [CommandMethod("AISPELL")]
        public void RunAiSpell()
        {
            ITextDocumentContext document =
                new AutodeskTextDocumentProvider().GetActiveDocument();

            if (document == null)
                return;

            UserConfigurationStore settingsStore = new UserConfigurationStore();

            using (DiagnosticOperation diagnostic = DiagnosticOperationFactory.Create(
                DiagnosticCommand.AiSpell,
                settingsStore))
            {
                try
                {
                    RunAiSpell(document, settingsStore, diagnostic);
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
                            "CivilSpellAI no pudo completar la revisión",
                            exception));
                }
            }
        }

        private static void RunAiSpell(
            ITextDocumentContext document,
            UserConfigurationStore settingsStore,
            DiagnosticOperation diagnostic)
        {
            TextSelection selected = document.SelectText();

            if (selected == null)
            {
                diagnostic.Complete(
                    DiagnosticCode.SelectionCancelled,
                    DiagnosticSeverity.Information,
                    0);
                document.WriteMessage("\nNo se seleccionó ningún texto.");
                return;
            }

            UserSettings settings = settingsStore.Load();
            PersonalGlossaryStore personalGlossary =
                new PersonalGlossaryStore(settingsStore.ConfigurationDirectory);
            OrganizationalGlossaryStore organizationalGlossary =
                new OrganizationalGlossaryStore();
            TechnicalGlossary glossary = TechnicalGlossary.LoadDefault();
            glossary.AddRange(organizationalGlossary.Load());
            glossary.AddRange(personalGlossary.Load());
            SpellEngine engine = new SpellEngine(glossary);
            CorrectionRequest request = new CorrectionRequest(
                selected.Snapshot,
                ReviewLanguage.Unknown,
                glossary.Terms,
                3,
                selected.LocationName);
            IReviewCoordinator coordinator = new ReviewCoordinator(
                LocalReviewProviderFactory.Create(
                    engine,
                    new LocalLearningStore(
                        settingsStore.ConfigurationDirectory)),
                new TechnicalTokenValidator());
            ReviewSession session = coordinator
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            System.Collections.Generic.List<ITextCorrectionProvider> additionalProviders =
                new System.Collections.Generic.List<ITextCorrectionProvider>();
            ITextCorrectionProvider openAiProvider;
            string openAiMessage;

            if (OpenAiProviderFactory.TryCreate(
                settings,
                out openAiProvider,
                out openAiMessage))
            {
                additionalProviders.Add(openAiProvider);
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
                additionalProviders.Add(
                    new FakeAiCorrectionProvider(settings.GetScenario()));
            }

            if (session.Proposals.Count == 0 && additionalProviders.Count == 0)
            {
                diagnostic.Complete(
                    DiagnosticCode.CommandCompleted,
                    DiagnosticSeverity.Information,
                    1);
                document.WriteMessage(
                    "\nNo se encontraron correcciones seguras para revisar.");
                return;
            }

            IReviewCoordinator additionalCoordinator = null;

            if (additionalProviders.Count > 0)
            {
                additionalCoordinator = new ReviewCoordinator(
                    additionalProviders,
                    new TechnicalTokenValidator());
            }

            SpellReviewWindow window = new SpellReviewWindow(
                session,
                additionalCoordinator);
            AcadApplication.ShowModalWindow(window);
            ReviewDecision decision = window.Decision;

            if (window.LastProviderFailure != null)
            {
                diagnostic.Record(
                    DiagnosticClassifier.FromProviderFailure(
                        window.LastProviderFailure.Kind),
                    DiagnosticSeverity.Warning,
                    1);
            }

            if (window.HasBlockedProposals)
            {
                diagnostic.Record(
                    DiagnosticCode.ValidationBlocked,
                    DiagnosticSeverity.Warning,
                    1);
            }

            if (decision.Kind == ReviewDecisionKind.Cancel)
            {
                diagnostic.Complete(
                    DiagnosticCode.OperationCancelled,
                    DiagnosticSeverity.Information,
                    1);
                document.WriteMessage("\nRevisión cancelada. No se modificó el texto.");
                return;
            }

            if (decision.Kind == ReviewDecisionKind.KeepOriginal)
            {
                diagnostic.Complete(
                    DiagnosticCode.CommandCompleted,
                    DiagnosticSeverity.Information,
                    1);
                document.WriteMessage("\nSe mantuvo el texto original.");
                return;
            }

            AtomicTextWriteResult writeResult = document.Apply(
                new AtomicTextWriteOperation(
                    selected.TargetId,
                    selected.Snapshot,
                    decision.SelectedText));

            if (writeResult.Status == AtomicTextWriteStatus.Applied &&
                decision.RememberPreference)
            {
                TryRecordPreference(
                    document,
                    diagnostic,
                    settingsStore,
                    request,
                    decision);
            }

            diagnostic.Complete(
                DiagnosticClassifier.FromWriteStatus(writeResult.Status),
                writeResult.Status == AtomicTextWriteStatus.Applied ||
                    writeResult.Status == AtomicTextWriteStatus.NoChange
                    ? DiagnosticSeverity.Information
                    : DiagnosticSeverity.Warning,
                1);
            WriteResult(document, writeResult.Status);
        }

        private static void TryRecordPreference(
            ITextDocumentContext document,
            DiagnosticOperation diagnostic,
            UserConfigurationStore settingsStore,
            CorrectionRequest request,
            ReviewDecision decision)
        {
            try
            {
                new LocalLearningStore(settingsStore.ConfigurationDirectory)
                    .Record(request, decision);
                document.WriteMessage(
                    "\nLa decisión se guardó en la memoria local del usuario.");
            }
            catch (System.IO.IOException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    1);
                document.WriteMessage(
                    "\nEl texto se aplicó, pero no pudo guardarse la preferencia local.");
            }
            catch (System.UnauthorizedAccessException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    1);
                document.WriteMessage(
                    "\nEl texto se aplicó, pero no hay permiso para guardar la preferencia local.");
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    1);
                document.WriteMessage(
                    "\nEl texto se aplicó, pero la memoria local no pudo actualizarse.");
            }
            catch (System.Security.SecurityException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    1);
                document.WriteMessage(
                    "\nEl texto se aplicó, pero la política de seguridad impidió actualizar la memoria local.");
            }
            catch (System.NotSupportedException)
            {
                diagnostic.Record(
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Warning,
                    1);
                document.WriteMessage(
                    "\nEl texto se aplicó, pero la ubicación de la memoria local no es compatible.");
            }
        }

        private static void WriteResult(
            ITextDocumentContext document,
            AtomicTextWriteStatus status)
        {
            switch (status)
            {
                case AtomicTextWriteStatus.Applied:
                    document.WriteMessage(
                        "\nTexto corregido aplicado. Use UNDO para revertirlo.");
                    break;
                case AtomicTextWriteStatus.NoChange:
                    document.WriteMessage("\nNo había cambios para aplicar.");
                    break;
                case AtomicTextWriteStatus.Conflict:
                    document.WriteMessage(
                        "\nEl texto cambió durante la revisión. No se aplicó la propuesta; ejecute AISPELL nuevamente.");
                    break;
                case AtomicTextWriteStatus.DocumentMismatch:
                    document.WriteMessage(
                        "\nEl documento activo ya no coincide con la revisión. No se aplicó la propuesta.");
                    break;
                default:
                    document.WriteMessage(
                        "\nEl objeto revisado ya no está disponible o cambió de tipo. No se aplicó la propuesta.");
                    break;
            }
        }
    }
}
