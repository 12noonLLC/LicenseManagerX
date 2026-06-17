using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Licensing;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Text;

namespace LicenseManagerX.UnitTests;

/// <summary>
/// Unit tests for the System.CommandLine CLI commands (show, create, update).
/// Tests validate that the CLI commands behave correctly and match legacy CLI behavior.
/// </summary>
[TestClass]
public class CliCommandsTests
{
	public TestContext TestContext { get; set; }

	private static string PathTestFolder = string.Empty;

	[ClassInitialize]
	public static void ClassSetup(TestContext testContext)
	{
		PathTestFolder = Path.Combine(testContext.TestRunResultsDirectory ?? Path.GetTempPath(), testContext.FullyQualifiedTestClassName);
		LicenseManagerX.LicenseManager.EnsureParentDirectoryExists(Path.Combine(PathTestFolder, "_"));
	}

	[ClassCleanup]
	public static void ClassTeardown()
	{
		// Optional: Clean up test folder
		if (Directory.Exists(PathTestFolder))
		{
			try
			{
				Directory.Delete(PathTestFolder, recursive: true);
			}
			catch
			{
				// Ignore cleanup errors
			}
		}
	}

	[TestCleanup]
	public void TestTeardown()
	{
		// Reset culture
		Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

		MyNow.UtcNow = () => DateTime.UtcNow;
		MyNow.Now = () => MyNow.UtcNow().ToLocalTime();
	}

	/// <summary>
	/// Helper to create a valid test keypair file with known properties.
	/// </summary>
	private static string CreateTestKeypairFile(string testName)
	{
		string keypairPath = Path.Combine(PathTestFolder, $"{testName}.private");

		LicenseManager manager = new()
		{
			Passphrase = "Test passphrase for CLI tests",
			ProductId = "TestProductID",
			Product = "Test Product",
			Version = "1.0.0",
			PublishDate = new DateOnly(2025, 1, 15),
			Name = "Test Customer",
			Email = "test@example.com",
			Company = "Test Company",
			StandardOrTrial = LicenseType.Standard,
			Quantity = 5,
			ExpirationDays = 0,
		};

		manager.CreateKeypair();
		manager.SaveKeypair(keypairPath);

		return keypairPath;
	}

	/// <summary>
	/// Helper to create a valid test license file from a keypair.
	/// </summary>
	private static string CreateTestLicenseFileFromKeypair(string keypairPath, string testName)
	{
		string licensePath = Path.Combine(PathTestFolder, $"{testName}.lic");

		LicenseManager manager = new();
		manager.LoadKeypair(keypairPath);
		manager.SaveLicenseFile(licensePath);

		return licensePath;
	}

	private static LicenseOption CreateValidKeypairCreateModel(string keypairPath) =>
		new(
			KeypairPath: new FileInfo(keypairPath),
			LicensePath: null,
			ProductVersion: "2.0.0",
			ProductPublishDate: new DateOnly(2026, 1, 15),
			ProductFeatures: new Dictionary<string, string> { ["Flavor"] = "Vanilla" },
			Type: LicenseType.Trial,
			Quantity: 7,
			ExpirationDays: 30,
			ExpirationDate: null,
			LicenseAttributes: new Dictionary<string, string> { ["Channel"] = "Retail" },
			LockPath: null,
			Passphrase: "Keypair create passphrase",
			ProductId: "KP-CREATE-ID",
			ProductName: "Keypair Create Product",
			LicenseeName: "Keypair User",
			LicenseeEmail: "keypair.user@example.com",
			LicenseeCompany: "Keypair Company");

	private static LicenseOption WithRequiredFieldNull(LicenseOption model, string fieldName) => fieldName switch
	{
		nameof(LicenseOption.Passphrase) => model with { Passphrase = null },
		nameof(LicenseOption.ProductId) => model with { ProductId = null },
		nameof(LicenseOption.ProductName) => model with { ProductName = null },
		nameof(LicenseOption.LicenseeName) => model with { LicenseeName = null },
		nameof(LicenseOption.LicenseeEmail) => model with { LicenseeEmail = null },
		_ => throw new InvalidOperationException($"Unknown field: {fieldName}")
	};

	[TestMethod]
	public void TestKeypairCreate_AppliesDefaultsWhenOptionalPropertiesAreMissing()
	{
		// Arrange
		DateTime fixedNow = new(2031, 7, 11, 10, 30, 0, DateTimeKind.Local);
		MyNow.Now = () => fixedNow;

		string keypairPath = Path.Combine(PathTestFolder, $"{TestContext.TestName}.private");
		File.Delete(keypairPath);

		LicenseOption model = CreateValidKeypairCreateModel(keypairPath) with
		{
			ProductVersion = null,
			ProductPublishDate = null,
			ProductFeatures = [],
			Type = null,
			Quantity = null,
			ExpirationDays = null,
			ExpirationDate = null,
			LicenseAttributes = [],
			LockPath = null,
			LicenseeCompany = null,
		};

		// Act
		CliCommands.KeypairCreate(model);

		// Assert
		Assert.IsTrue(File.Exists(keypairPath));

		LicenseManager saved = new();
		saved.LoadKeypair(keypairPath);

		Assert.AreEqual(model.Passphrase, saved.Passphrase);
		Assert.AreEqual(model.ProductId, saved.ProductId);
		Assert.AreEqual(model.ProductName, saved.Product);
		Assert.AreEqual(model.LicenseeName, saved.Name);
		Assert.AreEqual(model.LicenseeEmail, saved.Email);
		Assert.AreEqual(string.Empty, saved.Company);

		Assert.AreEqual("1.0.0", saved.Version);
		Assert.AreEqual(DateOnly.FromDateTime(fixedNow.Date), saved.PublishDate);
		Assert.AreEqual(LicenseType.Standard, saved.StandardOrTrial);
		Assert.AreEqual(1, saved.Quantity);
		Assert.AreEqual(0, saved.ExpirationDays);
		Assert.AreEqual(DateOnly.MaxValue, saved.ExpirationDate);
		Assert.IsEmpty(saved.ProductFeatures);
		Assert.IsEmpty(saved.LicenseAttributes);
		Assert.IsFalse(saved.IsLockedToAssembly);
		Assert.AreEqual(string.Empty, saved.PathAssembly);
	}

	[TestMethod]
	public void TestKeypairCreate_PersistsProvidedProperties()
	{
		// Arrange
		string keypairPath = Path.Combine(PathTestFolder, $"{TestContext.TestName}.private");
		string lockPath = Path.Combine(PathTestFolder, $"{TestContext.TestName}.lock");
		File.Delete(keypairPath);
		File.WriteAllText(lockPath, "lock");

		LicenseOption model = CreateValidKeypairCreateModel(keypairPath) with
		{
			LockPath = new FileInfo(lockPath),
			ProductVersion = "9.9.9",
			ProductPublishDate = new DateOnly(2030, 12, 25),
			ProductFeatures = new Dictionary<string, string> { ["Mode"] = "Pro", ["Seat"] = "10" },
			Type = LicenseType.Standard,
			Quantity = 25,
			ExpirationDays = 45,
			ExpirationDate = null,
			LicenseAttributes = new Dictionary<string, string> { ["Team"] = "Sales" },
			LicenseeCompany = "Contoso"
		};

		// Act
		CliCommands.KeypairCreate(model);

		// Assert
		Assert.IsTrue(File.Exists(keypairPath));

		LicenseManager saved = new();
		saved.LoadKeypair(keypairPath);

		Assert.AreEqual(model.Passphrase, saved.Passphrase);
		Assert.AreEqual(model.ProductId, saved.ProductId);
		Assert.AreEqual(model.ProductName, saved.Product);
		Assert.AreEqual(model.LicenseeName, saved.Name);
		Assert.AreEqual(model.LicenseeEmail, saved.Email);
		Assert.AreEqual(model.LicenseeCompany, saved.Company);

		Assert.AreEqual(model.ProductVersion, saved.Version);
		Assert.AreEqual(model.ProductPublishDate, saved.PublishDate);
		Assert.AreEqual(model.Type, saved.StandardOrTrial);
		Assert.AreEqual(model.Quantity, saved.Quantity);
		Assert.AreEqual(model.ExpirationDays, saved.ExpirationDays);
		Assert.IsTrue(saved.ProductFeatures.ContainsKey("Mode"));
		Assert.AreEqual("Pro", saved.ProductFeatures["Mode"]);
		Assert.IsTrue(saved.LicenseAttributes.ContainsKey("Team"));
		Assert.AreEqual("Sales", saved.LicenseAttributes["Team"]);
		Assert.IsTrue(saved.IsLockedToAssembly);
		Assert.AreEqual(lockPath, saved.PathAssembly);
	}

	[TestMethod]
	[DataRow(nameof(LicenseOption.Passphrase))]
	[DataRow(nameof(LicenseOption.ProductId))]
	[DataRow(nameof(LicenseOption.ProductName))]
	[DataRow(nameof(LicenseOption.LicenseeName))]
	[DataRow(nameof(LicenseOption.LicenseeEmail))]
	public void TestKeypairCreate_ThrowsWhenRequiredFieldMissing(string fieldName)
	{
		// Arrange
		string keypairPath = Path.Combine(PathTestFolder, $"{TestContext.TestName}_{fieldName}.private");
		LicenseOption model = WithRequiredFieldNull(CreateValidKeypairCreateModel(keypairPath), fieldName);

		// Act + Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => CliCommands.KeypairCreate(model));
	}

	[TestMethod]
	public void TestShowCommand_DisplaysKeypairInfo()
	{
		// Arrange
		string keypairPath = CreateTestKeypairFile(TestContext.TestName);

		// Capture console output
		StringBuilder consoleOutput = new();
		StringWriter stringWriter = new(consoleOutput);
		TextWriter originalOut = Console.Out;
		Console.SetOut(stringWriter);

		try
		{
			// Act
			int exitCode = CliParser.RunCommand(
				args: ["keypair", "show", keypairPath],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(0, exitCode, "Show command should return exit code 0 for valid keypair file");

			string output = consoleOutput.ToString();
			Assert.Contains("Product ID: TestProductID", output);
			Assert.Contains("Product: Test Product", output);
			Assert.Contains("Version: 1.0.0", output);
			Assert.Contains("Customer: Test Customer <test@example.com>", output);
			Assert.Contains("Company: Test Company", output);
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[TestMethod]
	public void TestShowCommand_DisplaysLicenseInfo()
	{
		// Arrange
		string keypairPath = CreateTestKeypairFile(TestContext.TestName);
		string licensePath = CreateTestLicenseFileFromKeypair(keypairPath, TestContext.TestName);

		// Capture console output
		StringBuilder consoleOutput = new();
		StringWriter stringWriter = new(consoleOutput);
		TextWriter originalOut = Console.Out;
		Console.SetOut(stringWriter);

		try
		{
			// Act
			int exitCode = CliParser.RunCommand(
				args: ["license", "show", keypairPath, "--license", licensePath],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(0, exitCode, "Show command should return exit code 0 for valid license file");

			string output = consoleOutput.ToString();
			// License files display customer info and license details
			Assert.Contains("Customer: Test Customer <test@example.com>", output);
			Assert.Contains("License type: Standard", output);
			Assert.Contains("Quantity: 5", output);
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[TestMethod]
	public void TestShowCommand_ReturnsErrorForNonExistentFile()
	{
		// Arrange
		string nonExistentPath = Path.Combine(PathTestFolder, "nonexistent.private");

		// Capture console error output
		StringBuilder consoleError = new();
		StringWriter stringWriter = new(consoleError);
		TextWriter originalError = Console.Error;
		Console.SetError(stringWriter);

		try
		{
			// Act
			int exitCode = CliParser.RunCommand(
				args: ["keypair", "show", nonExistentPath],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(1, exitCode, "Show command should return exit code 1 for missing file");
		}
		finally
		{
			Console.SetError(originalError);
		}
	}

	[TestMethod]
	public void TestCreateCommand_CreatesLicenseFile()
	{
		// Arrange
		string keypairPath = CreateTestKeypairFile(TestContext.TestName);
		string targetLicensePath = Path.Combine(PathTestFolder, $"{TestContext.TestName}_new.lic");

		// Ensure target does not exist
		if (File.Exists(targetLicensePath))
		{
			File.Delete(targetLicensePath);
		}

		// Capture console output
		StringBuilder consoleOutput = new();
		StringWriter stringWriter = new(consoleOutput);
		TextWriter originalOut = Console.Out;
		Console.SetOut(stringWriter);

		try
		{
			// Act
			int exitCode = CliParser.RunCommand(
				args: ["license", "create", keypairPath, "--license", targetLicensePath],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(0, exitCode, "Create command should return exit code 0");
			Assert.IsTrue(File.Exists(targetLicensePath), "License file should be created");

			string output = consoleOutput.ToString();
			Assert.Contains("License file created successfully", output);
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[TestMethod]
	public void TestCreateCommand_WithOverrides()
	{
		// Arrange
		string keypairPath = CreateTestKeypairFile(TestContext.TestName);
		string targetLicensePath = Path.Combine(PathTestFolder, $"{TestContext.TestName}_override.lic");

		if (File.Exists(targetLicensePath))
		{
			File.Delete(targetLicensePath);
		}

		// Capture console output
		StringBuilder consoleOutput = new();
		StringWriter stringWriter = new(consoleOutput);
		TextWriter originalOut = Console.Out;
		Console.SetOut(stringWriter);

		try
		{
			// Act - create with version override and expiration
			int exitCode = CliParser.RunCommand(
				args: [
					"license", "create", keypairPath, "--license", targetLicensePath,
					"--product-version", "2.0.1",
					"--expiration-days", "30"
				],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(0, exitCode, "Create command should succeed with overrides");
			Assert.IsTrue(File.Exists(targetLicensePath), "License file should be created");

			string output = consoleOutput.ToString();
			Assert.Contains("Applied CLI overrides", output);
			Assert.Contains("Product Version: 2.0.1", output);
			Assert.Contains("Expiration Days: 30", output);

			// Note: When loading a license file, version is stored in product features,
			// so direct property comparison may not work as expected. The console output
			// verification above confirms the overrides were applied during creation.
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[TestMethod]
	public void TestCreateCommand_ReturnsErrorWhenTargetLicenseExists()
	{
		// Arrange
		string keypairPath = CreateTestKeypairFile(TestContext.TestName);
		string targetLicensePath = Path.Combine(PathTestFolder, $"{TestContext.TestName}_overwrite.lic");

		// First create a license file
		LicenseManager manager = new();
		manager.LoadKeypair(keypairPath);
		manager.SaveLicenseFile(targetLicensePath);

		Assert.IsTrue(File.Exists(targetLicensePath), "Precondition: license file must exist");

		// Capture console error output
		StringBuilder consoleError = new();
		StringWriter errorWriter = new(consoleError);
		TextWriter originalError = Console.Error;
		Console.SetError(errorWriter);

		try
		{
			// Act
			int exitCode = CliParser.RunCommand(
				args: ["license", "create", keypairPath, "--license", targetLicensePath],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(1, exitCode, "Create command should fail when target license already exists");
		}
		finally
		{
			Console.SetError(originalError);
		}
	}

	[TestMethod]
	public void TestUpdateCommand_UpdatesKeypairVersion()
	{
		// Arrange
		string keypairPath = CreateTestKeypairFile(TestContext.TestName);

		// Verify initial version
		LicenseManager beforeManager = new();
		beforeManager.LoadKeypair(keypairPath);
		Assert.AreEqual("1.0.0", beforeManager.Version);

		// Capture console output
		StringBuilder consoleOutput = new();
		StringWriter stringWriter = new(consoleOutput);
		TextWriter originalOut = Console.Out;
		Console.SetOut(stringWriter);

		try
		{
			// Act
			int exitCode = CliParser.RunCommand(
				args: [
					"keypair", "update", keypairPath,
					"--product-version", "2.5.0"
				],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(0, exitCode, "Update command should succeed");

			string output = consoleOutput.ToString();
			Assert.Contains("Keypair file saved successfully", output);

			// Verify updated version
			LicenseManager afterManager = new();
			afterManager.LoadKeypair(keypairPath);
			Assert.AreEqual("2.5.0", afterManager.Version, "Version should be updated in keypair file");
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[TestMethod]
	public void TestUpdateCommand_UpdatesPublishDate()
	{
		// Arrange
		string keypairPath = CreateTestKeypairFile(TestContext.TestName);

		// Capture console output
		StringBuilder consoleOutput = new();
		StringWriter stringWriter = new(consoleOutput);
		TextWriter originalOut = Console.Out;
		Console.SetOut(stringWriter);

		try
		{
			// Act
			int exitCode = CliParser.RunCommand(
				args: [
					"keypair", "update", keypairPath,
					"--product-publish-date", "2025-06-15"
				],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(0, exitCode, "Update command should succeed");

			// Verify updated publish date
			LicenseManager afterManager = new();
			afterManager.LoadKeypair(keypairPath);
			Assert.AreEqual(new DateOnly(2025, 6, 15), afterManager.PublishDate, "Publish date should be updated");
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[TestMethod]
	public void TestUpdateCommand_UpdatesProductFeatures()
	{
		// Arrange
		string keypairPath = CreateTestKeypairFile(TestContext.TestName);

		// Capture console output
		StringBuilder consoleOutput = new();
		StringWriter stringWriter = new(consoleOutput);
		TextWriter originalOut = Console.Out;
		Console.SetOut(stringWriter);

		try
		{
			// Act
			int exitCode = CliParser.RunCommand(
				args: [
					"keypair", "update", keypairPath,
					"--product-features", "Color=Blue", "Size=Large"
				],
				keypairCreateCmd: CliCommands.KeypairCreate,
				licenseCreateCmd: CliCommands.LicenseCreate,
				keypairUpdateCmd: CliCommands.KeypairUpdate,
				licenseUpdateCmd: CliCommands.LicenseUpdate,
				keypairShowCmd: CliCommands.KeypairShow,
				licenseShowCmd: CliCommands.LicenseShow);

			// Assert
			Assert.AreEqual(0, exitCode, "Update command should succeed");

			// Verify updated features
			LicenseManager afterManager = new();
			afterManager.LoadKeypair(keypairPath);
			Assert.IsTrue(afterManager.ProductFeatures.ContainsKey("Color"), "Product features should contain Color");
			Assert.AreEqual("Blue", afterManager.ProductFeatures["Color"]);
			Assert.IsTrue(afterManager.ProductFeatures.ContainsKey("Size"), "Product features should contain Size");
			Assert.AreEqual("Large", afterManager.ProductFeatures["Size"]);
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}
}
