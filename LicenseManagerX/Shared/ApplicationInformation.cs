using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Shared;

/// <summary>
/// Provides version and metadata information about the executing assembly.
/// </summary>
/// <remarks>
/// For additional assemblies, derive a class and add more <see cref="FileVersionInfo"/> members.
/// </remarks>
/// <example>
/// <code>
/// public FileVersionInfo MyLibraryExeInfo { get; private set; }
///
/// string pathLibraryDLL = System.IO.Path.Combine(pathExe, "MyLibrary.dll");
/// LibraryDLLInfo = FileVersionInfo.GetVersionInfo(pathCoreDLL);
/// </code>
/// </example>
public class ApplicationInformation
{
	/// <summary>Gets the product name of the executing assembly.</summary>
	public string Name { get; private set; }

	/// <summary>Gets the company name of the executing assembly.</summary>
	public string Company { get; private set; }

	/// <summary>Gets the legal copyright of the executing assembly.</summary>
	public string Copyright { get; private set; }

	/// <summary>Gets the product version of the executing assembly (may include pre-release labels and build metadata).</summary>
	public string Version { get; private set; }

	/// <summary>Gets the file description of the executing assembly.</summary>
	public string FileTitle { get; private set; }

	/// <summary>Gets the file version of the executing assembly (e.g., "2.4.0.0").</summary>
	public string FileVersion { get; private set; }

	/// <summary>Gets the short product version without build revision or git hash (e.g., "2.4.0").</summary>
	public string VersionShort { get; private set; }

	/// <summary>Gets the product web site URL.</summary>
	public string WebSiteURL { get; private set; } = @"https://12noon.com";

	// AssemblyDescription
	// AssemblyConfiguration
	// AssemblyTrademark
	// AssemblyInformationalVersion

	/// <summary>
	/// Provides information about the version of the EXECUTING assembly.
	/// </summary>
	/// <remarks>
	/// This does NOT return information about THIS assembly (DLL).
	/// </remarks>
	public ApplicationInformation()
	{
		string asmPath = GetAssemblyPath();
		FileVersionInfo AppExeInfo = FileVersionInfo.GetVersionInfo(asmPath);

		//AssemblyName asmName = asm.GetName();

		//AssemblyTitleAttribute attrTitle = (AssemblyTitleAttribute)System.Attribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute));
		//string Title = attrTitle.Title;

		//AssemblyCompanyAttribute attrCompany = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyCompanyAttribute));
		//Company = attrCompany.Company;

		Name = AppExeInfo.ProductName ?? string.Empty;
		Company = AppExeInfo.CompanyName ?? string.Empty;
		Copyright = AppExeInfo.LegalCopyright ?? string.Empty;
		Version = AppExeInfo.ProductVersion ?? string.Empty;

		FileTitle = AppExeInfo.FileDescription ?? string.Empty;
		FileVersion = AppExeInfo.FileVersion ?? string.Empty;
		VersionShort = new Version(FileVersion).ToString(3);
	}


	/// <summary>
	/// Return the full path to the running EXE or DLL.
	/// Uses <see cref="AppContext.BaseDirectory"/> for the directory (always reliable,
	/// even in single-file publish) and derives the filename from
	/// <see cref="Environment.ProcessPath"/> or <see cref="Assembly.Location"/> as a fallback.
	/// </summary>
	public static string GetAssemblyPath()
	{
		// AppContext.BaseDirectory is always the folder containing the EXE, even in single-file publish.
		string directory = AppContext.BaseDirectory;

		// Prefer Environment.ProcessPath for the filename (works in single-file publish).
		string? processPath = Environment.ProcessPath;
		if (!string.IsNullOrWhiteSpace(processPath))
		{
			return Path.Combine(directory, Path.GetFileName(processPath));
		}

		// Fallback: entry assembly location filename (works in non-single-file publish).
#pragma warning disable IL3000 // Assembly.Location always returns empty string for assemblies embedded in a single-file app.
		string? assemblyLocation = Assembly.GetEntryAssembly()?.Location;
#pragma warning restore IL3000
		if (!string.IsNullOrWhiteSpace(assemblyLocation))
		{
			return Path.Combine(directory, Path.GetFileName(assemblyLocation));
		}

		throw new InvalidOperationException("Unable to resolve entry assembly path.");
	}
}
