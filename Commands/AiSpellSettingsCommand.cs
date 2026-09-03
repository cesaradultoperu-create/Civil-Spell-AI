using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Application;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Infrastructure;
using CivilSpellAI.UI;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CivilSpellAI.Commands
{
    public sealed class AiSpellSettingsCommand
    {
        [CommandMethod("AISPELLSETTINGS")]
        public void RunSettings()
        {
            ITextDocumentContext document =
                new AutodeskTextDocumentProvider().GetActiveDocument();
            UserConfigurationStore settingsStore =
                new UserConfigurationStore();

            using (DiagnosticOperation diagnostic = DiagnosticOperationFactory.Create(
                DiagnosticCommand.AiSpellSettings,
                settingsStore))
            {
                try
                {
                    PersonalGlossaryStore glossaryStore =
                        new PersonalGlossaryStore(settingsStore.ConfigurationDirectory);
                    SpellSettingsWindow window = new SpellSettingsWindow(
                        settingsStore,
                        glossaryStore);
                    AcadApplication.ShowModalWindow(window);

                    if (window.DiagnosticsWereDeleted)
                    {
                        diagnostic.Suppress();
                    }
                    else
                    {
                        diagnostic.Complete(
                            window.WasSaved
                                ? DiagnosticCode.CommandCompleted
                                : DiagnosticCode.OperationCancelled,
                            DiagnosticSeverity.Information,
                            0);
                    }

                    if (document != null && window.WasSaved)
                    {
                        document.WriteMessage(
                            "\nConfiguración local de CivilSpellAI guardada.");
                    }
                }
                catch (System.Exception exception)
                {
                    diagnostic.Complete(
                        DiagnosticClassifier.FromException(exception),
                        DiagnosticSeverity.Error,
                        0);

                    if (document != null)
                    {
                        document.WriteMessage(
                            "\n{0}",
                            UserFacingError.Create(
                                "No se pudo abrir la configuración de CivilSpellAI",
                                exception));
                    }
                }
            }
        }
    }
}
