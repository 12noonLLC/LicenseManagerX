using LicenseManager_12noon.Client;
using Standard.Licensing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace LicenseManagerX;

/// <summary>
/// CLI argument parser and validator.
/// </summary>
public class CliArgumentParser
{
	// Required
	public string PrivateFilePath { get; set; } = string.Empty;

	// At least one is required
	public bool SaveKeypair { get; set; } = false;
	public string LicenseFilePath { get; set; } = string.Empty;

	// Optional: Overwrite existing license file if true
	public bool ForceOverwrite { get; set; } = false;

	// Optional overrides
	public string? ProductVersion { get; set; }
	public DateOnly? ProductPublishDate { get; set; }
	public Dictionary<string, string> ProductFeatures { get; set; } = [];
	public LicenseType? LicenseType { get; set; }
	public int? Quantity { get; set; }
	public int? ExpirationDays { get; set; }
	public DateOnly? ExpirationDate { get; set; }
	public Dictionary<string, string> LicenseAttributes { get; set; } = [];
	public string? LockPath { get; set; }

	public bool HelpRequested { get; set; }

	/// <summary>
	/// Parse command line arguments.
	/// </summary>
	/// <param name="args">Command line arguments</param>
	/// <returns>Parsed arguments</returns>
	/// <exception cref="ArgumentException">Thrown when arguments are invalid</exception>
	public static CliArgumentParser Parse(string[] args)
	{
		CliArgumentParser parser = new();

		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i].ToLowerInvariant())
			{
				case "--private":
				case "-p":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --private argument");
					}
					string rawPrivatePath = args[++i];
					parser.PrivateFilePath = rawPrivatePath.Trim('"');
					break;

				case "--save":
				case "-s":
					parser.SaveKeypair = true;
					break;

				case "--license":
				case "-l":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --license argument");
					}
					string rawLicensePath = args[++i];
					parser.LicenseFilePath = rawLicensePath.Trim('"');
					break;

				case "--force":
				case "-f":
					parser.ForceOverwrite = true;
					break;

				case "--product-version":
				case "-v":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --product-version argument");
					}
					parser.ProductVersion = args[++i];
					break;

				case "--product-publish-date":
				case "-pd":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --product-publish-date argument");
					}
					if (!DateOnly.TryParse(args[++i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var publishDate))
					{
						throw new ArgumentException("Invalid product-publish date format. Use YYYY-MM-DD format");
					}
					parser.ProductPublishDate = publishDate;
					break;

				case "--product-features":
				case "-pf":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --product-features argument");
					}
					ParseKeyValuePairs(args[++i], parser.ProductFeatures, "product features");
					break;

				case "--type":
				case "-t":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --type argument");
					}
					string typeValue = args[++i];
					if (!Enum.TryParse<LicenseType>(typeValue, true, out var licenseType))
					{
						throw new ArgumentException($"Invalid license type '{typeValue}'. Valid values: Standard, Trial");
					}
					parser.LicenseType = licenseType;
					break;

				case "--quantity":
				case "-q":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --quantity argument");
					}
					if (!int.TryParse(args[++i], out var quantity) || quantity < 1)
					{
						throw new ArgumentException("Quantity must be a positive integer");
					}
					parser.Quantity = quantity;
					break;

				case "--expiration-days":
				case "-dy":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --expiration-days argument");
					}
					if (!int.TryParse(args[++i], out var expirationDays) || (expirationDays < 0))
					{
						throw new ArgumentException("Expiration days must be zero or a positive integer");
					}
					parser.ExpirationDays = expirationDays;
					break;

				case "--expiration-date":
				case "-dt":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --expiration-date argument");
					}
					if (!DateOnly.TryParseExact(args[++i], LicenseManager.DateFormat_Expiration, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly expirationDate))
					{
						throw new ArgumentException("Invalid expiration-date format. Use YYYY-MM-DD format");
					}
					parser.ExpirationDate = expirationDate;
					break;

				case "--license-attributes":
				case "-la":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --license-attributes argument");
					}
					ParseKeyValuePairs(args[++i], parser.LicenseAttributes, "license attributes");
					break;

				case "--lock":
					if (i + 1 >= args.Length)
					{
						throw new ArgumentException("Missing value for --lock argument");
					}
					// Ensure full path for consistency with GUI
					// Remove surrounding quotation marks, if any.
					string rawLockPath = args[++i];
					string cleanedLockPath = rawLockPath.Trim('"');
					parser.LockPath = Path.GetFullPath(cleanedLockPath);
					break;

				case "--help":
				case "-h":
				case "-?":
				case "/?":
					parser.HelpRequested = true;
					break;

				default:
					throw new ArgumentException($"Unknown argument: {args[i]}");
			}
		}

		return parser;
	}

	/// <summary>
	/// Parse key=value pairs from a string.
	/// </summary>
	/// <param name="input">Input string containing space-separated key=value pairs</param>
	/// <param name="dictionary">Dictionary to add the parsed pairs to</param>
	/// <param name="argumentName">Name of the argument for error messages</param>
	/// <exception cref="ArgumentException">Thrown when parsing fails</exception>
	private static void ParseKeyValuePairs(string input, Dictionary<string, string> dictionary, string argumentName)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return;
		}

		// Convert space-separated key=value pairs to newline-separated for parsing
		string[] pairs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string joinPairs = string.Join(Environment.NewLine, pairs);
		try
		{
			Dictionary<string, string> parsed = Shared.MultilineTextToDictionary.ConvertTextToDictionary(joinPairs);
			if (parsed.Count != pairs.Length)
			{
				throw new ArgumentException($"Invalid {argumentName} format. Expected key=value format.");
			}

			foreach (var kvp in parsed)
			{
				dictionary[kvp.Key] = kvp.Value;
			}
		}
		catch (Exception ex)
		{
			throw new ArgumentException($"Invalid {argumentName} format: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Validate parsed arguments.
	/// </summary>
	/// <exception cref="ArgumentException">Thrown when validation fails</exception>
	public void Validate()
	{
		if (HelpRequested)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(PrivateFilePath))
		{
			throw new ArgumentException("Private file path is required. Use --private or -p argument.");
		}

		if (!File.Exists(PrivateFilePath))
		{
			throw new FileNotFoundException($"Private file does not exist: {PrivateFilePath}");
		}

		// If neither --license nor --save is specified, error
		if (!SaveKeypair && string.IsNullOrWhiteSpace(LicenseFilePath))
		{
			throw new ArgumentException("Either --license or --save must be specified.");
		}

		// If license file specified, ensure it does not exist unless --force is set
		if (!string.IsNullOrWhiteSpace(LicenseFilePath))
		{
			if (File.Exists(LicenseFilePath) && !ForceOverwrite)
			{
				throw new ArgumentException($"License file already exists and will not be overwritten: {LicenseFilePath}");
			}

			// Make sure directory exists for the license file
			string? licenseDir = Path.GetDirectoryName(LicenseFilePath);
			if (!string.IsNullOrEmpty(licenseDir) && !Directory.Exists(licenseDir))
			{
				throw new DirectoryNotFoundException($"Directory does not exist for license file: {licenseDir}");
			}
		}

		/// Product Properties
		// Validate product features
		foreach (var feature in ProductFeatures)
		{
			if (LicenseManager.IsReservedFeatureName(feature.Key))
			{
				throw new ArgumentException($"'{feature.Key}' is a reserved product feature name and cannot be used.");
			}
		}

		/// License Properties
		// Validate mutually exclusive expiration options
		if (ExpirationDays.HasValue && ExpirationDate.HasValue)
		{
			throw new ArgumentException("Cannot specify both --expiration-days and --expiration-date. Use only one.");
		}

		// Validate license attributes
		foreach (var attribute in LicenseAttributes)
		{
			if (LicenseManager.IsReservedAttributeName(attribute.Key))
			{
				throw new ArgumentException($"'{attribute.Key}' is a reserved license attribute name and cannot be used.");
			}
		}

		// Validate lock file exists if specified
		if (!string.IsNullOrWhiteSpace(LockPath) && !File.Exists(LockPath))
		{
			throw new FileNotFoundException($"Lock file does not exist: {LockPath}");
		}
	}

	/// <summary>
	/// Apply CLI overrides to the license manager.
	/// </summary>
	/// <param name="manager">License manager to apply overrides to</param>
	public void ApplyOverrides(LicenseManager manager)
	{
		/// Product Properties
		if (!string.IsNullOrWhiteSpace(ProductVersion) && (manager.Version != ProductVersion))
		{
			manager.Version = ProductVersion;
		}

		if (ProductPublishDate.HasValue && (manager.PublishDate != ProductPublishDate.Value))
		{
			manager.PublishDate = ProductPublishDate.Value;
		}

		// Apply product features if any specified
		if (ProductFeatures.Count > 0)
		{
			// Create a new dictionary with existing features plus new ones
			Dictionary<string, string> newFeatures = new(manager.ProductFeatures);
			foreach (var feature in ProductFeatures)
			{
				newFeatures[feature.Key] = feature.Value;
			}
			manager.UpdateProductFeatures(newFeatures);
		}

		/// License Properties
		if (LicenseType.HasValue && (manager.StandardOrTrial != LicenseType.Value))
		{
			manager.StandardOrTrial = LicenseType.Value;
		}

		if (Quantity.HasValue && (manager.Quantity != Quantity.Value))
		{
			manager.Quantity = Quantity.Value;
		}

		if (ExpirationDays.HasValue && (manager.ExpirationDays != ExpirationDays.Value))
		{
			manager.ExpirationDays = ExpirationDays.Value;
			// ExpirationDate is automatically updated by the property change handler
		}
		else if (ExpirationDate.HasValue && (manager.ExpirationDate != ExpirationDate.Value))
		{
			manager.ExpirationDate = ExpirationDate.Value;
			manager.ExpirationDays = (int)(ExpirationDate.Value.ToDateTime(new()) - DateOnly.FromDateTime(MyNow.Now().Date).ToDateTime(new())).TotalDays;
		}

		// Apply license attributes if any specified
		if (LicenseAttributes.Count > 0)
		{
			// Create a new dictionary with existing attributes plus new ones
			Dictionary<string, string> newAttributes = new(manager.LicenseAttributes);
			foreach (var attribute in LicenseAttributes)
			{
				newAttributes[attribute.Key] = attribute.Value;
			}
			manager.UpdateLicenseAttributes(newAttributes);
		}

		// Apply lock path if specified
		if (!string.IsNullOrWhiteSpace(LockPath))
		{
			if (manager.PathAssembly != LockPath)
			{
				manager.PathAssembly = LockPath;
				manager.IsLockedToAssembly = true;
			}
		}
	}

	/// <summary>
	/// Show help text.
	/// </summary>
	public static void ShowHelp()
	{
		Console.WriteLine("License Manager X CLI - Create license files from .private files");
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine("  lmx --private <path>");
		Console.WriteLine("  lmx --private <path> --license <path> [options]");
		Console.WriteLine("  lmx --private <path> --save --license <path> [options]");
		Console.WriteLine("  lmx --private <path> --save [options]");
		Console.WriteLine();
		Console.WriteLine("Required Arguments:");
		Console.WriteLine("  --private, -p <path> Path to the .private file");
		Console.WriteLine();
		Console.WriteLine("Display keypair, save keypair, or create license:");
		Console.WriteLine("   You can specify one or more of these switches.");
		Console.WriteLine("   If you do not specify any of these, the CLI only displays the properties from the keypair file.");
		Console.WriteLine("  --save, -s           Save the keypair file");
		Console.WriteLine("  --license, -l <path> Path to the new .lic file (will not overwrite unless --force)");
		Console.WriteLine();
		Console.WriteLine("Optional Arguments:");
		Console.WriteLine("  --force, -f                        Overwrite the license file if it already exists");
		Console.WriteLine("  --product-version, -v <version>    Product version");
		Console.WriteLine("  --product-publish-date, -pd <date> Product publish date (YYYY-MM-DD format)");
		Console.WriteLine("  --product-features, -pf <pairs>    Product features as key=value pairs");
		Console.WriteLine("  --type, -t <Standard | Trial>      License type");
		Console.WriteLine("  --quantity, -q <number>            License quantity (positive integer)");
		Console.WriteLine("  --expiration-days, -dy <days>      Expiration in days (0 = no expiry)");
		Console.WriteLine("  --expiration-date, -dt <date>      Expiration date (YYYY-MM-DD format)");
		Console.WriteLine("  --license-attributes, -la <pairs>  License attributes as key=value pairs");
		Console.WriteLine("  --lock <path>                      Lock license to a specific file (e.g., EXE or DLL)");
		Console.WriteLine("  --help, -h                         Show this help");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  lmx -p my.private --save");
		Console.WriteLine("  lmx -p my.private -l standard.lic -s --type Standard");
		Console.WriteLine("  lmx -p my.private -l customer.lic");
		Console.WriteLine("  lmx -p my.private -l trial.lic --type Trial --expiration-days 30");
		Console.WriteLine("  lmx -p my.private -l enterprise.lic --quantity 100 --product-version 2.1.0");
		Console.WriteLine("  lmx -p my.private -l locked.lic --lock \"C:\\MyApp\\MyApp.exe\"");
		Console.WriteLine("  lmx -p my.private -l featured.lic --product-features \"Color=Blue Bird=Heron\"");
		Console.WriteLine("  lmx -p my.private -l attributed.lic --license-attributes \"Size=Large Color=Red\"");
		Console.WriteLine();
		Console.WriteLine("Notes:");
		Console.WriteLine("  - If the license file already exists, it will not be overwritten unless --force is specified");
		Console.WriteLine("  - Cannot override: passphrase, keys, product name, customer info");
		Console.WriteLine("  - Either expiration-days or expiration-date can be specified, not both");
		Console.WriteLine("  - Key=value pairs should be space-separated: \"key1=value1 key2=value2\"");
		Console.WriteLine("  - Reserved feature names: Product, Version, Publish Date");
		Console.WriteLine("  - Reserved attribute names: Product Identity, Assembly Identity, Expiration Days");
	}
}
