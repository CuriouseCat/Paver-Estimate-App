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
#elif ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
using Path = System.IO.Path;

/// <summary>
/// QuestPDF ships no native Android runtime library (verified against the latest release as of
/// this writing) -- referencing it on Android caused MSBuild to bundle QuestPDF's incompatible
/// Linux desktop native library instead, which crashed the app at the native level. QuestPDF is
/// excluded from the Android build entirely (see the csproj); this is a hand-rolled equivalent
/// using Android.Graphics.Pdf.PdfDocument, which is part of the Android SDK bindings and carries
/// no third-party native dependency at all. Keep this in sync with the QuestPDF version above
/// when the document layout changes.
/// </summary>
public static class PdfGenerator
{
    private const float PageWidth = 612f;  // US Letter at 72 dpi, matches QuestPDF.PageSizes.Letter
    private const float PageHeight = 792f;
    private const float Margin = 30f;
    private static readonly Color RedAccent = Color.Rgb(211, 47, 47);
    private static readonly Color DarkGray = Color.Rgb(97, 97, 97);
    private static readonly Color LightGray = Color.Rgb(224, 224, 224);

    public static void GenerateEstimatePdf(Stream stream, EstimateData data)
    {
        using var document = new PdfDocument();
        using var pageInfo = new PdfDocument.PageInfo.Builder((int)PageWidth, (int)PageHeight, 1).Create();
        var page = document.StartPage(pageInfo)!;
        var canvas = page.Canvas!;

        var y = DrawHeader(canvas, "ESTIMATE", data.Date);

        y += 15;
        canvas.DrawText($"Customer: {data.CustomerName}", Margin, y, TextPaint(11, bold: true));
        y += 16;
        canvas.DrawText($"Estimate #: {data.EstimateNumber}", Margin, y, TextPaint(9, DarkGray));
        y += 25;

        y = DrawTableHeader(canvas, y);
        y = DrawTableRow(canvas, y, "Paver Material", $"{data.SquareFootage:N0} sq ft", data.PaverPricePerSqFt, data.SquareFootage * data.PaverPricePerSqFt);
        y = DrawTableRow(canvas, y, "Base Material Cost", "1", data.BaseMaterialCost, data.BaseMaterialCost);
        y = DrawTableRow(canvas, y, "Labor", $"{data.LaborHours:N1} hrs", data.HourlyLaborRate, data.LaborTotal);
        y = DrawTableRow(canvas, y, "Extra Costs", "1", data.ExtraCosts, data.ExtraCosts);

        var totalsY = PageHeight - Margin - 90;
        DrawTotalsRow(canvas, totalsY, "Subtotal:", data.Subtotal, bold: false);
        DrawTotalsRow(canvas, totalsY + 16, "Margin (15%):", data.MarginAmount, bold: false);
        canvas.DrawLine(PageWidth - Margin - 230, totalsY + 24, PageWidth - Margin, totalsY + 24, LinePaint(1));
        DrawTotalsRow(canvas, totalsY + 40, "Grand Total:", data.GrandTotal, bold: true);
        DrawSignatureLine(canvas);

        document.FinishPage(page);
        WriteDocumentToStream(document, stream);
    }

    public static void GenerateChangeOrderPdf(Stream stream, ChangeOrderData data)
    {
        using var document = new PdfDocument();
        using var pageInfo = new PdfDocument.PageInfo.Builder((int)PageWidth, (int)PageHeight, 1).Create();
        var page = document.StartPage(pageInfo)!;
        var canvas = page.Canvas!;

        var y = DrawHeader(canvas, "CHANGE ORDER", data.Date);

        y += 15;
        canvas.DrawText($"Customer: {data.CustomerName}", Margin, y, TextPaint(11, bold: true));
        y += 16;
        canvas.DrawText($"Reference Estimate #: {data.OriginalEstimateNumber}", Margin, y, TextPaint(9, DarkGray));
        y += 25;

        canvas.DrawText("Description of Changes:", Margin, y, TextPaint(11, bold: true));
        y += 6;

        var descriptionText = string.IsNullOrWhiteSpace(data.DescriptionOfChanges) ? "-" : data.DescriptionOfChanges;
        var descriptionLines = WrapText(descriptionText, TextPaint(11), PageWidth - Margin * 2 - 16);
        var descriptionHeight = Math.Max(30f, descriptionLines.Count * 15f + 16f);
        canvas.DrawRect(Margin, y, PageWidth - Margin, y + descriptionHeight, FillPaint(Color.Rgb(245, 245, 245)));
        var lineY = y + 20;
        foreach (var line in descriptionLines)
        {
            canvas.DrawText(line, Margin + 8, lineY, TextPaint(11));
            lineY += 15;
        }
        y += descriptionHeight + 25;

        DrawChangeOrderRow(canvas, ref y, "Original Contract Amount", data.OriginalTotalAmount, bold: false);
        DrawChangeOrderRow(canvas, ref y, "Net Change", data.CostOfChange, bold: false);
        DrawChangeOrderRow(canvas, ref y, "Revised Contract Amount", data.RevisedTotal, bold: true);

        var totalsY = PageHeight - Margin - 60;
        DrawTotalsRow(canvas, totalsY, "Revised Total:", data.RevisedTotal, bold: true);
        DrawSignatureLine(canvas);

        document.FinishPage(page);
        WriteDocumentToStream(document, stream);
    }

    private static void WriteDocumentToStream(PdfDocument document, Stream stream) =>
        document.WriteTo(stream);

    private static float DrawHeader(Canvas canvas, string documentType, DateTime date)
    {
        var y = Margin + 18;
        canvas.DrawText("All Around Paver Restoration", Margin, y, TextPaint(20, bold: true));
        y += 20;
        canvas.DrawText(documentType, Margin, y, TextPaint(14, RedAccent, bold: true));
        y += 16;
        canvas.DrawText($"Date: {date:MM/dd/yyyy}", Margin, y, TextPaint(10, DarkGray));

        DrawLogo(canvas);

        return Margin + 70;
    }

    private static void DrawLogo(Canvas canvas)
    {
        const float logoWidth = 80f;
        const float logoHeight = 60f;
        var left = PageWidth - Margin - logoWidth;
        var top = Margin;

        var logoPath = Path.Combine(FileSystem.AppDataDirectory, "company_logo.png");
        Bitmap? logoBitmap = null;
        try
        {
            if (File.Exists(logoPath))
            {
                logoBitmap = BitmapFactory.DecodeFile(logoPath);
            }
        }
        catch
        {
            logoBitmap = null;
        }

        if (logoBitmap is not null)
        {
            var scale = Math.Min(logoWidth / logoBitmap.Width, logoHeight / logoBitmap.Height);
            var drawWidth = logoBitmap.Width * scale;
            var drawHeight = logoBitmap.Height * scale;
            var destLeft = left + (logoWidth - drawWidth) / 2;
            var destTop = top + (logoHeight - drawHeight) / 2;
            var destRect = new RectF(destLeft, destTop, destLeft + drawWidth, destTop + drawHeight);
            canvas.DrawBitmap(logoBitmap, null, destRect, new Paint(PaintFlags.AntiAlias));
            logoBitmap.Dispose();
        }
        else
        {
            var borderPaint = LinePaint(1);
            canvas.DrawRect(left, top, left + logoWidth, top + logoHeight, borderPaint);

            var placeholderPaint = TextPaint(9, DarkGray);
            placeholderPaint.TextAlign = Paint.Align.Center;
            canvas.DrawText("LOGO", left + logoWidth / 2, top + logoHeight / 2 + 3, placeholderPaint);
        }
    }

    private static float DrawTableHeader(Canvas canvas, float y)
    {
        var boldPaint = TextPaint(10, bold: true);
        canvas.DrawText("Description", Margin, y, boldPaint);
        DrawRightAligned(canvas, "Qty", Margin + 300, y, boldPaint);
        DrawRightAligned(canvas, "Rate", Margin + 380, y, boldPaint);
        DrawRightAligned(canvas, "Total", PageWidth - Margin, y, boldPaint);

        y += 6;
        canvas.DrawLine(Margin, y, PageWidth - Margin, y, LinePaint(1));
        return y + 18;
    }

    private static float DrawTableRow(Canvas canvas, float y, string description, string qty, decimal rate, decimal total)
    {
        var textPaint = TextPaint(10);
        canvas.DrawText(description, Margin, y, textPaint);
        DrawRightAligned(canvas, qty, Margin + 300, y, textPaint);
        DrawRightAligned(canvas, rate.ToString("C2"), Margin + 380, y, textPaint);
        DrawRightAligned(canvas, total.ToString("C2"), PageWidth - Margin, y, textPaint);

        y += 8;
        canvas.DrawLine(Margin, y, PageWidth - Margin, y, LinePaint(0.5f, LightGray));
        return y + 16;
    }

    private static void DrawChangeOrderRow(Canvas canvas, ref float y, string label, decimal value, bool bold)
    {
        var paint = TextPaint(10, bold: bold);
        canvas.DrawText(label, Margin, y, paint);
        DrawRightAligned(canvas, value.ToString("C2"), PageWidth - Margin, y, paint);

        y += 8;
        canvas.DrawLine(Margin, y, PageWidth - Margin, y, LinePaint(0.5f, LightGray));
        y += 16;
    }

    private static void DrawTotalsRow(Canvas canvas, float y, string label, decimal value, bool bold)
    {
        const float totalsLeft = PageWidth - Margin - 230;
        var paint = TextPaint(10, bold: bold);
        canvas.DrawText(label, totalsLeft, y, paint);
        DrawRightAligned(canvas, value.ToString("C2"), PageWidth - Margin, y, paint);
    }

    private static void DrawSignatureLine(Canvas canvas)
    {
        var y = PageHeight - Margin - 15;
        canvas.DrawLine(Margin, y, Margin + 260, y, LinePaint(1));
        canvas.DrawText("Customer Signature & Date", Margin, y + 12, TextPaint(9, DarkGray));
    }

    private static void DrawRightAligned(Canvas canvas, string text, float rightX, float y, Paint paint)
    {
        var originalAlign = paint.TextAlign;
        paint.TextAlign = Paint.Align.Right;
        canvas.DrawText(text, rightX, y, paint);
        paint.TextAlign = originalAlign;
    }

    private static List<string> WrapText(string text, Paint paint, float maxWidth)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var candidate = currentLine.Length == 0 ? word : $"{currentLine} {word}";
            if (paint.MeasureText(candidate) > maxWidth && currentLine.Length > 0)
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = candidate;
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine);
        }

        return lines.Count > 0 ? lines : new List<string> { string.Empty };
    }

    private static Paint TextPaint(float textSize, bool bold = false) => TextPaint(textSize, Color.Black, bold);

    private static Paint TextPaint(float textSize, Color color, bool bold = false)
    {
        var paint = new Paint(PaintFlags.AntiAlias)
        {
            TextSize = textSize,
            Color = color,
            UnderlineText = false,
        };
        paint.SetTypeface(bold ? Typeface.DefaultBold : Typeface.Default);
        return paint;
    }

    private static Paint LinePaint(float strokeWidth, Color? color = null)
    {
        var paint = new Paint(PaintFlags.AntiAlias)
        {
            Color = color ?? Color.Black,
            StrokeWidth = strokeWidth,
        };
        // Paint defaults to Fill; without Stroke, DrawRect (the logo placeholder border) would
        // render as a solid block instead of an outline.
        paint.SetStyle(Paint.Style.Stroke);
        return paint;
    }

    private static Paint FillPaint(Color color)
    {
        return new Paint(PaintFlags.AntiAlias)
        {
            Color = color,
        };
    }
}
#endif
