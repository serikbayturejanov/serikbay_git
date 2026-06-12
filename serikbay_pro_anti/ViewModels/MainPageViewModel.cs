using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControllerApp.Services;
using ControllerApp.Views;
using System.Collections.ObjectModel;

namespace ControllerApp.ViewModels;

public partial class MainPageViewModel : BaseViewModel
{
	private readonly IStorageService _storageService;
	private readonly IApiService _apiService;
	private readonly IExcelService _excelService;

	[ObservableProperty]
	private int _controllerNumber;

	[ObservableProperty]
	private string _controllerName = string.Empty;

	[ObservableProperty]
	private DateTime _selectedDate = DateTime.Today;

	[ObservableProperty]
	private ObservableCollection<string> _streets = new();

	[ObservableProperty]
	private string? _selectedStreet;

	[ObservableProperty]
	private string _apiUrl = string.Empty;

	[ObservableProperty]
	private bool _isDataLoaded;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isApiSettingsVisible;

	public MainPageViewModel(
		IStorageService storageService,
		IApiService apiService,
		IExcelService excelService)
	{
		_storageService = storageService;
		_apiService = apiService;
		_excelService = excelService;
		Title = "Басты бет";
	}

	[RelayCommand]
	public void Initialize()
	{
		var profile = _storageService.GetProfile();
		if (profile != null)
		{
			ControllerNumber = profile.Number;
			ControllerName = profile.FullName;
		}
		ApiUrl = _storageService.GetApiUrl();
		StatusMessage = "Серверден мәліметтерді жүктеу үшін 'Жаңарту' түймесін басыңыз.";
	}

	[RelayCommand]
	private void ToggleApiSettings()
	{
		IsApiSettingsVisible = !IsApiSettingsVisible;
		if (!IsApiSettingsVisible)
		{
			_storageService.SaveApiUrl(ApiUrl);
		}
	}

	[RelayCommand]
	private async Task SyncDataAsync()
	{
		IsBusy = true;
		StatusMessage = "Серверге қосылуда...";
		IsDataLoaded = false;
		Streets.Clear();
		SelectedStreet = null;

		try
		{
			_storageService.SaveApiUrl(ApiUrl); // Save URL before connecting

			bool isHealthy = await _apiService.CheckHealthAsync();
			if (!isHealthy)
			{
				StatusMessage = "Серверге қосылу мүмкін емес! API адресін тексеріңіз.";
				await Shell.Current.DisplayAlert("Қате", "Сервермен байланыс орнатылмады. Сервердің қосылып тұрғанын немесе адресін тексеріңіз.", "OK");
				return;
			}

			string dateStr = SelectedDate.ToString("dd.MM.yyyy");
			StatusMessage = $"{dateStr} күнгі файл жүктелуде...";

			// Try to download data for the selected date
			string? localPath = await _apiService.DownloadExcelByDateAsync(dateStr);

			if (string.IsNullOrEmpty(localPath))
			{
				StatusMessage = $"Таңдалған күнге ({dateStr}) сәйкес файл табылмады. Ең соңғы файл жүктелуде...";
				localPath = await _apiService.DownloadLatestExcelAsync();
			}

			if (string.IsNullOrEmpty(localPath))
			{
				StatusMessage = "Серверден Excel файл жүктелмеді.";
				await Shell.Current.DisplayAlert("Қате", "Серверден деректерді жүктеу сәтсіз аяқталды.", "OK");
				return;
			}

			StatusMessage = "Файлды өңдеу және көшелерді анықтау...";
			var (excelDate, loadedSubscribers) = await _excelService.ReadExcelAsync(localPath);

			// Extract unique streets for the current controller
			var uniqueStreets = loadedSubscribers
				.Where(s => s.Controller == ControllerNumber)
				.Select(s => s.Street)
				.Where(st => !string.IsNullOrWhiteSpace(st))
				.Distinct()
				.OrderBy(st => st)
				.ToList();

			if (!uniqueStreets.Any())
			{
				StatusMessage = $"Табылған файлда ({excelDate}) Сіздің контроллер нөміріңізге ({ControllerNumber}) сәйкес деректер жоқ.";
				await Shell.Current.DisplayAlert("Ескерту", $"Тіркелген контроллер нөмірі ({ControllerNumber}) бойынша бұл файлда абоненттер табылмады.", "OK");
				return;
			}

			foreach (var street in uniqueStreets)
			{
				Streets.Add(street);
			}

			IsDataLoaded = true;
			StatusMessage = $"Деректер сәтті жүктелді! Көшені таңдаңыз. (Файл күні: {excelDate})";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Қате: {ex.Message}";
			await Shell.Current.DisplayAlert("Қате", $"Мәлімет өңдеуде қате шықты: {ex.Message}", "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task ProceedAsync()
	{
		if (string.IsNullOrEmpty(SelectedStreet))
		{
			await Shell.Current.DisplayAlert("Ескерту", "Көшені таңдаңыз!", "OK");
			return;
		}

		string dateStr = SelectedDate.ToString("dd.MM.yyyy");

		// Navigate to SubscriberListPage and pass parameters
		await Shell.Current.GoToAsync($"{nameof(SubscriberListPage)}?Date={dateStr}&Street={Uri.EscapeDataString(SelectedStreet)}");
	}

	[RelayCommand]
	private async Task LogoutAsync()
	{
		bool confirm = await Shell.Current.DisplayAlert("Шығу", "Тіркелген деректерді өшіріп, профильді қайта тіркегіңіз келе ме?", "Иә", "Жоқ");
		if (confirm)
		{
			_storageService.ClearProfile();
			await Shell.Current.GoToAsync("//RegistrationPage");
		}
	}
}
