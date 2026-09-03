using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using CivilSpellAI.Application;

namespace CivilSpellAI.Infrastructure
{
    public static class DiagnosticOperationFactory
    {
        public static DiagnosticOperation Create(
            DiagnosticCommand command,
            UserConfigurationStore settingsStore)
        {
            if (settingsStore == null)
                throw new ArgumentNullException("settingsStore");

            UserSettings settings = settingsStore.Load();
            IDiagnosticSink sink = settings.DiagnosticsEnabled
                ? (IDiagnosticSink)new SafeDiagnosticFileSink(Path.Combine(
                    settingsStore.ConfigurationDirectory,
                    "diagnostics"))
                : new NullDiagnosticSink();
            string version = typeof(DiagnosticOperationFactory)
                .Assembly
                .GetName()
                .Version
                .ToString();
            return new DiagnosticOperation(sink, command, version);
        }
    }

    public sealed class SafeDiagnosticFileSink : IDiagnosticSink
    {
        public const long DefaultMaximumFileBytes = 2L * 1024L * 1024L;
        private readonly string diagnosticsDirectory;
        private readonly long maximumFileBytes;

        public SafeDiagnosticFileSink(string diagnosticsDirectory)
            : this(diagnosticsDirectory, DefaultMaximumFileBytes)
        {
        }

        public SafeDiagnosticFileSink(
            string diagnosticsDirectory,
            long maximumFileBytes)
        {
            if (string.IsNullOrWhiteSpace(diagnosticsDirectory))
                throw new ArgumentException("Se requiere el directorio diagnóstico.", "diagnosticsDirectory");

            if (maximumFileBytes < 1)
                throw new ArgumentOutOfRangeException("maximumFileBytes");

            this.diagnosticsDirectory = diagnosticsDirectory;
            this.maximumFileBytes = maximumFileBytes;
        }

        public string EventsPath
        {
            get { return Path.Combine(diagnosticsDirectory, "events.jsonl"); }
        }

        public string PreviousEventsPath
        {
            get { return Path.Combine(diagnosticsDirectory, "events.previous.jsonl"); }
        }

        public void Record(DiagnosticEvent diagnosticEvent)
        {
            if (diagnosticEvent == null)
                throw new ArgumentNullException("diagnosticEvent");

            try
            {
                Directory.CreateDirectory(diagnosticsDirectory);
                RotateIfNeeded();
                File.AppendAllText(
                    EventsPath,
                    DiagnosticEventSerializer.Serialize(diagnosticEvent) +
                        Environment.NewLine,
                    new UTF8Encoding(false));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (System.Security.SecurityException)
            {
            }
            catch (NotSupportedException)
            {
            }
        }

        private void RotateIfNeeded()
        {
            if (!File.Exists(EventsPath) ||
                new FileInfo(EventsPath).Length < maximumFileBytes)
            {
                return;
            }

            if (File.Exists(PreviousEventsPath))
                File.Delete(PreviousEventsPath);

            File.Move(EventsPath, PreviousEventsPath);
        }
    }

    public sealed class DiagnosticLogManager
    {
        private readonly string diagnosticsDirectory;

        public DiagnosticLogManager(string diagnosticsDirectory)
        {
            if (string.IsNullOrWhiteSpace(diagnosticsDirectory))
                throw new ArgumentException("Se requiere el directorio diagnóstico.", "diagnosticsDirectory");

            this.diagnosticsDirectory = diagnosticsDirectory;
        }

        public string EventsPath
        {
            get { return Path.Combine(diagnosticsDirectory, "events.jsonl"); }
        }

        public string PreviousEventsPath
        {
            get { return Path.Combine(diagnosticsDirectory, "events.previous.jsonl"); }
        }

        public bool HasEvents
        {
            get { return HasContent(EventsPath) || HasContent(PreviousEventsPath); }
        }

        public bool Export(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Se requiere el destino de exportación.", "destinationPath");

            if (!HasEvents)
                return false;

            if (PathsEqual(destinationPath, EventsPath) ||
                PathsEqual(destinationPath, PreviousEventsPath))
            {
                throw new IOException(
                    "El destino de exportación no puede ser el registro diagnóstico interno.");
            }

            AtomicFileExport.Write(
                destinationPath,
                temporaryPath => WriteExportFile(temporaryPath));

            return true;
        }

        public bool Delete()
        {
            bool deleted = false;

            if (File.Exists(EventsPath))
            {
                File.Delete(EventsPath);
                deleted = true;
            }

            if (File.Exists(PreviousEventsPath))
            {
                File.Delete(PreviousEventsPath);
                deleted = true;
            }

            return deleted;
        }

        private static void CopyIfPresent(string sourcePath, Stream destination)
        {
            if (!File.Exists(sourcePath))
                return;

            using (FileStream source = File.OpenRead(sourcePath))
                source.CopyTo(destination);
        }

        private void WriteExportFile(string temporaryPath)
        {
            using (FileStream destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                CopyIfPresent(PreviousEventsPath, destination);
                CopyIfPresent(EventsPath, destination);
            }
        }

        private static bool PathsEqual(string firstPath, string secondPath)
        {
            return string.Equals(
                Path.GetFullPath(firstPath).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),
                Path.GetFullPath(secondPath).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasContent(string path)
        {
            try
            {
                return File.Exists(path) && new FileInfo(path).Length > 0;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
    }

    public static class DiagnosticEventSerializer
    {
        public static string Serialize(DiagnosticEvent diagnosticEvent)
        {
            if (diagnosticEvent == null)
                throw new ArgumentNullException("diagnosticEvent");

            SerializableDiagnosticEvent value = new SerializableDiagnosticEvent
            {
                TimestampUtc = diagnosticEvent.TimestampUtc.ToString("o"),
                Version = diagnosticEvent.PluginVersion,
                Command = DiagnosticCatalog.GetCommand(diagnosticEvent.Command),
                Code = DiagnosticCatalog.GetCode(diagnosticEvent.Code),
                Severity = diagnosticEvent.Severity.ToString(),
                DurationMilliseconds = diagnosticEvent.DurationMilliseconds,
                ItemCount = diagnosticEvent.ItemCount
            };
            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(SerializableDiagnosticEvent));

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        [DataContract]
        private sealed class SerializableDiagnosticEvent
        {
            [DataMember(Name = "timestampUtc", Order = 1)]
            public string TimestampUtc { get; set; }

            [DataMember(Name = "version", Order = 2)]
            public string Version { get; set; }

            [DataMember(Name = "command", Order = 3)]
            public string Command { get; set; }

            [DataMember(Name = "code", Order = 4)]
            public string Code { get; set; }

            [DataMember(Name = "severity", Order = 5)]
            public string Severity { get; set; }

            [DataMember(Name = "durationMs", Order = 6)]
            public long DurationMilliseconds { get; set; }

            [DataMember(Name = "itemCount", Order = 7)]
            public int ItemCount { get; set; }
        }
    }
}
