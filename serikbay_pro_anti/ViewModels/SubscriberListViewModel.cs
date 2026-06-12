using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControllerApp.Models;
using ControllerApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ControllerApp.ViewModels;

[QueryProperty(nameof(Date), "Date")]
[QueryProperty(nameof(Street), "Street")]
public partial class SubscriberListViewModel : BaseViewModel
{
	private readonly IStorageService _storageService;
	private readonly IExcelService _excelService;
	private readonly IApiService _apiService;

	private string _date = string.Empty;
	private string _street = string.Empty;

	public string Date
	{
		get => _date;
		set
		{
			_date = Uri.UnescapeDataString(value);
			OnPropertyChanged();
			LoadDataCommand.Execute(null);
		}
	}

	public string Street
	{
		get => _street;
		set
		{
			_street = Uri.UnescapeDataString(value);
			OnPropertyChanged();
			LoadDataCommand.Execute(null);
		}
	}

	[ObservableProperty]
	private string _searchSubscriberId = string.Empty;

	[ObservableProperty]
	private string _searchFullName = string.Empty;

	[ObservableProperty]
	private string _searchAddress = string.Empty;

	[ObservableProperty]
	private ObservableCollection<Subscriber> _displayedSubscribers = new();

	private List<Subscriber> _allLoadedSubscribers = new(); // Entire Excel content
	private List<Subscriber> _filteredSubscribers = new();  // Current controller + street filtered

	private Dictionary<string, double> _lastValidEndValues = new();
	private string _currentSortColumn = string.Empty;
	private bool _isSortAscending = true;
	private int _controllerNumber;

	public SubscriberListViewModel(
		IStorageService storageService,
		IExcelService excelService,
		IApiService apiService)
	{
		_storageService = storageService;
		_excelService = excelService;
		_apiService = apiService;
		Title = "Абоненттер кестесі";

		var profile = _storageService.GetProfile();
		_controllerNumber = profile?.Number ?? 0;
	}

	partial void OnSearchSubscriberIdChanged(string value) => ApplyFilterAndSort();
	partial void OnSearchFullNameChanged(string value) => ApplyFilterAndSort();
	partial void OnSearchAddressChanged(string value) => ApplyFilterAndSort();

	[RelayCommand]
	private async Task LoadDataAsync()
	{
		if (string.IsNullOrEmpty(Date) || string.IsNullOrEmpty(Street))
			return;

		IsBusy = true;
		try
		{
			string localPath = _storageService.GetLastExcelPath();
			if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
			{
				await Shell.Current.DisplayAlert("Қате", "Жергілікті деректер файлы табылмады. Басты беттен деректерді қайта жүктеңіз.", "OK");
				await Shell.Current.GoToAsync("..");
				return;
			}

			var (_, allSubs) = await _excelService.ReadExcelAsync(localPath);
			_allLoadedSubscribers = allSubs;

			// Filter to active controller and street
			_filteredSubscribers = _allLoadedSubscribers
				.Where(s => s.Controller == _controllerNumber && s.Street.Equals(Street, StringComparison.OrdinalIgnoreCase))
				.ToList();

			_lastValidEndValues.Clear();
			foreach (var sub in _filteredSubscribers)
			{
				_lastValidEndValues[sub.SubscriberId] = sub.EndValue;
				sub.PropertyChanged += OnSubscriberPropertyChanged;
			}

			ApplyFilterAndSort();
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Қате", $"Деректерді жүктеуде қате: {ex.Message}", "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void OnSubscriberPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(Subscriber.EndValue))
		{
			if (sender is Subscriber sub)
			{
				// Perform validation
				if (sub.EndValue < sub.StartValue)
				{
					// Revert change
					sub.PropertyChanged -= OnSubscriberPropertyChanged;
					sub.EndValue = _lastValidEndValues.ContainsKey(sub.SubscriberId) ? _lastValidEndValues[sub.SubscriberId] : sub.StartValue;
					sub.Difference = sub.EndValue - sub.StartValue;
					sub.PropertyChanged += OnSubscriberPropertyChanged;

					MainThread.BeginInvokeOnMainThread(async () =>
					{
						await Shell.Current.DisplayAlert("Ескерту", "Енгізілген көрсеткіш бастапқы көрсеткіштен аз бола алмайды!", "Түсінікті");
					});
				}
				else
				{
					// Valid value
					sub.PropertyChanged -= OnSubscriberPropertyChanged;
					sub.Difference = sub.EndValue - sub.StartValue;
					_lastValidEndValues[sub.SubscriberId] = sub.EndValue;
					sub.PropertyChanged += OnSubscriberPropertyChanged;

					// Auto-save: Update the primary in-memory storage of all rows
					var match = _allLoadedSubscribers.FirstOrDefault(s => s.SubscriberId == sub.SubscriberId);
					if (match != null)
					{
						match.EndValue = sub.EndValue;
						match.Difference = sub.Difference;
					}

					// Write changes back to the cached excel file asynchronously
					SaveToCacheFile();
				}
			}
		}
	}

	private async void SaveToCacheFile()
	{
		try
		{
			string localPath = _storageService.GetLastExcelPath();
			if (!string.IsNullOrEmpty(localPath))
			{
				await _excelService.WriteExcelAsync(localPath, Date, _allLoadedSubscribers);
			}
		}
		catch
		{
			// Fail silently for background auto-save.
		}
	}

	private void ApplyFilterAndSort()
	{
		var query = _filteredSubscribers.AsEnumerable();

		// Apply search filters
		if (!string.IsNullOrWhiteSpace(SearchSubscriberId))
		{
			query = query.Where(s => s.SubscriberId.Contains(SearchSubscriberId.Trim(), StringComparison.OrdinalIgnoreCase));
		}
		if (!string.IsNullOrWhiteSpace(SearchFullName))
		{
			query = query.Where(s => s.FullName.Contains(SearchFullName.Trim(), StringComparison.OrdinalIgnoreCase));
		}
		if (!string.IsNullOrWhiteSpace(SearchAddress))
		{
			query = query.Where(s => s.Address.Contains(SearchAddress.Trim(), StringComparison.OrdinalIgnoreCase));
		}

		// Apply sorting
		if (!string.IsNullOrEmpty(_currentSortColumn))
		{
			query = _currentSortColumn switch
			{
				nameof(Subscriber.SubscriberId) => _isSortAscending ? query.OrderBy(s => s.SubscriberId) : query.OrderByDescending(s => s.SubscriberId),
				nameof(Subscriber.FullName) => _isSortAscending ? query.OrderBy(s => s.FullName) : query.OrderByDescending(s => s.FullName),
				nameof(Subscriber.Address) => _isSortAscending ? query.OrderBy(s => s.Address) : query.OrderByDescending(s => s.Address),
				nameof(Subscriber.StartValue) => _isSortAscending ? query.OrderBy(s => s.StartValue) : query.OrderByDescending(s => s.StartValue),
				nameof(Subscriber.EndValue) => _isSortAscending ? query.OrderBy(s => s.EndValue) : query.OrderByDescending(s => s.EndValue),
				nameof(Subscriber.Difference) => _isSortAscending ? query.OrderBy(s => s.Difference) : query.OrderByDescending(s => s.Difference),
				_ => query
			};
		}

		// Update UI Collection
		DisplayedSubscribers.Clear();
		foreach (var sub in query)
		{
			DisplayedSubscribers.Add(sub);
		}
	}

	[RelayCommand]
	private void Sort(string column)
	{
		if (_currentSortColumn == column)
		{
			_isSortAscending = !_isSortAscending;
		}
		else
		{
			_currentSortColumn = column;
			_isSortAscending = true;
		}

		ApplyFilterAndSort();
	}

	[RelayCommand]
	private async Task UploadDataAsync()
	{
		if (!_allLoadedSubscribers.Any())
		{
			await Shell.Current.DisplayAlert("Қате", "Жіберетін мәлімет жоқ!", "OK");
			return;
		}

		IsBusy = true;
		try
		{
			// Format export file name: data_dd.MM.yyyy_controller_{number}_completed.xlsx
			string fileName = $"data_{Date}_controller_{_controllerNumber}_completed.xlsx";
			string exportPath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);

			// Write entire list back to new completed file
			bool writeSuccess = await _excelService.WriteExcelAsync(exportPath, Date, _allLoadedSubscribers);
			if (!writeSuccess)
			{
				await Shell.Current.DisplayAlert("Қате", "Excel файлды жасау сәтсіз аяқталды.", "OK");
				return;
			}

			// Upload file to server
			bool uploadSuccess = await _apiService.UploadExcelAsync(exportPath, fileName);
			if (uploadSuccess)
			{
				await Shell.Current.DisplayAlert("Табысты", $"Мәліметтер сәтті жіберілді!\nФайл аты: {fileName}", "Жақсы");
				await Shell.Current.GoToAsync(".."); // Go back to MainPage
			}
			else
			{
				await Shell.Current.DisplayAlert("Қате", "Серверге файлды жүктеу сәтсіз аяқталды. Сервер байланысын тексеріңіз.", "OK");
			}
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Қате", $"Жіберу барысында қате орын алды: {ex.Message}", "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}
}
