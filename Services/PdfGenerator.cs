using AllAroundEstimates.Models;

namespace AllAroundEstimates.Services;

#if !ANDROID
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IContainer = QuestPDF.Infrastructure.IContainer;
using Colors = QuestPDF.Helpers.Colors;
using FontManager = QuestPDF.Drawing.FontManager;

public static class PdfGenerator
{
    private const string FontFamily = "Open Sans";
    private static bool _fontsRegistered;
    private static readonly object FontRegistrationLock = new();

    public static void GenerateEstimatePdf(Stream stream, EstimateData data)
    {
        EnsureFontsRegistered();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(11));

                page.Header().Element(c => ComposeHeader(c, "ESTIMATE", data.Date));
                page.Content().PaddingTop(15).Element(c => ComposeEstimateBody(c, data));
                page.Footer().Element(c => ComposeEstimateFooter(c, data));
            });
        });

        document.GeneratePdf(stream);
    }

    public static void GenerateChangeOrderPdf(Stream stream, ChangeOrderData data)
    {
        EnsureFontsRegistered();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(11));

                page.Header().Element(c => ComposeHeader(c, "CHANGE ORDER", data.Date));
                page.Content().PaddingTop(15).Element(c => ComposeChangeOrderBody(c, data));
                page.Footer().Element(c => ComposeChangeOrderFooter(c, data));
            });
        });

        document.GeneratePdf(stream);
    }

    /// <summary>
    /// QuestPDF defaults to a bundled font (Calibri-family) when no FontFamily is set explicitly.
    /// That font doesn't exist on Android/Linux, and QuestPDF throws when it can't resolve a
    /// requested family. Registering our own bundled TrueType font removes any dependency on
    /// fonts being pre-installed on the host OS.
    /// </summary>
    private static void EnsureFontsRegistered()
    {
        if (_fontsRegistered)
            return;

        lock (FontRegistrationLock)
        {
            if (_fontsRegistered)
                return;

            try
            {
                using var regularStream = FileSystem.OpenAppPackageFileAsync("OpenSans-Regular.ttf").GetAwaiter().GetResult();
                FontManager.RegisterFont(regularStream);

                using var semiboldStream = FileSystem.OpenAppPackageFileAsync("OpenSans-Semibold.ttf").GetAwaiter().GetResult();
                FontManager.RegisterFont(semiboldStream);
            }
            catch
            {
                // Best-effort: if bundled fonts can't be loaded, QuestPDF falls back to its own default,
                // which is still preferable to crashing this registration step.
            }
            finally
            {
                _fontsRegistered = true;
            }
        }
    }

    private static void ComposeHeader(IContainer container, string documentType, DateTime date)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("All Around Paver Restoration").FontSize(20).Bold();
                column.Item().PaddingTop(4).Text(documentType).FontSize(14).Bold().FontColor(Colors.Red.Medium);
                column.Item().PaddingTop(2).Text($"Date: {date:MM/dd/yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
            });

            row.ConstantItem(80).Height(60).Element(ComposeLogo);
        });
    }

    private static void ComposeLogo(IContainer container)
    {
        var logoPath = Path.Combine(FileSystem.AppDataDirectory, "company_logo.png");

        if (File.Exists(logoPath))
        {
            container.Image(logoPath).FitArea();
        }
        else
        {
            container
                .Border(1).BorderColor(Colors.Grey.Medium)
                .AlignCenter().AlignMiddle()
                .Text("LOGO").FontSize(9).FontColor(Colors.Grey.Medium);
        }
    }

    private static void ComposeEstimateBody(IContainer container, EstimateData data)
    {
        container.Column(column =>
        {
            column.Item().Text($"Customer: {data.CustomerName}").Bold();
            column.Item().Text($"Estimate #: {data.EstimateNumber}").FontSize(9).FontColor(Colors.Grey.Darken1);

            column.Item().PaddingTop(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Description");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Rate");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Total");

                    static IContainer HeaderCellStyle(IContainer c) => c
                        .DefaultTextStyle(x => x.Bold())
                        .PaddingVertical(5)
                        .BorderBottom(1).BorderColor(Colors.Black);
                });

                void AddRow(string description, string qty, decimal rate, decimal total)
                {
                    table.Cell().Element(CellStyle).Text(description);
                    table.Cell().Element(CellStyle).AlignRight().Text(qty);
                    table.Cell().Element(CellStyle).AlignRight().Text(rate.ToString("C2"));
                    table.Cell().Element(CellStyle).AlignRight().Text(total.ToString("C2"));

                    static IContainer CellStyle(IContainer c) => c
                        .PaddingVertical(6)
                        .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                }

                AddRow("Paver Material", $"{data.SquareFootage:N0} sq ft", data.PaverPricePerSqFt, data.SquareFootage * data.PaverPricePerSqFt);
                AddRow("Base Material Cost", "1", data.BaseMaterialCost, data.BaseMaterialCost);
                AddRow("Labor", $"{data.LaborHours:N1} hrs", data.HourlyLaborRate, data.LaborTotal);
                AddRow("Extra Costs", "1", data.ExtraCosts, data.ExtraCosts);
            });
        });
    }

    private static void ComposeEstimateFooter(IContainer container, EstimateData data)
    {
        container.Column(column =>
        {
            column.Item().AlignRight().Width(230).Column(totals =>
            {
                totals.Item().Row(r =>
                {
                    r.RelativeItem().Text("Subtotal:");
                    r.ConstantItem(90).AlignRight().Text(data.Subtotal.ToString("C2"));
                });
                totals.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text("Margin (15%):");
                    r.ConstantItem(90).AlignRight().Text(data.MarginAmount.ToString("C2"));
                });
                totals.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Black).PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Text("Grand Total:").Bold();
                    r.ConstantItem(90).AlignRight().Text(data.GrandTotal.ToString("C2")).Bold();
                });
            });

            column.Item().PaddingTop(35).Width(260).BorderBottom(1).BorderColor(Colors.Black);
            column.Item().PaddingTop(2).Text("Customer Signature & Date").FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void ComposeChangeOrderBody(IContainer container, ChangeOrderData data)
    {
        container.Column(column =>
        {
            column.Item().Text($"Customer: {data.CustomerName}").Bold();
            column.Item().Text($"Reference Estimate #: {data.OriginalEstimateNumber}").FontSize(9).FontColor(Colors.Grey.Darken1);

            column.Item().PaddingTop(15).Text("Description of Changes:").Bold();
            column.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(8)
                .Text(string.IsNullOrWhiteSpace(data.DescriptionOfChanges) ? "-" : data.DescriptionOfChanges);

            column.Item().PaddingTop(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                void AddRow(string label, decimal value, bool bold = false)
                {
                    table.Cell().Element(CellStyle).Text(label);
                    table.Cell().Element(CellStyle).AlignRight().Text(value.ToString("C2"));

                    IContainer CellStyle(IContainer c)
                    {
                        var styled = c.PaddingVertical(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                        return bold ? styled.DefaultTextStyle(x => x.Bold()) : styled;
                    }
                }

                AddRow("Original Contract Amount", data.OriginalTotalAmount);
                AddRow("Net Change", data.CostOfChange);
                AddRow("Revised Contract Amount", data.RevisedTotal, true);
            });
        });
    }

    private static void ComposeChangeOrderFooter(IContainer container, ChangeOrderData data)
    {
        container.Column(column =>
        {
            column.Item().AlignRight().Width(230).Row(r =>
            {
                r.RelativeItem().Text("Revised Total:").Bold();
                r.ConstantItem(90).AlignRight().Text(data.RevisedTotal.ToString("C2")).Bold();
            });

            column.Item().PaddingTop(35).Width(260).BorderBottom(1).BorderColor(Colors.Black);
            column.Item().PaddingTop(2).Text("Customer Signature & Date").FontSize(9).FontColor(Colors.Grey.Darken1);
        });
    }
}
#else
/// <summary>
/// QuestPDF ships no native Android runtime library (verified against the latest release as of
/// this writing). Referencing it on Android causes MSBuild to bundle QuestPDF's incompatible Linux
/// desktop native library instead, which crashes the app at the native level the moment anything
/// touches it. QuestPDF is excluded from the Android build entirely (see the csproj); this stub
/// keeps the app's other features working and fails PDF export with a message the UI can show,
/// instead of crashing.
/// </summary>
public static class PdfGenerator
{
    private const string NotSupportedMessage = "PDF export is not currently available on Android.";

    public static void GenerateEstimatePdf(Stream stream, EstimateData data) =>
        throw new PlatformNotSupportedException(NotSupportedMessage);

    public static void GenerateChangeOrderPdf(Stream stream, ChangeOrderData data) =>
        throw new PlatformNotSupportedException(NotSupportedMessage);
}
#endif
