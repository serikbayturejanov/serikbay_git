using ControllerApp.Models;

namespace ControllerApp.Services;

public interface IStorageService
{
	bool IsRegistered();
	ControllerProfile? GetProfile();
	void SaveProfile(ControllerProfile profile);
	void ClearProfile();

	string GetApiUrl();
	void SaveApiUrl(string url);

	string GetLastExcelPath();
	void SaveLastExcelPath(string path);
}
