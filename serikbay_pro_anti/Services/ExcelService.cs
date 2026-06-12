using ControllerApp.Models;
using ExcelDataReader;
using MiniExcelLibs;
using System.Text;

namespace ControllerApp.Services;

public class ExcelService : IExcelService
{
	public ExcelService()
	{
		// Critical for ExcelDataReader on mobile devices (Android/iOS)
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}

	public async Task<(string Date, List<Subscriber> Subscribers)> ReadExcelAsync(string filePath)
	{
		return await Task.Run(() =>
		{
			string dateStr = "09.06.2026"; // Fallback default
			var subscribers = new List<Subscriber>();

			if (!File.Exists(filePath))
				return (dateStr, subscribers);

			using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var reader = ExcelReaderFactory.CreateReader(stream);

			bool isHeaderFound = false;

			while (reader.Read())
			{
				if (reader.FieldCount < 2) continue;

				var firstCell = reader.GetValue(0)?.ToString()?.Trim();

				// 1. Read date header
				if (!isHeaderFound && firstCell == "дата")
				{
					dateStr = reader.GetValue(1)?.ToString()?.Trim() ?? dateStr;
					continue;
				}

				// 2. Detect main table header row
				if (firstCell == "контроллер")
				{
					isHeaderFound = true;
					continue;
				}

				// 3. Read subscriber records
				if (isHeaderFound)
				{
					if (string.IsNullOrWhiteSpace(firstCell))
						continue; // skip empty or tail rows

					try
					{
						int controller = Convert.ToInt32(reader.GetValue(0));
						string subscriberId = reader.GetValue(1)?.ToString() ?? string.Empty;
						string fullName = reader.GetValue(2)?.ToString() ?? string.Empty;
						string address = reader.GetValue(3)?.ToString() ?? string.Empty;
						double startValue = Convert.ToDouble(reader.GetValue(4) ?? 0.0);
						double endValue = Convert.ToDouble(reader.GetValue(5) ?? 0.0);
						double difference = Convert.ToDouble(reader.GetValue(6) ?? 0.0);

						subscribers.Add(new Subscriber
						{
							Controller = controller,
							SubscriberId = subscriberId,
							FullName = fullName,
							Address = address,
							StartValue = startValue,
							EndValue = endValue,
							Difference = difference
						});
					}
					catch
					{
						// Skip row if parsing fails to keep it robust
					}
				}
			}

			return (dateStr, subscribers);
		});
	}

	public async Task<bool> WriteExcelAsync(string filePath, string date, List<Subscriber> subscribers)
	{
		return await Task.Run(() =>
		{
			try
			{
				// Ensure folder exists
				var dir = Path.GetDirectoryName(filePath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}

				// Delete existing if any
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				// Create array structure matching the required excel layout
				var rows = new List<object[]>();

				// Row 1: date
				rows.Add(new object[] { "дата", date });

				// Row 2: empty line
				rows.Add(new object[] { "", "" });

				// Row 3: headers
				rows.Add(new object[] { "контроллер", "абонент", "Фио", "адрес", "нач_знач", "кон_знач", "разница" });

				// Row 4 onwards: subscriber data
				foreach (var sub in subscribers)
				{
					rows.Add(new object[]
					{
						sub.Controller,
						sub.SubscriberId,
						sub.FullName,
						sub.Address,
						sub.StartValue,
						sub.EndValue,
						sub.Difference
					});
				}

				// Save file using MiniExcel
				MiniExcel.SaveAs(filePath, rows, printHeader: false);
				return true;
			}
			catch
			{
				return false;
			}
		});
	}
}
