using AllAroundEstimates.Models;
using AllAroundEstimates.Services;

namespace AllAroundEstimates.Views;

[QueryProperty(nameof(CustomerNameFilter), "customerName")]
public partial class SavedEstimatesPage : ContentPage
{
    private List<SavedEstimate> _allEstimates = new();

    public string? CustomerNameFilter { get; set; }

    public SavedEstimatesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _allEstimates = EstimateStorage.GetPreviousEstimates();
        }
        catch
        {
            _allEstimates = new List<SavedEstimate>();
        }

        SearchEntry.Text = CustomerNameFilter ?? string.Empty;
        CustomerNameFilter = null; // consume it so returning here later starts unfiltered
        ApplyFilter(SearchEntry.Text);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplyFilter(e.NewTextValue);

    private void ApplyFilter(string? searchText)
    {
        searchText = searchText?.Trim() ?? string.Empty;
        EstimatesList.ItemsSource = string.IsNullOrEmpty(searchText)
            ? _allEstimates
            : _allEstimates.Where(e => e.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async void OnEstimateSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SavedEstimate selected)
            return;

        EstimatesList.SelectedItem = null;

        if (selected.EstimateDetail is null)
        {
            await DisplayAlertAsync("Not Available", "This estimate was saved before editing was supported, so it can't be reopened.", "OK");
            return;
        }

        var action = await DisplayActionSheetAsync(selected.DisplayText, "Cancel", null, "Edit Estimate", "Deposit Invoice", "Final Invoice");

        switch (action)
        {
            case "Edit Estimate":
                await Shell.Current.GoToAsync($"{nameof(NewEstimatePage)}?estimateNumber={Uri.EscapeDataString(selected.EstimateNumber)}");
                break;
            case "Deposit Invoice":
                await CreateInvoiceAsync(selected.EstimateDetail, InvoiceType.Deposit);
                break;
            case "Final Invoice":
                await CreateInvoiceAsync(selected.EstimateDetail, InvoiceType.Final);
                break;
        }
    }

    private async Task CreateInvoiceAsync(EstimateData data, InvoiceType type)
    {
        var priorDeposits = InvoiceStorage.GetInvoicesForEstimate(data.EstimateNumber)
            .Where(i => i.Type == InvoiceType.Deposit)
            .Sum(i => i.Amount);

        var suggestedAmount = type == InvoiceType.Deposit
            ? data.TotalDue * 0.5m
            : Math.Max(0m, data.TotalDue - priorDeposits);

        var amountText = await DisplayPromptAsync(
            type == InvoiceType.Deposit ? "Deposit Invoice" : "Final Invoice",
            "Enter the invoice amount:",
            initialValue: suggestedAmount.ToString("0.##"),
            keyboard: Keyboard.Numeric);

        if (amountText is null)
            return;

        if (!decimal.TryParse(amountText, out var amount))
        {
            await DisplayAlertAsync("Invalid Input", "Please enter a valid amount.", "OK");
            return;
        }

        var invoice = new Invoice
        {
            EstimateNumber = data.EstimateNumber,
            CustomerName = data.CustomerName,
            Type = type,
            Amount = amount
        };

        try
        {
            using var stream = new MemoryStream();
            PdfGenerator.GenerateInvoicePdf(stream, data, invoice);

            InvoiceStorage.AddInvoice(invoice);

            var fileName = $"{(type == InvoiceType.Deposit ? "Deposit" : "Final")}Invoice_{data.EstimateNumber}.pdf";
            await PdfExportService.ExportPdfAsync(fileName, stream);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
    }
}
