using ControllerApp.Models;

namespace ControllerApp.Services;

public interface IExcelService
{
	Task<(string Date, List<Subscriber> Subscribers)> ReadExcelAsync(string filePath);
	Task<bool> WriteExcelAsync(string filePath, string date, List<Subscriber> subscribers);
}
