namespace AllAroundEstimates.Views;

public partial class MainMenuPage : ContentPage
{
    public MainMenuPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await SeedDefaultLogoAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Startup Warning", $"Could not prepare the default logo: {ex.Message}", "OK");
        }
    }

    private static async Task SeedDefaultLogoAsync()
    {
        var logoPath = Path.Combine(FileSystem.AppDataDirectory, "company_logo.png");
        if (File.Exists(logoPath))
            return;

        using var sourceStream = await FileSystem.OpenAppPackageFileAsync("company_logo_default.png");
        using var destinationStream = File.Create(logoPath);
        await sourceStream.CopyToAsync(destinationStream);
    }

    private async void OnNewEstimateClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NewEstimatePage));
    }

    private async void OnChangeOrderClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ChangeOrderPage));
    }

    private async void OnTimeCardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TimeCardPage));
    }

    private async void OnCustomersClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CustomersPage));
    }

    private async void OnLoadEstimatesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SavedEstimatesPage));
    }

    private async void OnUploadLogoClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a company logo",
                FileTypes = FilePickerFileType.Images
            });

            if (result is null)
                return;

            var logoPath = Path.Combine(FileSystem.AppDataDirectory, "company_logo.png");

            using var sourceStream = await result.OpenReadAsync();
            using var destinationStream = File.Create(logoPath);
            await sourceStream.CopyToAsync(destinationStream);

            await DisplayAlertAsync("Logo Saved", "Your company logo has been saved and will appear on future PDF exports.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Upload Failed", ex.Message, "OK");
        }
    }
}
