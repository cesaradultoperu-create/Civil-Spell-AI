using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CivilSpellAI.Spell
{
    /// <summary>
    /// Offline first-pass checker for Spanish and English Civil 3D annotations.
    /// It intentionally corrects only known, low-risk spelling errors.
    /// </summary>
    public class SpellEngine
    {
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
            new CorrectionRule(@"\bsubrasantee\b", "subrasante"),
            new CorrectionRule(@"\bpavimentoo\b", "pavimento"),
            new CorrectionRule(@"\bubcacion\b", "ubicación"),
            new CorrectionRule(@"\bdiseno\b", "diseño"),
            new CorrectionRule(@"\bseccion\b", "sección"),
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

        public SpellEngine()
            : this(TechnicalGlossary.LoadDefault())
        {
        }

        public SpellEngine(TechnicalGlossary glossary)
        {
            this.glossary = glossary ?? TechnicalGlossary.LoadDefault();
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
            string corrected = ProtectGlossaryTerms(text, protectedTerms);
            List<CorrectionChange> changes = new List<CorrectionChange>();

            if (language == TextLanguage.Spanish || language == TextLanguage.Mixed || language == TextLanguage.Unknown)
                corrected = ApplyRules(corrected, SpanishRules, changes);

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
