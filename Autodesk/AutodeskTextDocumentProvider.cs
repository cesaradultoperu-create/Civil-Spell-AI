using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CivilSpellAI.Application;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CivilSpellAI.Autodesk
{
    public sealed class AutodeskTextDocumentProvider : ITextDocumentProvider
    {
        public ITextDocumentContext GetActiveDocument()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return null;

            return new TextDocumentContext(
                new AutodeskTextDocumentAdapter(document),
                new AutodeskTextDocumentState(document));
        }

        private sealed class AutodeskTextDocumentState : ITextDocumentState
        {
            private readonly Document capturedDocument;

            public AutodeskTextDocumentState(Document capturedDocument)
            {
                this.capturedDocument = capturedDocument;
            }

            public bool IsCurrent(string documentId)
            {
                Document active = AcadApplication.DocumentManager.MdiActiveDocument;

                return object.ReferenceEquals(active, capturedDocument) &&
                    string.Equals(
                        SelectionHelper.GetDocumentId(active.Database),
                        documentId,
                        StringComparison.OrdinalIgnoreCase);
            }

            public void WriteMessage(string format, params object[] arguments)
            {
                Document active = AcadApplication.DocumentManager.MdiActiveDocument;

                if (active != null)
                    active.Editor.WriteMessage(format, arguments);
            }
        }

        private sealed class AutodeskTextDocumentAdapter : ITextDocumentAdapter
        {
            private readonly Document document;
            private readonly Dictionary<string, ObjectId> targets;

            public AutodeskTextDocumentAdapter(Document document)
            {
                if (document == null)
                    throw new ArgumentNullException("document");

                this.document = document;
                targets = new Dictionary<string, ObjectId>(StringComparer.Ordinal);
                DocumentId = SelectionHelper.GetDocumentId(document.Database);
            }

            public string DocumentId { get; private set; }

            public TextSelection SelectText()
            {
                SelectionHelper.SelectedText selected = SelectionHelper.SelectText(
                    document.Editor,
                    document.Database);
                return Register(selected);
            }

            public IList<TextSelection> ScanAllTexts()
            {
                IList<SelectionHelper.SelectedText> selectedTexts =
                    SelectionHelper.ScanAllTexts(document.Database);
                List<TextSelection> selections = new List<TextSelection>();

                foreach (SelectionHelper.SelectedText selected in selectedTexts)
                {
                    TextSelection selection = Register(selected);

                    if (selection != null)
                        selections.Add(selection);
                }

                return selections.AsReadOnly();
            }

            public AtomicTextWriteResult Apply(AtomicTextWriteOperation operation)
            {
                ObjectId objectId;

                if (!TryResolve(operation, out objectId))
                    return InvalidTarget(operation);

                TextWriteStatus status = TextWriter.Apply(
                    document.Database,
                    objectId,
                    operation.Snapshot,
                    operation.ApprovedText);
                return ToAtomicResult(status, status == TextWriteStatus.Applied ? 1 : 0, operation);
            }

            public AtomicTextWriteResult ApplyBatch(
                IEnumerable<AtomicTextWriteOperation> operations)
            {
                List<TextWriteRequest> requests = new List<TextWriteRequest>();

                foreach (AtomicTextWriteOperation operation in operations)
                {
                    ObjectId objectId;

                    if (!TryResolve(operation, out objectId))
                        return InvalidTarget(operation);

                    requests.Add(new TextWriteRequest(
                        objectId,
                        operation.Snapshot,
                        operation.ApprovedText));
                }

                TextBatchWriteResult result = TextWriter.ApplyBatch(
                    document.Database,
                    requests);
                return new AtomicTextWriteResult(
                    Map(result.Status),
                    result.AppliedCount,
                    result.FailedHandle);
            }

            private TextSelection Register(SelectionHelper.SelectedText selected)
            {
                if (selected == null)
                    return null;

                string targetId = selected.Id.ToString();
                targets[targetId] = selected.Id;
                return new TextSelection(
                    targetId,
                    selected.Snapshot,
                    selected.LocationName);
            }

            private bool TryResolve(
                AtomicTextWriteOperation operation,
                out ObjectId objectId)
            {
                objectId = ObjectId.Null;
                return operation != null &&
                    targets.TryGetValue(operation.TargetId, out objectId);
            }

            private static AtomicTextWriteResult InvalidTarget(
                AtomicTextWriteOperation operation)
            {
                return new AtomicTextWriteResult(
                    AtomicTextWriteStatus.InvalidTarget,
                    0,
                    operation == null ? null : operation.Snapshot.ObjectHandle);
            }

            private static AtomicTextWriteResult ToAtomicResult(
                TextWriteStatus status,
                int appliedCount,
                AtomicTextWriteOperation operation)
            {
                return new AtomicTextWriteResult(
                    Map(status),
                    appliedCount,
                    status == TextWriteStatus.Applied || operation == null
                        ? null
                        : operation.Snapshot.ObjectHandle);
            }

            private static AtomicTextWriteStatus Map(TextWriteStatus status)
            {
                switch (status)
                {
                    case TextWriteStatus.Applied:
                        return AtomicTextWriteStatus.Applied;
                    case TextWriteStatus.NoChange:
                        return AtomicTextWriteStatus.NoChange;
                    case TextWriteStatus.Conflict:
                        return AtomicTextWriteStatus.Conflict;
                    case TextWriteStatus.DocumentMismatch:
                        return AtomicTextWriteStatus.DocumentMismatch;
                    default:
                        return AtomicTextWriteStatus.InvalidTarget;
                }
            }
        }
    }
}
