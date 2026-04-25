using System.Windows;
using System.Collections.Generic;

namespace LicenseManagerX_Example;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public string LicenseType { get; private set; } = string.Empty;
	public string ExpirationDate { get; private set; } = string.Empty;
	public int ExpirationDays { get; private set; } = 0;
	public int Quantity { get; private set; } = 0;

	public string Product { get; private set; } = string.Empty;
	public string Version { get; private set; } = string.Empty;
	public string PublishDate { get; private set; } = string.Empty;

	public string Licensee { get; private set; } = string.Empty;
	public string Email { get; private set; } = string.Empty;
	public string Company { get; private set; } = string.Empty;

	public bool IsLockedToAssembly { get; private set; } = false;
	public string ProductId { get; private set; } = string.Empty;

	public Dictionary<string, string> ProductFeatures { get; private set; } = [];
	public Dictionary<string, string> LicenseAttributes { get; private set; } = [];

	public MainWindow()
	{
		InitializeComponent();

		DataContext = this;

		CtlValid.Text = App.IsLicensed ? "Licensed" : "NOT Licensed";

		if (App.IsLicensed)
		{
			LicenseType = App.License.StandardOrTrial.ToString();
			// Alternative: (App.License.ExpirationDateUTC == DateTime.MaxValue.Date)
			ExpirationDate = (App.License.ExpirationDays == 0) ? "Never" : (App.License.ExpirationDate.ToString("D") ?? "None");
			ExpirationDays = App.License.ExpirationDays;
			Quantity = App.License.Quantity;
			Product = App.License.Product;
			Version = App.License.Version;
			PublishDate = App.License.PublishDate?.ToString("D") ?? "None";
			Licensee = App.License.Name;
			Email = App.License.Email;
			Company = App.License.Company;
			IsLockedToAssembly = App.License.IsLockedToAssembly;
			ProductId = App.License.ProductId;

			// Load custom product features
			ProductFeatures.Clear();
			if (App.License.ProductFeatures.Count == 0)
			{
				ProductFeatures.Add("No custom product features", string.Empty);
			}
			else
			{
				foreach (var feature in App.License.ProductFeatures)
				{
					ProductFeatures.Add(feature.Key, feature.Value);
				}
			}

			// Load custom license attributes
			LicenseAttributes.Clear();
			if (App.License.LicenseAttributes.Count == 0)
			{
				LicenseAttributes.Add("No custom license attributes", string.Empty);
			}
			else
			{
				foreach (var attribute in App.License.LicenseAttributes)
				{
					LicenseAttributes.Add(attribute.Key, attribute.Value);
				}
			}
		}
	}
}
