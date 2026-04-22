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
		Assembly asm = Assembly.GetEntryAssembly() ?? throw new ArgumentNullException(nameof(Assembly));
		//string pathExe = Path.GetDirectoryName(asm.Location);

		// Get information about this assembly (not necessarily the executing assembly).
		FileVersionInfo AppExeInfo = FileVersionInfo.GetVersionInfo(asm.Location);

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
	/// Return the path to the main (entry) assembly (.exe).
	/// (NOT including the filename of the executable).
	/// </summary>
	/// <returns>
	/// <c>C:\Path\To\Executable</c>
	/// </returns>
	/// <example>
	/// <code>
	/// string path = ApplicationInformation.GetAssemblyPath();
	/// </code>
	/// </example>
	/// <returns>Path of the main (entry) assembly (.exe)</returns>
	public static string GetAssemblyPath()
	{
		Assembly? asm = Assembly.GetEntryAssembly();
		return Path.GetDirectoryName(asm?.Location) ?? string.Empty;
	}
}
