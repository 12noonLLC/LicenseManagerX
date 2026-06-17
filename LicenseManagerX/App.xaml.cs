using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: InternalsVisibleTo("LicenseManagerX.UnitTests")]

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
			Console.Error.Flush();
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
	/// Execute the CLI functionality using System.CommandLine.
	/// </summary>
	/// <remarks>
	/// To test this, copy the contents of LicenseManagerX's Debug folder
	/// to "...\artifacts\bin\LicenseManagerX.Console\LicenseManagerX"
	/// and run the LicenseManagerX.Console.exe with appropriate arguments.
	/// </remarks>
	/// <param name="args">Command line arguments</param>
	/// <returns>Exit code: 0 for success, 1 for failure</returns>
	private static int RunCliMode(string[] args)
	{
		try
		{
			return CliParser.RunCommand(args,
													keypairCreateCmd: CliCommands.KeypairCreate,
													licenseCreateCmd: CliCommands.LicenseCreate,
													keypairUpdateCmd: CliCommands.KeypairUpdate,
													licenseUpdateCmd: CliCommands.LicenseUpdate,
													keypairShowCmd:   CliCommands.KeypairShow,
													licenseShowCmd:   CliCommands.LicenseShow);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Unexpected error: {ex.Message}");
			Console.Error.WriteLine($"Details: {ex}");
			return 1;
		}
	}
}
