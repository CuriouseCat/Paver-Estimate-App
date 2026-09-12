using Microsoft.Extensions.DependencyInjection;

namespace AllAroundEstimates;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		_ = SeedDefaultLogoAsync();
		return new Window(new AppShell());
	}

	private static async Task SeedDefaultLogoAsync()
	{
		try
		{
			var logoPath = Path.Combine(FileSystem.AppDataDirectory, "company_logo.png");
			if (File.Exists(logoPath))
				return;

			using var sourceStream = await FileSystem.OpenAppPackageFileAsync("company_logo_default.png");
			using var destinationStream = File.Create(logoPath);
			await sourceStream.CopyToAsync(destinationStream);
		}
		catch
		{
			// Best-effort seeding only; PdfGenerator already falls back to a placeholder box if this never runs.
		}
	}
}