using System;
using System.Collections.Generic;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Application
{
    public sealed class TextSelection
    {
        public TextSelection(string targetId, TextSnapshot snapshot)
            : this(targetId, snapshot, "Desconocido")
        {
        }

        public TextSelection(
            string targetId,
            TextSnapshot snapshot,
            string locationName)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("Se requiere el destino seleccionado.", "targetId");

            if (snapshot == null)
                throw new ArgumentNullException("snapshot");

            TargetId = targetId;
            Snapshot = snapshot;
            LocationName = string.IsNullOrWhiteSpace(locationName)
                ? "Desconocido"
                : locationName.Trim();
        }

        public string TargetId { get; private set; }

        public TextSnapshot Snapshot { get; private set; }

        public string LocationName { get; private set; }

        public string Text
        {
            get { return Snapshot.OriginalText; }
        }
    }

    public interface ITextDocumentProvider
    {
        ITextDocumentContext GetActiveDocument();
    }

    public interface ITextDocumentContext
    {
        string DocumentId { get; }

        TextSelection SelectText();

        IList<TextSelection> ScanAllTexts();

        AtomicTextWriteResult Apply(AtomicTextWriteOperation operation);

        AtomicTextWriteResult ApplyBatch(
            IEnumerable<AtomicTextWriteOperation> operations);

        void WriteMessage(string format, params object[] arguments);
    }

    public interface ITextDocumentAdapter
    {
        string DocumentId { get; }

        TextSelection SelectText();

        IList<TextSelection> ScanAllTexts();

        AtomicTextWriteResult Apply(AtomicTextWriteOperation operation);

        AtomicTextWriteResult ApplyBatch(
            IEnumerable<AtomicTextWriteOperation> operations);
    }

    public interface ITextDocumentState
    {
        bool IsCurrent(string documentId);

        void WriteMessage(string format, params object[] arguments);
    }

    public sealed class TextDocumentContext : ITextDocumentContext
    {
        private readonly ITextDocumentAdapter adapter;
        private readonly ITextDocumentState documentState;

        public TextDocumentContext(
            ITextDocumentAdapter adapter,
            ITextDocumentState documentState)
        {
            if (adapter == null)
                throw new ArgumentNullException("adapter");

            if (documentState == null)
                throw new ArgumentNullException("documentState");

            this.adapter = adapter;
            this.documentState = documentState;
            DocumentId = adapter.DocumentId;
        }

        public string DocumentId { get; private set; }

        public TextSelection SelectText()
        {
            return IsCurrent() ? adapter.SelectText() : null;
        }

        public IList<TextSelection> ScanAllTexts()
        {
            if (!IsCurrent())
                return new List<TextSelection>().AsReadOnly();

            return adapter.ScanAllTexts();
        }

        public AtomicTextWriteResult Apply(AtomicTextWriteOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");

            if (!IsCurrent())
                return DocumentMismatch(operation.Snapshot.ObjectHandle);

            return adapter.Apply(operation);
        }

        public AtomicTextWriteResult ApplyBatch(
            IEnumerable<AtomicTextWriteOperation> operations)
        {
            if (operations == null)
                throw new ArgumentNullException("operations");

            List<AtomicTextWriteOperation> pending =
                new List<AtomicTextWriteOperation>(operations);

            if (pending.Count == 0)
            {
                return new AtomicTextWriteResult(
                    AtomicTextWriteStatus.NoChange,
                    0,
                    null);
            }

            if (!IsCurrent())
            {
                AtomicTextWriteOperation first = pending[0];
                return DocumentMismatch(
                    first == null ? null : first.Snapshot.ObjectHandle);
            }

            return adapter.ApplyBatch(pending);
        }

        public void WriteMessage(string format, params object[] arguments)
        {
            documentState.WriteMessage(format, arguments);
        }

        private bool IsCurrent()
        {
            return documentState.IsCurrent(DocumentId);
        }

        private static AtomicTextWriteResult DocumentMismatch(string handle)
        {
            return new AtomicTextWriteResult(
                AtomicTextWriteStatus.DocumentMismatch,
                0,
                handle);
        }
    }
}
