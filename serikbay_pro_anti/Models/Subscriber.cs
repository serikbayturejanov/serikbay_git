using CommunityToolkit.Mvvm.ComponentModel;

namespace ControllerApp.Models;

public partial class Subscriber : ObservableObject
{
	[ObservableProperty]
	private int _controller;

	[ObservableProperty]
	private string _subscriberId = string.Empty;

	[ObservableProperty]
	private string _fullName = string.Empty;

	[ObservableProperty]
	private string _address = string.Empty;

	[ObservableProperty]
	private double _startValue;

	[ObservableProperty]
	private double _endValue;

	[ObservableProperty]
	private double _difference;

	// Extracted street property for street-filtering
	public string Street
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Address))
				return string.Empty;

			// Example: "ул Сакен 1" -> street is "ул Сакен" or we extract the street prefix.
			// Let's strip the last token if it's a number (house number) to get the street.
			var parts = Address.Split(' ');
			if (parts.Length > 1 && double.TryParse(parts[^1], out _))
			{
				return string.Join(" ", parts[..^1]);
			}
			return Address;
		}
	}
}
