using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

/*
 * Build this with:
 * 	> dotnet publish LicenseManagerX.Console.cs -c Release -r win-x64 -p:OutputType=Exe -p:PublishSingleFile=true --self-contained true
 */

string consoleDir = AppContext.BaseDirectory;
string packageRoot = Path.GetFullPath(Path.Combine(consoleDir, ".."));
string target = "LicenseManagerX";
string pathTarget = Path.Combine(packageRoot, target, target + ".exe");

if (!File.Exists(pathTarget))
{
	Console.Error.WriteLine("❌ Could not find the main app at: " + pathTarget);
	Console.WriteLine($"📦 Contents of {packageRoot}:");

	string[] dirs = Directory.GetDirectories(packageRoot);
	string[] files = Directory.GetFiles(packageRoot);

	Console.WriteLine();
	Console.WriteLine("Folders:");
	foreach (string dir in dirs)
	{
		Console.WriteLine("  📁 " + Path.GetFileName(dir));
	}

	Console.WriteLine();
	Console.WriteLine("Files:");
	foreach (string file in files)
	{
		Console.WriteLine("  📄 " + Path.GetFileName(file));
	}

	string subDir = Path.Combine(packageRoot, target);
	if (Directory.Exists(subDir))
	{
		Console.WriteLine();
		Console.WriteLine($"📂 Contents of {target}/:");

		string[] subFiles = Directory.GetFiles(subDir);
		foreach (string file in subFiles)
		{
			Console.WriteLine("  📄 " + Path.GetFileName(file));
		}
	}
	else
	{
		Console.WriteLine();
		Console.WriteLine($"🔍 Subfolder '{target}' not found.");
	}

	return;
}

if (args.Length == 0)
{
	// Launch GUI instead
	Process.Start(pathTarget);
}
else
{
	ProcessStartInfo psi = new()
	{
		FileName = pathTarget,
		UseShellExecute = false,
		RedirectStandardOutput = true,
		RedirectStandardError = true,
	};
	foreach (string arg in args)
	{
		psi.ArgumentList.Add(arg);
	}

	Process process = Process.Start(psi);
	if (process is null)
	{
		Console.Error.WriteLine($"❌ Failed to open the main application: {psi.FileName}");
		Environment.ExitCode = 1;
		return;
	}

	Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync();
	Task<string> stdErrTask = process.StandardError.ReadToEndAsync();

	process.WaitForExit();
	Task.WaitAll(stdOutTask, stdErrTask);

	string stdOut = stdOutTask.Result;
	if (!string.IsNullOrEmpty(stdOut))
	{
		Console.Out.Write(stdOut);
	}

	string stdErr = stdErrTask.Result;
	if (!string.IsNullOrEmpty(stdErr))
	{
		Console.Error.Write(stdErr);
	}

	Environment.ExitCode = process.ExitCode;
}
