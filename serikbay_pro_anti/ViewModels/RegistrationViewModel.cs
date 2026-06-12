using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControllerApp.Models;
using ControllerApp.Services;

namespace ControllerApp.ViewModels;

public partial class RegistrationViewModel : BaseViewModel
{
	private readonly IStorageService _storageService;

	[ObservableProperty]
	private string _controllerNumberStr = string.Empty;

	[ObservableProperty]
	private string _fullName = string.Empty;

	[ObservableProperty]
	private string _errorMessage = string.Empty;

	public RegistrationViewModel(IStorageService storageService)
	{
		_storageService = storageService;
		Title = "Тіркелу";
	}

	[RelayCommand]
	private async Task RegisterAsync()
	{
		ErrorMessage = string.Empty;

		if (!int.TryParse(ControllerNumberStr, out int number) || number <= 0)
		{
			ErrorMessage = "Контроллер нөмірі дұрыс емес!";
			return;
		}

		if (string.IsNullOrWhiteSpace(FullName))
		{
			ErrorMessage = "Аты-жөніңізді толық енгізіңіз!";
			return;
		}

		IsBusy = true;

		try
		{
			var profile = new ControllerProfile
			{
				Number = number,
				FullName = FullName.Trim()
			};

			_storageService.SaveProfile(profile);

			// Navigate to MainPage
			await Shell.Current.GoToAsync("//MainPage");
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Қате орын алды: {ex.Message}";
		}
		finally
		{
			IsBusy = false;
		}
	}
}
