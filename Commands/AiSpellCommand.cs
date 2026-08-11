using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using CivilSpellAI.Spell;
using CivilSpellAI.Autodesk;

namespace CivilSpellAI.Commands
{
    public class AiSpellCommand
    {
        [CommandMethod("AISPELL")]
        public void RunAiSpell()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;

            var selected = SelectionHelper.SelectText(ed, doc.Database);

            if (selected == null)
            {
                ed.WriteMessage("\nNo se seleccionó ningún texto.");
                return;
            }


            SpellEngine engine = new SpellEngine();

            var result = engine.Analyze(selected.Text);


            ed.WriteMessage("\nCivilSpell AI resultado (idioma: " +
                GetLanguageDisplayName(result.Language) + "): ");

            foreach (var change in result.Changes)
            {
                ed.WriteMessage($"\n{change.Original} -> {change.Corrected}");
            }

            if (!result.HasChanges)
            {
                ed.WriteMessage("\nNo se encontraron correcciones seguras para aplicar.");
                return;
            }


            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                Entity entity =
                    tr.GetObject(selected.Id, OpenMode.ForWrite) as Entity;


                CivilSpellAI.Autodesk.TextEditor.ReplaceText(entity, result.CorrectedText);


                tr.Commit();
            }


            ed.WriteMessage("\nTexto corregido aplicado.");
        }

        private static string GetLanguageDisplayName(TextLanguage language)
        {
            switch (language)
            {
                case TextLanguage.Spanish:
                    return "español";
                case TextLanguage.English:
                    return "inglés";
                case TextLanguage.Mixed:
                    return "mixto";
                default:
                    return "no identificado";
            }
        }
    }
}
