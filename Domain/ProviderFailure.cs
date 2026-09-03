using System;

namespace CivilSpellAI.Domain
{
    public enum ProviderFailureKind
    {
        Unavailable,
        Configuration,
        Authentication,
        Network,
        Timeout,
        InvalidResponse,
        Unexpected
    }

    public sealed class ProviderFailure
    {
        public ProviderFailure(
            string providerName,
            ProviderFailureKind kind,
            string message)
        {
            ProviderName = string.IsNullOrWhiteSpace(providerName)
                ? "Proveedor"
                : providerName;
            Kind = kind;
            Message = message ?? string.Empty;
        }

        public string ProviderName { get; private set; }

        public ProviderFailureKind Kind { get; private set; }

        public string Message { get; private set; }
    }

    public sealed class CorrectionProviderException : Exception
    {
        public CorrectionProviderException(
            ProviderFailureKind kind,
            string message)
            : base(message)
        {
            Kind = kind;
        }

        public ProviderFailureKind Kind { get; private set; }
    }
}
