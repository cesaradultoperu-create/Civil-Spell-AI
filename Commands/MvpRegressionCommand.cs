#if DEBUG
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Application;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Domain;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CivilSpellAI.Commands
{
    /// <summary>
    /// Diagnostic commands for the disposable MVP regression drawing. They
    /// deliberately use stale snapshots and must never be exposed in Release.
    /// </summary>
    public sealed class MvpRegressionCommand
    {
        private const string StaleSuffix = "__SNAPSHOT_OBSOLETO__";
        private const string ProposedSuffix = " [NO DEBE ESCRIBIRSE]";
        private const string UndoTestSuffix = " [PRUEBA UNDO]";
        private static List<TextSnapshot> pendingUndoVerification;

        [CommandMethod("AISPELLTESTCONFLICT")]
        public void TestIndividualConflict()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return;

            Editor editor = document.Editor;
            ITextDocumentContext context =
                new AutodeskTextDocumentProvider().GetActiveDocument();
            TextSelection selected = context == null ? null : context.SelectText();

            if (selected == null)
            {
                editor.WriteMessage("\nPrueba cancelada: no se seleccionó texto.");
                return;
            }

            TextSnapshot staleSnapshot = CreateStaleSnapshot(selected.Snapshot);
            AtomicTextWriteResult result = context.Apply(
                new AtomicTextWriteOperation(
                    selected.TargetId,
                    staleSnapshot,
                    selected.Text + ProposedSuffix));
            bool unchanged = HasExpectedText(
                document.Database,
                selected.Snapshot.ObjectHandle,
                selected.Text);

            if (result.Status == AtomicTextWriteStatus.Conflict && unchanged)
            {
                editor.WriteMessage(
                    "\nPASS SAFE-02: se detectó el snapshot obsoleto y el texto permaneció intacto.");
                return;
            }

            editor.WriteMessage(
                "\nFAIL SAFE-02: estado {0}; texto intacto = {1}. No continúe el piloto.",
                result.Status,
                unchanged);
        }

        [CommandMethod("AISPELLTESTBATCHCONFLICT")]
        public void TestBatchConflict()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return;

            Editor editor = document.Editor;
            ITextDocumentContext context =
                new AutodeskTextDocumentProvider().GetActiveDocument();
            IList<TextSelection> texts = context == null
                ? new List<TextSelection>().AsReadOnly()
                : context.ScanAllTexts();

            if (texts.Count < 2)
            {
                editor.WriteMessage(
                    "\nLa prueba BATCH-02 requiere al menos dos DBText/MText en el dibujo desechable.");
                return;
            }

            TextSelection first = texts[0];
            TextSelection second = texts[1];
            List<AtomicTextWriteOperation> requests =
                new List<AtomicTextWriteOperation>
            {
                new AtomicTextWriteOperation(
                    first.TargetId,
                    first.Snapshot,
                    first.Text + ProposedSuffix),
                new AtomicTextWriteOperation(
                    second.TargetId,
                    CreateStaleSnapshot(second.Snapshot),
                    second.Text + ProposedSuffix)
            };

            AtomicTextWriteResult result = context.ApplyBatch(requests);
            bool firstUnchanged = HasExpectedText(
                document.Database,
                first.Snapshot.ObjectHandle,
                first.Text);
            bool secondUnchanged = HasExpectedText(
                document.Database,
                second.Snapshot.ObjectHandle,
                second.Text);

            if (result.Status == AtomicTextWriteStatus.Conflict &&
                result.AppliedCount == 0 &&
                firstUnchanged &&
                secondUnchanged)
            {
                editor.WriteMessage(
                    "\nPASS BATCH-02: el conflicto canceló el lote completo sin modificar textos.");
                return;
            }

            editor.WriteMessage(
                "\nFAIL BATCH-02: estado {0}; aplicados {1}; textos intactos = {2}/{3}. No continúe el piloto.",
                result.Status,
                result.AppliedCount,
                firstUnchanged,
                secondUnchanged);
        }

        [CommandMethod("AISPELLTESTDOCUMENTSWITCH", CommandFlags.Session)]
        public void TestDocumentSwitch()
        {
            DocumentCollection documents = AcadApplication.DocumentManager;
            Document original = documents.MdiActiveDocument;

            if (original == null)
                return;

            Document alternate = FindAlternateDocument(documents, original);

            if (alternate == null)
            {
                original.Editor.WriteMessage(
                    "\nLa prueba requiere dos dibujos desechables abiertos.");
                return;
            }

            ITextDocumentContext context =
                new AutodeskTextDocumentProvider().GetActiveDocument();
            TextSelection selected;

            using (DocumentLock documentLock = original.LockDocument())
                selected = context.SelectText();

            if (selected == null)
            {
                original.Editor.WriteMessage(
                    "\nPrueba cancelada: no se seleccionó texto.");
                return;
            }

            AtomicTextWriteResult result;

            try
            {
                documents.MdiActiveDocument = alternate;
                result = context.Apply(new AtomicTextWriteOperation(
                    selected.TargetId,
                    selected.Snapshot,
                    selected.Text + ProposedSuffix));
            }
            finally
            {
                documents.MdiActiveDocument = original;
            }

            bool unchanged;

            using (DocumentLock documentLock = original.LockDocument())
            {
                unchanged = HasExpectedText(
                    original.Database,
                    selected.Snapshot.ObjectHandle,
                    selected.Text);
            }

            if (result.Status == AtomicTextWriteStatus.DocumentMismatch && unchanged)
            {
                original.Editor.WriteMessage(
                    "\nPASS DOC-01: el cambio de documento bloqueó la escritura y el texto permaneció intacto.");
                return;
            }

            original.Editor.WriteMessage(
                "\nFAIL DOC-01: estado {0}; texto intacto = {1}. No continúe el piloto.",
                result.Status,
                unchanged);
        }

        [CommandMethod("AISPELLTESTBATCHUNDO")]
        public void TestBatchUndo()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return;

            Editor editor = document.Editor;
            ITextDocumentContext context =
                new AutodeskTextDocumentProvider().GetActiveDocument();
            IList<TextSelection> texts = context == null
                ? new List<TextSelection>().AsReadOnly()
                : context.ScanAllTexts();

            if (texts.Count < 2)
            {
                editor.WriteMessage(
                    "\nLa prueba UNDO requiere al menos dos DBText/MText en el dibujo desechable.");
                return;
            }

            List<AtomicTextWriteOperation> requests =
                new List<AtomicTextWriteOperation>();
            pendingUndoVerification = new List<TextSnapshot>();

            for (int index = 0; index < 2; index++)
            {
                TextSelection selected = texts[index];
                pendingUndoVerification.Add(selected.Snapshot);
                requests.Add(new AtomicTextWriteOperation(
                    selected.TargetId,
                    selected.Snapshot,
                    selected.Text + UndoTestSuffix));
            }

            AtomicTextWriteResult result = context.ApplyBatch(requests);

            if (result.Status == AtomicTextWriteStatus.Applied &&
                result.AppliedCount == requests.Count)
            {
                editor.WriteMessage(
                    "\nREADY UNDO-04: BATCH APPLIED; RUN ONE U, THEN AISPELLTESTBATCHUNDOVERIFY.");
                return;
            }

            pendingUndoVerification = null;
            editor.WriteMessage(
                "\nFAIL UNDO-04: estado {0}; aplicados {1}. No continúe el piloto.",
                result.Status,
                result.AppliedCount);
        }

        [CommandMethod("AISPELLTESTBATCHUNDOVERIFY")]
        public void VerifyBatchUndo()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return;

            Editor editor = document.Editor;

            if (pendingUndoVerification == null ||
                pendingUndoVerification.Count == 0)
            {
                editor.WriteMessage(
                    "\nFAIL UNDO-04: no existe un lote pendiente de verificación.");
                return;
            }

            bool restored = true;

            foreach (TextSnapshot snapshot in pendingUndoVerification)
            {
                restored = restored && HasExpectedText(
                    document.Database,
                    snapshot.ObjectHandle,
                    snapshot.OriginalText);
            }

            pendingUndoVerification = null;

            if (restored)
            {
                editor.WriteMessage(
                    "\nPASS UNDO-04: ONE U RESTORED THE ENTIRE BATCH.");
                return;
            }

            editor.WriteMessage(
                "\nFAIL UNDO-04: una sola operación U no restauró todo el lote. No continúe el piloto.");
        }

        private static TextSnapshot CreateStaleSnapshot(TextSnapshot snapshot)
        {
            return new TextSnapshot(
                snapshot.DocumentId,
                snapshot.ObjectHandle,
                snapshot.EntityType,
                snapshot.OriginalText + StaleSuffix);
        }

        private static bool HasExpectedText(
            Database database,
            string objectHandle,
            string expectedText)
        {
            foreach (SelectionHelper.SelectedText selected in
                SelectionHelper.ScanAllTexts(database))
            {
                if (string.Equals(
                    selected.Snapshot.ObjectHandle,
                    objectHandle,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return string.Equals(
                        selected.Text,
                        expectedText,
                        StringComparison.Ordinal);
                }
            }

            return false;
        }

        private static Document FindAlternateDocument(
            DocumentCollection documents,
            Document original)
        {
            foreach (Document candidate in documents)
            {
                if (!object.ReferenceEquals(candidate, original))
                    return candidate;
            }

            return null;
        }
    }
}
#endif
