namespace AllAroundEstimates.Views;

public partial class MainMenuPage : ContentPage
{
    public MainMenuPage()
    {
        InitializeComponent();
    }

    private async void OnNewEstimateClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NewEstimatePage));
    }

    private async void OnChangeOrderClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ChangeOrderPage));
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
