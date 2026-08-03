using ClosedXML.Excel;
using LifeBalance.Reporting.Application.Common.Interfaces;

namespace LifeBalance.Reporting.Infrastructure.ReportGeneration;

/// <summary>
/// Generates an OpenXML (.xlsx) spreadsheet using ClosedXML.
/// </summary>
public sealed class ExcelReportGenerator : IExcelReportGenerator
{
    /// <inheritdoc/>
    public byte[] Generate(ReportExportData data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Report");

        worksheet.Cell(1, 1).Value = data.Title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;

        worksheet.Cell(2, 1).Value = data.Subtitle;
        worksheet.Cell(3, 1).Value = $"Generated: {data.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}";

        var headerRow = 5;
        for (var c = 0; c < data.Columns.Count; c++)
        {
            var cell = worksheet.Cell(headerRow, c + 1);
            cell.Value = data.Columns[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1A, 0x23, 0x7E);
            cell.Style.Font.FontColor = XLColor.White;
        }

        var rowIndex = headerRow + 1;
        foreach (var row in data.Rows)
        {
            for (var c = 0; c < row.Count; c++)
            {
                worksheet.Cell(rowIndex, c + 1).Value = row[c];
            }

            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
