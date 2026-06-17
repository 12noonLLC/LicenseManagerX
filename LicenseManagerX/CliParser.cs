using Shared;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;

namespace LicenseManagerX;

/// <summary>
/// Provides command-line parsing and execution for LicenseManagerX.
/// </summary>
/// <remarks>
/// LicenseManagerX.exe
/// ├── version
/// ├── help
/// ├── keypair
/// │   ├── create
/// │   ├── update
/// │   └── show
/// └── license
///     ├── create
///     ├── update
///     └── show
/// </remarks>
public static class CliParser
{
	public static RootCommand Test_BuildRootCommand() => BuildRootCommand();

	private static Action<LicenseOption> KeypairCreateCommand    = _ => throw new NotImplementedException("Stub: create keypair file.");
	private static Action<LicenseOption> KeypairUpdateCommand    = _ => throw new NotImplementedException("Stub: update keypair file.");
	private static Action<FileInfo>      KeypairShowCommand      =     _ => throw new NotImplementedException("Stub: display keypair properties.");
	private static Action<LicenseOption> LicenseCreateCommand    = _ => throw new NotImplementedException("Stub: create license file.");
	private static Action<LicenseOption> LicenseUpdateCommand    = _ => throw new NotImplementedException("Stub: update license file.");
	private static Action<FileInfo, FileInfo> LicenseShowCommand = (_, _) => throw new NotImplementedException("Stub: display license properties.");

	private static readonly ApplicationInformation AppInfo = new();

	internal static int RunCommand(
		string[] args,
		Action<LicenseOption>      keypairCreateCmd,
		Action<LicenseOption>      keypairUpdateCmd,
		Action<FileInfo>           keypairShowCmd,
		Action<LicenseOption>      licenseCreateCmd,
		Action<LicenseOption>      licenseUpdateCmd,
		Action<FileInfo, FileInfo> licenseShowCmd)
	{
		KeypairCreateCommand = keypairCreateCmd;
		KeypairUpdateCommand = keypairUpdateCmd;
		KeypairShowCommand   = keypairShowCmd;
		LicenseCreateCommand = licenseCreateCmd;
		LicenseUpdateCommand = licenseUpdateCmd;
		LicenseShowCommand   = licenseShowCmd;

		// Build command structure
		RootCommand root = BuildRootCommand();

		// Check for parsing errors
		ParseResult parseResult = root.Parse(args);
		if (parseResult.Errors.Count > 0)
		{
			foreach (ParseError error in parseResult.Errors)
			{
				Console.Error.WriteLine(error.Message);
			}

			return 1;
		}

		// Run command
		return parseResult.Invoke();
	}

	private static RootCommand BuildRootCommand()
	{
		RootCommand root = new("Create, update, or display the properties of a keypair file or a license file.");
		root.Subcommands.Add(BuildVersionCommand());
		root.Subcommands.Add(BuildKeypairCommand());
		root.Subcommands.Add(BuildLicenseCommand());

		// Extend help with examples and version/copyright information
		CustomHelpAction.AttachCustomHelp(root);

		return root;
	}

	private static Command BuildVersionCommand()
	{
		Command version = new("version");
		version.Description = "Show version information.";
		version.SetAction(_ => Console.WriteLine($"{AppInfo.Name} {AppInfo.VersionShort} by {AppInfo.Company}"));
		return version;
	}

	private static Command BuildKeypairCommand()
	{
		Command keypair = new("keypair");
		keypair.Description = "Create, update, or display keypair file properties.";
		keypair.Options.Add(new HelpOption());
		keypair.Subcommands.Add(BuildKeypairCreateCommand());
		keypair.Subcommands.Add(BuildKeypairUpdateCommand());
		keypair.Subcommands.Add(BuildKeypairShowCommand());
		return keypair;
	}

	private static Command BuildLicenseCommand()
	{
		Command license = new("license");
		license.Description = "Create, update, or display license file properties.";
		license.Options.Add(new HelpOption());
		license.Subcommands.Add(BuildLicenseCreateCommand());
		license.Subcommands.Add(BuildLicenseUpdateCommand());
		license.Subcommands.Add(BuildLicenseShowCommand());
		return license;
	}

	private static Command BuildKeypairCreateCommand()
	{
		KeypairCreateCommandOptions options = CliParserOptions.BuildKeypairCreateOptions();
		Command create = new("create");
		create.Description = "Create a keypair file.";
		create.Arguments.Add(options.KeypairPathArgument);
		create.Options.Add(options.PassphraseOption);
		create.Options.Add(options.ProductIdOption);
		create.Options.Add(options.ProductNameOption);
		AddUserIdentityOptions(create, options.UserIdentityOptions);
		AddMetadataOptions(create, options.LicenseMetadataOptions);
		AddKeypairPathValidator(create, options.KeypairPathArgument, mustExist: false);
		create.SetAction(parseResult =>
		{
			KeypairCreateOptions model = CliParserOptions.BindKeypairCreateOptions(parseResult, options);
			KeypairCreateCommand(ToLicenseOption(model));
		});
		return create;
	}

	private static Command BuildKeypairUpdateCommand()
	{
		KeypairUpdateCommandOptions options = CliParserOptions.BuildKeypairUpdateOptions();
		Command update = new("update");
		update.Description = "Update a keypair file.";
		update.Arguments.Add(options.KeypairPathArgument);
		AddMetadataOptions(update, options.LicenseMetadataOptions);
		AddKeypairPathValidator(update, options.KeypairPathArgument, mustExist: true);
		update.SetAction(parseResult =>
		{
			KeypairUpdateOptions model = CliParserOptions.BindKeypairUpdateOptions(parseResult, options);
			KeypairUpdateCommand(ToLicenseOption(model));
		});
		return update;
	}

	private static Command BuildKeypairShowCommand()
	{
		KeypairShowCommandOptions options = CliParserOptions.BuildKeypairShowOptions();
		Command show = new("show");
		show.Description = "Display properties from a keypair file.";
		show.Arguments.Add(options.KeypairPathArgument);
		AddKeypairPathValidator(show, options.KeypairPathArgument, mustExist: true);
		show.SetAction(parseResult =>
		{
			KeypairShowOptions model = CliParserOptions.BindKeypairShowOptions(parseResult, options);
			KeypairShowCommand(model.KeypairPath);
		});
		return show;
	}

	private static Command BuildLicenseCreateCommand()
	{
		LicenseCreateCommandOptions options = CliParserOptions.BuildLicenseCreateOptions();
		options.LicensePathOption.Required = true;

		Command create = new("create");
		create.Description = "Create a license file based on a keypair file.";
		create.Arguments.Add(options.KeypairPathArgument);
		create.Options.Add(options.LicensePathOption);
		AddMetadataOptions(create, options.LicenseMetadataOptions);
		AddKeypairPathValidator(create, options.KeypairPathArgument, mustExist: true);
		AddLicensePathValidator(create, options.LicensePathOption, mustExist: false);
		create.SetAction(parseResult =>
		{
			LicenseCreateOptions model = CliParserOptions.BindLicenseCreateOptions(parseResult, options);
			LicenseCreateCommand(ToLicenseOption(model));
		});
		return create;
	}

	private static Command BuildLicenseUpdateCommand()
	{
		LicenseUpdateCommandOptions options = CliParserOptions.BuildLicenseUpdateOptions();
		options.LicensePathOption.Required = true;

		Command update = new("update");
		update.Description = "Update a license file.";
		update.Arguments.Add(options.KeypairPathArgument);
		update.Options.Add(options.LicensePathOption);
		AddMetadataOptions(update, options.LicenseMetadataOptions);
		AddKeypairPathValidator(update, options.KeypairPathArgument, mustExist: true);
		AddLicensePathValidator(update, options.LicensePathOption, mustExist: true);
		update.SetAction(parseResult =>
		{
			LicenseUpdateOptions model = CliParserOptions.BindLicenseUpdateOptions(parseResult, options);
			LicenseUpdateCommand(ToLicenseOption(model));
		});
		return update;
	}

	private static Command BuildLicenseShowCommand()
	{
		LicenseShowCommandOptions options = CliParserOptions.BuildLicenseShowOptions();
		options.LicensePathOption.Required = true;

		Command show = new("show");
		show.Description = "Display properties from a license file.";
		show.Arguments.Add(options.KeypairPathArgument);
		show.Options.Add(options.LicensePathOption);
		AddKeypairPathValidator(show, options.KeypairPathArgument, mustExist: true);
		AddLicensePathValidator(show, options.LicensePathOption, mustExist: true);
		show.SetAction(parseResult =>
		{
			LicenseShowOptions model = CliParserOptions.BindLicenseShowOptions(parseResult, options);
			LicenseShowCommand(model.KeypairPath, model.LicensePath);
		});
		return show;
	}

	private static void AddMetadataOptions(Command command, LicenseMetadataOptions options)
	{
		command.Options.Add(options.ProductVersionOption);
		command.Options.Add(options.ProductPublishDateOption);
		command.Options.Add(options.ProductFeaturesOption);
		command.Options.Add(options.TypeOption);
		command.Options.Add(options.QuantityOption);
		command.Options.Add(options.ExpirationDaysOption);
		command.Options.Add(options.ExpirationDateOption);
		command.Options.Add(options.LicenseAttributesOption);
		command.Options.Add(options.LockOption);
		CliParserOptions.AddMetadataCommandValidators(command, options);
	}

	private static void AddUserIdentityOptions(Command command, UserIdentityOptions options)
	{
		command.Options.Add(options.LicenseeNameOption);
		command.Options.Add(options.LicenseeEmailOption);
		command.Options.Add(options.LicenseeCompanyOption);
	}

	private static void AddKeypairPathValidator(Command command, Argument<FileInfo> keypairPathArgument, bool mustExist)
	{
		command.Validators.Add(result =>
		{
			FileInfo? keypair = result.GetValue(keypairPathArgument);
			if (keypair is null)
			{
				return;
			}

			if (mustExist && !File.Exists(keypair.FullName))
			{
				result.AddError($"keypair file does not exist: {keypair.FullName}");
				return;
			}

			if (!mustExist && File.Exists(keypair.FullName))
			{
				result.AddError($"keypair file already exists: {keypair.FullName}");
				return;
			}
		});
	}

	private static void AddLicensePathValidator(Command command, Option<FileInfo> licensePathOption, bool mustExist)
	{
		command.Validators.Add(result =>
		{
			FileInfo? license = result.GetValue(licensePathOption);
			if (license is null)
			{
				return;
			}

			if (mustExist && !File.Exists(license.FullName))
			{
				result.AddError($"{licensePathOption.Name} file does not exist: {license.FullName}");
				return;
			}

			if (!mustExist && File.Exists(license.FullName))
			{
				result.AddError($"{licensePathOption.Name} file already exists: {license.FullName}");
				return;
			}
		});
	}

	private static LicenseOption ToLicenseOption(KeypairCreateOptions options) =>
		new(
			options.KeypairPath,
			LicensePath: null,
			options.Metadata.ProductVersion,
			options.Metadata.ProductPublishDate,
			options.Metadata.ProductFeatures,
			options.Metadata.Type,
			options.Metadata.Quantity,
			options.Metadata.ExpirationDays,
			options.Metadata.ExpirationDate,
			options.Metadata.LicenseAttributes,
			options.Metadata.LockPath,
			options.Passphrase,
			options.ProductId,
			options.ProductName,
			options.LicenseeName,
			options.LicenseeEmail,
			options.LicenseeCompany);

	private static LicenseOption ToLicenseOption(KeypairUpdateOptions options) =>
		new(
			options.KeypairPath,
			LicensePath: null,
			options.Metadata.ProductVersion,
			options.Metadata.ProductPublishDate,
			options.Metadata.ProductFeatures,
			options.Metadata.Type,
			options.Metadata.Quantity,
			options.Metadata.ExpirationDays,
			options.Metadata.ExpirationDate,
			options.Metadata.LicenseAttributes,
			options.Metadata.LockPath,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

	private static LicenseOption ToLicenseOption(LicenseCreateOptions options) =>
		new(
			options.KeypairPath,
			options.LicensePath,
			options.Metadata.ProductVersion,
			options.Metadata.ProductPublishDate,
			options.Metadata.ProductFeatures,
			options.Metadata.Type,
			options.Metadata.Quantity,
			options.Metadata.ExpirationDays,
			options.Metadata.ExpirationDate,
			options.Metadata.LicenseAttributes,
			options.Metadata.LockPath,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

	private static LicenseOption ToLicenseOption(LicenseUpdateOptions options) =>
		new(
			options.KeypairPath,
			options.LicensePath,
			options.Metadata.ProductVersion,
			options.Metadata.ProductPublishDate,
			options.Metadata.ProductFeatures,
			options.Metadata.Type,
			options.Metadata.Quantity,
			options.Metadata.ExpirationDays,
			options.Metadata.ExpirationDate,
			options.Metadata.LicenseAttributes,
			options.Metadata.LockPath,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);
}

internal sealed record LicenseOption(
	FileInfo KeypairPath,
	FileInfo? LicensePath,
	string? ProductVersion,
	DateOnly? ProductPublishDate,
	Dictionary<string, string> ProductFeatures,
	Standard.Licensing.LicenseType? Type,
	int? Quantity,
	int? ExpirationDays,
	DateOnly? ExpirationDate,
	Dictionary<string, string> LicenseAttributes,
	FileInfo? LockPath,
	string? Passphrase,
	string? ProductId,
	string? ProductName,
	string? LicenseeName,
	string? LicenseeEmail,
	string? LicenseeCompany);

internal class CustomHelpAction(HelpAction _defaultAction) : SynchronousCommandLineAction
{
	/// <summary>
	/// Replace default help action with custom one to include
	/// version and copyright information and additional examples.
	/// </summary>
	/// <remarks>
	/// Because of this, we need to add new `HelpOption` objects to
	/// the commands so that they do not also use this custom handler.
	/// There is no need to also add `HelpOption` to leaf commands
	/// because they will inherit the `HelpOption` from their parent command.
	/// </remarks>
	/// <param name="root"></param>
	internal static void AttachCustomHelp(RootCommand root)
	{
		foreach (Option option in root.Options)
		{
			if (option is HelpOption helpOption)
			{
				helpOption.Action = new CustomHelpAction((HelpAction)helpOption.Action!);
				break;
			}
		}
	}

	public override int Invoke(ParseResult parseResult)
	{
		ApplicationInformation appInfo = new();

		// Output custom header
		parseResult.InvocationConfiguration.Output.WriteLine();
		parseResult.InvocationConfiguration.Output.WriteLine($"{appInfo.Name} {appInfo.VersionShort}");
		parseResult.InvocationConfiguration.Output.WriteLine($"{appInfo.Copyright}");
		parseResult.InvocationConfiguration.Output.WriteLine(appInfo.Company);
		parseResult.InvocationConfiguration.Output.WriteLine(appInfo.WebSiteURL);
		parseResult.InvocationConfiguration.Output.WriteLine();

		// Output default help
		int result = _defaultAction.Invoke(parseResult);

		// Output custom footer
		parseResult.InvocationConfiguration.Output.WriteLine("Examples:");
		parseResult.InvocationConfiguration.Output.WriteLine("  ('lmx' is a Windows App Alias for LicenseManagerX.exe.)");
		parseResult.InvocationConfiguration.Output.WriteLine();
		parseResult.InvocationConfiguration.Output.WriteLine("  Create new keypair file:");
		parseResult.InvocationConfiguration.Output.WriteLine("    lmx keypair create MyKeypair.private --passphrase secret --product-id MyApp --product-name \"My App\" --licensee-name \"Jane User\" --licensee-email jane@example.com");
		parseResult.InvocationConfiguration.Output.WriteLine();
		parseResult.InvocationConfiguration.Output.WriteLine("  Update existing keypair file with some new property values:");
		parseResult.InvocationConfiguration.Output.WriteLine("    lmx keypair update MyKeypair.private --expiration-date 2030-10-24 --quantity 10 --type Standard");
		parseResult.InvocationConfiguration.Output.WriteLine();
		parseResult.InvocationConfiguration.Output.WriteLine("  Show keypair properties:");
		parseResult.InvocationConfiguration.Output.WriteLine("    lmx keypair show MyKeypair.private");
		parseResult.InvocationConfiguration.Output.WriteLine();
		parseResult.InvocationConfiguration.Output.WriteLine("  Create new license file based on existing keypair file:");
		parseResult.InvocationConfiguration.Output.WriteLine("    lmx license create MyKeypair.private --license MyLicense.lic");
		parseResult.InvocationConfiguration.Output.WriteLine();
		parseResult.InvocationConfiguration.Output.WriteLine("  Create new license file based on existing keypair file and make it Standard and locked to a file:");
		parseResult.InvocationConfiguration.Output.WriteLine("    lmx license create MyKeypair.private --license MyLicense.lic --type Standard --lock \"C:\\MyApp\\MyApp.exe\"");
		parseResult.InvocationConfiguration.Output.WriteLine();
		parseResult.InvocationConfiguration.Output.WriteLine("  Update existing license file with some new property values:");
		parseResult.InvocationConfiguration.Output.WriteLine("    lmx license update MyKeypair.private --license MyLicense.lic --product-version 2.31.46 --license-attributes cat=dog whale=giraffe");
		parseResult.InvocationConfiguration.Output.WriteLine();
		parseResult.InvocationConfiguration.Output.WriteLine("  Show license properties:");
		parseResult.InvocationConfiguration.Output.WriteLine("    lmx license show MyKeypair.private --license MyLicense.lic");
		parseResult.InvocationConfiguration.Output.WriteLine();

		return result;
	}
}
