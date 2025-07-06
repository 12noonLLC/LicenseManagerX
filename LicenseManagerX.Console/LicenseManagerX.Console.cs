﻿using System;
using System.Diagnostics;
using System.IO;

/*
 * Build this with:
 * 	> dotnet publish LicenseManagerX.Console.cs -c Release -r win-x64 -p:OutputType=Exe -p:PublishSingleFile=true --self-contained true
 */

var consoleDir = AppContext.BaseDirectory;
var packageRoot = Path.GetFullPath(Path.Combine(consoleDir, ".."));
string target = "LicenseManagerX";
string pathTarget = Path.Combine(packageRoot, target, target + ".exe");

if (!File.Exists(pathTarget))
{
	Console.Error.WriteLine("❌ Could not find the main app at: " + pathTarget);
	Console.WriteLine($"📦 Contents of {packageRoot}:");

	var dirs = Directory.GetDirectories(packageRoot);
	var files = Directory.GetFiles(packageRoot);

	Console.WriteLine();
	Console.WriteLine("Folders:");
	foreach (var dir in dirs)
	{
		Console.WriteLine("  📁 " + Path.GetFileName(dir));
	}

	Console.WriteLine();
	Console.WriteLine("Files:");
	foreach (var file in files)
	{
		Console.WriteLine("  📄 " + Path.GetFileName(file));
	}

	var subDir = Path.Combine(packageRoot, target);
	if (Directory.Exists(subDir))
	{
		Console.WriteLine();
		Console.WriteLine($"📂 Contents of {target}/:");

		var subFiles = Directory.GetFiles(subDir);
		foreach (var file in subFiles)
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
	};
	foreach (string arg in args)
	{
		psi.ArgumentList.Add(arg);
	}

	Process process = Process.Start(psi);
	process.WaitForExit();
}
