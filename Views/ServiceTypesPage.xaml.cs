using AllAroundEstimates.Models;
using AllAroundEstimates.Services;

namespace AllAroundEstimates.Views;

public partial class ServiceTypesPage : ContentPage
{
    private readonly List<(Entry Name, Entry Price, Entry HourlyRate)> _rows = new();

    public ServiceTypesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        TypesContainer.Children.Clear();
        _rows.Clear();

        List<ServiceType> serviceTypes;
        try
        {
            serviceTypes = ServiceTypeStorage.LoadServiceTypes();
        }
        catch
        {
            serviceTypes = new List<ServiceType>();
        }

        foreach (var serviceType in serviceTypes)
        {
            AddRow(serviceType.Name, serviceType.PricePerSqFt, serviceType.HourlyLaborRate);
        }
    }

    private void OnAddTypeClicked(object sender, EventArgs e)
    {
        AddRow(string.Empty, 0, 0);
    }

    private void AddRow(string name, decimal pricePerSqFt, decimal hourlyRate)
    {
        var nameEntry = new Entry { Placeholder = "Type name (e.g. Installation)", HorizontalOptions = LayoutOptions.Fill, Text = name };
        var priceEntry = new Entry { Placeholder = "$/sqft", Keyboard = Keyboard.Numeric, WidthRequest = 100, Text = pricePerSqFt.ToString("0.##") };
        var hourlyRateEntry = new Entry { Placeholder = "Hourly $", Keyboard = Keyboard.Numeric, WidthRequest = 90, Text = hourlyRate.ToString("0.##") };

        var removeButton = new Button
        {
            Text = "✕",
            FontSize = 12,
            Padding = new Thickness(8, 4),
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Red
        };

        var nameRow = new VerticalStackLayout { Spacing = 4 };
        nameRow.Add(nameEntry);

        var ratesRow = new HorizontalStackLayout { Spacing = 8 };
        ratesRow.Add(new Label { Text = "$/sqft:", VerticalOptions = LayoutOptions.Center, FontSize = 12 });
        ratesRow.Add(priceEntry);
        ratesRow.Add(new Label { Text = "Hourly $:", VerticalOptions = LayoutOptions.Center, FontSize = 12 });
        ratesRow.Add(hourlyRateEntry);
        ratesRow.Add(removeButton);

        var card = new Border
        {
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            Padding = new Thickness(10, 8)
        };
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Add(nameRow);
        stack.Add(ratesRow);
        card.Content = stack;

        removeButton.Clicked += (_, _) =>
        {
            TypesContainer.Children.Remove(card);
            _rows.RemoveAll(r => r.Name == nameEntry);
        };

        TypesContainer.Children.Add(card);
        _rows.Add((nameEntry, priceEntry, hourlyRateEntry));
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var serviceTypes = new List<ServiceType>();

        foreach (var (nameEntry, priceEntry, hourlyRateEntry) in _rows)
        {
            var name = nameEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (!decimal.TryParse(priceEntry.Text, out var price) ||
                !decimal.TryParse(hourlyRateEntry.Text, out var hourlyRate))
            {
                await DisplayAlertAsync("Invalid Input", $"Please enter valid numbers for \"{name}\".", "OK");
                return;
            }

            serviceTypes.Add(new ServiceType
            {
                Name = name,
                PricePerSqFt = price,
                HourlyLaborRate = hourlyRate
            });
        }

        try
        {
            ServiceTypeStorage.SaveServiceTypes(serviceTypes);
            await DisplayAlertAsync("Saved", "Service types saved.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}
