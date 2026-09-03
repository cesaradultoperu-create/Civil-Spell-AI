using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CivilSpellAI.Domain
{
    public sealed class TextDiffer : ITextDiffer
    {
        private const long MaximumMatrixCells = 1000000;

        private static readonly Regex TokenPattern = new Regex(
            @"\s+|[\p{L}\p{M}\p{N}_]+|[^\s\p{L}\p{M}\p{N}_]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public IList<TextDifference> Calculate(string originalText, string proposedText)
        {
            string original = originalText ?? string.Empty;
            string proposed = proposedText ?? string.Empty;
            List<Token> originalTokens = Tokenize(original);
            List<Token> proposedTokens = Tokenize(proposed);

            if ((long)(originalTokens.Count + 1) *
                (proposedTokens.Count + 1) > MaximumMatrixCells)
            {
                if (string.Equals(original, proposed, StringComparison.Ordinal))
                    return new List<TextDifference>().AsReadOnly();

                return new List<TextDifference>
                {
                    new TextDifference(0, original, 0, proposed)
                }.AsReadOnly();
            }

            int[,] commonLengths = BuildCommonLengthTable(originalTokens, proposedTokens);
            List<TextDifference> differences = new List<TextDifference>();
            int originalIndex = 0;
            int proposedIndex = 0;

            while (originalIndex < originalTokens.Count || proposedIndex < proposedTokens.Count)
            {
                if (TokensEqual(originalTokens, originalIndex, proposedTokens, proposedIndex))
                {
                    originalIndex++;
                    proposedIndex++;
                    continue;
                }

                int originalChangeStart = originalIndex;
                int proposedChangeStart = proposedIndex;

                while (originalIndex < originalTokens.Count || proposedIndex < proposedTokens.Count)
                {
                    if (TokensEqual(originalTokens, originalIndex, proposedTokens, proposedIndex))
                        break;

                    if (proposedIndex >= proposedTokens.Count ||
                        (originalIndex < originalTokens.Count &&
                         commonLengths[originalIndex + 1, proposedIndex] >=
                         commonLengths[originalIndex, proposedIndex + 1]))
                    {
                        originalIndex++;
                    }
                    else
                    {
                        proposedIndex++;
                    }
                }

                differences.Add(CreateDifference(
                    original,
                    originalTokens,
                    originalChangeStart,
                    originalIndex,
                    proposed,
                    proposedTokens,
                    proposedChangeStart,
                    proposedIndex));
            }

            return differences.AsReadOnly();
        }

        private static int[,] BuildCommonLengthTable(
            IList<Token> original,
            IList<Token> proposed)
        {
            int[,] lengths = new int[original.Count + 1, proposed.Count + 1];

            for (int originalIndex = original.Count - 1; originalIndex >= 0; originalIndex--)
            {
                for (int proposedIndex = proposed.Count - 1; proposedIndex >= 0; proposedIndex--)
                {
                    if (string.Equals(
                        original[originalIndex].Value,
                        proposed[proposedIndex].Value,
                        StringComparison.Ordinal))
                    {
                        lengths[originalIndex, proposedIndex] =
                            lengths[originalIndex + 1, proposedIndex + 1] + 1;
                    }
                    else
                    {
                        lengths[originalIndex, proposedIndex] = Math.Max(
                            lengths[originalIndex + 1, proposedIndex],
                            lengths[originalIndex, proposedIndex + 1]);
                    }
                }
            }

            return lengths;
        }

        private static TextDifference CreateDifference(
            string originalText,
            IList<Token> originalTokens,
            int originalStartIndex,
            int originalEndIndex,
            string proposedText,
            IList<Token> proposedTokens,
            int proposedStartIndex,
            int proposedEndIndex)
        {
            int originalStart = GetStart(originalText, originalTokens, originalStartIndex);
            int originalEnd = GetEnd(originalStart, originalTokens, originalStartIndex, originalEndIndex);
            int proposedStart = GetStart(proposedText, proposedTokens, proposedStartIndex);
            int proposedEnd = GetEnd(proposedStart, proposedTokens, proposedStartIndex, proposedEndIndex);

            return new TextDifference(
                originalStart,
                originalText.Substring(originalStart, originalEnd - originalStart),
                proposedStart,
                proposedText.Substring(proposedStart, proposedEnd - proposedStart));
        }

        private static int GetStart(string text, IList<Token> tokens, int tokenIndex)
        {
            return tokenIndex < tokens.Count ? tokens[tokenIndex].Start : text.Length;
        }

        private static int GetEnd(
            int start,
            IList<Token> tokens,
            int startIndex,
            int endIndex)
        {
            if (endIndex <= startIndex)
                return start;

            Token last = tokens[endIndex - 1];
            return last.Start + last.Length;
        }

        private static bool TokensEqual(
            IList<Token> original,
            int originalIndex,
            IList<Token> proposed,
            int proposedIndex)
        {
            return originalIndex < original.Count &&
                   proposedIndex < proposed.Count &&
                   string.Equals(
                       original[originalIndex].Value,
                       proposed[proposedIndex].Value,
                       StringComparison.Ordinal);
        }

        private static List<Token> Tokenize(string text)
        {
            List<Token> tokens = new List<Token>();

            foreach (Match match in TokenPattern.Matches(text))
                tokens.Add(new Token(match.Index, match.Length, match.Value));

            return tokens;
        }

        private sealed class Token
        {
            public Token(int start, int length, string value)
            {
                Start = start;
                Length = length;
                Value = value;
            }

            public int Start { get; private set; }

            public int Length { get; private set; }

            public string Value { get; private set; }
        }
    }
}
