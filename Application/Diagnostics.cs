using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Application
{
    public enum DiagnosticCommand
    {
        AiSpell,
        AiSpellAll,
        AiSpellSettings,
        Regression
    }

    public enum DiagnosticSeverity
    {
        Information,
        Warning,
        Error
    }

    public enum DiagnosticCode
    {
        CommandCompleted,
        SelectionCancelled,
        SelectionInvalid,
        ValidationBlocked,
        OperationCancelled,
        Conflict,
        DocumentMismatch,
        ConfigurationMissing,
        ConfigurationFailure,
        NetworkUnavailable,
        AuthenticationRejected,
        Timeout,
        InvalidResponse,
        WriteInvalidTarget,
        WriteFailure,
        UnexpectedFailure
    }

    public static class DiagnosticCatalog
    {
        public static string GetCode(DiagnosticCode code)
        {
            switch (code)
            {
                case DiagnosticCode.CommandCompleted: return "CMD-000";
                case DiagnosticCode.SelectionCancelled: return "SEL-001";
                case DiagnosticCode.SelectionInvalid: return "SEL-002";
                case DiagnosticCode.ValidationBlocked: return "VAL-001";
                case DiagnosticCode.OperationCancelled: return "CAN-001";
                case DiagnosticCode.Conflict: return "CON-001";
                case DiagnosticCode.DocumentMismatch: return "DOC-001";
                case DiagnosticCode.ConfigurationMissing: return "CFG-001";
                case DiagnosticCode.ConfigurationFailure: return "CFG-002";
                case DiagnosticCode.NetworkUnavailable: return "NET-001";
                case DiagnosticCode.AuthenticationRejected: return "AUT-001";
                case DiagnosticCode.Timeout: return "TMO-001";
                case DiagnosticCode.InvalidResponse: return "RSP-001";
                case DiagnosticCode.WriteInvalidTarget: return "WRT-001";
                case DiagnosticCode.WriteFailure: return "WRT-002";
                case DiagnosticCode.UnexpectedFailure: return "GEN-001";
                default: throw new ArgumentOutOfRangeException("code");
            }
        }

        public static string GetCommand(DiagnosticCommand command)
        {
            switch (command)
            {
                case DiagnosticCommand.AiSpell: return "AISPELL";
                case DiagnosticCommand.AiSpellAll: return "AISPELLALL";
                case DiagnosticCommand.AiSpellSettings: return "AISPELLSETTINGS";
                case DiagnosticCommand.Regression: return "REGRESSION";
                default: throw new ArgumentOutOfRangeException("command");
            }
        }
    }

    public static class DiagnosticClassifier
    {
        public static DiagnosticCode FromProviderFailure(ProviderFailureKind kind)
        {
            switch (kind)
            {
                case ProviderFailureKind.Configuration:
                    return DiagnosticCode.ConfigurationMissing;
                case ProviderFailureKind.Authentication:
                    return DiagnosticCode.AuthenticationRejected;
                case ProviderFailureKind.Network:
                case ProviderFailureKind.Unavailable:
                    return DiagnosticCode.NetworkUnavailable;
                case ProviderFailureKind.Timeout:
                    return DiagnosticCode.Timeout;
                case ProviderFailureKind.InvalidResponse:
                    return DiagnosticCode.InvalidResponse;
                default:
                    return DiagnosticCode.UnexpectedFailure;
            }
        }

        public static DiagnosticCode FromWriteStatus(AtomicTextWriteStatus status)
        {
            switch (status)
            {
                case AtomicTextWriteStatus.Applied:
                case AtomicTextWriteStatus.NoChange:
                    return DiagnosticCode.CommandCompleted;
                case AtomicTextWriteStatus.Conflict:
                    return DiagnosticCode.Conflict;
                case AtomicTextWriteStatus.DocumentMismatch:
                    return DiagnosticCode.DocumentMismatch;
                case AtomicTextWriteStatus.InvalidTarget:
                    return DiagnosticCode.WriteInvalidTarget;
                default:
                    return DiagnosticCode.WriteFailure;
            }
        }

        public static DiagnosticCode FromException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            Exception current = exception;

            while (current != null)
            {
                DiagnosticCode? classified = TryClassify(current);

                if (classified.HasValue)
                    return classified.Value;

                AggregateException aggregate = current as AggregateException;

                if (aggregate != null)
                {
                    foreach (Exception inner in aggregate.Flatten().InnerExceptions)
                    {
                        DiagnosticCode nested = FromException(inner);

                        if (nested != DiagnosticCode.UnexpectedFailure)
                            return nested;
                    }
                }

                current = current.InnerException;
            }

            return DiagnosticCode.UnexpectedFailure;
        }

        private static DiagnosticCode? TryClassify(Exception exception)
        {
            CorrectionProviderException provider =
                exception as CorrectionProviderException;

            if (provider != null)
                return FromProviderFailure(provider.Kind);

            if (exception is TimeoutException)
                return DiagnosticCode.Timeout;

            if (exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException ||
                exception is System.Runtime.Serialization.SerializationException)
            {
                return DiagnosticCode.ConfigurationFailure;
            }

            if (exception is OperationCanceledException)
                return DiagnosticCode.OperationCancelled;

            return null;
        }
    }

    public static class UserFacingError
    {
        public static string Create(string message, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Se requiere un mensaje seguro.", "message");

            if (exception == null)
                throw new ArgumentNullException("exception");

            string normalized = message.Trim();

            if (!normalized.EndsWith(".", StringComparison.Ordinal))
                normalized += ".";

            return normalized + " Código de soporte: " +
                DiagnosticCatalog.GetCode(
                    DiagnosticClassifier.FromException(exception)) +
                ".";
        }
    }

    public sealed class DiagnosticEvent
    {
        public DiagnosticEvent(
            DateTime timestampUtc,
            string pluginVersion,
            DiagnosticCommand command,
            DiagnosticCode code,
            DiagnosticSeverity severity,
            long durationMilliseconds,
            int itemCount)
        {
            Version parsedVersion;

            if (!Version.TryParse(pluginVersion, out parsedVersion))
                throw new ArgumentException("Se requiere una versión válida.", "pluginVersion");

            if (!Enum.IsDefined(typeof(DiagnosticCommand), command))
                throw new ArgumentOutOfRangeException("command");

            if (!Enum.IsDefined(typeof(DiagnosticCode), code))
                throw new ArgumentOutOfRangeException("code");

            if (!Enum.IsDefined(typeof(DiagnosticSeverity), severity))
                throw new ArgumentOutOfRangeException("severity");

            if (durationMilliseconds < 0)
                throw new ArgumentOutOfRangeException("durationMilliseconds");

            if (itemCount < 0)
                throw new ArgumentOutOfRangeException("itemCount");

            TimestampUtc = timestampUtc.Kind == DateTimeKind.Utc
                ? timestampUtc
                : timestampUtc.ToUniversalTime();
            PluginVersion = parsedVersion.ToString();
            Command = command;
            Code = code;
            Severity = severity;
            DurationMilliseconds = durationMilliseconds;
            ItemCount = itemCount;
        }

        public DateTime TimestampUtc { get; private set; }

        public string PluginVersion { get; private set; }

        public DiagnosticCommand Command { get; private set; }

        public DiagnosticCode Code { get; private set; }

        public DiagnosticSeverity Severity { get; private set; }

        public long DurationMilliseconds { get; private set; }

        public int ItemCount { get; private set; }
    }

    public interface IDiagnosticSink
    {
        void Record(DiagnosticEvent diagnosticEvent);
    }

    public sealed class NullDiagnosticSink : IDiagnosticSink
    {
        public void Record(DiagnosticEvent diagnosticEvent)
        {
        }
    }

    public sealed class DiagnosticOperation : IDisposable
    {
        private readonly IDiagnosticSink sink;
        private readonly DiagnosticCommand command;
        private readonly string pluginVersion;
        private readonly Stopwatch stopwatch;
        private bool completed;

        public DiagnosticOperation(
            IDiagnosticSink sink,
            DiagnosticCommand command,
            string pluginVersion)
        {
            if (sink == null)
                throw new ArgumentNullException("sink");

            Version parsedVersion;

            if (!Version.TryParse(pluginVersion, out parsedVersion))
                throw new ArgumentException("Se requiere una versión válida.", "pluginVersion");

            this.sink = sink;
            this.command = command;
            this.pluginVersion = parsedVersion.ToString();
            stopwatch = Stopwatch.StartNew();
        }

        public void Complete(
            DiagnosticCode code,
            DiagnosticSeverity severity,
            int itemCount)
        {
            if (completed)
                return;

            completed = true;
            stopwatch.Stop();
            RecordCore(code, severity, itemCount);
        }

        public void Record(
            DiagnosticCode code,
            DiagnosticSeverity severity,
            int itemCount)
        {
            if (completed)
                return;

            RecordCore(code, severity, itemCount);
        }

        public void Suppress()
        {
            if (completed)
                return;

            completed = true;
            stopwatch.Stop();
        }

        private void RecordCore(
            DiagnosticCode code,
            DiagnosticSeverity severity,
            int itemCount)
        {
            try
            {
                sink.Record(new DiagnosticEvent(
                    DateTime.UtcNow,
                    pluginVersion,
                    command,
                    code,
                    severity,
                    stopwatch.ElapsedMilliseconds,
                    itemCount));
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (!completed)
            {
                Complete(
                    DiagnosticCode.UnexpectedFailure,
                    DiagnosticSeverity.Error,
                    0);
            }
        }
    }
}
