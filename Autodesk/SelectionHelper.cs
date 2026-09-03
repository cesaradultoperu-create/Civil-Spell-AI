using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Autodesk
{
    public class SelectionHelper
    {
        public sealed class SelectedText
        {
            public SelectedText(ObjectId id, TextSnapshot snapshot)
                : this(id, snapshot, "Desconocido")
            {
            }

            public SelectedText(
                ObjectId id,
                TextSnapshot snapshot,
                string locationName)
            {
                Id = id;
                Snapshot = snapshot;
                LocationName = string.IsNullOrWhiteSpace(locationName)
                    ? "Desconocido"
                    : locationName.Trim();
            }

            public ObjectId Id { get; private set; }

            public TextSnapshot Snapshot { get; private set; }

            public string LocationName { get; private set; }

            public string Text
            {
                get { return Snapshot.OriginalText; }
            }
        }

        public static SelectedText SelectText(Editor editor, Database database)
        {
            PromptEntityOptions options =
                new PromptEntityOptions("\nSeleccione un texto: ");

            options.SetRejectMessage("\nDebe seleccionar un objeto de texto.");
            options.AddAllowedClass(typeof(DBText), true);
            options.AddAllowedClass(typeof(MText), true);

            PromptEntityResult result = editor.GetEntity(options);

            if (result.Status != PromptStatus.OK)
                return null;

            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            {
                Entity entity = transaction.GetObject(
                    result.ObjectId,
                    OpenMode.ForRead) as Entity;
                string text;

                if (!TextEditor.TryGetText(entity, out text))
                    return null;

                TextSnapshot snapshot = new TextSnapshot(
                    GetDocumentId(database),
                    entity.Handle.ToString(),
                    entity.GetType().Name,
                    text);

                return new SelectedText(
                    result.ObjectId,
                    snapshot,
                    GetLayoutName(transaction, entity.OwnerId));
            }
        }

        public static IList<SelectedText> ScanAllTexts(Database database)
        {
            List<SelectedText> selectedTexts = new List<SelectedText>();

            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = transaction.GetObject(
                    database.BlockTableId,
                    OpenMode.ForRead) as BlockTable;

                foreach (ObjectId recordId in blockTable)
                {
                    BlockTableRecord record = transaction.GetObject(
                        recordId,
                        OpenMode.ForRead) as BlockTableRecord;

                    if (record == null || !record.IsLayout)
                        continue;

                    foreach (ObjectId entityId in record)
                    {
                        Entity entity = transaction.GetObject(
                            entityId,
                            OpenMode.ForRead) as Entity;
                        string text;

                        if (!TextEditor.TryGetText(entity, out text))
                            continue;

                        selectedTexts.Add(new SelectedText(
                            entityId,
                            new TextSnapshot(
                                GetDocumentId(database),
                                entity.Handle.ToString(),
                                entity.GetType().Name,
                                text),
                            GetLayoutName(transaction, recordId)));
                    }
                }
            }

            selectedTexts.Sort(delegate(SelectedText left, SelectedText right)
            {
                return string.Compare(
                    left.Snapshot.ObjectHandle,
                    right.Snapshot.ObjectHandle,
                    System.StringComparison.OrdinalIgnoreCase);
            });
            return selectedTexts.AsReadOnly();
        }

        internal static string GetDocumentId(Database database)
        {
            return database.FingerprintGuid.ToString();
        }

        private static string GetLayoutName(
            Transaction transaction,
            ObjectId recordId)
        {
            BlockTableRecord record = transaction.GetObject(
                recordId,
                OpenMode.ForRead) as BlockTableRecord;

            if (record == null || record.LayoutId.IsNull)
                return "Desconocido";

            Layout layout = transaction.GetObject(
                record.LayoutId,
                OpenMode.ForRead) as Layout;
            return layout == null ? "Desconocido" : layout.LayoutName;
        }
    }
}
