using AllAroundEstimates.Models;
using AllAroundEstimates.Services;

namespace AllAroundEstimates.Views;

public partial class ChangeOrderPage : ContentPage
{
    private List<SavedEstimate> _previousEstimates = new();
    private ChangeOrderData? _currentChangeOrder;

    public ChangeOrderPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _previousEstimates = EstimateStorage.GetPreviousEstimates();
            PreviousEstimatePicker.ItemsSource = _previousEstimates
                .Select(e => $"{e.CustomerName} — {e.EstimateNumber} — {e.TotalAmount:C2}")
                .ToList();
        }
        catch
        {
            // Best-effort only; the picker just stays empty and the fields below can still be
            // filled in manually.
            _previousEstimates = new List<SavedEstimate>();
        }
    }

    private void OnPreviousEstimateSelected(object sender, EventArgs e)
    {
        var index = PreviousEstimatePicker.SelectedIndex;
        if (index < 0 || index >= _previousEstimates.Count)
            return;

        var selected = _previousEstimates[index];
        CustomerNameEntry.Text = selected.CustomerName;
        OriginalEstimateNumberEntry.Text = selected.EstimateNumber;
        OriginalTotalEntry.Text = selected.TotalAmount.ToString("F2");
    }

    private ChangeOrderData? BuildChangeOrderData()
    {
        if (!decimal.TryParse(OriginalTotalEntry.Text, out var originalTotal) ||
            !decimal.TryParse(CostOfChangeEntry.Text, out var costOfChange))
        {
            return null;
        }

        return new ChangeOrderData
        {
            OriginalEstimateNumber = string.IsNullOrWhiteSpace(OriginalEstimateNumberEntry.Text) ? "N/A" : OriginalEstimateNumberEntry.Text,
            CustomerName = string.IsNullOrWhiteSpace(CustomerNameEntry.Text) ? "N/A" : CustomerNameEntry.Text,
            Date = DateTime.Now,
            OriginalTotalAmount = originalTotal,
            DescriptionOfChanges = DescriptionEditor.Text ?? string.Empty,
            CostOfChange = costOfChange
        };
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        var data = BuildChangeOrderData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers for the amounts.", "OK");
            return;
        }

        _currentChangeOrder = data;

        SummaryEditor.Text =
            $"Original Total:  {data.OriginalTotalAmount:C2}\n" +
            $"Cost of Change:  {data.CostOfChange:C2}\n" +
            $"Revised Total:   {data.RevisedTotal:C2}";
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var data = BuildChangeOrderData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers for the amounts.", "OK");
            return;
        }

        _currentChangeOrder = data;

        EstimateStorage.AddEstimate(new SavedEstimate
        {
            CustomerName = data.CustomerName,
            EstimateNumber = data.OriginalEstimateNumber,
            TotalAmount = data.RevisedTotal,
            IsChangeOrder = true
        });

        await DisplayAlertAsync("Saved", "Change order saved successfully.", "OK");
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        var data = BuildChangeOrderData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers for the amounts.", "OK");
            return;
        }

        _currentChangeOrder = data;

        try
        {
            using var stream = new MemoryStream();
            PdfGenerator.GenerateChangeOrderPdf(stream, data);

            var fileName = $"ChangeOrder_{data.OriginalEstimateNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            await PdfExportService.ExportPdfAsync(fileName, stream);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
    }
}
