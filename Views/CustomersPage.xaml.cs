using AllAroundEstimates.Services;

namespace AllAroundEstimates.Views;

public partial class CustomersPage : ContentPage
{
    private List<string> _allCustomers = new();

    public CustomersPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _allCustomers = EstimateStorage.GetDistinctCustomerNames();
        }
        catch
        {
            _allCustomers = new List<string>();
        }

        SearchEntry.Text = string.Empty;
        CustomersList.ItemsSource = _allCustomers;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.Trim() ?? string.Empty;
        CustomersList.ItemsSource = string.IsNullOrEmpty(searchText)
            ? _allCustomers
            : _allCustomers.Where(n => n.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not string selectedName)
            return;

        CustomersList.SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(SavedEstimatesPage)}?customerName={Uri.EscapeDataString(selectedName)}");
    }
}
