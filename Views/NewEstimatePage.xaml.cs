using AllAroundEstimates.Models;
using AllAroundEstimates.Services;

namespace AllAroundEstimates.Views;

[QueryProperty(nameof(EstimateNumberToLoad), "estimateNumber")]
public partial class NewEstimatePage : ContentPage
{
    private readonly List<(Picker Type, Entry SquareFootage, Entry Price)> _serviceLineRows = new();
    private readonly List<(Entry Description, Entry Amount)> _customChargeRows = new();
    private List<ServiceType> _serviceTypes = new();
    private EstimateData? _currentEstimate;

    public string? EstimateNumberToLoad { get; set; }

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

        // Refresh every existing row's type list (in case types were added/renamed via Manage
        // Types), preserving each row's current selection by name where it still exists.
        var typeNames = _serviceTypes.Select(t => t.Name).ToList();
        foreach (var (typePicker, _, _) in _serviceLineRows)
        {
            var previouslySelectedName = typePicker.SelectedItem as string;
            typePicker.ItemsSource = typeNames;
            if (previouslySelectedName is not null)
            {
                typePicker.SelectedIndex = typeNames.IndexOf(previouslySelectedName);
            }
        }

        if (!string.IsNullOrWhiteSpace(EstimateNumberToLoad))
        {
            var numberToLoad = EstimateNumberToLoad;
            EstimateNumberToLoad = null; // consume it so navigating back here later doesn't re-load and wipe edits
            LoadEstimateByNumber(numberToLoad);
        }
        else if (_serviceLineRows.Count == 0)
        {
            // Start every fresh estimate with one blank line so the form isn't empty/confusing.
            AddServiceLineRow();
        }
    }

    private async void LoadEstimateByNumber(string estimateNumber)
    {
        SavedEstimate? saved;
        try
        {
            saved = EstimateStorage.LoadEstimates()
                .FirstOrDefault(e => !e.IsChangeOrder && e.EstimateNumber == estimateNumber);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not load that estimate: {ex.Message}", "OK");
            return;
        }

        if (saved?.EstimateDetail is null)
        {
            await DisplayAlertAsync("Not Found", "That estimate's details could not be found.", "OK");
            return;
        }

        PopulateForm(saved.EstimateDetail);
    }

    private void PopulateForm(EstimateData data)
    {
        _currentEstimate = data;

        CustomerNameEntry.Text = data.CustomerName;
        CustomerPhoneEntry.Text = data.CustomerPhone;
        CustomerEmailEntry.Text = data.CustomerEmail;
        CustomerAddressEntry.Text = data.CustomerAddress;

        ServiceLinesContainer.Children.Clear();
        _serviceLineRows.Clear();
        foreach (var item in data.ServiceLineItems)
        {
            AddServiceLineRow(item.ServiceTypeName, item.SquareFootage, item.PricePerSqFt);
        }
        if (_serviceLineRows.Count == 0)
        {
            AddServiceLineRow();
        }

        BaseMaterialCostEntry.Text = data.BaseMaterialCost.ToString("0.##");
        LaborHoursEntry.Text = data.LaborHours.ToString("0.##");
        NumberOfEmployeesEntry.Text = data.NumberOfEmployees.ToString("0.##");
        HourlyRateEntry.Text = data.HourlyLaborRate.ToString("0.##");
        ExtraCostsEntry.Text = data.ExtraCosts.ToString("0.##");

        CustomChargesContainer.Children.Clear();
        _customChargeRows.Clear();
        foreach (var charge in data.CustomCharges)
        {
            AddCustomChargeRow(charge.Description, charge.Amount);
        }

        SummaryEditor.Text = string.Empty;
    }

    private async void OnManageTypesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ServiceTypesPage));
    }

    private void OnAddServiceLineClicked(object sender, EventArgs e) => AddServiceLineRow();

    private void AddServiceLineRow(string? serviceTypeName = null, decimal? squareFootage = null, decimal? pricePerSqFt = null)
    {
        var initialIndex = string.IsNullOrEmpty(serviceTypeName)
            ? -1
            : _serviceTypes.FindIndex(t => t.Name == serviceTypeName);

        var typePicker = new Picker
        {
            Title = "Service type",
            ItemsSource = _serviceTypes.Select(t => t.Name).ToList(),
            SelectedIndex = initialIndex,
            WidthRequest = 150
        };

        var sqFtEntry = new Entry { Placeholder = "Sq Ft", Keyboard = Keyboard.Numeric, WidthRequest = 75, Text = squareFootage?.ToString("0.##") ?? string.Empty };
        var priceEntry = new Entry { Placeholder = "$/sq ft", Keyboard = Keyboard.Numeric, WidthRequest = 85, Text = pricePerSqFt?.ToString("0.##") ?? string.Empty };

        typePicker.SelectedIndexChanged += (_, _) =>
        {
            var idx = typePicker.SelectedIndex;
            if (idx >= 0 && idx < _serviceTypes.Count)
            {
                priceEntry.Text = _serviceTypes[idx].PricePerSqFt.ToString("0.##");
            }
        };

        var calcButton = new Button
        {
            Text = "Calc",
            FontSize = 12,
            Padding = new Thickness(8, 4)
        };
        calcButton.Clicked += async (_, _) =>
        {
            await Navigation.PushModalAsync(new SquareFootageCalculatorPage(total =>
            {
                sqFtEntry.Text = total.ToString("0.##");
            }));
        };

        var removeButton = new Button
        {
            Text = "✕",
            FontSize = 12,
            Padding = new Thickness(8, 4),
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Red
        };

        var row = new HorizontalStackLayout { Spacing = 8 };
        row.Add(typePicker);
        row.Add(sqFtEntry);
        row.Add(calcButton);
        row.Add(priceEntry);
        row.Add(removeButton);

        removeButton.Clicked += (_, _) =>
        {
            ServiceLinesContainer.Children.Remove(row);
            _serviceLineRows.RemoveAll(r => r.Type == typePicker);
        };

        ServiceLinesContainer.Children.Add(row);
        _serviceLineRows.Add((typePicker, sqFtEntry, priceEntry));
    }

    private void OnAddCustomChargeClicked(object sender, EventArgs e) => AddCustomChargeRow();

    private void AddCustomChargeRow(string description = "", decimal? amount = null)
    {
        var descriptionEntry = new Entry { Placeholder = "Description", HorizontalOptions = LayoutOptions.Fill, Text = description };
        var amountEntry = new Entry { Placeholder = "0.00", Keyboard = Keyboard.Numeric, WidthRequest = 90, Text = amount?.ToString("0.##") ?? string.Empty };

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
        var serviceLineItems = new List<ServiceLineItem>();
        foreach (var (typePicker, sqFtEntry, priceEntry) in _serviceLineRows)
        {
            var sqFtText = sqFtEntry.Text?.Trim() ?? string.Empty;
            var priceText = priceEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(sqFtText) && string.IsNullOrEmpty(priceText))
                continue; // fully blank row, skip it

            if (!decimal.TryParse(sqFtText, out var sqFt) || !decimal.TryParse(priceText, out var price))
                return null;

            var typeName = typePicker.SelectedIndex >= 0 && typePicker.SelectedIndex < _serviceTypes.Count
                ? _serviceTypes[typePicker.SelectedIndex].Name
                : string.Empty;

            serviceLineItems.Add(new ServiceLineItem { ServiceTypeName = typeName, SquareFootage = sqFt, PricePerSqFt = price });
        }

        if (!decimal.TryParse(BaseMaterialCostEntry.Text, out var baseCost) ||
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

        return new EstimateData
        {
            EstimateNumber = _currentEstimate?.EstimateNumber ?? $"EST-{DateTime.Now:yyyyMMddHHmmss}",
            CustomerName = string.IsNullOrWhiteSpace(CustomerNameEntry.Text) ? "N/A" : CustomerNameEntry.Text,
            CustomerPhone = CustomerPhoneEntry.Text ?? string.Empty,
            CustomerEmail = CustomerEmailEntry.Text ?? string.Empty,
            CustomerAddress = CustomerAddressEntry.Text ?? string.Empty,
            Date = DateTime.Now,
            ServiceLineItems = serviceLineItems,
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
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields, including service lines and any custom charges.", "OK");
            return;
        }

        _currentEstimate = data;

        var summary = string.Empty;
        foreach (var item in data.ServiceLineItems)
        {
            var label = string.IsNullOrEmpty(item.ServiceTypeName) ? "Material" : item.ServiceTypeName;
            summary += $"{label,-14} {item.SquareFootage:N0} sqft x {item.PricePerSqFt:C2} = {item.Total:C2}\n";
        }

        summary +=
            $"Base Material:   {data.BaseMaterialCost:C2}\n" +
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

    private static void SaveEstimateRecord(EstimateData data)
    {
        EstimateStorage.SaveOrUpdateEstimate(new SavedEstimate
        {
            CustomerName = data.CustomerName,
            EstimateNumber = data.EstimateNumber,
            TotalAmount = data.TotalDue,
            IsChangeOrder = false,
            EstimateDetail = data
        });
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var data = BuildEstimateData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields, including service lines and any custom charges.", "OK");
            return;
        }

        _currentEstimate = data;
        SaveEstimateRecord(data);

        await DisplayAlertAsync("Saved", $"Estimate saved under \"{data.CustomerName}\". It can be pulled up later from Customers or Load Saved Estimates.", "OK");
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        var data = BuildEstimateData();
        if (data is null)
        {
            await DisplayAlertAsync("Invalid Input", "Please enter valid numbers in all fields, including service lines and any custom charges.", "OK");
            return;
        }

        _currentEstimate = data;

        try
        {
            using var stream = new MemoryStream();
            PdfGenerator.GenerateEstimatePdf(stream, data);

            // Auto-save on export, per owner request -- happens even if the OS save dialog below
            // is subsequently cancelled, since the estimate itself was still finalized enough to
            // export.
            SaveEstimateRecord(data);

            var fileName = $"Estimate_{data.EstimateNumber}.pdf";
            await PdfExportService.ExportPdfAsync(fileName, stream);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Failed", ex.Message, "OK");
        }
    }
}
