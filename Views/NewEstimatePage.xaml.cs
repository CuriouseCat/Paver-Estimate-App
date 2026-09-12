using AllAroundEstimates.Models;
using AllAroundEstimates.Services;

namespace AllAroundEstimates.Views;

public partial class NewEstimatePage : ContentPage
{
    private EstimateData? _currentEstimate;

    public NewEstimatePage()
    {
        InitializeComponent();
    }

    private EstimateData? BuildEstimateData()
    {
        if (!decimal.TryParse(SquareFootageEntry.Text, out var sqFt) ||
            !decimal.TryParse(PaverPriceEntry.Text, out var paverPrice) ||
            !decimal.TryParse(BaseMaterialCostEntry.Text, out var baseCost) ||
            !decimal.TryParse(LaborHoursEntry.Text, out var laborHours) ||
            !decimal.TryParse(HourlyRateEntry.Text, out var hourlyRate) ||
            !decimal.TryParse(ExtraCostsEntry.Text, out var extraCosts))
        {
            return null;
        }

        return new EstimateData
        {
            EstimateNumber = _currentEstimate?.EstimateNumber ?? $"EST-{DateTime.Now:yyyyMMddHHmmss}",
            CustomerName = string.IsNullOrWhiteSpace(CustomerNameEntry.Text) ? "N/A" : CustomerNameEntry.Text,
            Date = DateTime.Now,
            SquareFootage = sqFt,
            PaverPricePerSqFt = paverPrice,
            BaseMaterialCost = baseCost,
            LaborHours = laborHours,
            HourlyLaborRate = hourlyRate,
            ExtraCosts = extraCosts
        };
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        var data = BuildEstimateData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields.", "OK");
            return;
        }

        _currentEstimate = data;

        SummaryEditor.Text =
            $"Material Total:  {data.MaterialTotal:C2}\n" +
            $"Labor Total:     {data.LaborTotal:C2}\n" +
            $"Extra Costs:     {data.ExtraCosts:C2}\n" +
            $"Subtotal:        {data.Subtotal:C2}\n" +
            $"Margin (15%):    {data.MarginAmount:C2}\n" +
            $"Grand Total:     {data.GrandTotal:C2}";
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var data = BuildEstimateData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields.", "OK");
            return;
        }

        _currentEstimate = data;

        EstimateStorage.AddEstimate(new SavedEstimate
        {
            CustomerName = data.CustomerName,
            TotalAmount = data.GrandTotal,
            IsChangeOrder = false
        });

        await DisplayAlertAsync("Saved", "Estimate saved successfully.", "OK");
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        var data = BuildEstimateData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields.", "OK");
            return;
        }

        _currentEstimate = data;

        using var stream = new MemoryStream();
        PdfGenerator.GenerateEstimatePdf(stream, data);

        var fileName = $"Estimate_{data.EstimateNumber}.pdf";

        try
        {
            await PdfExportService.ExportPdfAsync(fileName, stream);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
    }
}
