using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CivilSpellAI.Domain
{
    public sealed class TechnicalTokenValidator : IProposalValidator
    {
        public const int MaximumValidatedTextCharacters = 200000;
        private const string GlossaryCategory = "glossary";
        private const string StationCategory = "station";
        private const string RatioCategory = "ratio";
        private const string CodeCategory = "code";
        private const string NumberCategory = "number";
        private const string UnitCategory = "unit";
        private const string FormattingCategory = "formatting";

        private static readonly Regex FormattingPattern = new Regex(
            @"\\(?:U\+[0-9A-Fa-f]{4}|[A-Za-z][^;\\{}]*;|[PpXxNnLlOoKk]|[\\{}~])|[{}]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex StationPattern = new Regex(
            @"(?<![\p{L}\p{N}_])[-+]?\d+\+\d+(?:[.,]\d+)?(?![\p{L}\p{N}_])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex RatioPattern = new Regex(
            @"(?<![\p{L}\p{N}_])\d+(?:[.,]\d+)?[\p{L}]?\s*[:/]\s*\d+(?:[.,]\d+)?[\p{L}]?(?![\p{L}\p{N}_])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex CodePattern = new Regex(
            @"(?<![\p{L}\p{N}_])(?=[\p{L}\p{N}-]*\p{L})(?=[\p{L}\p{N}-]*\p{N})[\p{L}\p{N}]+(?:-[\p{L}\p{N}]+)*(?![\p{L}\p{N}_])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex NumberPattern = new Regex(
            @"(?<![\p{L}\p{N}_])[-+]?\d+(?:[.,]\d+)*(?![\p{L}\p{N}_])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex UnitPattern = new Regex(
            @"(?<![\p{L}\p{N}_])(?:kN/m²|kN/m2|N/mm²|N/mm2|kg/m³|kg/m3|kg/m²|kg/m2|m³/s|m3/s|m²/s|m2/s|km/h|m/s|ft³|ft3|ft²|ft2|ft/s|in³|in3|in²|in2|mm³|mm3|mm²|mm2|cm³|cm3|cm²|cm2|km²|km2|m³|m3|m²|m2|MPa|kPa|Pa|psi|bar|kN|N|kg|ha|L/s|l/s|rad|gon|deg|mm|cm|km|m|ft|in)(?![\p{L}\p{N}_])|[%°Ø⌀±]|(?<=\d)['""′″]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> KnownUnits = new HashSet<string>(
            new[]
            {
                "kN/m²", "kN/m2", "N/mm²", "N/mm2", "kg/m³",
                "kg/m3", "kg/m²", "kg/m2", "m³/s", "m3/s", "m²/s", "m2/s",
                "km/h", "m/s", "ft³", "ft3", "ft²", "ft2", "ft/s",
                "in³", "in3", "in²", "in2", "mm³", "mm3", "mm²",
                "mm2", "cm³", "cm3", "cm²", "cm2", "km²", "km2",
                "m³", "m3", "m²", "m2", "MPa", "kPa", "Pa", "psi",
                "bar", "kN", "N", "kg", "ha", "L/s", "l/s", "rad",
                "gon", "deg", "mm", "cm", "km", "m", "ft", "in",
                "%", "°", "Ø", "⌀", "±"
            },
            StringComparer.OrdinalIgnoreCase);

        private readonly ITextDiffer differ;

        public TechnicalTokenValidator()
            : this(new TextDiffer())
        {
        }

        public TechnicalTokenValidator(ITextDiffer differ)
        {
            if (differ == null)
                throw new ArgumentNullException("differ");

            this.differ = differ;
        }

        public ProposalValidationResult Validate(
            CorrectionRequest request,
            CorrectionProposal proposal)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (proposal == null)
                throw new ArgumentNullException("proposal");

            if (request.Text.Length > MaximumValidatedTextCharacters ||
                proposal.ProposedText.Length > MaximumValidatedTextCharacters)
            {
                return new ProposalValidationResult(
                    new TextDifference[0],
                    new[]
                    {
                        new ValidationIssue(
                            "text_too_long",
                            "El texto supera el límite de validación segura.")
                    });
            }

            IList<TextDifference> changes = differ.Calculate(
                request.Text,
                proposal.ProposedText);
            List<ValidationIssue> issues = new List<ValidationIssue>();

            if (string.IsNullOrWhiteSpace(proposal.ProposedText))
            {
                issues.Add(new ValidationIssue(
                    "empty_text",
                    "La propuesta no puede dejar el texto vacío."));
            }

            if (changes.Count == 0)
            {
                issues.Add(new ValidationIssue(
                    "no_changes",
                    "La propuesta no contiene cambios reales."));
            }

            IList<ProtectedToken> originalTokens = ExtractTokens(
                request.Text,
                request.GlossaryTerms);
            IList<ProtectedToken> proposedTokens = ExtractTokens(
                proposal.ProposedText,
                request.GlossaryTerms);

            if (proposal.Source == ProposalSource.LocalRules)
            {
                ValidateLocalGlossaryCategory(
                    originalTokens,
                    proposedTokens,
                    issues);
            }
            else
            {
                ValidateCategory(
                    GlossaryCategory,
                    "términos del glosario",
                    originalTokens,
                    proposedTokens,
                    issues);
            }
            ValidateCategory(StationCategory, "estaciones", originalTokens, proposedTokens, issues);
            ValidateCategory(RatioCategory, "relaciones técnicas", originalTokens, proposedTokens, issues);
            ValidateCategory(CodeCategory, "códigos técnicos", originalTokens, proposedTokens, issues);
            ValidateCategory(NumberCategory, "valores numéricos", originalTokens, proposedTokens, issues);
            ValidateCategory(UnitCategory, "unidades", originalTokens, proposedTokens, issues);
            ValidateCategory(FormattingCategory, "códigos de formato de AutoCAD", originalTokens, proposedTokens, issues);

            return new ProposalValidationResult(changes, issues);
        }

        private static void ValidateCategory(
            string category,
            string displayName,
            IEnumerable<ProtectedToken> originalTokens,
            IEnumerable<ProtectedToken> proposedTokens,
            ICollection<ValidationIssue> issues)
        {
            string[] originalValues = originalTokens
                .Where(token => token.Category == category)
                .Select(token => token.Value)
                .ToArray();
            string[] proposedValues = proposedTokens
                .Where(token => token.Category == category)
                .Select(token => token.Value)
                .ToArray();

            if (!originalValues.SequenceEqual(proposedValues, StringComparer.Ordinal))
            {
                issues.Add(new ValidationIssue(
                    category + "_changed",
                    "La propuesta altera " + displayName + " protegidos."));
            }
        }

        private static void ValidateLocalGlossaryCategory(
            IEnumerable<ProtectedToken> originalTokens,
            IEnumerable<ProtectedToken> proposedTokens,
            ICollection<ValidationIssue> issues)
        {
            string[] originalValues = originalTokens
                .Where(token => token.Category == GlossaryCategory)
                .Select(token => token.Value)
                .ToArray();
            string[] proposedValues = proposedTokens
                .Where(token => token.Category == GlossaryCategory)
                .Select(token => token.Value)
                .ToArray();
            int proposedIndex = 0;

            foreach (string originalValue in originalValues)
            {
                while (proposedIndex < proposedValues.Length &&
                    !string.Equals(
                        originalValue,
                        proposedValues[proposedIndex],
                        StringComparison.Ordinal))
                {
                    proposedIndex++;
                }

                if (proposedIndex >= proposedValues.Length)
                {
                    issues.Add(new ValidationIssue(
                        GlossaryCategory + "_changed",
                        "La propuesta altera términos del glosario protegidos."));
                    return;
                }

                proposedIndex++;
            }
        }

        private static IList<ProtectedToken> ExtractTokens(
            string text,
            IEnumerable<string> glossaryTerms)
        {
            List<ProtectedToken> candidates = new List<ProtectedToken>();
            AddMatches(candidates, text, FormattingPattern, FormattingCategory, -1, false);
            AddGlossaryCandidates(candidates, text, glossaryTerms);
            AddMatches(candidates, text, StationPattern, StationCategory, 1, false);
            AddMatches(candidates, text, RatioPattern, RatioCategory, 2, false);
            AddMatches(candidates, text, CodePattern, CodeCategory, 2, true);
            AddMatches(candidates, text, UnitPattern, UnitCategory, 3, false);
            AddMatches(candidates, text, NumberPattern, NumberCategory, 4, false);

            List<ProtectedToken> selected = new List<ProtectedToken>();

            foreach (ProtectedToken candidate in candidates
                .OrderBy(token => token.Priority)
                .ThenBy(token => token.Start)
                .ThenByDescending(token => token.Length))
            {
                if (!selected.Any(token => token.Overlaps(candidate)))
                    selected.Add(candidate);
            }

            return selected.OrderBy(token => token.Start).ToList().AsReadOnly();
        }

        private static void AddGlossaryCandidates(
            ICollection<ProtectedToken> candidates,
            string text,
            IEnumerable<string> glossaryTerms)
        {
            if (glossaryTerms == null)
                return;

            foreach (string term in glossaryTerms
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(value => value.Length))
            {
                string[] words = Regex.Split(term, @"\s+");

                if (words.Length == 0 ||
                    (text ?? string.Empty).IndexOf(
                        words[0],
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                string pattern = @"(?<![\p{L}\p{N}_])" +
                    string.Join(@"\s+", words.Select(Regex.Escape)) +
                    @"(?![\p{L}\p{N}_])";
                Regex glossaryPattern = new Regex(
                    pattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                AddMatches(candidates, text, glossaryPattern, GlossaryCategory, 0, false);
            }
        }

        private static void AddMatches(
            ICollection<ProtectedToken> candidates,
            string text,
            Regex pattern,
            string category,
            int priority,
            bool skipKnownUnits)
        {
            foreach (Match match in pattern.Matches(text ?? string.Empty))
            {
                if (skipKnownUnits && KnownUnits.Contains(match.Value))
                    continue;

                candidates.Add(new ProtectedToken(
                    category,
                    match.Value,
                    match.Index,
                    match.Length,
                    priority));
            }
        }

        private sealed class ProtectedToken
        {
            public ProtectedToken(
                string category,
                string value,
                int start,
                int length,
                int priority)
            {
                Category = category;
                Value = value;
                Start = start;
                Length = length;
                Priority = priority;
            }

            public string Category { get; private set; }

            public string Value { get; private set; }

            public int Start { get; private set; }

            public int Length { get; private set; }

            public int Priority { get; private set; }

            public bool Overlaps(ProtectedToken other)
            {
                return Start < other.Start + other.Length &&
                       other.Start < Start + Length;
            }
        }
    }
}
