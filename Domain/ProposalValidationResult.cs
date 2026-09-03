using System;
using System.Collections.Generic;

namespace CivilSpellAI.Domain
{
    public sealed class ValidationIssue
    {
        public ValidationIssue(string code, string message)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Se requiere un código.", "code");

            Code = code;
            Message = message ?? string.Empty;
        }

        public string Code { get; private set; }

        public string Message { get; private set; }
    }

    public sealed class ProposalValidationResult
    {
        public ProposalValidationResult(
            IEnumerable<TextDifference> changes,
            IEnumerable<ValidationIssue> issues)
        {
            Changes = CopyNonNull(changes);
            Issues = CopyNonNull(issues);
        }

        public IList<TextDifference> Changes { get; private set; }

        public IList<ValidationIssue> Issues { get; private set; }

        public bool CanApply
        {
            get { return Issues.Count == 0 && Changes.Count > 0; }
        }

        private static IList<T> CopyNonNull<T>(IEnumerable<T> values)
            where T : class
        {
            List<T> copied = new List<T>();

            foreach (T value in values ?? new T[0])
            {
                if (value != null)
                    copied.Add(value);
            }

            return copied.AsReadOnly();
        }
    }
}
