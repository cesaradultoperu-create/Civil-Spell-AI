using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using CivilSpellAI.Application;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Autodesk
{
    public enum TextWriteStatus
    {
        Applied,
        NoChange,
        Conflict,
        DocumentMismatch,
        InvalidTarget
    }

    public static class TextWriter
    {
        public static TextWriteStatus Apply(
            Database database,
            ObjectId objectId,
            TextSnapshot snapshot,
            string approvedText)
        {
            if (database == null)
                throw new ArgumentNullException("database");

            if (snapshot == null)
                throw new ArgumentNullException("snapshot");

            if (approvedText == null)
                throw new ArgumentNullException("approvedText");

            string targetId = GetTargetId(objectId);
            Dictionary<string, ObjectId> targets =
                new Dictionary<string, ObjectId>(StringComparer.Ordinal)
                {
                    { targetId, objectId }
                };
            AtomicTextWriteResult result = AtomicTextWriter.Apply(
                new AutodeskTextStore(database, targets),
                new AtomicTextWriteOperation(targetId, snapshot, approvedText));
            return Map(result.Status);
        }

        public static TextBatchWriteResult ApplyBatch(
            Database database,
            IEnumerable<TextWriteRequest> requests)
        {
            if (database == null)
                throw new ArgumentNullException("database");

            if (requests == null)
                throw new ArgumentNullException("requests");

            List<TextWriteRequest> pending = new List<TextWriteRequest>(requests);

            if (pending.Count == 0)
                return new TextBatchWriteResult(TextWriteStatus.NoChange, 0, null);

            Dictionary<string, ObjectId> targets =
                new Dictionary<string, ObjectId>(StringComparer.Ordinal);
            List<AtomicTextWriteOperation> operations =
                new List<AtomicTextWriteOperation>();

            foreach (TextWriteRequest request in pending)
            {
                if (request == null)
                {
                    return new TextBatchWriteResult(
                        TextWriteStatus.InvalidTarget,
                        0,
                        null);
                }

                string targetId = GetTargetId(request.ObjectId);
                targets[targetId] = request.ObjectId;
                operations.Add(new AtomicTextWriteOperation(
                    targetId,
                    request.Snapshot,
                    request.ApprovedText));
            }

            AtomicTextWriteResult result = AtomicTextWriter.ApplyBatch(
                new AutodeskTextStore(database, targets),
                operations);
            return new TextBatchWriteResult(
                Map(result.Status),
                result.AppliedCount,
                result.FailedHandle);
        }

        private static string GetTargetId(ObjectId objectId)
        {
            return objectId.ToString();
        }

        private static TextWriteStatus Map(AtomicTextWriteStatus status)
        {
            switch (status)
            {
                case AtomicTextWriteStatus.Applied:
                    return TextWriteStatus.Applied;
                case AtomicTextWriteStatus.NoChange:
                    return TextWriteStatus.NoChange;
                case AtomicTextWriteStatus.Conflict:
                    return TextWriteStatus.Conflict;
                case AtomicTextWriteStatus.DocumentMismatch:
                    return TextWriteStatus.DocumentMismatch;
                default:
                    return TextWriteStatus.InvalidTarget;
            }
        }

        private sealed class AutodeskTextStore : IAtomicTextStore
        {
            private readonly Database database;
            private readonly IDictionary<string, ObjectId> targets;

            public AutodeskTextStore(
                Database database,
                IDictionary<string, ObjectId> targets)
            {
                this.database = database;
                this.targets = targets;
            }

            public IAtomicTextTransaction BeginTransaction()
            {
                return new AutodeskTextTransaction(database, targets);
            }
        }

        private sealed class AutodeskTextTransaction : IAtomicTextTransaction
        {
            private readonly Transaction transaction;
            private readonly IDictionary<string, ObjectId> targets;
            private readonly Dictionary<string, Entity> loadedEntities;

            public AutodeskTextTransaction(
                Database database,
                IDictionary<string, ObjectId> targets)
            {
                transaction = database.TransactionManager.StartTransaction();
                this.targets = targets;
                loadedEntities = new Dictionary<string, Entity>(StringComparer.Ordinal);
                DocumentId = SelectionHelper.GetDocumentId(database);
            }

            public string DocumentId { get; private set; }

            public bool TryRead(string targetId, out TextTargetState state)
            {
                state = null;
                ObjectId objectId;

                if (!targets.TryGetValue(targetId, out objectId) ||
                    !objectId.IsValid ||
                    objectId.IsNull ||
                    objectId.IsErased)
                {
                    return false;
                }

                Entity entity = transaction.GetObject(
                    objectId,
                    OpenMode.ForRead,
                    false) as Entity;
                string currentText;

                if (entity == null || !TextEditor.TryGetText(entity, out currentText))
                    return false;

                loadedEntities[targetId] = entity;
                state = new TextTargetState(
                    entity.Handle.ToString(),
                    entity.GetType().Name,
                    currentText);
                return true;
            }

            public void Write(string targetId, string approvedText)
            {
                Entity entity;

                if (!loadedEntities.TryGetValue(targetId, out entity))
                    throw new InvalidOperationException("El destino no fue validado antes de escribir.");

                entity.UpgradeOpen();
                TextEditor.ReplaceText(entity, approvedText);
            }

            public void Commit()
            {
                transaction.Commit();
            }

            public void Dispose()
            {
                transaction.Dispose();
            }
        }
    }

    public sealed class TextWriteRequest
    {
        public TextWriteRequest(
            ObjectId objectId,
            TextSnapshot snapshot,
            string approvedText)
        {
            if (snapshot == null)
                throw new ArgumentNullException("snapshot");

            if (approvedText == null)
                throw new ArgumentNullException("approvedText");

            ObjectId = objectId;
            Snapshot = snapshot;
            ApprovedText = approvedText;
        }

        public ObjectId ObjectId { get; private set; }

        public TextSnapshot Snapshot { get; private set; }

        public string ApprovedText { get; private set; }
    }

    public sealed class TextBatchWriteResult
    {
        public TextBatchWriteResult(
            TextWriteStatus status,
            int appliedCount,
            string failedHandle)
        {
            Status = status;
            AppliedCount = appliedCount;
            FailedHandle = failedHandle;
        }

        public TextWriteStatus Status { get; private set; }

        public int AppliedCount { get; private set; }

        public string FailedHandle { get; private set; }
    }
}
