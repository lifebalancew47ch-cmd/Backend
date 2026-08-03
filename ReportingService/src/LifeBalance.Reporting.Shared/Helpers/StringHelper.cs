namespace LifeBalance.Reporting.Shared.Helpers;

/// <summary>
/// String helper utilities shared across the service.
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Returns a value indicating whether the string is null, empty or only whitespace.
    /// </summary>
    public static bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Normalizes a text by trimming and collapsing inner whitespace.
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex
            .Replace(value.Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Sanitizes a value to be used safely inside a file name.
    /// </summary>
    public static string ToSafeFileName(string value)
        => System.Text.RegularExpressions.Regex.Replace(value, @"[^a-zA-Z0-9_-]+", "_");
}
