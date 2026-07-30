using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace CivilSpellAI.Autodesk
{
    public class SelectionHelper
    {
        public class SelectedText
        {
            public ObjectId Id { get; set; }
            public string Text { get; set; }
        }


        public static SelectedText SelectText(Editor ed, Database db)
        {
            PromptEntityOptions options =
                new PromptEntityOptions("\nSeleccione un texto: ");

            options.SetRejectMessage("\nDebe seleccionar un objeto de texto.");

            options.AddAllowedClass(typeof(DBText), true);
            options.AddAllowedClass(typeof(MText), true);

            PromptEntityResult result = ed.GetEntity(options);

            if (result.Status != PromptStatus.OK)
                return null;


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity entity =
                    tr.GetObject(result.ObjectId, OpenMode.ForRead) as Entity;

                string text = "";

                if (entity is DBText dbText)
                {
                    text = dbText.TextString;
                }

                if (entity is MText mText)
                {
                    text = mText.Contents;
                }

                tr.Commit();


                return new SelectedText
                {
                    Id = result.ObjectId,
                    Text = text
                };
            }
        }
    }
}