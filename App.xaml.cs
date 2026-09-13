namespace AllAroundEstimates;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	/// <summary>
	/// Last-resort safety net so a fatal crash is diagnosable without a debugger/adb attached.
	/// AppDomain.UnhandledException fires very late (the process is already terminating), so
	/// both the file write and the on-screen alert below are best-effort, not guaranteed.
	/// </summary>
	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var details = (e.ExceptionObject as Exception)?.ToString() ?? e.ExceptionObject?.ToString() ?? "Unknown fatal error";

		try
		{
			var logPath = Path.Combine(FileSystem.AppDataDirectory, "crash_log.txt");
			File.WriteAllText(logPath, $"{DateTime.Now:O}{Environment.NewLine}{details}");
		}
		catch
		{
			// Best-effort only -- if even this fails there's nothing more we can do.
		}

		try
		{
			var page = Application.Current?.Windows.FirstOrDefault()?.Page;
			if (page is null)
				return;

			var message = details.Length > 4000 ? details[..4000] : details;

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await page.DisplayAlertAsync("Fatal Error", message, "OK");
				}
				catch
				{
					// Best-effort only.
				}
			});

			// Give the alert a moment to actually render before the OS finishes tearing the process down.
			Thread.Sleep(8000);
		}
		catch
		{
			// Best-effort only.
		}
	}
}