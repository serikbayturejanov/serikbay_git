using ControllerApp.ViewModels;

namespace ControllerApp.Views;

public partial class SubscriberListPage : ContentPage
{
	public SubscriberListPage(SubscriberListViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
