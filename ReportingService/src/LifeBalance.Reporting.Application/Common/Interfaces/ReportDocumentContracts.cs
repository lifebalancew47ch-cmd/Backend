namespace LifeBalance.Reporting.Application.Common.Interfaces;

/// <summary>
/// Tabular data used to render a downloadable report document.
/// </summary>
public sealed record ReportExportData(
    string Title,
    string Subtitle,
    DateTime GeneratedAtUtc,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>
/// The result of a report document generation: file name, content type and bytes.
/// </summary>
public sealed record ReportExportResult(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Generates a PDF document from <see cref="ReportExportData"/>.
/// </summary>
public interface IPdfReportGenerator
{
    /// <summary>Renders the data as a portable PDF document.</summary>
    byte[] Generate(ReportExportData data);
}

/// <summary>
/// Generates an Excel (.xlsx) workbook from <see cref="ReportExportData"/>.
/// </summary>
public interface IExcelReportGenerator
{
    /// <summary>Renders the data as an OpenXML spreadsheet.</summary>
    byte[] Generate(ReportExportData data);
}

/// <summary>
/// Generates a UTF-8 CSV document from <see cref="ReportExportData"/>.
/// </summary>
public interface ICsvReportGenerator
{
    /// <summary>Renders the data as a comma separated values document.</summary>
    byte[] Generate(ReportExportData data);
}
