namespace ControllerApp.Services;

public interface IApiService
{
	Task<bool> CheckHealthAsync();
	Task<string?> DownloadLatestExcelAsync();
	Task<string?> DownloadExcelByDateAsync(string date);
	Task<bool> UploadExcelAsync(string filePath, string fileName);
}
