namespace LifeBalance.Dashboard.Shared.Helpers;

/// <summary>
/// Utility helpers for working with strings.
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Converts a string to snake_case. For example, "DashboardWidget" → "dashboard_widget".
    /// </summary>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return System.Text.RegularExpressions.Regex
            .Replace(input, "([a-z0-9])([A-Z])", "$1_$2")
            .ToLowerInvariant();
    }

    /// <summary>
    /// Truncates a string to the given maximum length, appending "…" if truncated.
    /// </summary>
    public static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, maxLength - 1), "…");
    }
}
