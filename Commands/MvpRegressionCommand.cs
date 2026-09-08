#if DEBUG
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Application;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Domain;
using CivilSpellAI.Infrastructure;
using CivilSpellAI.Spell;
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
        private static List<TextSnapshot> pendingExtendedTextVerification;

        [CommandMethod("AISPELLTESTDICTIONARY")]
        public void TestOfflineDictionary()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return;

            HunspellSpanishDictionary dictionary =
                HunspellSpanishDictionary.LoadDefault();
            string corrected = new SpellEngine(
                new TechnicalGlossary(),
                dictionary).CorrectText("ADMINISTRACION GENERAL");

            if (dictionary.IsLoaded &&
                string.Equals(
                    corrected,
                    "ADMINISTRACIÓN GENERAL",
                    StringComparison.Ordinal))
            {
                document.Editor.WriteMessage(
                    "\nPASS DICT-01: OFFLINE ES_PE DICTIONARY LOADED AND RESTORED THE ACCENT.");
                return;
            }

            document.Editor.WriteMessage(
                "\nFAIL DICT-01: OFFLINE ES_PE DICTIONARY DID NOT PRODUCE THE EXPECTED RESULT.");
        }

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
                    "\nLa prueba BATCH-02 requiere al menos dos textos compatibles en el dibujo desechable.");
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
                    "\nLa prueba UNDO requiere al menos dos textos compatibles en el dibujo desechable.");
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

        [CommandMethod("AISPELLTESTEXTENDEDCREATE")]
        public void CreateExtendedTextFixture()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return;

            Database database = document.Database;

            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = transaction.GetObject(
                    database.BlockTableId,
                    OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite) as BlockTableRecord;

                MLeader leader = new MLeader();
                leader.SetDatabaseDefaults(database);
                leader.ContentType = ContentType.MTextContent;

                using (MText leaderText = new MText())
                {
                    leaderText.SetDatabaseDefaults(database);
                    leaderText.Contents = "PRUEBA GEOMENBRANAA";
                    leaderText.TextHeight = 2.5;
                    leader.MText = leaderText;
                }

                int leaderIndex = leader.AddLeader();
                int leaderLineIndex = leader.AddLeaderLine(leaderIndex);
                leader.AddFirstVertex(
                    leaderLineIndex,
                    new Point3d(0.0, 32.0, 0.0));
                leader.AddLastVertex(
                    leaderLineIndex,
                    new Point3d(12.0, 32.0, 0.0));
                leader.TextLocation = new Point3d(15.0, 32.0, 0.0);
                modelSpace.AppendEntity(leader);
                transaction.AddNewlyCreatedDBObject(leader, true);

                Table table = new Table();
                table.SetDatabaseDefaults(database);
                table.TableStyle = database.Tablestyle;
                table.SetSize(2, 2);
                table.Position = new Point3d(0.0, 40.0, 0.0);
                table.Cells[1, 0].TextString = "PRUEBA CARRETERAA";
                table.Cells[1, 1].TextString = "PRUEBA ALINEAMIENTOO";
                modelSpace.AppendEntity(table);
                transaction.AddNewlyCreatedDBObject(table, true);
                table.GenerateLayout();

                blockTable.UpgradeOpen();
                BlockTableRecord definition = new BlockTableRecord();
                definition.Name = "CIVILSPELLAI_TEST_" +
                    Guid.NewGuid().ToString("N");
                ObjectId definitionId = blockTable.Add(definition);
                transaction.AddNewlyCreatedDBObject(definition, true);

                DBText nestedText = new DBText();
                nestedText.SetDatabaseDefaults(database);
                nestedText.Position = Point3d.Origin;
                nestedText.Height = 2.5;
                nestedText.TextString = "PRUEBA SUVRASANTE";
                definition.AppendEntity(nestedText);
                transaction.AddNewlyCreatedDBObject(nestedText, true);

                AttributeDefinition attributeDefinition =
                    new AttributeDefinition();
                attributeDefinition.SetDatabaseDefaults(database);
                attributeDefinition.Position = new Point3d(0.0, -4.0, 0.0);
                attributeDefinition.Height = 2.5;
                attributeDefinition.Tag = "CIVILSPELLAI_NOTA";
                attributeDefinition.Prompt = "Nota";
                attributeDefinition.TextString = "PRUEBA DRENAGE";
                definition.AppendEntity(attributeDefinition);
                transaction.AddNewlyCreatedDBObject(attributeDefinition, true);

                BlockReference blockReference = new BlockReference(
                    new Point3d(40.0, 40.0, 0.0),
                    definitionId);
                modelSpace.AppendEntity(blockReference);
                transaction.AddNewlyCreatedDBObject(blockReference, true);

                AttributeReference attribute = new AttributeReference();
                attribute.SetAttributeFromBlock(
                    attributeDefinition,
                    blockReference.BlockTransform);
                attribute.TextString = attributeDefinition.TextString;
                blockReference.AttributeCollection.AppendAttribute(attribute);
                transaction.AddNewlyCreatedDBObject(attribute, true);

                transaction.Commit();
            }

            pendingExtendedTextVerification = null;
            document.Editor.WriteMessage(
                "\nREADY EXTENDED-01: FIXTURE CREATED; RUN AISPELLTESTEXTENDEDAPPLY.");
        }

        [CommandMethod("AISPELLTESTEXTENDEDAPPLY")]
        public void ApplyExtendedTextFixture()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return;

            ITextDocumentContext context =
                new AutodeskTextDocumentProvider().GetActiveDocument();
            IList<TextSelection> selections = context == null
                ? new List<TextSelection>().AsReadOnly()
                : context.ScanAllTexts();
            ExtendedTextCase[] cases = CreateExtendedTextCases();
            List<AtomicTextWriteOperation> operations =
                new List<AtomicTextWriteOperation>();
            pendingExtendedTextVerification = new List<TextSnapshot>();

            foreach (ExtendedTextCase expected in cases)
            {
                TextSelection match = null;

                foreach (TextSelection selection in selections)
                {
                    if (string.Equals(
                        selection.Snapshot.EntityType,
                        expected.EntityType,
                        StringComparison.Ordinal) &&
                        string.Equals(
                            selection.Text,
                            expected.OriginalText,
                            StringComparison.Ordinal))
                    {
                        if (match != null)
                        {
                            FailExtendedApply(
                                document.Editor,
                                "destino duplicado " + expected.EntityType);
                            return;
                        }

                        match = selection;
                    }
                }

                if (match == null)
                {
                    FailExtendedApply(
                        document.Editor,
                        "no se detectó " + expected.EntityType);
                    return;
                }

                pendingExtendedTextVerification.Add(match.Snapshot);
                operations.Add(new AtomicTextWriteOperation(
                    match.TargetId,
                    match.Snapshot,
                    expected.ApprovedText));
            }

            AtomicTextWriteResult result = context.ApplyBatch(operations);
            bool updated = result.Status == AtomicTextWriteStatus.Applied &&
                result.AppliedCount == cases.Length;

            for (int index = 0; index < cases.Length && updated; index++)
            {
                updated = HasExpectedText(
                    document.Database,
                    pendingExtendedTextVerification[index].ObjectHandle,
                    cases[index].ApprovedText);
            }

            if (!updated)
            {
                FailExtendedApply(
                    document.Editor,
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "estado {0}; aplicados {1}",
                        result.Status,
                        result.AppliedCount));
                return;
            }

            document.Editor.WriteMessage(
                "\nREADY EXTENDED-01: 5 TARGETS APPLIED; RUN ONE U, THEN AISPELLTESTEXTENDEDVERIFY.");
        }

        [CommandMethod("AISPELLTESTEXTENDEDVERIFY")]
        public void VerifyExtendedTextFixture()
        {
            Document document = AcadApplication.DocumentManager.MdiActiveDocument;

            if (document == null)
                return;

            if (pendingExtendedTextVerification == null ||
                pendingExtendedTextVerification.Count != 5)
            {
                document.Editor.WriteMessage(
                    "\nFAIL EXTENDED-01: no existe una prueba pendiente completa.");
                return;
            }

            bool restored = true;

            foreach (TextSnapshot snapshot in pendingExtendedTextVerification)
            {
                restored = restored && HasExpectedText(
                    document.Database,
                    snapshot.ObjectHandle,
                    snapshot.OriginalText);
            }

            pendingExtendedTextVerification = null;
            document.Editor.WriteMessage(
                restored
                    ? "\nPASS EXTENDED-01: MLEADER, TABLE CELL, BLOCK TEXT AND ATTRIBUTE RESTORED BY ONE U."
                    : "\nFAIL EXTENDED-01: una sola U no restauró los cinco destinos.");
        }

        private static void FailExtendedApply(Editor editor, string reason)
        {
            pendingExtendedTextVerification = null;
            editor.WriteMessage(
                "\nFAIL EXTENDED-01: {0}. No continúe la prueba.",
                reason);
        }

        private static ExtendedTextCase[] CreateExtendedTextCases()
        {
            return new[]
            {
                new ExtendedTextCase(
                    "MLeader",
                    "PRUEBA GEOMENBRANAA",
                    "PRUEBA GEOMEMBRANA"),
                new ExtendedTextCase(
                    "TableCell",
                    "PRUEBA CARRETERAA",
                    "PRUEBA CARRETERA"),
                new ExtendedTextCase(
                    "TableCell",
                    "PRUEBA ALINEAMIENTOO",
                    "PRUEBA ALINEAMIENTO"),
                new ExtendedTextCase(
                    "DBText",
                    "PRUEBA SUVRASANTE",
                    "PRUEBA SUBRASANTE"),
                new ExtendedTextCase(
                    "AttributeReference",
                    "PRUEBA DRENAGE",
                    "PRUEBA DRENAJE")
            };
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

        private sealed class ExtendedTextCase
        {
            public ExtendedTextCase(
                string entityType,
                string originalText,
                string approvedText)
            {
                EntityType = entityType;
                OriginalText = originalText;
                ApprovedText = approvedText;
            }

            public string EntityType { get; private set; }

            public string OriginalText { get; private set; }

            public string ApprovedText { get; private set; }
        }
    }
}
#endif
