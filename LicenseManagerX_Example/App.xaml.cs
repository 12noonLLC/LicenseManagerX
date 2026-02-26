using System;
using System.Windows;

namespace LicenseManagerX_Example;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	private const string Product = @"My Sample App";
	private const string ProductID = @"My Sample App TRIAL";
	private const string PublicKey = @"MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE8rVkInZuKd56LlNb9vTqcSaAD8hwsc/iMn++wHppvyOfNexHnid+03PcKTn6MwXwv7D43fmqZtbYGSmccNA1cQ==";

	protected override void OnStartup(StartupEventArgs e)
	{
		// Get these values from the license manager (or in the keypair .private file).
		CheckLicense(Product, ProductID, PublicKey);

		base.OnStartup(e);
	}

	public static LicenseManager_12noon.Client.LicenseFile License = new();
	public static bool IsLicensed { get; private set; }

	private static void CheckLicense(string productName, string productID, string publicKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(productID);
		ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);

		License = new();
		IsLicensed = License.IsLicenseValid(productID, publicKey, out string messages);
		if (!IsLicensed)
		{
			MessageBox.Show(messages, productName, MessageBoxButton.OK, MessageBoxImage.Error);

			// You can terminate the application or continue and limit features.
			//Application.Current.Shutdown();
		}
	}
}
