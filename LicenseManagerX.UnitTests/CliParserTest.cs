using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Licensing;
using System.CommandLine;

namespace LicenseManagerX.UnitTests;

[TestClass]
public class CliParserTest
{
	public TestContext TestContext { get; private set; }

	private static string PathTestFolder = string.Empty;

	private string PathLicenseFile = string.Empty;
	private string PathKeypairFile = string.Empty;
	private string PathLockFile = string.Empty;

	[ClassInitialize]
	public static void ClassSetup(TestContext testContext)
	{
		PathTestFolder = Path.Combine(testContext.TestRunResultsDirectory ?? Path.GetTempPath(), testContext.FullyQualifiedTestClassName);
		LicenseManager.EnsureParentDirectoryExists(Path.Combine(PathTestFolder, "_"));
	}

	[ClassCleanup]
	public static void ClassTeardown()
	{
	}

	[TestInitialize]
	public void TestSetup()
	{
		PathLicenseFile = Path.Combine(PathTestFolder, TestContext.TestName + LicenseManager.FileExtension_License);
		PathKeypairFile = Path.Combine(PathTestFolder, TestContext.TestName + LicenseManager.FileExtension_PrivateKey);
		PathLockFile = Path.Combine(PathTestFolder, TestContext.TestName + ".lock.txt");
	}

	[TestCleanup]
	public void TestTeardown()
	{
		File.Delete(PathLicenseFile);
		File.Delete(PathKeypairFile);
		File.Delete(PathLockFile);

		// Reset culture to English
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
	}


	// ===== PARSING TESTS =====

	[TestMethod]
	public void TestParseBasicCreateCommand()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseUpdateCommandWithLicense()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		CreateValidLicenseFile(PathKeypairFile, PathLicenseFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "update", PathKeypairFile, "--license", PathLicenseFile, "--quantity", "5"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseUpdateCommandWithoutLicense()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["keypair", "update", PathKeypairFile, "--product-version", "2.0.0"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseShowCommand()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["keypair", "show", PathKeypairFile];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseShowCommandWithLicense()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		CreateValidLicenseFile(PathKeypairFile, PathLicenseFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "show", PathKeypairFile, "--license", PathLicenseFile];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseVersionCommand()
	{
		// Arrange
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["version"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseAllOptions()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--type", "Trial",
			"--quantity", "5",
			"--expiration-days", "30",
			"--product-version", "2.1.0",
			"--product-publish-date", "2023-12-01",
		];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseShortOptions()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"-t", "Trial",
			"-q", "5",
			"-dy", "30",
			"-pv", "2.1.0",
			"-pd", "2023-12-01",
		];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	// ===== VALIDATION TESTS =====

	[TestMethod]
	public void TestParseInvalidLicenseType()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--type", "Invalid"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
	}

	[TestMethod]
	public void TestParseInvalidQuantity()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--quantity", "0"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("--quantity"), result.Errors);
	}

	[TestMethod]
	public void TestParseNegativeExpirationDays()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--expiration-days", "-1"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("--expiration-days"), result.Errors);
	}

	[TestMethod]
	public void TestParseMissingKeypairArgument()
	{
		// Arrange
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", "--license", PathLicenseFile];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
	}

	[TestMethod]
	public void TestParseNonexistentKeypairFile()
	{
		// Arrange
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", "nonexistent.private", "--license", PathLicenseFile];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("does not exist"), result.Errors);
	}

	[TestMethod]
	public void TestParseCreateWithExistingLicenseFile()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		File.WriteAllText(PathLicenseFile, "Existing license file");
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("already exists"), result.Errors);
	}

	[TestMethod]
	public void TestParseUpdateWithNonexistentLicenseFile()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "update", PathKeypairFile, "--license", PathLicenseFile];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("does not exist"), result.Errors);
	}

	[TestMethod]
	public void TestParseExpirationDaysZeroWithExpirationDate()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--expiration-days", "0", "--expiration-date", "2025-12-31"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("--expiration-days cannot be combined with --expiration-date"), result.Errors);
	}

	// ===== KEY-VALUE PAIR PARSING TESTS =====

	[TestMethod]
	public void TestParseProductFeatures()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--product-features", "Color=Blue", "Bird=Heron", "Edition=Pro"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseLicenseAttributes()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--license-attributes", "Size=Large", "Department=Engineering", "Location=Seattle"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseKeyValuePairs_InvalidFormat1()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--product-features", "InvalidPair"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("Expected key=value") || e.Message.Contains("invalid pair"), result.Errors);
	}

	[TestMethod]
	public void TestParseKeyValuePairs_InvalidFormat2()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--product-features", "=Value"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("Key cannot be empty") || e.Message.Contains("Expected key=value") || e.Message.Contains("invalid pair"), result.Errors);
	}

	[TestMethod]
	public void TestParseKeyValuePairs_EmptyValue()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--product-features", "Key1="];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("Value cannot be empty") || e.Message.Contains("Expected key=value") || e.Message.Contains("invalid pair"), result.Errors);
	}

	// ===== LOCK OPTION TESTS =====

	[TestMethod]
	public void TestParseLockOption()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		File.WriteAllText(PathLockFile, "lock content");
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--lock", PathLockFile];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseLockOption_NonexistentFile()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--lock", "nonexistent.lock"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsTrue(result.Errors.Any());
		Assert.Contains(e => e.Message.Contains("File not found") || e.Message.Contains("does not exist"), result.Errors);
	}

	// ===== DATE PARSING TESTS =====

	[TestMethod]
	public void TestParseProductPublishDate()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--product-publish-date", "2023-12-01"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	[TestMethod]
	public void TestParseExpirationDate()
	{
		// Arrange
		CreateValidKeypairFile(PathKeypairFile);
		RootCommand root = CliParser.Test_BuildRootCommand();
		string[] args = ["license", "create", PathKeypairFile, "--license", PathLicenseFile, "--expiration-date", "2025-12-31"];

		// Act
		ParseResult result = root.Parse(args);

		// Assert
		Assert.IsFalse(result.Errors.Any(), $"Parse errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
	}

	// ===== APPLY OVERRIDES TESTS (CliCommands) =====

	[TestMethod]
	public void TestApplyOverrides()
	{
		// Arrange
		LicenseManager manager = new()
		{
			StandardOrTrial = LicenseType.Standard,
			Quantity = 1,
			ExpirationDays = 0,
			Version = "1.0.0",
		};

		LicenseOption model = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: "2.1.0",
			ProductPublishDate: new DateOnly(2023, 12, 1),
			ProductFeatures: [],
			Type: LicenseType.Trial,
			Quantity: 5,
			ExpirationDays: 30,
			ExpirationDate: null,
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act
		CliCommands.ApplyOverrides(manager, model);

		// Assert
		Assert.AreEqual(LicenseType.Trial, manager.StandardOrTrial);
		Assert.AreEqual(5, manager.Quantity);
		Assert.AreEqual(30, manager.ExpirationDays);
		Assert.AreEqual("2.1.0", manager.Version);
		Assert.AreEqual(new DateOnly(2023, 12, 1), manager.PublishDate);
	}

	[TestMethod]
	public void TestApplyExpirationDateOverride()
	{
		// Arrange
		LicenseManager manager = new();
		DateOnly today = DateOnly.FromDateTime(MyNow.Now());
		DateOnly expirationDate = today.AddDays(45);

		LicenseOption model = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: null,
			ProductPublishDate: null,
			ProductFeatures: [],
			Type: null,
			Quantity: null,
			ExpirationDays: null,
			ExpirationDate: expirationDate,
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act
		CliCommands.ApplyOverrides(manager, model);

		// Assert
		Assert.AreEqual(expirationDate, manager.ExpirationDate);
		Assert.AreEqual(45, manager.ExpirationDays);
	}

	[TestMethod]
	public void TestApplyOverrides_NoChangeWhenValuesAreSame()
	{
		// Arrange
		LicenseManager manager = new()
		{
			StandardOrTrial = LicenseType.Trial,
			Quantity = 5,
			ExpirationDays = 30,
			Version = "2.1.0",
		};

		LicenseOption model = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: "2.1.0",
			ProductPublishDate: null,
			ProductFeatures: [],
			Type: LicenseType.Trial,
			Quantity: 5,
			ExpirationDays: 30,
			ExpirationDate: null,
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act
		CliCommands.ApplyOverrides(manager, model);

		// Assert - Values should remain the same
		Assert.AreEqual(LicenseType.Trial, manager.StandardOrTrial);
		Assert.AreEqual(5, manager.Quantity);
		Assert.AreEqual(30, manager.ExpirationDays);
		Assert.AreEqual("2.1.0", manager.Version);
	}

	[TestMethod]
	public void TestApplyOverrides_ChangesOnlyDifferentValues()
	{
		// Arrange
		LicenseManager manager = new()
		{
			StandardOrTrial = LicenseType.Standard,
			Quantity = 1,
			ExpirationDays = 14,
			Version = "1.0.0",
		};

		LicenseOption model = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: "2.1.0",
			ProductPublishDate: null,
			ProductFeatures: [],
			Type: LicenseType.Standard,
			Quantity: 5,
			ExpirationDays: 14,
			ExpirationDate: null,
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act
		CliCommands.ApplyOverrides(manager, model);

		// Assert
		Assert.AreEqual(LicenseType.Standard, manager.StandardOrTrial);	// Unchanged
		Assert.AreEqual(5, manager.Quantity);										// Changed
		Assert.AreEqual(14, manager.ExpirationDays);								// Unchanged
		Assert.AreEqual("2.1.0", manager.Version);								// Changed
	}

	[TestMethod]
	public void TestApplyOverrides_ExpirationDateComparison()
	{
		// Arrange
		DateOnly existingDate = DateOnly.FromDateTime(MyNow.Now()).AddDays(30);
		LicenseManager manager = new()
		{
			ExpirationDate = existingDate,
		};

		LicenseOption model1 = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: null,
			ProductPublishDate: null,
			ProductFeatures: [],
			Type: null,
			Quantity: null,
			ExpirationDays: null,
			ExpirationDate: existingDate,
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		LicenseOption model2 = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: null,
			ProductPublishDate: null,
			ProductFeatures: [],
			Type: null,
			Quantity: null,
			ExpirationDays: null,
			ExpirationDate: existingDate.AddDays(15),
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act & Assert - Same date should not change anything
		CliCommands.ApplyOverrides(manager, model1);
		Assert.AreEqual(existingDate, manager.ExpirationDate);

		// Act & Assert - Different date should change the value
		CliCommands.ApplyOverrides(manager, model2);
		Assert.AreEqual(existingDate.AddDays(15), manager.ExpirationDate);
	}

	[TestMethod]
	public void TestApplyOverrides_PublishDateComparison()
	{
		// Arrange
		DateOnly existingDate = new(2023, 6, 1);
		LicenseManager manager = new()
		{
			PublishDate = existingDate,
		};

		LicenseOption model1 = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: null,
			ProductPublishDate: existingDate,
			ProductFeatures: [],
			Type: null,
			Quantity: null,
			ExpirationDays: null,
			ExpirationDate: null,
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		LicenseOption model2 = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: null,
			ProductPublishDate: new DateOnly(2023, 12, 1),
			ProductFeatures: [],
			Type: null,
			Quantity: null,
			ExpirationDays: null,
			ExpirationDate: null,
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act & Assert - Same date should not change anything
		CliCommands.ApplyOverrides(manager, model1);
		Assert.AreEqual(existingDate, manager.PublishDate);

		// Act & Assert - Different date should change the value
		CliCommands.ApplyOverrides(manager, model2);
		Assert.AreEqual(new DateOnly(2023, 12, 1), manager.PublishDate);
	}

	[TestMethod]
	public void TestApplyLockOverride()
	{
		// Arrange
		LicenseManager manager = new();
		File.WriteAllText(PathLockFile, "lock content");

		LicenseOption model = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: null,
			ProductPublishDate: null,
			ProductFeatures: [],
			Type: null,
			Quantity: null,
			ExpirationDays: null,
			ExpirationDate: null,
			LicenseAttributes: [],
			LockPath: new FileInfo(PathLockFile),
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act
		CliCommands.ApplyOverrides(manager, model);

		// Assert
		Assert.AreEqual(PathLockFile, manager.PathAssembly);
		Assert.IsTrue(manager.IsLockedToAssembly);
	}

	[TestMethod]
	public void TestApplyProductFeaturesOverride()
	{
		// Arrange
		LicenseManager manager = new();
		manager.ProductFeatures["ExistingFeature"] = "ExistingValue";

		System.Collections.Generic.Dictionary<string, string> features = new(StringComparer.OrdinalIgnoreCase)
		{
			["Color"] = "Blue",
			["Edition"] = "Pro",
		};

		LicenseOption model = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: null,
			ProductPublishDate: null,
			ProductFeatures: features,
			Type: null,
			Quantity: null,
			ExpirationDays: null,
			ExpirationDate: null,
			LicenseAttributes: [],
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act
		CliCommands.ApplyOverrides(manager, model);

		// Assert
		Assert.HasCount(3, manager.ProductFeatures);
		Assert.AreEqual("ExistingValue", manager.ProductFeatures["ExistingFeature"]);
		Assert.AreEqual("Blue", manager.ProductFeatures["Color"]);
		Assert.AreEqual("Pro", manager.ProductFeatures["Edition"]);
	}

	[TestMethod]
	public void TestApplyLicenseAttributesOverride()
	{
		// Arrange
		LicenseManager manager = new();
		manager.LicenseAttributes["ExistingAttr"] = "ExistingValue";

		System.Collections.Generic.Dictionary<string, string> attributes = new(StringComparer.OrdinalIgnoreCase)
		{
			["Size"] = "Large",
			["Department"] = "Engineering",
		};

		LicenseOption model = new(
			KeypairPath: new FileInfo(PathKeypairFile),
			LicensePath: null,
			ProductVersion: null,
			ProductPublishDate: null,
			ProductFeatures: [],
			Type: null,
			Quantity: null,
			ExpirationDays: null,
			ExpirationDate: null,
			LicenseAttributes: attributes,
			LockPath: null,
			Passphrase: null,
			ProductId: null,
			ProductName: null,
			LicenseeName: null,
			LicenseeEmail: null,
			LicenseeCompany: null);

		// Act
		CliCommands.ApplyOverrides(manager, model);

		// Assert
		Assert.HasCount(3, manager.LicenseAttributes);
		Assert.AreEqual("ExistingValue", manager.LicenseAttributes["ExistingAttr"]);
		Assert.AreEqual("Large", manager.LicenseAttributes["Size"]);
		Assert.AreEqual("Engineering", manager.LicenseAttributes["Department"]);
	}

	// ===== HELPER METHODS =====

	private static void CreateValidKeypairFile(string pathKeypair)
	{
		LicenseManager manager = new();
		manager.Passphrase = "CLI integration test passphrase";
		manager.CreateKeypair();
		manager.ProductId = "CLI Integration Product";
		manager.Product = "CLI Integration App";
		manager.Version = "1.0.0";
		manager.Quantity = 1;
		manager.ExpirationDays = 0;
		manager.Name = "CLI User";
		manager.Email = "cli@example.com";
		manager.SaveKeypair(pathKeypair);
	}

	private static void CreateValidLicenseFile(string pathKeypair, string pathLicense)
	{
		LicenseManager manager = new();
		manager.LoadKeypair(pathKeypair);
		manager.SaveLicenseFile(pathLicense);
	}
}
