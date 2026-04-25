using Standard.Licensing;
using Standard.Licensing.Validation;
using System;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace LicenseManager_12noon.Client;

/// <summary>
/// Validate license file with public key and product ID.
/// </summary>
/// <example>
/// LicenseRecord license = new();
/// bool isValid = license.IsLicenseValid("My Product ID", "The Public Key", out string messages);
/// if (isValid)
/// {
///	// VALID
///	if (license.StandardOrTrial == LicenseType.Trial)
///	{
///		// Example: LIMIT FEATURES FOR TRIAL
///	}
/// }
/// </example>
public partial class LicenseFile
{
	public const string FileExtension_License = ".lic";

	private const string MESSAGE_LICENSE_MISSING1 = "Unable to find license file {0}.";
	private const string MESSAGE_LICENSE_INVALID_PRODUCT_IDENTITY1 = "License file {0} is not associated with this product.";
	private const string MESSAGE_LICENSE_INVALID_PRODUCT_INSTANCE2 = "License file {0} is not associated with this instance of the product {1}.";
	private const string MESSAGE_LICENSE_INVALID = "License validation failure.";
	private const string MESSAGE_LICENSE_RESOLVE = "Please contact your company's IT department or Support at 12noon.com.";

	private const string ProductFeature_Name_Product = "Product";
	private const string ProductFeature_Name_Version = "Version";
	private const string ProductFeature_Name_PublishDate = "Publish Date";

	private const string Attribute_Name_ProductIdentity = "Product Identity";
	private const string Attribute_Name_AssemblyIdentity = "Assembly Identity";
	private const string Attribute_Name_ExpirationDays = "Expiration Days";

	/*
	 * These properties are set when a license has been validated.
	 */
	public LicenseType StandardOrTrial { get; private set; } = LicenseType.Standard;
	public DateOnly ExpirationDate { get; private set; } = DateOnly.MaxValue;
	[Obsolete("Use ExpirationDate instead. ExpirationDateUTC is kept for backward compatibility and will be removed in a future version.")]
	public DateTime ExpirationDateUTC
	{
		get
		{
			if (ExpirationDate == DateOnly.MaxValue)
			{
				return DateTime.MaxValue;
			}

			return ExpirationDate.ToDateTime(TimeOnly.MinValue);
		}
	}
	public int ExpirationDays { get; private set; }
	public int Quantity { get; private set; } = 1;

	public string Product { get; private set; } = string.Empty;
	public string Version { get; private set; } = string.Empty;
	public DateOnly? PublishDate { get; private set; }

	public Dictionary<string, string> ProductFeatures { get; private set; } = [];
	public Dictionary<string, string> LicenseAttributes { get; private set; } = [];

	public string Name { get; private set; } = string.Empty;
	public string Email { get; private set; } = string.Empty;
	public string Company { get; private set; } = string.Empty;

	public bool IsLockedToAssembly { get; private set; } = false;
	public string ProductId { get; private set; } = string.Empty;
	public static string CreateProductIdentity(string productId, string keyPublic) => productId + " " + keyPublic;


	public LicenseFile()
	{
	}

	/// <summary>
	/// This is the public-facing API used by the licensed app to validate its license.
	/// If the license is valid, it loads the license information into their corresponding properties.
	/// </summary>
	/// <example>
	/// bool isValid = IsLicenseValid("My Product ID", "My Public Key", out string messages);
	/// if (!isValid)
	/// {
	///	MessageBox.Show("License is invalid." + Environment.NewLine + messages, "My Product");
	///	return false;
	/// }
	/// MessageBox.Show("License validated successfully.", "My Product");
	/// </example>
	/// <param name="productID">String to verify that the license file is associated with this product.</param>
	/// <param name="publicKey">Public encryption key</param>
	/// <param name="messages">Output parameter to hold messages, especially if the license is invalid.</param>
	/// <returns>True if the license is valid, otherwise false.</returns>
	public bool IsLicenseValid(string productID, string publicKey, out string messages)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(productID);
		ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);

		return IsThisLicenseValid(productID, publicKey, GetLicensePath(), GetAssemblyPath(), out messages);
	}

	/// <summary>
	/// Validate the passed license file.
	/// If the license is valid, it loads the license information into their corresponding properties.
	/// All exceptions are caught.
	/// </summary>
	/// <remarks>
	/// Although the "assembly" is not required to be an assembly (it can be a
	/// text file, etc.) it makes more sense for it to be the calling assembly
	/// so that it can be verified to match the license file.
	/// </remarks>
	/// <param name="productID">String to verify that the license file is associated with this product.</param>
	/// <param name="publicKey">Public encryption key</param>
	/// <param name="pathLicense">Path to license file (.lic).</param>
	/// <param name="pathAssembly">Path to the calling assembly associated with the license file.</param>
	/// <param name="messages">Output parameter to hold messages, especially if the license is invalid.</param>
	/// <returns>True if the license is valid, otherwise false.</returns>
	public bool IsThisLicenseValid(string productID, string publicKey, string pathLicense, string pathAssembly, out string messages)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(productID);
		ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);
		ArgumentException.ThrowIfNullOrWhiteSpace(pathLicense);

		ProductId = productID;
		IsLockedToAssembly = false;

		Product = string.Empty;
		Version = string.Empty;
		PublishDate = null;

		StandardOrTrial = LicenseType.Trial;
		ExpirationDate = DateOnly.MaxValue;
		ExpirationDays = 0;
		Quantity = 1;

		Name = string.Empty;
		Email = string.Empty;
		Company = string.Empty;

		try
		{
			if (!File.Exists(pathLicense))
			{
				messages = string.Format(MESSAGE_LICENSE_MISSING1, pathLicense) + Environment.NewLine + Environment.NewLine + MESSAGE_LICENSE_RESOLVE;
				return false;
			}

			string xmlLicense = File.ReadAllText(pathLicense, Encoding.UTF8);
			License license = License.Load(xmlLicense);

			List<IValidationFailure> validationFailures = [];

			/// Required
			string identityProductLicense = license.AdditionalAttributes.Get(Attribute_Name_ProductIdentity);
			string identityProductCaller = SecureHash.ComputeSHA256Hash(CreateProductIdentity(productID, publicKey));
			if (identityProductCaller != identityProductLicense)
			{
				validationFailures.Add(
					new GeneralValidationFailure()
					{
						Message = string.Format(MESSAGE_LICENSE_INVALID_PRODUCT_IDENTITY1, pathLicense),
						HowToResolve = MESSAGE_LICENSE_RESOLVE,
					}
				);
			}

			/// Optional: If an assembly hash is in the license file, test it.
			string identityAssemblyLicense = license.AdditionalAttributes.Get(Attribute_Name_AssemblyIdentity);
			if (!string.IsNullOrWhiteSpace(identityAssemblyLicense))
			{
				IsLockedToAssembly = true;
				string identityAssemblyCaller = SecureHash.ComputeSHA256HashFile(pathAssembly);
				if (identityAssemblyCaller != identityAssemblyLicense)
				{
					validationFailures.Add(
						new GeneralValidationFailure()
						{
							Message = string.Format(MESSAGE_LICENSE_INVALID_PRODUCT_INSTANCE2, pathLicense, pathAssembly),
							HowToResolve = MESSAGE_LICENSE_RESOLVE,
						}
					);
				}
			}

			string expirationDays = license.AdditionalAttributes.Get(Attribute_Name_ExpirationDays);
			IEnumerable<IValidationFailure> loadErrors =
				license
					.Validate()
					// ExpirationDate() compares local calendar dates (date-only semantics).
					// Use today's local date directly. The license is invalid on the expiration date.
					.ExpirationDate(DateTime.SpecifyKind(DateOnly.FromDateTime(MyNow.Now()).ToDateTime(new()), DateTimeKind.Local))
					.When(lic => !string.IsNullOrEmpty(expirationDays))
					// Only check the expiry WHEN the license is Trial.
					// https://github.com/junian/Standard.Licensing/issues/21
					//.When(lic => lic.Type == LicenseType.Trial)
					.And()
					.ProductBuildDate([ Assembly.GetExecutingAssembly() ])
					.And()
					.Signature(publicKey)
					.AssertValidLicense()
				??
				[
					new GeneralValidationFailure()
					{
						Message = MESSAGE_LICENSE_INVALID,
						HowToResolve = MESSAGE_LICENSE_RESOLVE,
					}
				];
			if (loadErrors.Any())
			{
				validationFailures.AddRange(loadErrors);
			}

			// There may be other validation failures from earlier.
			if (validationFailures.Count > 0)
			{
				List<string> errorMessages = [];
				foreach (var failure in validationFailures)
				{
					errorMessages.Add($"{failure.GetType().Name}: {failure.Message}{Environment.NewLine}{failure.HowToResolve}");
				}
				messages = string.Join(Environment.NewLine, errorMessages);
				return false;
			}

			Product = license.ProductFeatures.Get(ProductFeature_Name_Product);
			Version = license.ProductFeatures.Get(ProductFeature_Name_Version);
			string s = license.ProductFeatures.Get(ProductFeature_Name_PublishDate);
			if (!string.IsNullOrEmpty(s))
			{
				PublishDate = DateOnly.Parse(s, CultureInfo.InvariantCulture);
			}

			// Load custom product features
			ProductFeatures.Clear();
			foreach (var feature in license.ProductFeatures.GetAll())
			{
				// Skip the reserved feature names we already handle specifically
				if ((feature.Key != ProductFeature_Name_Product) &&
					 (feature.Key != ProductFeature_Name_Version) &&
					 (feature.Key != ProductFeature_Name_PublishDate))
				{
					ProductFeatures.Add(feature.Key, feature.Value);
				}
			}

			// Load custom additional attributes
			LicenseAttributes.Clear();
			foreach (var attribute in license.AdditionalAttributes.GetAll())
			{
				// Skip the reserved attribute names we already handle specifically
				if ((attribute.Key != Attribute_Name_ProductIdentity) &&
					 (attribute.Key != Attribute_Name_AssemblyIdentity) &&
					 (attribute.Key != Attribute_Name_ExpirationDays))
				{
					LicenseAttributes.Add(attribute.Key, attribute.Value);
				}
			}

			StandardOrTrial = license.Type;
			// If the expiration date is set, get the number of days REMAINING until expiry.
			if (license.Expiration.Date != DateTime.MaxValue.Date)
			{
				// Expiration is interpreted as date-only; take the calendar date component.
				ExpirationDate = DateOnly.FromDateTime(license.Expiration);
				ExpirationDays = (int)(ExpirationDate.ToDateTime(new()) - DateOnly.FromDateTime(MyNow.Now()).ToDateTime(new())).TotalDays;
			}
			/// This is the number of days until expiration ORIGINALLY specified.
			//if (!string.IsNullOrEmpty(expirationDays))
			//{
			//	ExpirationDays = Convert.ToInt32(expirationDays);
			//}

			Quantity = license.Quantity;

			Name = license.Customer.Name;
			Email = license.Customer.Email;
			Company = license.Customer.Company ?? string.Empty;

			messages = string.Empty;
			return true;
		}
		catch (FileNotFoundException ex)
		{
			messages = ex.Message;
			return false;
		}
		catch (Exception ex)
		{
			messages = ex.Message;
			return false;
		}
	}

	private static string GetLicensePath() => Path.ChangeExtension(GetExePath(), FileExtension_License);

	/// <summary>
	/// Return the path to the main (entry) assembly (.exe/.dll).
	/// </summary>
	/// <example>
	/// C:\Path\To\Executable.exe
	/// </example>
	/// <see cref="Assembly.GetEntryAssembly" />
	/// <seealso cref="Assembly.GetExecutingAssembly" />
	/// <returns>Path to the main (entry) assembly (.exe)</returns>
	private static string GetAssemblyPath() => GetExePath();

	/// <summary>
	/// Return the full path to the running EXE.
	/// Uses <see cref="AppContext.BaseDirectory"/> for the directory (always reliable,
	/// even in single-file publish) and derives the filename from
	/// <see cref="Environment.ProcessPath"/> or <see cref="Assembly.Location"/> as a fallback.
	/// </summary>
	private static string GetExePath()
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

	/// <summary>
	/// Gets a product feature value by its name.
	/// </summary>
	/// <param name="featureName">Name of the feature to retrieve.</param>
	/// <returns>The value of the requested feature.</returns>
	/// <exception cref="ArgumentException">Thrown when the feature does not exist.</exception>
	public string GetProductFeature(string featureName)
	{
		if (ProductFeatures.TryGetValue(featureName, out string? value))
		{
			return value;
		}

		throw new ArgumentException($"Product feature '{featureName}' does not exist in this license.", nameof(featureName));
	}

	/// <summary>
	/// Checks if the license contains a specific product feature.
	/// </summary>
	/// <param name="featureName">Name of the feature to check.</param>
	/// <returns>True if the feature exists, otherwise false.</returns>
	public bool HasProductFeature(string featureName) => ProductFeatures.ContainsKey(featureName);

	/// <summary>
	/// Gets an license attribute value by its name.
	/// </summary>
	/// <param name="attributeName">Name of the attribute to retrieve.</param>
	/// <returns>The value of the requested attribute.</returns>
	/// <exception cref="ArgumentException">Thrown when the attribute does not exist.</exception>
	public string GetLicenseAttribute(string attributeName)
	{
		if (LicenseAttributes.TryGetValue(attributeName, out string? value))
		{
			return value;
		}

		throw new ArgumentException($"License attribute '{attributeName}' does not exist in this license.", nameof(attributeName));
	}

	/// <summary>
	/// Checks if the license contains a specific license attribute.
	/// </summary>
	/// <param name="attributeName">Name of the attribute to check.</param>
	/// <returns>True if the attribute exists, otherwise false.</returns>
	public bool HasLicenseAttribute(string attributeName) => LicenseAttributes.ContainsKey(attributeName);
}
