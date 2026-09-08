using System;
using System.Globalization;
using System.Text;

namespace CivilSpellAI.UI
{
    /// <summary>
    /// Produces a readable preview of MText contents. The returned value is
    /// display-only; the original formatted string remains the write payload.
    /// </summary>
    internal static class MTextDisplayFormatter
    {
        private const string ParagraphSeparator = "  ·  ";

        public static string Format(string entityType, string value)
        {
            if (!IsMTextEntity(entityType) || string.IsNullOrEmpty(value))
                return value;

            StringBuilder display = new StringBuilder(value.Length);

            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];

                if (current == '{' || current == '}')
                    continue;

                if (current != '\\' || index + 1 >= value.Length)
                {
                    display.Append(current);
                    continue;
                }

                char command = value[++index];

                if (command == '\\' || command == '{' || command == '}')
                {
                    display.Append(command);
                    continue;
                }

                if (command == '~')
                {
                    display.Append(' ');
                    continue;
                }

                if (command == 'P' || command == 'p' ||
                    command == 'N' || command == 'n' ||
                    command == 'X' || command == 'x')
                {
                    if (command == 'p' && HasTerminator(value, index + 1))
                    {
                        index = FindTerminator(value, index + 1);
                    }
                    else
                    {
                        display.Append(ParagraphSeparator);
                    }

                    continue;
                }

                if ((command == 'U' || command == 'u') &&
                    index + 5 < value.Length &&
                    value[index + 1] == '+')
                {
                    string hexadecimal = value.Substring(index + 2, 4);
                    int codePoint;

                    if (int.TryParse(
                        hexadecimal,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out codePoint))
                    {
                        display.Append((char)codePoint);
                        index += 5;
                        continue;
                    }
                }

                if (command == 'S' || command == 's')
                {
                    int terminator = FindTerminator(value, index + 1);

                    if (terminator >= 0)
                    {
                        string stacked = value.Substring(
                            index + 1,
                            terminator - index - 1);
                        display.Append(stacked
                            .Replace('#', '/')
                            .Replace('^', '/'));
                        index = terminator;
                        continue;
                    }
                }

                if (IsToggle(command))
                    continue;

                int end = FindTerminator(value, index + 1);

                if (end >= 0)
                {
                    index = end;
                    continue;
                }

                // Unknown unterminated control: omit only the slash and keep
                // the following character visible rather than hiding content.
                display.Append(command);
            }

            return display.ToString().Trim();
        }

        private static bool IsMTextEntity(string entityType)
        {
            return string.Equals(entityType, "MText", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entityType, "MLeader", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsToggle(char command)
        {
            return command == 'L' || command == 'l' ||
                command == 'O' || command == 'o' ||
                command == 'K' || command == 'k';
        }

        private static bool HasTerminator(string value, int start)
        {
            return FindTerminator(value, start) >= 0;
        }

        private static int FindTerminator(string value, int start)
        {
            for (int index = start; index < value.Length; index++)
            {
                if (value[index] == ';')
                    return index;

                if (value[index] == '\\' || value[index] == '{' || value[index] == '}')
                    return -1;
            }

            return -1;
        }
    }
}
