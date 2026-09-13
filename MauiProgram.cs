using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
#if !ANDROID
using QuestPDF.Infrastructure;
#endif

namespace AllAroundEstimates;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
#if !ANDROID
		// QuestPDF isn't referenced on Android at all (see the csproj) -- it ships no native
		// Android runtime, and this line was the earliest point in the app's startup where the
		// QuestPDF assembly got force-loaded on every launch.
		QuestPDF.Settings.License = LicenseType.Community;
#endif

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
