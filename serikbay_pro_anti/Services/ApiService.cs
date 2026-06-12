using System.Net.Http.Headers;

namespace ControllerApp.Services;

public class ApiService : IApiService
{
	private readonly IStorageService _storageService;
	private readonly HttpClient _httpClient;

	public ApiService(IStorageService storageService)
	{
		_storageService = storageService;
		_httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
	}

	private string BaseUrl => _storageService.GetApiUrl().TrimEnd('/');

	public async Task<bool> CheckHealthAsync()
	{
		try
		{
			var response = await _httpClient.GetAsync($"{BaseUrl}/api/health");
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	public async Task<string?> DownloadLatestExcelAsync()
	{
		try
		{
			var url = $"{BaseUrl}/api/download";
			var response = await _httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode) return null;

			// Extract filename from Content-Disposition if present, else use default
			string fileName = "data_latest.xlsx";
			if (response.Content.Headers.ContentDisposition?.FileName != null)
			{
				fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
			}

			var localPath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);
			using (var stream = await response.Content.ReadAsStreamAsync())
			using (var fileStream = File.Create(localPath))
			{
				await stream.CopyToAsync(fileStream);
			}

			_storageService.SaveLastExcelPath(localPath);
			return localPath;
		}
		catch
		{
			return null;
		}
	}

	public async Task<string?> DownloadExcelByDateAsync(string date)
	{
		try
		{
			// Format: date is e.g. "09.06.2026"
			var url = $"{BaseUrl}/api/download/{date}";
			var response = await _httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode) return null;

			string fileName = $"data_{date}.xlsx";
			if (response.Content.Headers.ContentDisposition?.FileName != null)
			{
				fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
			}

			var localPath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);
			using (var stream = await response.Content.ReadAsStreamAsync())
			using (var fileStream = File.Create(localPath))
			{
				await stream.CopyToAsync(fileStream);
			}

			_storageService.SaveLastExcelPath(localPath);
			return localPath;
		}
		catch
		{
			return null;
		}
	}

	public async Task<bool> UploadExcelAsync(string filePath, string fileName)
	{
		try
		{
			if (!File.Exists(filePath)) return false;

			var url = $"{BaseUrl}/api/upload";
			using var content = new MultipartFormDataContent();
			using var fileStream = File.OpenRead(filePath);
			using var streamContent = new StreamContent(fileStream);
			streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

			content.Add(streamContent, "file", fileName);

			var response = await _httpClient.PostAsync(url, content);
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}
}
