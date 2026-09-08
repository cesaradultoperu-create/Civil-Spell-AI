using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CivilSpellAI.Spell
{
    /// <summary>
    /// Offline first-pass checker for Spanish and English Civil 3D annotations.
    /// It intentionally corrects only known, low-risk spelling errors.
    /// </summary>
    public class SpellEngine
    {
        private static readonly Regex MTextFormattingPattern = new Regex(
            @"\\(?:U\+[0-9A-Fa-f]{4}|[A-Za-z][^;\\{}]*;|[PpXxNnLlOoKk]|[\\{}~])|[{}]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly CorrectionRule[] SpanishRules =
        {
            new CorrectionRule(@"\bestructuraa\b", "estructura"),
            new CorrectionRule(@"\bestruturaa\b", "estructura"),
            new CorrectionRule(@"\bcarreteraa\b", "carretera"),
            new CorrectionRule(@"\bcunetaa\b", "cuneta"),
            new CorrectionRule(@"\balcantarillaa\b", "alcantarilla"),
            new CorrectionRule(@"\bdrenage\b", "drenaje"),
            new CorrectionRule(@"\bseñalizacion\b", "señalización"),
            new CorrectionRule(@"\binterseccion\b", "intersección"),
            new CorrectionRule(@"\btopografiaa\b", "topografía"),
            new CorrectionRule(@"\btopografico\b", "topográfico"),
            new CorrectionRule(@"\btopografica\b", "topográfica"),
            new CorrectionRule(@"\blevantamientoo\b", "levantamiento"),
            new CorrectionRule(@"\bcoordenadass\b", "coordenadas"),
            new CorrectionRule(@"\bnivelacionn?\b", "nivelación"),
            new CorrectionRule(@"\belevacionn?\b", "elevación"),
            new CorrectionRule(@"\bcotaas\b", "cotas"),
            new CorrectionRule(@"\bcalzadaa\b", "calzada"),
            new CorrectionRule(@"\bbermaa\b", "berma"),
            new CorrectionRule(@"\btaludd\b", "talud"),
            new CorrectionRule(@"\bpendientee\b", "pendiente"),
            new CorrectionRule(@"\brasantee\b", "rasante"),
            new CorrectionRule(@"\balineamientoo\b", "alineamiento"),
            new CorrectionRule(@"\balineamineto\b", "alineamiento"),
            new CorrectionRule(@"\balineacion\b", "alineación"),
            new CorrectionRule(@"\bdrenajee\b", "drenaje"),
            new CorrectionRule(@"\bsumideroo\b", "sumidero"),
            new CorrectionRule(@"\bestructurass\b", "estructuras"),
            new CorrectionRule(@"\bcimentacionn?\b", "cimentación"),
            new CorrectionRule(@"\blineaa\b", "línea"),
            new CorrectionRule(@"\bpolilineaa\b", "polilínea"),
            new CorrectionRule(@"\bareaa\b", "área"),
            new CorrectionRule(@"\bproyectoo\b", "proyecto"),
            new CorrectionRule(@"\bplano(?:ss|os)\b", "planos"),
            new CorrectionRule(@"\bseccionn\b", "sección"),
            new CorrectionRule(@"\bexcavacionn?\b", "excavación"),
            new CorrectionRule(@"\bcompactacionn?\b", "compactación"),
            new CorrectionRule(@"\bgeomenbrana\b", "geomembrana"),
            new CorrectionRule(@"\bsuvrasante\b", "subrasante"),
            new CorrectionRule(@"\bsovrrevestimiento\b", "sobrerrevestimiento"),
            new CorrectionRule(@"\buvicaci[oó]n\b", "ubicación"),
            new CorrectionRule(@"\bhuvicar\b", "ubicar"),
            new CorrectionRule(@"\brehubicadas\b", "reubicadas"),
            new CorrectionRule(@"\bdezarenador\b", "desarenador"),
            new CorrectionRule(@"\bdetaye\b", "detalle"),
            new CorrectionRule(@"\bconecci[oó]n\b", "conexión"),
            new CorrectionRule(@"\btriquetados\b", "etiquetados"),
            new CorrectionRule(@"\bsubrasantee\b", "subrasante"),
            new CorrectionRule(@"\bpavimentoo\b", "pavimento"),
            new CorrectionRule(@"\bubcacion\b", "ubicación"),
            new CorrectionRule(@"\bubicacion\b", "ubicación"),
            new CorrectionRule(@"\bnumero\b", "número"),
            new CorrectionRule(@"\bnumeros\b", "números"),
            new CorrectionRule(@"\btitulo\b", "título"),
            new CorrectionRule(@"\btitulos\b", "títulos"),
            new CorrectionRule(@"\brevision\b", "revisión"),
            new CorrectionRule(@"\bemision\b", "emisión"),
            new CorrectionRule(@"\blamina\b", "lámina"),
            new CorrectionRule(@"\blaminas\b", "láminas"),
            new CorrectionRule(@"\bgeologia\b", "geología"),
            new CorrectionRule(@"\btipico\b", "típico"),
            new CorrectionRule(@"\btipica\b", "típica"),
            new CorrectionRule(@"\bconexion\b", "conexión"),
            new CorrectionRule(@"\blimites\b", "límites"),
            new CorrectionRule(@"\bconstruccion\b", "construcción"),
            new CorrectionRule(@"\binstrumentacion\b", "instrumentación"),
            new CorrectionRule(@"\binundacion\b", "inundación"),
            new CorrectionRule(@"\bestacion\b", "estación"),
            new CorrectionRule(@"\bsubterranea\b", "subterránea"),
            new CorrectionRule(@"\bdemas\b", "demás"),
            new CorrectionRule(@"\bseran\b", "serán"),
            new CorrectionRule(@"\bperimetro\b", "perímetro"),
            new CorrectionRule(@"\blixiviacion\b", "lixiviación"),
            new CorrectionRule(@"\bdebera\b", "deberá"),
            new CorrectionRule(@"\bdeberan\b", "deberán"),
            new CorrectionRule(@"\bevitandose\b", "evitándose"),
            new CorrectionRule(@"\bfacil\b", "fácil"),
            new CorrectionRule(@"\bidentificacion\b", "identificación"),
            new CorrectionRule(@"\bningun\b", "ningún"),
            new CorrectionRule(@"\btopograficamente\b", "topográficamente"),
            new CorrectionRule(@"\bpodra\b", "podrá"),
            new CorrectionRule(@"\btuver[ií]a\b", "tubería"),
            new CorrectionRule(@"\btuver[ií]as\b", "tuberías"),
            new CorrectionRule(@"\btuberias\b", "tuberías"),
            new CorrectionRule(@"\blineas\b", "líneas"),
            new CorrectionRule(@"\bevaluacion\b", "evaluación"),
            new CorrectionRule(@"\bgarrapon\b", "garrapón"),
            new CorrectionRule(@"\bdiseno\b", "diseño"),
            new CorrectionRule(@"\bseccion\b", "sección"),
            new CorrectionRule(@"\bqbofedal\b", "bofedal"),
            new CorrectionRule(@"\binvestigacci[oó]n\b", "investigación"),
            new CorrectionRule(@"\bjeot[eé]cnica\b", "geotécnica"),
            new CorrectionRule(@"\bjeot[eé]cnico\b", "geotécnico"),
            new CorrectionRule(@"\bjeot[eé]cnicas\b", "geotécnicas"),
            new CorrectionRule(@"\bjeot[eé]cnicos\b", "geotécnicos"),
            new CorrectionRule(@"\ba\s+sido\b", "ha sido"),
            new CorrectionRule(@"\besta\s+en\b", "está en")
        };

        private static readonly CorrectionRule[] EnglishRules =
        {
            new CorrectionRule(@"\bexistent\b", "existing"),
            new CorrectionRule(@"\baligment\b", "alignment"),
            new CorrectionRule(@"\bstrcture\b", "structure"),
            new CorrectionRule(@"\bdesgin\b", "design"),
            new CorrectionRule(@"\belevaion\b", "elevation"),
            new CorrectionRule(@"\bsurfce\b", "surface")
        };

        private static readonly string[] SpanishSignals =
        {
            "la", "las", "del", "para", "estructura", "alineamiento",
            "estructuraa", "estruturaa", "alineamineto", "superficie", "diseño", "diseno",
            "sección", "seccion", "ubicación", "ubcacion"
        };

        private static readonly string[] EnglishSignals =
        {
            "the", "and", "with", "existing", "existent", "alignment",
            "surface", "design", "section", "elevation", "structure"
        };

        private readonly TechnicalGlossary glossary;
        private readonly ISpellingDictionary spanishDictionary;
        private readonly ConcurrentDictionary<string, Lazy<string>> dictionaryCorrectionCache;

        public SpellEngine()
            : this(TechnicalGlossary.LoadDefault(), null)
        {
        }

        public SpellEngine(TechnicalGlossary glossary)
            : this(glossary, null)
        {
        }

        public SpellEngine(
            TechnicalGlossary glossary,
            ISpellingDictionary spanishDictionary)
        {
            this.glossary = glossary ?? TechnicalGlossary.LoadDefault();
            this.spanishDictionary = spanishDictionary;
            dictionaryCorrectionCache =
                new ConcurrentDictionary<string, Lazy<string>>(
                    StringComparer.OrdinalIgnoreCase);
        }

        // Kept for compatibility with the original command API.
        public Dictionary<string, string> CheckText(string text)
        {
            Dictionary<string, string> corrections =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CorrectionResult result = Analyze(text);

            foreach (CorrectionChange change in result.Changes)
            {
                if (!corrections.ContainsKey(change.Original))
                    corrections.Add(change.Original, change.Corrected);
            }

            return corrections;
        }

        public string CorrectText(string text)
        {
            return Analyze(text).CorrectedText;
        }

        public CorrectionResult Analyze(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new CorrectionResult(
                    text,
                    text,
                    TextLanguage.Unknown,
                    new List<CorrectionChange>());
            }

            TextLanguage language = DetectLanguage(text);
            List<ProtectedTerm> protectedTerms = new List<ProtectedTerm>();
            string corrected = ProtectMTextFormatting(text, protectedTerms);
            corrected = ProtectGlossaryTerms(corrected, protectedTerms);
            List<CorrectionChange> changes = new List<CorrectionChange>();

            if (language == TextLanguage.Spanish || language == TextLanguage.Mixed || language == TextLanguage.Unknown)
            {
                corrected = ApplyRules(corrected, SpanishRules, changes);
                corrected = ApplySpanishDictionary(corrected, changes);
            }

            if (language == TextLanguage.English || language == TextLanguage.Mixed || language == TextLanguage.Unknown)
                corrected = ApplyRules(corrected, EnglishRules, changes);

            corrected = RestoreGlossaryTerms(corrected, protectedTerms);

            return new CorrectionResult(text, corrected, language, changes);
        }

        public TextLanguage DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return TextLanguage.Unknown;

            int spanishScore = ScoreSignals(text, SpanishSignals);
            int englishScore = ScoreSignals(text, EnglishSignals);

            if (spanishScore > 0 && englishScore > 0)
                return TextLanguage.Mixed;

            if (spanishScore > 0)
                return TextLanguage.Spanish;

            if (englishScore > 0)
                return TextLanguage.English;

            return TextLanguage.Unknown;
        }

        private static int ScoreSignals(string text, IEnumerable<string> signals)
        {
            int score = 0;

            foreach (string signal in signals)
            {
                if (Regex.IsMatch(text, @"\b" + Regex.Escape(signal) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    score++;
            }

            return score;
        }

        private static string ApplyRules(string text, IEnumerable<CorrectionRule> rules, List<CorrectionChange> changes)
        {
            string corrected = text;

            foreach (CorrectionRule rule in rules)
            {
                corrected = Regex.Replace(
                    corrected,
                    rule.Pattern,
                    delegate(Match match)
                    {
                        string replacement = MatchCapitalization(match.Value, rule.Replacement);
                        changes.Add(new CorrectionChange(match.Value, replacement));
                        return replacement;
                    },
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            return corrected;
        }

        private string ApplySpanishDictionary(
            string text,
            List<CorrectionChange> changes)
        {
            if (spanishDictionary == null || !spanishDictionary.IsLoaded)
                return text;

            return Regex.Replace(
                text,
                @"(?<![\p{L}\p{N}_\\])\p{L}{4,}(?![\p{L}\p{N}_])",
                delegate(Match match)
                {
                    string original = match.Value;

                    if (!CanCheckDictionaryWord(original))
                        return original;

                    string lookup = original.ToLowerInvariant();
                    Lazy<string> cached = dictionaryCorrectionCache.GetOrAdd(
                        lookup,
                        key => new Lazy<string>(
                            () => FindSafeDictionaryCorrection(key),
                            true));
                    string suggestion = cached.Value;

                    if (string.IsNullOrEmpty(suggestion))
                        return original;

                    // All-uppercase annotations frequently contain surnames,
                    // identifiers and abbreviations. In that context only an
                    // unambiguous accent restoration is safe; known drawing
                    // typos are handled by the explicit rules above.
                    if (original.ToUpperInvariant() == original &&
                        !string.Equals(
                            RemoveDiacritics(original),
                            RemoveDiacritics(suggestion),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return original;
                    }

                    string replacement = MatchCapitalization(original, suggestion);

                    if (string.Equals(original, replacement, StringComparison.Ordinal))
                        return original;

                    changes.Add(new CorrectionChange(original, replacement));
                    return replacement;
                },
                RegexOptions.CultureInvariant);
        }

        private bool CanCheckDictionaryWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length < 4)
                return false;

            if (word.Length <= 5 && word.ToUpperInvariant() == word)
                return false;

            for (int index = 1; index < word.Length; index++)
            {
                if (char.IsUpper(word[index]) && char.IsLower(word[index - 1]))
                    return false;
            }

            return true;
        }

        private string FindSafeDictionaryCorrection(string word)
        {
            if (spanishDictionary.Check(word))
                return null;

            IReadOnlyList<string> rawSuggestions = spanishDictionary.Suggest(word);
            List<string> candidates = rawSuggestions
                .Where(IsSingleDictionaryWord)
                .Select(candidate => candidate.ToLowerInvariant())
                .Where(candidate => !string.Equals(
                    candidate,
                    word,
                    StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            if (candidates.Count == 0)
                return null;

            string normalizedWord = RemoveDiacritics(word);
            List<string> accentOnly = candidates
                .Where(candidate => string.Equals(
                    RemoveDiacritics(candidate),
                    normalizedWord,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (accentOnly.Count == 1)
                return accentOnly[0];

            if (accentOnly.Count > 1)
                return null;

            List<string> oneEdit = candidates
                .Where(candidate => DamerauLevenshteinDistance(
                    normalizedWord,
                    RemoveDiacritics(candidate)) == 1)
                .ToList();

            return oneEdit.Count == 1 ? oneEdit[0] : null;
        }

        private static bool IsSingleDictionaryWord(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate) || candidate.Length < 4)
                return false;

            for (int index = 0; index < candidate.Length; index++)
            {
                if (!char.IsLetter(candidate[index]))
                    return false;
            }

            return true;
        }

        private static string RemoveDiacritics(string value)
        {
            string decomposed = (value ?? string.Empty).Normalize(
                NormalizationForm.FormD);
            StringBuilder result = new StringBuilder(decomposed.Length);

            foreach (char character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                    UnicodeCategory.NonSpacingMark)
                {
                    result.Append(char.ToLowerInvariant(character));
                }
            }

            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        private static int DamerauLevenshteinDistance(string left, string right)
        {
            int[,] distance = new int[left.Length + 1, right.Length + 1];

            for (int leftIndex = 0; leftIndex <= left.Length; leftIndex++)
                distance[leftIndex, 0] = leftIndex;

            for (int rightIndex = 0; rightIndex <= right.Length; rightIndex++)
                distance[0, rightIndex] = rightIndex;

            for (int leftIndex = 1; leftIndex <= left.Length; leftIndex++)
            {
                for (int rightIndex = 1; rightIndex <= right.Length; rightIndex++)
                {
                    int substitutionCost =
                        left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                    int best = Math.Min(
                        distance[leftIndex - 1, rightIndex] + 1,
                        Math.Min(
                            distance[leftIndex, rightIndex - 1] + 1,
                            distance[leftIndex - 1, rightIndex - 1] +
                                substitutionCost));

                    if (leftIndex > 1 &&
                        rightIndex > 1 &&
                        left[leftIndex - 1] == right[rightIndex - 2] &&
                        left[leftIndex - 2] == right[rightIndex - 1])
                    {
                        best = Math.Min(
                            best,
                            distance[leftIndex - 2, rightIndex - 2] + 1);
                    }

                    distance[leftIndex, rightIndex] = best;
                }
            }

            return distance[left.Length, right.Length];
        }

        private string ProtectGlossaryTerms(string text, List<ProtectedTerm> protectedTerms)
        {
            List<string> terms = new List<string>(glossary.Terms);
            string tokenPrefix = CreateTokenPrefix(text);
            terms.Sort(delegate(string left, string right)
            {
                return right.Length.CompareTo(left.Length);
            });

            string protectedText = text;

            foreach (string term in terms)
            {
                string firstWord = Regex.Split(term, @"\s+")[0];

                if (protectedText.IndexOf(
                    firstWord,
                    StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                string pattern = BuildGlossaryPattern(term);

                protectedText = Regex.Replace(
                    protectedText,
                    pattern,
                    delegate(Match match)
                    {
                        string token = CreateProtectedToken(
                            tokenPrefix,
                            protectedTerms.Count);
                        protectedTerms.Add(new ProtectedTerm(token, match.Value));
                        return token;
                    },
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            return protectedText;
        }

        private static string ProtectMTextFormatting(
            string text,
            List<ProtectedTerm> protectedTerms)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string tokenPrefix = CreateTokenPrefix(text);
            return MTextFormattingPattern.Replace(
                text,
                delegate(Match match)
                {
                    string token = CreateProtectedToken(
                        tokenPrefix,
                        protectedTerms.Count);
                    protectedTerms.Add(new ProtectedTerm(token, match.Value));
                    return token;
                });
        }

        private static string BuildGlossaryPattern(string term)
        {
            string[] words = Regex.Split(term ?? string.Empty, @"\s+");
            return @"(?<![\p{L}\p{N}_])" +
                string.Join(@"\s+", words.Select(Regex.Escape)) +
                @"(?![\p{L}\p{N}_])";
        }

        private static string CreateTokenPrefix(string text)
        {
            string prefix = "\uE000\uE002";

            while ((text ?? string.Empty).IndexOf(
                prefix,
                StringComparison.Ordinal) >= 0)
            {
                prefix += "\uE002";
            }

            return prefix;
        }

        private static string CreateProtectedToken(string prefix, int index)
        {
            System.Text.StringBuilder token =
                new System.Text.StringBuilder(prefix);
            int value = index;

            do
            {
                token.Append((char)('\uE100' + (value % 10)));
                value /= 10;
            }
            while (value > 0);

            token.Append('\uE001');
            return token.ToString();
        }

        private static string RestoreGlossaryTerms(string text, IEnumerable<ProtectedTerm> protectedTerms)
        {
            string restoredText = text;

            foreach (ProtectedTerm protectedTerm in protectedTerms)
                restoredText = restoredText.Replace(protectedTerm.Token, protectedTerm.Value);

            return restoredText;
        }

        private static string MatchCapitalization(string original, string replacement)
        {
            if (original.ToUpperInvariant() == original)
                return replacement.ToUpperInvariant();

            if (char.IsUpper(original[0]))
                return char.ToUpperInvariant(replacement[0]) + replacement.Substring(1);

            return replacement;
        }

        private sealed class CorrectionRule
        {
            public CorrectionRule(string pattern, string replacement)
            {
                Pattern = pattern;
                Replacement = replacement;
            }

            public string Pattern { get; private set; }
            public string Replacement { get; private set; }
        }

        private sealed class ProtectedTerm
        {
            public ProtectedTerm(string token, string value)
            {
                Token = token;
                Value = value;
            }

            public string Token { get; private set; }
            public string Value { get; private set; }
        }
    }
}
