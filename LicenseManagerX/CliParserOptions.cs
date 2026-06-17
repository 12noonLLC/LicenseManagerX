using CommunityToolkit.Diagnostics;
using Standard.Licensing;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LicenseManagerX;

internal static class CliParserOptions
{
	internal static KeypairCreateCommandOptions BuildKeypairCreateOptions()
	{
		CommonFileOptions fileOptions = BuildCommonPathOptions();
		LicenseMetadataOptions metadataOptions = BuildLicenseMetadataOptions();
		UserIdentityOptions userIdentityOptions = BuildUserIdentityOptions();

		Option<string> passphraseOption = new("--passphrase");
		passphraseOption.Description = "Passphrase used to protect the keypair.";
		passphraseOption.Required = true;

		Option<string> productIdOption = new("--product-id");
		productIdOption.Description = "Product ID.";
		productIdOption.Required = true;

		Option<string> productNameOption = new("--product-name");
		productNameOption.Description = "Product name.";
		productNameOption.Required = true;

		userIdentityOptions.LicenseeNameOption.Required = true;
		userIdentityOptions.LicenseeEmailOption.Required = true;

		return new KeypairCreateCommandOptions(
			fileOptions.KeypairPathArgument,
			passphraseOption,
			productIdOption,
			productNameOption,
			userIdentityOptions,
			metadataOptions);
	}

	internal static KeypairUpdateCommandOptions BuildKeypairUpdateOptions()
	{
		CommonFileOptions fileOptions = BuildCommonPathOptions();
		return new KeypairUpdateCommandOptions(
			fileOptions.KeypairPathArgument,
			BuildLicenseMetadataOptions());
	}

	internal static KeypairShowCommandOptions BuildKeypairShowOptions()
	{
		CommonFileOptions fileOptions = BuildCommonPathOptions();
		return new KeypairShowCommandOptions(fileOptions.KeypairPathArgument);
	}

	internal static LicenseCreateCommandOptions BuildLicenseCreateOptions()
	{
		CommonFileOptions fileOptions = BuildCommonPathOptions();
		return new LicenseCreateCommandOptions(
			fileOptions.KeypairPathArgument,
			fileOptions.LicensePathOption,
			BuildLicenseMetadataOptions());
	}

	internal static LicenseUpdateCommandOptions BuildLicenseUpdateOptions()
	{
		CommonFileOptions fileOptions = BuildCommonPathOptions();
		return new LicenseUpdateCommandOptions(
			fileOptions.KeypairPathArgument,
			fileOptions.LicensePathOption,
			BuildLicenseMetadataOptions());
	}

	internal static LicenseShowCommandOptions BuildLicenseShowOptions()
	{
		CommonFileOptions fileOptions = BuildCommonPathOptions();
		return new LicenseShowCommandOptions(fileOptions.KeypairPathArgument, fileOptions.LicensePathOption);
	}

	internal static CommonFileOptions BuildCommonPathOptions()
	{
		Argument<FileInfo> keypairPathArgument = new("keypair");
		keypairPathArgument.Description = "Path to the keypair file.";
		keypairPathArgument.AcceptLegalFilePathsOnly();

		Option<FileInfo> licensePathOption = new("--license");
		licensePathOption.Description = "Path to the license file.";
		licensePathOption.AcceptLegalFilePathsOnly();

		return new CommonFileOptions(keypairPathArgument, licensePathOption);
	}

	internal static LicenseMetadataOptions BuildLicenseMetadataOptions()
	{
		Option<string?> productVersionOption = new("--product-version", "-pv");
		productVersionOption.Description = "Product version.";

		Option<DateOnly?> productPublishDateOption = new("--product-publish-date", "-pd");
		productPublishDateOption.Description = $"Product publish date in {LicenseManager.DateFormat_Expiration} format.";
		productPublishDateOption.Validators.Add(CreateDateOnlyValidator(productPublishDateOption));

		Option<Dictionary<string, string>?> productFeaturesOption = new("--product-features", "-pf");
		productFeaturesOption.Description = "Product features as key=value pairs separated by spaces.";
		productFeaturesOption.AllowMultipleArgumentsPerToken = true;
		productFeaturesOption.CustomParser = ParseKeyValuePairs;
		productFeaturesOption.Validators.Add(CreateReservedNameValidator(productFeaturesOption, LicenseManager.IsReservedFeatureName, "product feature"));

		Option<LicenseType?> typeOption = new("--type", "-t");
		typeOption.Description = $"License type: {string.Join(", ", Enum.GetNames<LicenseType>())}";
		typeOption.AcceptOnlyFromAmong(Enum.GetNames<LicenseType>());

		Option<int?> quantityOption = new("--quantity", "-q");
		quantityOption.Description = "License quantity (positive integer).";
		quantityOption.Validators.Add(CreateMinimumIntValidator(quantityOption, minimum: 1, $"--{quantityOption.Name} must be a positive integer."));

		Option<int?> expirationDaysOption = new("--expiration-days", "-dy");
		expirationDaysOption.Description = "Expiration in days (0 = no expiry).";
		expirationDaysOption.Validators.Add(CreateMinimumIntValidator(expirationDaysOption, minimum: 0, $"--{expirationDaysOption.Name} must be 0 or greater."));

		Option<DateOnly?> expirationDateOption = new("--expiration-date", "-dt");
		expirationDateOption.Description = $"Expiration date in {LicenseManager.DateFormat_Expiration} format.";
		expirationDateOption.Validators.Add(CreateDateOnlyValidator(expirationDateOption));

		Option<Dictionary<string, string>?> licenseAttributesOption = new("--license-attributes", "-la");
		licenseAttributesOption.Description = "License attributes as key=value pairs separated by spaces.";
		licenseAttributesOption.AllowMultipleArgumentsPerToken = true;
		licenseAttributesOption.CustomParser = ParseKeyValuePairs;
		licenseAttributesOption.Validators.Add(CreateReservedNameValidator(licenseAttributesOption, LicenseManager.IsReservedAttributeName, "license attribute"));

		Option<FileInfo> lockOption = new("--lock");
		lockOption.Description = "Lock license to a specific file.";
		lockOption.AcceptExistingOnly();

		return new LicenseMetadataOptions(
			productVersionOption,
			productPublishDateOption,
			productFeaturesOption,
			typeOption,
			quantityOption,
			expirationDaysOption,
			expirationDateOption,
			licenseAttributesOption,
			lockOption);
	}

	internal static UserIdentityOptions BuildUserIdentityOptions()
	{
		Option<string?> licenseeNameOption = new("--licensee-name");
		licenseeNameOption.Description = "Licensee name.";

		Option<string?> licenseeEmailOption = new("--licensee-email");
		licenseeEmailOption.Description = "Licensee email.";

		Option<string?> licenseeCompanyOption = new("--licensee-company");
		licenseeCompanyOption.Description = "Licensee company.";

		return new UserIdentityOptions(licenseeNameOption, licenseeEmailOption, licenseeCompanyOption);
	}

	internal static void AddMetadataCommandValidators(Command command, LicenseMetadataOptions metadataOptions)
	{
		command.Validators.Add(result =>
		{
			int? expirationDays = result.GetValue(metadataOptions.ExpirationDaysOption);
			DateOnly? expirationDate = result.GetValue(metadataOptions.ExpirationDateOption);
			if (expirationDays is not null && expirationDate is not null)
			{
				result.AddError($"{metadataOptions.ExpirationDaysOption.Name} cannot be combined with {metadataOptions.ExpirationDateOption.Name}.");
			}
		});
	}

	internal static KeypairCreateOptions BindKeypairCreateOptions(ParseResult parseResult, KeypairCreateCommandOptions options)
	{
		FileInfo? keypairPath = parseResult.GetValue(options.KeypairPathArgument);
		Guard.IsNotNull(keypairPath, nameof(keypairPath));

		string? passphrase = parseResult.GetValue(options.PassphraseOption);
		Guard.IsNotNull(passphrase, nameof(passphrase));

		string? productId = parseResult.GetValue(options.ProductIdOption);
		Guard.IsNotNull(productId, nameof(productId));

		string? productName = parseResult.GetValue(options.ProductNameOption);
		Guard.IsNotNull(productName, nameof(productName));

		string? licenseeName = parseResult.GetValue(options.UserIdentityOptions.LicenseeNameOption);
		Guard.IsNotNull(licenseeName, nameof(licenseeName));

		string? licenseeEmail = parseResult.GetValue(options.UserIdentityOptions.LicenseeEmailOption);
		Guard.IsNotNull(licenseeEmail, nameof(licenseeEmail));

		return new KeypairCreateOptions(
			keypairPath,
			passphrase,
			productId,
			productName,
			licenseeName,
			licenseeEmail,
			parseResult.GetValue(options.UserIdentityOptions.LicenseeCompanyOption),
			BuildLicenseMetadataValues(parseResult, options.LicenseMetadataOptions));
	}

	internal static KeypairUpdateOptions BindKeypairUpdateOptions(ParseResult parseResult, KeypairUpdateCommandOptions options)
	{
		FileInfo? keypairPath = parseResult.GetValue(options.KeypairPathArgument);
		Guard.IsNotNull(keypairPath, nameof(keypairPath));

		return new KeypairUpdateOptions(
			keypairPath,
			BuildLicenseMetadataValues(parseResult, options.LicenseMetadataOptions));
	}

	internal static KeypairShowOptions BindKeypairShowOptions(ParseResult parseResult, KeypairShowCommandOptions options)
	{
		FileInfo? keypairPath = parseResult.GetValue(options.KeypairPathArgument);
		Guard.IsNotNull(keypairPath, nameof(keypairPath));
		return new KeypairShowOptions(keypairPath);
	}

	internal static LicenseCreateOptions BindLicenseCreateOptions(ParseResult parseResult, LicenseCreateCommandOptions options)
	{
		FileInfo? keypairPath = parseResult.GetValue(options.KeypairPathArgument);
		Guard.IsNotNull(keypairPath, nameof(keypairPath));

		FileInfo? licensePath = parseResult.GetValue(options.LicensePathOption);
		Guard.IsNotNull(licensePath, nameof(licensePath));

		return new LicenseCreateOptions(keypairPath, licensePath, BuildLicenseMetadataValues(parseResult, options.LicenseMetadataOptions));
	}

	internal static LicenseUpdateOptions BindLicenseUpdateOptions(ParseResult parseResult, LicenseUpdateCommandOptions options)
	{
		FileInfo? keypairPath = parseResult.GetValue(options.KeypairPathArgument);
		Guard.IsNotNull(keypairPath, nameof(keypairPath));

		FileInfo? licensePath = parseResult.GetValue(options.LicensePathOption);
		Guard.IsNotNull(licensePath, nameof(licensePath));

		return new LicenseUpdateOptions(keypairPath, licensePath, BuildLicenseMetadataValues(parseResult, options.LicenseMetadataOptions));
	}

	internal static LicenseShowOptions BindLicenseShowOptions(ParseResult parseResult, LicenseShowCommandOptions options)
	{
		FileInfo? keypairPath = parseResult.GetValue(options.KeypairPathArgument);
		Guard.IsNotNull(keypairPath, nameof(keypairPath));

		FileInfo? licensePath = parseResult.GetValue(options.LicensePathOption);
		Guard.IsNotNull(licensePath, nameof(licensePath));

		return new LicenseShowOptions(keypairPath, licensePath);
	}

	private static LicenseMetadataValues BuildLicenseMetadataValues(ParseResult parseResult, LicenseMetadataOptions options) =>
		new(
			parseResult.GetValue(options.ProductVersionOption),
			parseResult.GetValue(options.ProductPublishDateOption),
			parseResult.GetValue(options.ProductFeaturesOption) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
			parseResult.GetValue(options.TypeOption),
			parseResult.GetValue(options.QuantityOption),
			parseResult.GetValue(options.ExpirationDaysOption),
			parseResult.GetValue(options.ExpirationDateOption),
			parseResult.GetValue(options.LicenseAttributesOption) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
			parseResult.GetValue(options.LockOption)
		);

	private static Action<SymbolResult> CreateDateOnlyValidator(Option<DateOnly?> option)
	{
		return result =>
		{
			DateOnly? dateValue = result.GetValue(option);
			if (dateValue is null)
			{
				return;
			}

			string rendered = dateValue.Value.ToString(LicenseManager.DateFormat_Expiration, CultureInfo.InvariantCulture);
			if (!DateOnly.TryParseExact(rendered, LicenseManager.DateFormat_Expiration, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
			{
				result.AddError($"Date-only must be in the format {LicenseManager.DateFormat_Expiration} (got: {rendered})");
			}
		};
	}

	private static Action<SymbolResult> CreateMinimumIntValidator(Option<int?> option, int minimum, string message)
	{
		return result =>
		{
			int? value = result.GetValue(option);
			if (value is not null && (value < minimum))
			{
				result.AddError(message);
			}
		};
	}

	private static Action<SymbolResult> CreateReservedNameValidator(
		Option<Dictionary<string, string>?> option,
		Func<string, bool> isReserved,
		string noun)
	{
		return result =>
		{
			Dictionary<string, string>? values;
			try
			{
				values = result.GetValue(option);
			}
			catch (InvalidOperationException)
			{
				return;
			}

			if (values is null)
			{
				return;
			}

			foreach (KeyValuePair<string, string> pair in values)
			{
				if (isReserved(pair.Key))
				{
					result.AddError($"--{option.Name} contains the reserved {noun} name '{pair.Key}' and cannot be used.");
				}
			}
		};
	}

	private static Dictionary<string, string>? ParseKeyValuePairs(ArgumentResult result)
	{
		if (result.Tokens.Count == 0)
		{
			return null;
		}

		Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

		foreach (string token in result.Tokens.Select(t => t.Value))
		{
			int equalsIndex = token.IndexOf('=');
			if ((equalsIndex <= 0) || (equalsIndex == token.Length - 1))
			{
				result.AddError($"--{result.Argument.Name} contains an invalid pair '{token}'. Expected key=value.");
				continue;
			}

			string key = token[..equalsIndex].Trim();
			string value = token[(equalsIndex + 1)..].Trim();

			if (key.Length == 0)
			{
				result.AddError($"--{result.Argument.Name} contains an invalid pair '{token}'. Key cannot be empty.");
				continue;
			}

			if (value.Length == 0)
			{
				result.AddError($"--{result.Argument.Name} contains an invalid pair '{token}'. Value cannot be empty.");
				continue;
			}

			if (!values.TryAdd(key, value))
			{
				result.AddError($"--{result.Argument.Name} contains a duplicate key '{key}'.");
				continue;
			}
		}

		return values;
	}
}

internal sealed record CommonFileOptions(
	Argument<FileInfo> KeypairPathArgument,
	Option<FileInfo> LicensePathOption);

internal sealed record LicenseMetadataOptions(
	Option<string?> ProductVersionOption,
	Option<DateOnly?> ProductPublishDateOption,
	Option<Dictionary<string, string>?> ProductFeaturesOption,
	Option<LicenseType?> TypeOption,
	Option<int?> QuantityOption,
	Option<int?> ExpirationDaysOption,
	Option<DateOnly?> ExpirationDateOption,
	Option<Dictionary<string, string>?> LicenseAttributesOption,
	Option<FileInfo> LockOption);

internal sealed record UserIdentityOptions(
	Option<string?> LicenseeNameOption,
	Option<string?> LicenseeEmailOption,
	Option<string?> LicenseeCompanyOption);

internal sealed record KeypairCreateCommandOptions(
	Argument<FileInfo> KeypairPathArgument,
	Option<string> PassphraseOption,
	Option<string> ProductIdOption,
	Option<string> ProductNameOption,
	UserIdentityOptions UserIdentityOptions,
	LicenseMetadataOptions LicenseMetadataOptions);

internal sealed record KeypairUpdateCommandOptions(
	Argument<FileInfo> KeypairPathArgument,
	LicenseMetadataOptions LicenseMetadataOptions);

internal sealed record KeypairShowCommandOptions(Argument<FileInfo> KeypairPathArgument);

internal sealed record LicenseCreateCommandOptions(
	Argument<FileInfo> KeypairPathArgument,
	Option<FileInfo> LicensePathOption,
	LicenseMetadataOptions LicenseMetadataOptions);

internal sealed record LicenseUpdateCommandOptions(
	Argument<FileInfo> KeypairPathArgument,
	Option<FileInfo> LicensePathOption,
	LicenseMetadataOptions LicenseMetadataOptions);

internal sealed record LicenseShowCommandOptions(
	Argument<FileInfo> KeypairPathArgument,
	Option<FileInfo> LicensePathOption);

internal sealed record LicenseMetadataValues(
	string? ProductVersion,
	DateOnly? ProductPublishDate,
	Dictionary<string, string> ProductFeatures,
	LicenseType? Type,
	int? Quantity,
	int? ExpirationDays,
	DateOnly? ExpirationDate,
	Dictionary<string, string> LicenseAttributes,
	FileInfo? LockPath);

internal sealed record KeypairCreateOptions(
	FileInfo KeypairPath,
	string Passphrase,
	string ProductId,
	string ProductName,
	string LicenseeName,
	string LicenseeEmail,
	string? LicenseeCompany,
	LicenseMetadataValues Metadata);

internal sealed record KeypairUpdateOptions(
	FileInfo KeypairPath,
	LicenseMetadataValues Metadata);

internal sealed record KeypairShowOptions(FileInfo KeypairPath);

internal sealed record LicenseCreateOptions(
	FileInfo KeypairPath,
	FileInfo LicensePath,
	LicenseMetadataValues Metadata);

internal sealed record LicenseUpdateOptions(
	FileInfo KeypairPath,
	FileInfo LicensePath,
	LicenseMetadataValues Metadata);

internal sealed record LicenseShowOptions(
	FileInfo KeypairPath,
	FileInfo LicensePath);
