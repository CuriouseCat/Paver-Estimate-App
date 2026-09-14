using AllAroundEstimates.Models;
using AllAroundEstimates.Services;

namespace AllAroundEstimates.Views;

public partial class NewEstimatePage : ContentPage
{
    private readonly List<(Entry Description, Entry Amount)> _customChargeRows = new();
    private List<ServiceType> _serviceTypes = new();
    private EstimateData? _currentEstimate;

    public NewEstimatePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _serviceTypes = ServiceTypeStorage.LoadServiceTypes();
        }
        catch
        {
            _serviceTypes = new List<ServiceType>();
        }

        ServiceTypePicker.ItemsSource = _serviceTypes.Select(t => t.Name).ToList();
    }

    private void OnServiceTypeSelected(object sender, EventArgs e)
    {
        var index = ServiceTypePicker.SelectedIndex;
        if (index < 0 || index >= _serviceTypes.Count)
            return;

        var selected = _serviceTypes[index];
        PaverPriceEntry.Text = selected.PaverPricePerSqFt.ToString("0.##");
        HourlyRateEntry.Text = selected.HourlyLaborRate.ToString("0.##");
    }

    private async void OnManageTypesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ServiceTypesPage));
    }

    private void OnAddCustomChargeClicked(object sender, EventArgs e)
    {
        var descriptionEntry = new Entry { Placeholder = "Description", HorizontalOptions = LayoutOptions.Fill };
        var amountEntry = new Entry { Placeholder = "0.00", Keyboard = Keyboard.Numeric, WidthRequest = 90 };

        var removeButton = new Button
        {
            Text = "✕",
            FontSize = 12,
            Padding = new Thickness(8, 4),
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Red
        };

        var row = new HorizontalStackLayout { Spacing = 8 };
        row.Add(descriptionEntry);
        row.Add(amountEntry);
        row.Add(removeButton);

        removeButton.Clicked += (_, _) =>
        {
            CustomChargesContainer.Children.Remove(row);
            _customChargeRows.RemoveAll(r => r.Description == descriptionEntry && r.Amount == amountEntry);
        };

        CustomChargesContainer.Children.Add(row);
        _customChargeRows.Add((descriptionEntry, amountEntry));
    }

    private EstimateData? BuildEstimateData()
    {
        if (!decimal.TryParse(SquareFootageEntry.Text, out var sqFt) ||
            !decimal.TryParse(PaverPriceEntry.Text, out var paverPrice) ||
            !decimal.TryParse(BaseMaterialCostEntry.Text, out var baseCost) ||
            !decimal.TryParse(LaborHoursEntry.Text, out var laborHours) ||
            !decimal.TryParse(NumberOfEmployeesEntry.Text, out var numberOfEmployees) ||
            !decimal.TryParse(HourlyRateEntry.Text, out var hourlyRate) ||
            !decimal.TryParse(ExtraCostsEntry.Text, out var extraCosts))
        {
            return null;
        }

        var customCharges = new List<CustomCharge>();
        foreach (var (descriptionEntry, amountEntry) in _customChargeRows)
        {
            var description = descriptionEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(description))
                continue;

            if (!decimal.TryParse(amountEntry.Text, out var amount))
                return null;

            customCharges.Add(new CustomCharge { Description = description, Amount = amount });
        }

        var serviceTypeIndex = ServiceTypePicker.SelectedIndex;
        var serviceTypeName = serviceTypeIndex >= 0 && serviceTypeIndex < _serviceTypes.Count
            ? _serviceTypes[serviceTypeIndex].Name
            : string.Empty;

        return new EstimateData
        {
            EstimateNumber = _currentEstimate?.EstimateNumber ?? $"EST-{DateTime.Now:yyyyMMddHHmmss}",
            CustomerName = string.IsNullOrWhiteSpace(CustomerNameEntry.Text) ? "N/A" : CustomerNameEntry.Text,
            CustomerPhone = CustomerPhoneEntry.Text ?? string.Empty,
            CustomerEmail = CustomerEmailEntry.Text ?? string.Empty,
            CustomerAddress = CustomerAddressEntry.Text ?? string.Empty,
            ServiceTypeName = serviceTypeName,
            Date = DateTime.Now,
            SquareFootage = sqFt,
            PaverPricePerSqFt = paverPrice,
            BaseMaterialCost = baseCost,
            LaborHours = laborHours,
            NumberOfEmployees = numberOfEmployees,
            HourlyLaborRate = hourlyRate,
            ExtraCosts = extraCosts,
            CustomCharges = customCharges
        };
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        var data = BuildEstimateData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields, including any custom charges.", "OK");
            return;
        }

        _currentEstimate = data;

        var summary =
            $"Material Total:  {data.MaterialTotal:C2}\n" +
            $"Labor Total:     {data.LaborTotal:C2}  ({data.TotalLaborHours:N1} total hrs)\n" +
            $"Extra Costs:     {data.ExtraCosts:C2}\n";

        foreach (var charge in data.CustomCharges)
        {
            summary += $"{charge.Description,-16} {charge.Amount:C2}\n";
        }

        summary +=
            $"Subtotal:        {data.Subtotal:C2}\n" +
            $"Margin (20%):    {data.MarginAmount:C2}\n" +
            $"Grand Total:     {data.GrandTotal:C2}\n" +
            $"\n" +
            $"Sales Tax (6%):    {data.SalesTaxAmount:C2}\n" +
            $"Employee Tax (2.7%): {data.EmployeeTaxAmount:C2}\n" +
            $"Total Due:         {data.TotalDue:C2}\n" +
            $"\n" +
            $"Corp. Tax (5.5%, not billed): {data.CorporateTaxAmount:C2}";

        SummaryEditor.Text = summary;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var data = BuildEstimateData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields, including any custom charges.", "OK");
            return;
        }

        _currentEstimate = data;

        EstimateStorage.AddEstimate(new SavedEstimate
        {
            CustomerName = data.CustomerName,
            EstimateNumber = data.EstimateNumber,
            TotalAmount = data.TotalDue,
            IsChangeOrder = false
        });

        await DisplayAlertAsync("Saved", $"Estimate saved under \"{data.CustomerName}\". It can be pulled up later from Change Order.", "OK");
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        var data = BuildEstimateData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields, including any custom charges.", "OK");
            return;
        }

        _currentEstimate = data;

        try
        {
            using var stream = new MemoryStream();
            PdfGenerator.GenerateEstimatePdf(stream, data);

            var fileName = $"Estimate_{data.EstimateNumber}.pdf";
            await PdfExportService.ExportPdfAsync(fileName, stream);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
    }
}
