using System;
using System.Collections.Generic;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Application
{
    public enum AtomicTextWriteStatus
    {
        Applied,
        NoChange,
        Conflict,
        DocumentMismatch,
        InvalidTarget
    }

    public sealed class AtomicTextWriteOperation
    {
        public AtomicTextWriteOperation(
            string targetId,
            TextSnapshot snapshot,
            string approvedText)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("Se requiere el destino de escritura.", "targetId");

            if (snapshot == null)
                throw new ArgumentNullException("snapshot");

            if (approvedText == null)
                throw new ArgumentNullException("approvedText");

            TargetId = targetId;
            Snapshot = snapshot;
            ApprovedText = approvedText;
        }

        public string TargetId { get; private set; }

        public TextSnapshot Snapshot { get; private set; }

        public string ApprovedText { get; private set; }
    }

    public sealed class TextTargetState
    {
        public TextTargetState(string objectHandle, string entityType, string currentText)
        {
            if (string.IsNullOrWhiteSpace(objectHandle))
                throw new ArgumentException("Se requiere el handle actual.", "objectHandle");

            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Se requiere el tipo actual.", "entityType");

            ObjectHandle = objectHandle;
            EntityType = entityType;
            CurrentText = currentText ?? string.Empty;
        }

        public string ObjectHandle { get; private set; }

        public string EntityType { get; private set; }

        public string CurrentText { get; private set; }
    }

    public sealed class AtomicTextWriteResult
    {
        public AtomicTextWriteResult(
            AtomicTextWriteStatus status,
            int appliedCount,
            string failedHandle)
        {
            Status = status;
            AppliedCount = appliedCount;
            FailedHandle = failedHandle;
        }

        public AtomicTextWriteStatus Status { get; private set; }

        public int AppliedCount { get; private set; }

        public string FailedHandle { get; private set; }
    }

    public interface IAtomicTextStore
    {
        IAtomicTextTransaction BeginTransaction();
    }

    public interface IAtomicTextTransaction : IDisposable
    {
        string DocumentId { get; }

        bool TryRead(string targetId, out TextTargetState state);

        void Write(string targetId, string approvedText);

        void Commit();
    }

    public static class AtomicTextWriter
    {
        public static AtomicTextWriteResult Apply(
            IAtomicTextStore store,
            AtomicTextWriteOperation operation)
        {
            if (store == null)
                throw new ArgumentNullException("store");

            if (operation == null)
                throw new ArgumentNullException("operation");

            using (IAtomicTextTransaction transaction = store.BeginTransaction())
            {
                AtomicTextWriteStatus validation = Validate(
                    transaction,
                    operation,
                    true);

                if (validation != AtomicTextWriteStatus.Applied)
                {
                    return new AtomicTextWriteResult(
                        validation,
                        0,
                        operation.Snapshot.ObjectHandle);
                }

                transaction.Write(operation.TargetId, operation.ApprovedText);
                transaction.Commit();
                return new AtomicTextWriteResult(
                    AtomicTextWriteStatus.Applied,
                    1,
                    null);
            }
        }

        public static AtomicTextWriteResult ApplyBatch(
            IAtomicTextStore store,
            IEnumerable<AtomicTextWriteOperation> operations)
        {
            if (store == null)
                throw new ArgumentNullException("store");

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

            using (IAtomicTextTransaction transaction = store.BeginTransaction())
            {
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (AtomicTextWriteOperation operation in pending)
                {
                    if (operation == null || !seen.Add(operation.TargetId))
                    {
                        return new AtomicTextWriteResult(
                            AtomicTextWriteStatus.InvalidTarget,
                            0,
                            operation == null ? null : operation.Snapshot.ObjectHandle);
                    }

                    AtomicTextWriteStatus validation = Validate(
                        transaction,
                        operation,
                        true);

                    if (validation != AtomicTextWriteStatus.Applied)
                    {
                        return new AtomicTextWriteResult(
                            validation,
                            0,
                            operation.Snapshot.ObjectHandle);
                    }
                }

                foreach (AtomicTextWriteOperation operation in pending)
                    transaction.Write(operation.TargetId, operation.ApprovedText);

                transaction.Commit();
                return new AtomicTextWriteResult(
                    AtomicTextWriteStatus.Applied,
                    pending.Count,
                    null);
            }
        }

        private static AtomicTextWriteStatus Validate(
            IAtomicTextTransaction transaction,
            AtomicTextWriteOperation operation,
            bool detectNoChange)
        {
            TextSnapshot snapshot = operation.Snapshot;

            if (!string.Equals(
                transaction.DocumentId,
                snapshot.DocumentId,
                StringComparison.OrdinalIgnoreCase))
            {
                return AtomicTextWriteStatus.DocumentMismatch;
            }

            TextTargetState state;

            if (!transaction.TryRead(operation.TargetId, out state) || state == null)
                return AtomicTextWriteStatus.InvalidTarget;

            if (!string.Equals(
                    state.ObjectHandle,
                    snapshot.ObjectHandle,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    state.EntityType,
                    snapshot.EntityType,
                    StringComparison.Ordinal))
            {
                return AtomicTextWriteStatus.InvalidTarget;
            }

            if (detectNoChange && string.Equals(
                operation.ApprovedText,
                snapshot.OriginalText,
                StringComparison.Ordinal))
            {
                return AtomicTextWriteStatus.NoChange;
            }

            if (!string.Equals(
                state.CurrentText,
                snapshot.OriginalText,
                StringComparison.Ordinal))
            {
                return AtomicTextWriteStatus.Conflict;
            }

            return AtomicTextWriteStatus.Applied;
        }
    }
}
