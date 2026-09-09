using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Infrastructure
{
    public interface ILanguageToolTransport
    {
        Task<string> CheckAsync(
            string text,
            string languageCode,
            TimeSpan timeout,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Converts LanguageTool's offset-based local response into one complete,
    /// conservative correction. The provider itself performs no internet calls;
    /// its transport must point to a loopback-only LanguageTool server.
    /// </summary>
    public sealed class LanguageToolCorrectionProvider : ITextCorrectionProvider
    {
        private const int MaximumInputCharacters = 20000;
        private const int MaximumResponseCharacters = 500000;
        private static readonly Regex AutoCadFormattingPattern = new Regex(
            @"\\(?:U\+[0-9A-Fa-f]{4}|[A-Za-z][^;\\{}]*;|[PpXxNnLlOoKk]|[\\{}~])|[{}]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly ILanguageToolTransport transport;
        private readonly TimeSpan timeout;

        public LanguageToolCorrectionProvider(
            ILanguageToolTransport transport,
            TimeSpan timeout)
        {
            if (transport == null)
                throw new ArgumentNullException("transport");

            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("timeout");

            this.transport = transport;
            this.timeout = timeout;
        }

        public string Name
        {
            get { return "Gramática local"; }
        }

        public async Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
            CorrectionRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.Text.Length > MaximumInputCharacters)
                throw InvalidResponse("El texto supera el límite local de 20 000 caracteres.");

            FormattingProjection projection = CreateFormattingProjection(request.Text);
            string responseJson = await transport.CheckAsync(
                projection.Text,
                GetLanguageCode(request.Language),
                timeout,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return ParseResponse(responseJson, request, projection.ProtectedRanges);
        }

        internal static IReadOnlyList<CorrectionProposal> ParseResponse(
            string responseJson,
            CorrectionRequest request)
        {
            return ParseResponse(
                responseJson,
                request,
                new List<ProtectedRange>().AsReadOnly());
        }

        private static IReadOnlyList<CorrectionProposal> ParseResponse(
            string responseJson,
            CorrectionRequest request,
            IList<ProtectedRange> protectedRanges)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (string.IsNullOrWhiteSpace(responseJson))
                throw InvalidResponse("LanguageTool local devolvió una respuesta vacía.");

            if (responseJson.Length > MaximumResponseCharacters)
                throw InvalidResponse("LanguageTool local devolvió una respuesta demasiado extensa.");

            LanguageToolResponse response;

            try
            {
                response = Deserialize<LanguageToolResponse>(responseJson);
            }
            catch (SerializationException)
            {
                throw InvalidResponse("LanguageTool local devolvió JSON no válido.");
            }

            if (response == null || response.Matches == null || response.Matches.Count == 0)
                return new List<CorrectionProposal>().AsReadOnly();

            List<ReplacementEdit> edits = SelectNonOverlappingEdits(
                request.Text,
                response.Matches,
                protectedRanges);

            if (edits.Count == 0)
                return new List<CorrectionProposal>().AsReadOnly();

            StringBuilder corrected = new StringBuilder(request.Text);

            foreach (ReplacementEdit edit in edits.OrderByDescending(item => item.Offset))
            {
                corrected.Remove(edit.Offset, edit.Length);
                corrected.Insert(edit.Offset, edit.Value);
            }

            string proposedText = corrected.ToString();

            if (proposedText.Length > MaximumInputCharacters)
            {
                throw InvalidResponse(
                    "LanguageTool local devolvió una corrección demasiado extensa.");
            }

            if (string.Equals(proposedText, request.Text, StringComparison.Ordinal))
                return new List<CorrectionProposal>().AsReadOnly();

            return new List<CorrectionProposal>
            {
                new CorrectionProposal(
                    proposedText,
                    ProposalSource.LocalRules,
                    MapLanguage(request.Language),
                    string.Format(
                        "Revisión gramatical local: {0} cambio(s).",
                        edits.Count),
                    null,
                    null)
            }.AsReadOnly();
        }

        private static List<ReplacementEdit> SelectNonOverlappingEdits(
            string original,
            IEnumerable<LanguageToolMatch> matches,
            IEnumerable<ProtectedRange> protectedRanges)
        {
            List<ReplacementEdit> selected = new List<ReplacementEdit>();
            int occupiedUntil = 0;

            foreach (LanguageToolMatch match in matches
                .Where(item => item != null)
                .OrderBy(item => item.Offset)
                .ThenByDescending(item => item.Length))
            {
                if (match.Offset < 0 || match.Length < 0 ||
                    match.Offset > original.Length - match.Length)
                {
                    throw InvalidResponse(
                        "LanguageTool local devolvió una posición fuera del texto.");
                }

                if (match.Offset < occupiedUntil || match.Replacements == null ||
                    protectedRanges.Any(range => range.Overlaps(match.Offset, match.Length)))
                    continue;

                LanguageToolReplacement replacement = match.Replacements
                    .FirstOrDefault(item => item != null && item.Value != null);

                if (replacement == null)
                    continue;

                string current = original.Substring(match.Offset, match.Length);

                if (string.Equals(current, replacement.Value, StringComparison.Ordinal))
                    continue;

                selected.Add(new ReplacementEdit(
                    match.Offset,
                    match.Length,
                    replacement.Value));
                occupiedUntil = match.Offset + match.Length;
            }

            return selected;
        }

        private static FormattingProjection CreateFormattingProjection(string original)
        {
            StringBuilder masked = new StringBuilder(original);
            List<ProtectedRange> ranges = new List<ProtectedRange>();

            foreach (Match match in AutoCadFormattingPattern.Matches(original))
            {
                ranges.Add(new ProtectedRange(match.Index, match.Length));

                for (int index = match.Index; index < match.Index + match.Length; index++)
                    masked[index] = ' ';

                // Preserve a sentence boundary for AutoCAD paragraph commands
                // without changing string length or LanguageTool offsets.
                if (match.Length == 2 && match.Value[0] == '\\' &&
                    (match.Value[1] == 'P' || match.Value[1] == 'p'))
                {
                    masked[match.Index + 1] = '\n';
                }
            }

            return new FormattingProjection(masked.ToString(), ranges.AsReadOnly());
        }

        private static string GetLanguageCode(ReviewLanguage language)
        {
            return language == ReviewLanguage.English ? "en-US" : "es";
        }

        private static ReviewLanguage MapLanguage(ReviewLanguage language)
        {
            return language == ReviewLanguage.English
                ? ReviewLanguage.English
                : ReviewLanguage.Spanish;
        }

        private static T Deserialize<T>(string json)
        {
            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(T));

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (T)serializer.ReadObject(stream);
        }

        private static CorrectionProviderException InvalidResponse(string message)
        {
            return new CorrectionProviderException(
                ProviderFailureKind.InvalidResponse,
                message);
        }

        private sealed class ReplacementEdit
        {
            public ReplacementEdit(int offset, int length, string value)
            {
                Offset = offset;
                Length = length;
                Value = value;
            }

            public int Offset { get; private set; }
            public int Length { get; private set; }
            public string Value { get; private set; }
        }

        private sealed class FormattingProjection
        {
            public FormattingProjection(string text, IList<ProtectedRange> protectedRanges)
            {
                Text = text;
                ProtectedRanges = protectedRanges;
            }

            public string Text { get; private set; }
            public IList<ProtectedRange> ProtectedRanges { get; private set; }
        }

        private sealed class ProtectedRange
        {
            public ProtectedRange(int offset, int length)
            {
                Offset = offset;
                Length = length;
            }

            public int Offset { get; private set; }
            public int Length { get; private set; }

            public bool Overlaps(int offset, int length)
            {
                return offset < Offset + Length && Offset < offset + length;
            }
        }

        [DataContract]
        private sealed class LanguageToolResponse
        {
            [DataMember(Name = "matches")]
            public List<LanguageToolMatch> Matches { get; set; }
        }

        [DataContract]
        private sealed class LanguageToolMatch
        {
            [DataMember(Name = "offset")]
            public int Offset { get; set; }

            [DataMember(Name = "length")]
            public int Length { get; set; }

            [DataMember(Name = "replacements")]
            public List<LanguageToolReplacement> Replacements { get; set; }
        }

        [DataContract]
        private sealed class LanguageToolReplacement
        {
            [DataMember(Name = "value")]
            public string Value { get; set; }
        }
    }
}
