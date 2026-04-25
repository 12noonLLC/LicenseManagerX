using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace LicenseManagerX;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	[LibraryImport("kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool AttachConsole(int dwProcessId);
	private const int ATTACH_PARENT_PROCESS = -1;

	[LibraryImport("kernel32.dll", EntryPoint = "FreeConsole", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool FreeConsole();


	/// <summary>
	/// Application startup handler that determines whether to run in CLI or GUI mode.
	/// </summary>
	/// <param name="e">Startup event arguments containing command line arguments</param>
	protected override void OnStartup(StartupEventArgs e)
	{
		// Check if command line arguments were passed
		if (e.Args.Length > 0)
		{
			AttachConsole(ATTACH_PARENT_PROCESS);

			// Run in CLI mode
			int exitCode = RunCliMode(e.Args);

			Console.Out.Flush();
			FreeConsole();

			Shutdown(exitCode);
			return;
		}

		base.OnStartup(e);

		// Run in GUI mode - create and show the main window
		Window? window = Activator.CreateInstance(typeof(MainWindow), nonPublic: true) as Window;
		window?.Show();
	}

	/// <summary>
	/// Execute the CLI functionality.
	/// </summary>
	/// <param name="args">Command line arguments</param>
	/// <returns>Exit code: 0 for success, 1 for failure</returns>
	private static int RunCliMode(string[] args)
	{
		try
		{
			// Parse command line arguments
			var parsedArgs = CliArgumentParser.Parse(args);

			// Show help if requested or no arguments provided
			if (parsedArgs.HelpRequested || (args.Length == 0))
			{
				CliArgumentParser.ShowHelp();
				return 0;
			}

			// Validate arguments, but do not require --license or --save for display mode
			if (!parsedArgs.SaveKeypair && string.IsNullOrWhiteSpace(parsedArgs.LicenseFilePath))
			{
				return DisplayKeypairProperties(parsedArgs);
			}

			// Validate arguments
			parsedArgs.Validate();

			// Create license manager and load private file
			LicenseManager manager = new();
			manager.LoadKeypair(parsedArgs.PrivateFilePath);
			Console.WriteLine($"Loaded private information for: {manager.Product} {manager.Version}");

			// Apply CLI overrides
			parsedArgs.ApplyOverrides(manager);

			// Show what was overridden
			bool hasOverrides = !string.IsNullOrEmpty(parsedArgs.ProductVersion) || parsedArgs.ProductPublishDate.HasValue ||
										(parsedArgs.ProductFeatures.Count > 0) ||
										parsedArgs.LicenseType.HasValue || parsedArgs.Quantity.HasValue ||
										parsedArgs.ExpirationDays.HasValue ||	parsedArgs.ExpirationDate.HasValue ||
										(parsedArgs.LicenseAttributes.Count > 0) ||
										!string.IsNullOrEmpty(parsedArgs.LockPath);

			if (hasOverrides)
			{
				DisplayOverrideProperties(parsedArgs, manager);
			}

			// Save keypair if requested
			if (parsedArgs.SaveKeypair)
			{
				Console.WriteLine();
				Console.WriteLine($"Saving keypair file: {parsedArgs.PrivateFilePath}");
				manager.SaveKeypair(parsedArgs.PrivateFilePath);
				Console.WriteLine("Keypair file saved successfully.");
			}

			// Create license file if requested
			if (!string.IsNullOrWhiteSpace(parsedArgs.LicenseFilePath))
			{
				Console.WriteLine();
				Console.WriteLine($"Creating license file: {parsedArgs.LicenseFilePath}");
				manager.SaveLicenseFile(parsedArgs.LicenseFilePath);
				Console.WriteLine("License file created successfully.");
			}

			return 0;
		}
		catch (ArgumentException ex)
		{
			Console.Error.WriteLine($"Error: {ex.Message}");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Use --help for usage information.");
			return 1;
		}
		catch (FileNotFoundException ex)
		{
			Console.Error.WriteLine($"Error: File not found - {ex.Message}");
			return 1;
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.Error.WriteLine($"Error: Access denied - {ex.Message}");
			return 1;
		}
		catch (IOException ex)
		{
			Console.Error.WriteLine($"Error: I/O error - {ex.Message}");
			return 1;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Unexpected error: {ex.Message}");
			Console.Error.WriteLine($"Details: {ex}");
			return 1;
		}
	}

	private static int DisplayKeypairProperties(CliArgumentParser parsedArgs)
	{
		// Only require --private to exist
		if (string.IsNullOrWhiteSpace(parsedArgs.PrivateFilePath))
		{
			Console.Error.WriteLine("Error: Private file path is required. Use --private or -p argument.");
			return 1;
		}
		if (!File.Exists(parsedArgs.PrivateFilePath))
		{
			Console.Error.WriteLine($"Error: Private file does not exist: {parsedArgs.PrivateFilePath}");
			return 1;
		}

		// Load and display keypair info only
		LicenseManager displayManager = new();
		displayManager.LoadKeypair(parsedArgs.PrivateFilePath);

		Console.WriteLine();

		Console.WriteLine($"Product ID: {displayManager.ProductId}");
		Console.WriteLine($"Public key: {displayManager.KeyPublic}");
		Console.WriteLine();

		Console.WriteLine($"Product: {displayManager.Product}");
		Console.WriteLine($"Version: {displayManager.Version}");
		if (displayManager.ProductFeatures.Count > 0)
		{
			Console.WriteLine($"Product features:");
			foreach (var feature in displayManager.ProductFeatures)
			{
				Console.WriteLine($"  {feature.Key} = {feature.Value}");
			}
		}
		Console.WriteLine();

		Console.WriteLine($"Customer: {displayManager.Name} <{displayManager.Email}>");
		if (!string.IsNullOrEmpty(displayManager.Company))
		{
			Console.WriteLine($"Company: {displayManager.Company}");
		}
		Console.WriteLine();

		Console.WriteLine($"License type: {displayManager.StandardOrTrial}");
		Console.WriteLine($"Quantity: {displayManager.Quantity}");
		if (displayManager.ExpirationDays > 0)
		{
			Console.WriteLine($"Expiration days: {displayManager.ExpirationDays} ({((displayManager.ExpirationDate == DateOnly.MaxValue) ? "None" : displayManager.ExpirationDate):yyyy-MM-dd})");
		}
		if (displayManager.LicenseAttributes.Count > 0)
		{
			Console.WriteLine($"License attributes:");
			foreach (var attribute in displayManager.LicenseAttributes)
			{
				Console.WriteLine($"  {attribute.Key} = {attribute.Value}");
			}
		}
		if (displayManager.IsLockedToAssembly && !string.IsNullOrEmpty(displayManager.PathAssembly))
		{
			Console.WriteLine($"Lock file: {displayManager.PathAssembly} ({(File.Exists(displayManager.PathAssembly) ? "Exists" : "Does NOT exist")})");
		}

		return 0;
	}

	private static void DisplayOverrideProperties(CliArgumentParser parsedArgs, LicenseManager manager)
	{
		Console.WriteLine();
		Console.WriteLine("Applied CLI overrides:");
		if (!string.IsNullOrEmpty(parsedArgs.ProductVersion))
		{
			Console.WriteLine($"  Product Version: {manager.Version}");
		}
		if (parsedArgs.ProductPublishDate.HasValue)
		{
			Console.WriteLine($"  Product Publish Date: {manager.PublishDate}");
		}
		if (parsedArgs.ProductFeatures.Count > 0)
		{
			Console.WriteLine($"  Product Features:");
			foreach (var feature in parsedArgs.ProductFeatures)
			{
				Console.WriteLine($"    {feature.Key} = {feature.Value}");
			}
		}
		if (parsedArgs.LicenseType.HasValue)
		{
			Console.WriteLine($"  License Type: {manager.StandardOrTrial}");
		}
		if (parsedArgs.Quantity.HasValue)
		{
			Console.WriteLine($"  Quantity: {manager.Quantity}");
		}
		if (parsedArgs.ExpirationDays.HasValue)
		{
			Console.WriteLine($"  Expiration Days: {manager.ExpirationDays}");
		}
		if (parsedArgs.ExpirationDate.HasValue)
		{
			Console.WriteLine($"  Expiration Date: {manager.ExpirationDate:yyyy-MM-dd}");
		}
		if (parsedArgs.LicenseAttributes.Count > 0)
		{
			Console.WriteLine($"  License Attributes:");
			foreach (var attribute in parsedArgs.LicenseAttributes)
			{
				Console.WriteLine($"    {attribute.Key} = {attribute.Value}");
			}
		}
		if (!string.IsNullOrEmpty(parsedArgs.LockPath))
		{
			Console.WriteLine($"  Lock File: {manager.PathAssembly}");
		}
	}
}
