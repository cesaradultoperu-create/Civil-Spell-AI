using System;
using System.Text.RegularExpressions;

static class TitleParts
{
    public static string[] Split(string title)
    {
        title = (title ?? "").Trim().ToUpperInvariant();
        var separator = Regex.Match(title, @"\s+-\s*|-\s+");
        return separator.Success
            ? new[] { title.Substring(0, separator.Index).TrimEnd(), title.Substring(separator.Index + separator.Length).TrimStart() }
            : new[] { title, "" };
    }
}
