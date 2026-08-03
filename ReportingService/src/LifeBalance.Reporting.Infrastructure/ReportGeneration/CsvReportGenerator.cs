using System.Globalization;
using System.Text;
using LifeBalance.Reporting.Application.Common.Interfaces;

namespace LifeBalance.Reporting.Infrastructure.ReportGeneration;

/// <summary>
/// Generates a UTF-8 CSV document.
/// </summary>
public sealed class CsvReportGenerator : ICsvReportGenerator
{
    /// <inheritdoc/>
    public byte[] Generate(ReportExportData data)
    {
        var builder = new StringBuilder();

        builder.AppendLine(data.Title);
        builder.AppendLine(data.Subtitle);
        builder.AppendLine($"Generated,{data.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}");
        builder.AppendLine();

        builder.AppendLine(string.Join(",", data.Columns.Select(Escape)));

        foreach (var row in data.Rows)
        {
            builder.AppendLine(string.Join(",", row.Select(Escape)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        if (normalized.Contains(',') || normalized.Contains('"') || normalized.Contains('\n') || normalized.Contains('\r'))
        {
            return $"\"{normalized}\"";
        }

        return normalized;
    }
}
