using ControllerApp.Views;

namespace ControllerApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for pages that we navigate to programmatically
		Routing.RegisterRoute(nameof(SubscriberListPage), typeof(SubscriberListPage));

		// Check registration status on startup
		CheckRegistrationStatus();
	}

	private async void CheckRegistrationStatus()
	{
		// Delay slightly to let the application render first
		await Task.Yield();

		bool isRegistered = Preferences.Default.ContainsKey("ControllerNumber");
		if (isRegistered)
		{
			await GoToAsync("//MainPage");
		}
		else
		{
			await GoToAsync("//RegistrationPage");
		}
	}
}
