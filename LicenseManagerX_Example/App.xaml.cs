using System;
using System.Windows;
using System.Windows.Threading;

namespace LicenseManagerX_Example;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	private const string Product = @"My Sample App";
	private const string ProductID = @"My Sample App TRIAL";
	private const string PublicKey = @"MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE8rVkInZuKd56LlNb9vTqcSaAD8hwsc/iMn++wHppvyOfNexHnid+03PcKTn6MwXwv7D43fmqZtbYGSmccNA1cQ==";

	public App()
	{
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
	}

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
			//Current.Shutdown();
		}
	}

	private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		MessageBox.Show(e.Exception.ToString(), "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
		e.Handled = true;
		Current.Shutdown();
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		MessageBox.Show(e.ExceptionObject.ToString(), "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
	}
}
