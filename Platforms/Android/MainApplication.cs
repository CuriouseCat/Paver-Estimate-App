using Android.App;
using Android.Runtime;
using Android.Util;

namespace AllAroundEstimates;

[Application]
public class MainApplication : MauiApplication
{
	private const string LogTag = "AllAroundEstimates";

	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
		{
			Log.Error(LogTag, "UNHANDLED (Android/.NET): " + args.Exception);
			args.Handled = false;
		};

		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			Log.Error(LogTag, "UNHANDLED (AppDomain): " + args.ExceptionObject);
		};

		TaskScheduler.UnobservedTaskException += (sender, args) =>
		{
			Log.Error(LogTag, "UNOBSERVED TASK EXCEPTION: " + args.Exception);
			args.SetObserved();
		};
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
