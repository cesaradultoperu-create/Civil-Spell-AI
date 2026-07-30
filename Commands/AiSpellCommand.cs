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

            var result = engine.CheckText(selected.Text);


            ed.WriteMessage("\nCivilSpell AI resultado:");

            foreach (var item in result)
            {
                ed.WriteMessage($"\n{item.Key} -> {item.Value}");
            }


            string correctedText = engine.CorrectText(selected.Text);


            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                Entity entity =
                    tr.GetObject(selected.Id, OpenMode.ForWrite) as Entity;


                CivilSpellAI.Autodesk.TextEditor.ReplaceText(entity, correctedText);


                tr.Commit();
            }


            ed.WriteMessage("\nTexto corregido aplicado.");
        }
    }
}