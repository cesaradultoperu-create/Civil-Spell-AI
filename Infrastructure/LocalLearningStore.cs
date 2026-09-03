using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Infrastructure
{
    public sealed class LearningRecord
    {
        internal LearningRecord(
            string id,
            string sourceText,
            string selectedText,
            ReviewLanguage language,
            bool isEnabled,
            int acceptanceCount,
            string createdUtc,
            string lastUsedUtc)
        {
            Id = id;
            SourceText = sourceText;
            SelectedText = selectedText;
            Language = language;
            IsEnabled = isEnabled;
            AcceptanceCount = acceptanceCount;
            CreatedUtc = createdUtc;
            LastUsedUtc = lastUsedUtc;
        }

        public string Id { get; private set; }

        public string SourceText { get; private set; }

        public string SelectedText { get; private set; }

        public ReviewLanguage Language { get; private set; }

        public bool IsEnabled { get; private set; }

        public int AcceptanceCount { get; private set; }

        public string CreatedUtc { get; private set; }

        public string LastUsedUtc { get; private set; }
    }

    public sealed class LocalLearningStore : ILearningStore
    {
        public const int CurrentSchemaVersion = 1;
        private const int MaximumRecords = 500;
        private const int MaximumCandidateRecords = 5000;
        public const int MaximumTextCharacters = 20000;
        public const long MaximumFileBytes = 16L * 1024L * 1024L;
        private static readonly object FileSync = new object();
        private readonly string filePath;

        public LocalLearningStore(string configurationDirectory)
        {
            if (string.IsNullOrWhiteSpace(configurationDirectory))
            {
                throw new ArgumentException(
                    "Se requiere el directorio de configuración.",
                    "configurationDirectory");
            }

            filePath = Path.Combine(configurationDirectory, "learning.v1.json");
        }

        public string FilePath
        {
            get { return filePath; }
        }

        public IReadOnlyList<CorrectionProposal> FindSuggestions(
            CorrectionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            string sourceKey = NormalizeKey(request.Text);

            lock (FileSync)
            {
                LearningFile file = LoadFile();
                return file.Records
                    .Where(record => record.IsEnabled &&
                        string.Equals(
                            record.SourceKey,
                            sourceKey,
                            StringComparison.Ordinal) &&
                        !string.Equals(
                            record.SelectedText,
                            request.Text,
                            StringComparison.Ordinal))
                    .OrderByDescending(record => record.AcceptanceCount)
                    .ThenByDescending(record => record.LastUsedUtc)
                    .Take(request.MaximumAlternatives)
                    .Select(record => new CorrectionProposal(
                        record.SelectedText,
                        ProposalSource.LearnedPreference,
                        ParseLanguage(record.Language),
                        "Preferencia local recordada y sujeta a nueva validación.",
                        null,
                        null))
                    .ToList()
                    .AsReadOnly();
            }
        }

        public void Record(CorrectionRequest request, ReviewDecision decision)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (decision == null)
                throw new ArgumentNullException("decision");

            if (!decision.RememberPreference ||
                (decision.Kind != ReviewDecisionKind.ApplyProposal &&
                 decision.Kind != ReviewDecisionKind.ManualEdit) ||
                string.IsNullOrWhiteSpace(decision.SelectedText) ||
                request.Text.Length > MaximumTextCharacters ||
                decision.SelectedText.Length > MaximumTextCharacters ||
                string.Equals(
                    request.Text,
                    decision.SelectedText,
                    StringComparison.Ordinal))
                return;

            CorrectionProposal proposal = new CorrectionProposal(
                decision.SelectedText,
                ProposalSource.LearnedPreference,
                request.Language,
                "Preferencia local.",
                null,
                null);

            if (!new TechnicalTokenValidator().Validate(request, proposal).CanApply)
                return;

            lock (FileSync)
            {
                LearningFile file = LoadFile();
                string sourceKey = NormalizeKey(request.Text);
                LearningRecordData existing = file.Records.FirstOrDefault(record =>
                    string.Equals(record.SourceKey, sourceKey, StringComparison.Ordinal) &&
                    string.Equals(
                        record.SelectedText,
                        decision.SelectedText,
                        StringComparison.Ordinal));
                string now = DateTime.UtcNow.ToString("o");

                if (existing == null)
                {
                    file.Records.Add(new LearningRecordData
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        SourceText = request.Text,
                        SourceKey = sourceKey,
                        SelectedText = decision.SelectedText,
                        Language = request.Language.ToString(),
                        IsEnabled = true,
                        AcceptanceCount = 1,
                        CreatedUtc = now,
                        LastUsedUtc = now
                    });
                }
                else
                {
                    existing.SourceText = request.Text;
                    existing.Language = request.Language.ToString();
                    existing.IsEnabled = true;
                    if (existing.AcceptanceCount < int.MaxValue)
                        existing.AcceptanceCount++;
                    existing.LastUsedUtc = now;
                }

                file.Records = file.Records
                    .OrderByDescending(record => record.LastUsedUtc)
                    .Take(MaximumRecords)
                    .ToList();
                SaveFile(file);
            }
        }

        public IList<LearningRecord> GetRecords()
        {
            lock (FileSync)
            {
                return LoadFile().Records
                    .OrderByDescending(record => record.LastUsedUtc)
                    .Select(ToPublicRecord)
                    .ToList()
                    .AsReadOnly();
            }
        }

        public void UpdateEnabledStates(IDictionary<string, bool> enabledStates)
        {
            if (enabledStates == null)
                throw new ArgumentNullException("enabledStates");

            lock (FileSync)
            {
                LearningFile file = LoadFile();
                bool changed = false;

                foreach (LearningRecordData record in file.Records)
                {
                    bool enabled;

                    if (enabledStates.TryGetValue(record.Id, out enabled) &&
                        record.IsEnabled != enabled)
                    {
                        record.IsEnabled = enabled;
                        changed = true;
                    }
                }

                if (!changed)
                    return;

                SaveFile(file);
            }
        }

        public bool Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            lock (FileSync)
            {
                LearningFile file = LoadFile();
                int removed = file.Records.RemoveAll(record =>
                    string.Equals(record.Id, id, StringComparison.Ordinal));

                if (removed == 0)
                    return false;

                SaveFile(file);
                return true;
            }
        }

        public void Clear()
        {
            lock (FileSync)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                string temporaryPath = filePath + ".tmp";

                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        public bool Export(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Se requiere el destino.", "destinationPath");

            if (PathsEqual(destinationPath, filePath) ||
                PathsEqual(destinationPath, filePath + ".tmp"))
            {
                throw new IOException(
                    "El destino de exportación no puede ser el archivo interno de memoria.");
            }

            lock (FileSync)
            {
                LearningFile file = LoadFile();

                if (file.Records.Count == 0)
                    return false;

                AtomicFileExport.Write(
                    destinationPath,
                    temporaryPath => WriteFile(file, temporaryPath));
                return true;
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

        private LearningFile LoadFile()
        {
            string[] candidates = { filePath, filePath + ".tmp" };

            foreach (string candidate in candidates)
            {
                LearningFile file;

                if (TryLoadFile(candidate, out file))
                    return file;
            }

            return NewFile();
        }

        private static bool TryLoadFile(
            string sourcePath,
            out LearningFile file)
        {
            file = null;

            if (!File.Exists(sourcePath))
                return false;

            try
            {
                if (new FileInfo(sourcePath).Length > MaximumFileBytes)
                    return false;

                using (FileStream stream = File.OpenRead(sourcePath))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(LearningFile));
                    file = serializer.ReadObject(stream) as LearningFile;

                    if (file == null ||
                        file.SchemaVersion != CurrentSchemaVersion)
                    {
                        file = null;
                        return false;
                    }

                    if (file.Records == null)
                        file.Records = new List<LearningRecordData>();

                    NormalizeRecords(file);
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (SerializationException)
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
        }

        private void SaveFile(LearningFile file)
        {
            string directory = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directory);
            string temporaryPath = filePath + ".tmp";

            WriteFile(file, temporaryPath);
            UserConfigurationStore.ReplaceFile(temporaryPath, filePath);
        }

        private static void WriteFile(LearningFile file, string destinationPath)
        {
            using (FileStream stream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(LearningFile));
                serializer.WriteObject(stream, file);
            }
        }

        private static LearningFile NewFile()
        {
            return new LearningFile
            {
                SchemaVersion = CurrentSchemaVersion,
                Records = new List<LearningRecordData>()
            };
        }

        private static void NormalizeRecords(LearningFile file)
        {
            List<LearningRecordData> normalized =
                new List<LearningRecordData>();
            HashSet<string> ids =
                new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, LearningRecordData> preferences =
                new Dictionary<string, LearningRecordData>(StringComparer.Ordinal);
            int inspected = 0;

            foreach (LearningRecordData record in file.Records)
            {
                inspected++;

                if (inspected > MaximumCandidateRecords)
                    break;

                if (record == null ||
                    string.IsNullOrWhiteSpace(record.Id) ||
                    string.IsNullOrWhiteSpace(record.SourceText) ||
                    string.IsNullOrWhiteSpace(record.SelectedText) ||
                    record.SourceText.Length > MaximumTextCharacters ||
                    record.SelectedText.Length > MaximumTextCharacters ||
                    string.Equals(
                        record.SourceText,
                        record.SelectedText,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                record.SourceKey = NormalizeKey(record.SourceText);
                record.Language = ParseLanguage(record.Language).ToString();

                if (record.AcceptanceCount < 1)
                    record.AcceptanceCount = 1;

                string preferenceKey = GetPreferenceKey(
                    record.SourceKey,
                    record.SelectedText);
                LearningRecordData duplicate;
                preferences.TryGetValue(preferenceKey, out duplicate);

                if (duplicate != null)
                {
                    long combinedCount =
                        (long)duplicate.AcceptanceCount + record.AcceptanceCount;
                    duplicate.AcceptanceCount = combinedCount > int.MaxValue
                        ? int.MaxValue
                        : (int)combinedCount;

                    if (string.CompareOrdinal(
                        record.LastUsedUtc,
                        duplicate.LastUsedUtc) > 0)
                    {
                        duplicate.SourceText = record.SourceText;
                        duplicate.Language = record.Language;
                        duplicate.IsEnabled = record.IsEnabled;
                        duplicate.LastUsedUtc = record.LastUsedUtc;
                    }

                    if (string.IsNullOrWhiteSpace(duplicate.CreatedUtc) ||
                        (!string.IsNullOrWhiteSpace(record.CreatedUtc) &&
                         string.CompareOrdinal(
                            record.CreatedUtc,
                            duplicate.CreatedUtc) < 0))
                    {
                        duplicate.CreatedUtc = record.CreatedUtc;
                    }

                    continue;
                }

                if (!ids.Add(record.Id))
                    continue;

                normalized.Add(record);
                preferences.Add(preferenceKey, record);
            }

            file.Records = normalized
                .OrderByDescending(record => record.LastUsedUtc)
                .Take(MaximumRecords)
                .ToList();
        }

        private static string GetPreferenceKey(
            string sourceKey,
            string selectedText)
        {
            string source = sourceKey ?? string.Empty;
            return source.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
                ":" + source + (selectedText ?? string.Empty);
        }

        private static LearningRecord ToPublicRecord(LearningRecordData record)
        {
            return new LearningRecord(
                record.Id,
                record.SourceText,
                record.SelectedText,
                ParseLanguage(record.Language),
                record.IsEnabled,
                record.AcceptanceCount,
                record.CreatedUtc,
                record.LastUsedUtc);
        }

        private static ReviewLanguage ParseLanguage(string value)
        {
            ReviewLanguage language;
            return Enum.TryParse(value, true, out language)
                ? language
                : ReviewLanguage.Unknown;
        }

        private static string NormalizeKey(string value)
        {
            return Regex.Replace(
                (value ?? string.Empty).Trim(),
                @"\s+",
                " ").ToUpperInvariant();
        }

        [DataContract]
        private sealed class LearningFile
        {
            [DataMember(Name = "schemaVersion", Order = 1)]
            public int SchemaVersion { get; set; }

            [DataMember(Name = "records", Order = 2)]
            public List<LearningRecordData> Records { get; set; }
        }

        [DataContract]
        private sealed class LearningRecordData
        {
            [DataMember(Name = "id", Order = 1)]
            public string Id { get; set; }

            [DataMember(Name = "sourceText", Order = 2)]
            public string SourceText { get; set; }

            [DataMember(Name = "sourceKey", Order = 3)]
            public string SourceKey { get; set; }

            [DataMember(Name = "selectedText", Order = 4)]
            public string SelectedText { get; set; }

            [DataMember(Name = "language", Order = 5)]
            public string Language { get; set; }

            [DataMember(Name = "isEnabled", Order = 6)]
            public bool IsEnabled { get; set; }

            [DataMember(Name = "acceptanceCount", Order = 7)]
            public int AcceptanceCount { get; set; }

            [DataMember(Name = "createdUtc", Order = 8)]
            public string CreatedUtc { get; set; }

            [DataMember(Name = "lastUsedUtc", Order = 9)]
            public string LastUsedUtc { get; set; }
        }
    }
}
