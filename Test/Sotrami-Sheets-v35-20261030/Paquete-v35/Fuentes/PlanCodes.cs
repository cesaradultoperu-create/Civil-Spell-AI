using System.Text.RegularExpressions;

// One scope for import, sheet registration, title fields and lifecycle commands.
static class PlanCodes
{
    public static bool IsSupported(string code)
    {
        return code != null && Regex.IsMatch(code,
            @"^1107\.001\.00\.(?:14-200|16-300)-\d{2}-P-\d{3}$",
            RegexOptions.IgnoreCase);
    }

    public static string Stage(string code)
    {
        return IsSupported(code) ? code.Substring(12, 6) : "";
    }
}
