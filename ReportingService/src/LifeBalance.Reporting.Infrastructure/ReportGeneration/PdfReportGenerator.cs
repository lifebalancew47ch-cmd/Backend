using LifeBalance.Reporting.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LifeBalance.Reporting.Infrastructure.ReportGeneration;

/// <summary>
/// Generates a portable PDF document using QuestPDF.
/// </summary>
public sealed class PdfReportGenerator : IPdfReportGenerator
{
    static PdfReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc/>
    public byte[] Generate(ReportExportData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text(data.Title).SemiBold().FontSize(18);
                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Text(data.Subtitle).FontColor(Colors.Grey.Darken2);
                    column.Item().Text($"Generated: {data.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}").FontColor(Colors.Grey.Darken2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in data.Columns)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var column in data.Columns)
                            {
                                header.Cell().Background(Colors.Blue.Darken4).Padding(5)
                                    .Text(column).FontColor(Colors.White).SemiBold();
                            }
                        });

                        foreach (var row in data.Rows)
                        {
                            foreach (var cell in row)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).Text(cell);
                            }
                        }
                    });
                });
                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }
}
