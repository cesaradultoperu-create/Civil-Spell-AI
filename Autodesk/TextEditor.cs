using Autodesk.AutoCAD.DatabaseServices;

namespace CivilSpellAI.Autodesk
{
    internal sealed class AutodeskTextTarget
    {
        private AutodeskTextTarget(ObjectId objectId, int tableRow, int tableColumn)
        {
            ObjectId = objectId;
            TableRow = tableRow;
            TableColumn = tableColumn;
        }

        public ObjectId ObjectId { get; private set; }

        public int TableRow { get; private set; }

        public int TableColumn { get; private set; }

        public bool IsTableCell
        {
            get { return TableRow >= 0 && TableColumn >= 0; }
        }

        public string TargetId
        {
            get
            {
                return IsTableCell
                    ? string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0}|TABLE|{1}|{2}",
                        ObjectId,
                        TableRow,
                        TableColumn)
                    : ObjectId.ToString();
            }
        }

        public string GetHandle(Entity entity)
        {
            if (entity == null)
                throw new System.ArgumentNullException("entity");

            return IsTableCell
                ? string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0}[R{1}C{2}]",
                    entity.Handle,
                    TableRow + 1,
                    TableColumn + 1)
                : entity.Handle.ToString();
        }

        public string GetEntityType(Entity entity)
        {
            if (entity == null)
                throw new System.ArgumentNullException("entity");

            return IsTableCell ? "TableCell" : entity.GetType().Name;
        }

        public static AutodeskTextTarget ForEntity(ObjectId objectId)
        {
            return new AutodeskTextTarget(objectId, -1, -1);
        }

        public static AutodeskTextTarget ForTableCell(
            ObjectId objectId,
            int row,
            int column)
        {
            return new AutodeskTextTarget(objectId, row, column);
        }
    }

    public class TextEditor
    {
        public static bool TryGetText(Entity entity, out string text)
        {
            return TryGetText(
                entity,
                entity == null
                    ? null
                    : AutodeskTextTarget.ForEntity(entity.ObjectId),
                out text);
        }

        internal static bool TryGetText(
            Entity entity,
            AutodeskTextTarget target,
            out string text)
        {
            if (entity == null || target == null)
            {
                text = null;
                return false;
            }

            if (target.IsTableCell)
                return TryGetTableCellText(entity as Table, target, out text);

            DBText dbText = entity as DBText;

            if (dbText != null)
            {
                text = dbText.TextString;
                return true;
            }

            MText mText = entity as MText;

            if (mText != null)
            {
                text = mText.Contents;
                return true;
            }

            MLeader mLeader = entity as MLeader;

            if (mLeader != null &&
                mLeader.ContentType == ContentType.MTextContent)
            {
                using (MText leaderText = mLeader.MText)
                {
                    if (leaderText == null)
                    {
                        text = null;
                        return false;
                    }

                    text = leaderText.Contents;
                    return true;
                }
            }

            text = null;
            return false;
        }

        public static void ReplaceText(Entity entity, string newText)
        {
            ReplaceText(
                entity,
                entity == null
                    ? null
                    : AutodeskTextTarget.ForEntity(entity.ObjectId),
                newText);
        }

        internal static void ReplaceText(
            Entity entity,
            AutodeskTextTarget target,
            string newText)
        {
            if (entity == null)
                throw new System.ArgumentNullException("entity");

            if (target == null)
                throw new System.ArgumentNullException("target");

            if (target.IsTableCell)
            {
                Table table = entity as Table;
                string currentText;

                if (!TryGetTableCellText(table, target, out currentText))
                {
                    throw new System.ArgumentException(
                        "La celda de tabla dejó de ser texto editable simple.",
                        "entity");
                }

                table.Cells[target.TableRow, target.TableColumn]
                    .Contents[0]
                    .TextString = newText;
                table.RecomputeTableBlock(true);
                return;
            }

            DBText dbText = entity as DBText;

            if (dbText != null)
            {
                dbText.TextString = newText;
                return;
            }

            MText mText = entity as MText;

            if (mText != null)
            {
                mText.Contents = newText;
                return;
            }

            MLeader mLeader = entity as MLeader;

            if (mLeader != null &&
                mLeader.ContentType == ContentType.MTextContent)
            {
                using (MText leaderText = mLeader.MText)
                {
                    if (leaderText == null)
                    {
                        throw new System.ArgumentException(
                            "El MLeader dejó de contener texto editable.",
                            "entity");
                    }

                    leaderText.Contents = newText;
                    mLeader.MText = leaderText;
                    return;
                }
            }

            throw new System.ArgumentException(
                "La entidad no es DBText, MText ni MLeader con contenido MText.",
                "entity");
        }

        internal static bool TryGetTableCellText(
            Table table,
            AutodeskTextTarget target,
            out string text)
        {
            text = null;

            if (table == null ||
                target == null ||
                !target.IsTableCell ||
                target.TableRow < 0 ||
                target.TableColumn < 0 ||
                target.TableRow >= table.Rows.Count ||
                target.TableColumn >= table.Columns.Count)
            {
                return false;
            }

            Cell cell = table.Cells[target.TableRow, target.TableColumn];

            if (cell.IsContentEditable != true ||
                !cell.DataLink.IsNull ||
                cell.Contents.Count != 1)
                return false;

            if (cell.IsMerged == true)
            {
                CellRange range = cell.GetMergeRange();

                if (!range.IsNull &&
                    (range.TopRow != target.TableRow ||
                    range.LeftColumn != target.TableColumn))
                {
                    return false;
                }
            }

            CellContent content = cell.Contents[0];

            if (content == null ||
                content.HasFormula ||
                !content.FieldId.IsNull ||
                !content.BlockTableRecordId.IsNull ||
                (content.Value != null && !(content.Value is string)))
            {
                return false;
            }

            text = content.TextString;
            return text != null;
        }
    }
}
