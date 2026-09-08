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
                : this(
                    AutodeskTextTarget.ForEntity(id),
                    snapshot,
                    "Desconocido")
            {
            }

            public SelectedText(
                ObjectId id,
                TextSnapshot snapshot,
                string locationName)
                : this(
                    AutodeskTextTarget.ForEntity(id),
                    snapshot,
                    locationName)
            {
            }

            internal SelectedText(
                AutodeskTextTarget target,
                TextSnapshot snapshot,
                string locationName)
            {
                if (target == null)
                    throw new System.ArgumentNullException("target");

                Target = target;
                Snapshot = snapshot;
                LocationName = string.IsNullOrWhiteSpace(locationName)
                    ? "Desconocido"
                    : locationName.Trim();
            }

            public ObjectId Id
            {
                get { return Target.ObjectId; }
            }

            internal AutodeskTextTarget Target { get; private set; }

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
            options.AddAllowedClass(typeof(MLeader), true);
            options.AddAllowedClass(typeof(AttributeReference), true);

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

                AutodeskTextTarget target =
                    AutodeskTextTarget.ForEntity(result.ObjectId);
                TextSnapshot snapshot = new TextSnapshot(
                    GetDocumentId(database),
                    target.GetHandle(entity),
                    target.GetEntityType(entity),
                    text);

                return new SelectedText(
                    target,
                    snapshot,
                    GetLayoutName(transaction, entity.OwnerId));
            }
        }

        public static IList<SelectedText> ScanAllTexts(Database database)
        {
            List<SelectedText> selectedTexts = new List<SelectedText>();
            HashSet<string> targetIds =
                new HashSet<string>(System.StringComparer.Ordinal);
            HashSet<ObjectId> scannedBlockDefinitions = new HashSet<ObjectId>();

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

                    ScanRecord(
                        transaction,
                        record,
                        GetLayoutName(transaction, recordId),
                        GetDocumentId(database),
                        selectedTexts,
                        targetIds,
                        scannedBlockDefinitions);
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

        private static void ScanRecord(
            Transaction transaction,
            BlockTableRecord record,
            string locationName,
            string documentId,
            ICollection<SelectedText> selectedTexts,
            ISet<string> targetIds,
            ISet<ObjectId> scannedBlockDefinitions)
        {
            foreach (ObjectId entityId in record)
            {
                Entity entity = transaction.GetObject(
                    entityId,
                    OpenMode.ForRead,
                    false) as Entity;

                if (entity == null)
                    continue;

                Table table = entity as Table;

                if (table != null)
                {
                    AddTableCells(
                        table,
                        locationName,
                        documentId,
                        selectedTexts,
                        targetIds);
                    continue;
                }

                BlockReference blockReference = entity as BlockReference;

                if (blockReference != null)
                {
                    ScanBlockReference(
                        transaction,
                        blockReference,
                        locationName,
                        documentId,
                        selectedTexts,
                        targetIds,
                        scannedBlockDefinitions);
                    continue;
                }

                AttributeDefinition attributeDefinition =
                    entity as AttributeDefinition;

                if (attributeDefinition != null && !attributeDefinition.Constant)
                    continue;

                AddTarget(
                    entity,
                    AutodeskTextTarget.ForEntity(entityId),
                    locationName,
                    documentId,
                    selectedTexts,
                    targetIds);
            }
        }

        private static void ScanBlockReference(
            Transaction transaction,
            BlockReference blockReference,
            string parentLocation,
            string documentId,
            ICollection<SelectedText> selectedTexts,
            ISet<string> targetIds,
            ISet<ObjectId> scannedBlockDefinitions)
        {
            BlockTableRecord definition = transaction.GetObject(
                blockReference.BlockTableRecord,
                OpenMode.ForRead,
                false) as BlockTableRecord;

            if (definition == null ||
                definition.IsFromExternalReference ||
                definition.IsFromOverlayReference ||
                definition.IsDependent)
            {
                return;
            }

            string locationName = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} / Bloque: {1}",
                parentLocation,
                string.IsNullOrWhiteSpace(definition.Name)
                    ? "Sin nombre"
                    : definition.Name);

            foreach (ObjectId attributeId in blockReference.AttributeCollection)
            {
                AttributeReference attribute = transaction.GetObject(
                    attributeId,
                    OpenMode.ForRead,
                    false) as AttributeReference;

                if (attribute == null || attribute.IsConstant)
                    continue;

                AddTarget(
                    attribute,
                    AutodeskTextTarget.ForEntity(attributeId),
                    locationName,
                    documentId,
                    selectedTexts,
                    targetIds);
            }

            if (!scannedBlockDefinitions.Add(definition.ObjectId))
                return;

            ScanRecord(
                transaction,
                definition,
                locationName,
                documentId,
                selectedTexts,
                targetIds,
                scannedBlockDefinitions);
        }

        private static void AddTableCells(
            Table table,
            string locationName,
            string documentId,
            ICollection<SelectedText> selectedTexts,
            ISet<string> targetIds)
        {
            for (int row = 0; row < table.Rows.Count; row++)
            {
                for (int column = 0; column < table.Columns.Count; column++)
                {
                    AddTarget(
                        table,
                        AutodeskTextTarget.ForTableCell(
                            table.ObjectId,
                            row,
                            column),
                        locationName,
                        documentId,
                        selectedTexts,
                        targetIds);
                }
            }
        }

        private static void AddTarget(
            Entity entity,
            AutodeskTextTarget target,
            string locationName,
            string documentId,
            ICollection<SelectedText> selectedTexts,
            ISet<string> targetIds)
        {
            string text;

            if (targetIds.Contains(target.TargetId) ||
                !TextEditor.TryGetText(entity, target, out text) ||
                string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            targetIds.Add(target.TargetId);
            selectedTexts.Add(new SelectedText(
                target,
                new TextSnapshot(
                    documentId,
                    target.GetHandle(entity),
                    target.GetEntityType(entity),
                    text),
                locationName));
        }

        internal static string GetDocumentId(Database database)
        {
            return database.FingerprintGuid.ToString();
        }

        private static string GetLayoutName(
            Transaction transaction,
            ObjectId recordId)
        {
            DBObject owner = transaction.GetObject(
                recordId,
                OpenMode.ForRead,
                false);
            BlockTableRecord record = owner as BlockTableRecord;

            if (record == null)
            {
                BlockReference blockReference = owner as BlockReference;

                if (blockReference != null)
                {
                    record = transaction.GetObject(
                        blockReference.OwnerId,
                        OpenMode.ForRead,
                        false) as BlockTableRecord;
                }
            }

            if (record == null || record.LayoutId.IsNull)
                return "Desconocido";

            Layout layout = transaction.GetObject(
                record.LayoutId,
                OpenMode.ForRead) as Layout;
            return layout == null ? "Desconocido" : layout.LayoutName;
        }
    }
}
