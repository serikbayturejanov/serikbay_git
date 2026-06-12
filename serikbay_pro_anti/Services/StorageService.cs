using ControllerApp.Models;

namespace ControllerApp.Services;

public class StorageService : IStorageService
{
	private const string KeyControllerNumber = "ControllerNumber";
	private const string KeyControllerName = "ControllerName";
	private const string KeyApiUrl = "ApiUrl";
	private const string KeyLastExcelPath = "LastExcelPath";

	public bool IsRegistered()
	{
		return Preferences.Default.ContainsKey(KeyControllerNumber);
	}

	public ControllerProfile? GetProfile()
	{
		if (!IsRegistered())
			return null;

		return new ControllerProfile
		{
			Number = Preferences.Default.Get<int>(KeyControllerNumber, 0),
			FullName = Preferences.Default.Get<string>(KeyControllerName, string.Empty)
		};
	}

	public void SaveProfile(ControllerProfile profile)
	{
		Preferences.Default.Set<int>(KeyControllerNumber, profile.Number);
		Preferences.Default.Set<string>(KeyControllerName, profile.FullName);
	}

	public void ClearProfile()
	{
		Preferences.Default.Remove(KeyControllerNumber);
		Preferences.Default.Remove(KeyControllerName);
	}

	public string GetApiUrl()
	{
		string defaultUrl = DeviceInfo.Current.Platform == DevicePlatform.Android 
			? "http://10.0.2.2:3000" 
			: "http://localhost:3000";

		return Preferences.Default.Get<string>(KeyApiUrl, defaultUrl);
	}

	public void SaveApiUrl(string url)
	{
		Preferences.Default.Set<string>(KeyApiUrl, url);
	}

	public string GetLastExcelPath()
	{
		return Preferences.Default.Get<string>(KeyLastExcelPath, string.Empty);
	}

	public void SaveLastExcelPath(string path)
	{
		Preferences.Default.Set<string>(KeyLastExcelPath, path);
	}
}
