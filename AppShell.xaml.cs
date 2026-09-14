using AllAroundEstimates.Views;

namespace AllAroundEstimates;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(NewEstimatePage), typeof(NewEstimatePage));
		Routing.RegisterRoute(nameof(ChangeOrderPage), typeof(ChangeOrderPage));
		Routing.RegisterRoute(nameof(TimeCardPage), typeof(TimeCardPage));
		Routing.RegisterRoute(nameof(ServiceTypesPage), typeof(ServiceTypesPage));
		Routing.RegisterRoute(nameof(CustomersPage), typeof(CustomersPage));
		Routing.RegisterRoute(nameof(SavedEstimatesPage), typeof(SavedEstimatesPage));
	}
}
