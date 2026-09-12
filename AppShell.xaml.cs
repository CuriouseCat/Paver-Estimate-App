using AllAroundEstimates.Views;

namespace AllAroundEstimates;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(NewEstimatePage), typeof(NewEstimatePage));
		Routing.RegisterRoute(nameof(ChangeOrderPage), typeof(ChangeOrderPage));
	}
}
