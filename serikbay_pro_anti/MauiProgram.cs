using Microsoft.Extensions.Logging;
using ControllerApp.Services;
using ControllerApp.ViewModels;
using ControllerApp.Views;

namespace ControllerApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Services
		builder.Services.AddSingleton<IStorageService, StorageService>();
		builder.Services.AddSingleton<IApiService, ApiService>();
		builder.Services.AddSingleton<IExcelService, ExcelService>();

		// Register ViewModels
		builder.Services.AddTransient<RegistrationViewModel>();
		builder.Services.AddTransient<MainPageViewModel>();
		builder.Services.AddTransient<SubscriberListViewModel>();

		// Register Views
		builder.Services.AddTransient<RegistrationPage>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<SubscriberListPage>();

		return builder.Build();
	}
}
