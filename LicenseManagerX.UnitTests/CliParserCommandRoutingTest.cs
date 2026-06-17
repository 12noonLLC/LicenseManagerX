using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Standard.Licensing;

namespace LicenseManagerX.UnitTests;

/// <summary>
/// Tests for verifying that CLI commands are correctly routed to the appropriate handlers
/// and that parsed arguments are correctly bound to the handler parameters.
/// </summary>
[TestClass]
public class CliParserCommandRoutingTest
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

		// Create test keypair file (required for parser validation)
		CreateTestKeypairFile();
	}

	[TestCleanup]
	public void TestTeardown()
	{
		File.Delete(PathLicenseFile);
		File.Delete(PathKeypairFile);
		File.Delete(PathLockFile);
	}

	private void CreateTestKeypairFile()
	{
		LicenseManager manager = new()
		{
			Passphrase = "Test passphrase for parser routing tests",
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
		manager.SaveKeypair(PathKeypairFile);
	}

	private void CreateTestLicenseFile()
	{
		LicenseManager manager = new();
		manager.LoadKeypair(PathKeypairFile);
		manager.SaveLicenseFile(PathLicenseFile);
	}

	private static ParseResult Parse(string[] args)
	{
		RootCommand root = CliParser.Test_BuildRootCommand();
		return root.Parse(args);
	}

	[TestMethod]
	public void TestKeypairUpdateCommand_RejectsProtectedOptions()
	{
		// Arrange
		string[][] protectedOptionArguments =
		[
			["--passphrase", "Updated passphrase"],
			["--product-name", "Updated Product"],
			["--licensee-name", "Updated User"],
			["--licensee-email", "updated@example.com"],
			["--licensee-company", "Updated Company"],
		];

		foreach (string[] protectedOptionArgument in protectedOptionArguments)
		{
			string[] args = ["keypair", "update", PathKeypairFile, .. protectedOptionArgument];

			// Act
			ParseResult result = Parse(args);

			// Assert
			Assert.IsNotEmpty(result.Errors, $"keypair update should reject {protectedOptionArgument[0]}");
			Assert.Contains(
				error => error.Message.Contains(protectedOptionArgument[0], StringComparison.Ordinal), result.Errors,
				$"Expected parse errors to mention {protectedOptionArgument[0]}");
		}
	}

	[TestMethod]
	public void TestKeypairCreateCommand_AllowsProtectedOptions()
	{
		// Arrange
		string targetKeypairPath = Path.Combine(PathTestFolder, $"{TestContext.TestName}.create.private");
		File.Delete(targetKeypairPath);
		string[] args =
		[
			"keypair",
			"create",
			targetKeypairPath,
			"--passphrase", "My test passphrase",
			"--product-id", "MyProductId",
			"--product-name", "My Product",
			"--licensee-name", "Jane User",
			"--licensee-email", "jane@example.com",
			"--licensee-company", "ACME",
		];

		// Act
		ParseResult result = Parse(args);

		// Assert
		Assert.IsEmpty(result.Errors, string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
	}

	[TestMethod]
	public void TestCreateCommand_InvokesCreateHandlerWithCorrectArguments()
	{
		// Arrange
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--type", "Trial",
			"--quantity", "10",
			"--expiration-days", "30",
			"--product-version", "2.0.0",
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: captureHandler,
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler called: KeypairUpdate"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler called: LicenseUpdate"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler called: KeypairShow"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler called: LicenseShow"));

		// Assert
		Assert.AreEqual(0, exitCode, "Exit code should be 0 for successful command");
		Assert.IsNotNull(capturedOption, "Create handler should have been invoked");
		Assert.AreEqual(PathKeypairFile, capturedOption.KeypairPath.FullName, "Keypair path should match");
		Assert.AreEqual(PathLicenseFile, capturedOption.LicensePath?.FullName, "License path should match");
		Assert.AreEqual(LicenseType.Trial, capturedOption.Type, "License type should be Trial");
		Assert.AreEqual(10, capturedOption.Quantity, "Quantity should be 10");
		Assert.AreEqual(30, capturedOption.ExpirationDays, "Expiration days should be 30");
		Assert.AreEqual("2.0.0", capturedOption.ProductVersion, "Product version should be 2.0.0");
	}

	[TestMethod]
	public void TestKeypairCreateCommand_InvokesCreateKeypairHandlerWithRequiredOptions()
	{
		// Arrange
		string targetKeypairPath = Path.Combine(PathTestFolder, $"{TestContext.TestName}.private");
		File.Delete(targetKeypairPath);

		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		string[] args = [
			"keypair",
			"create",
			targetKeypairPath,
			"--passphrase", "My test passphrase",
			"--product-id", "MyProductId",
			"--product-name", "My Product",
			"--licensee-name", "Jane User",
			"--licensee-email", "jane@example.com",
			"--licensee-company", "ACME",
			"--product-version", "1.2.3"
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: captureHandler,
			licenseCreateCmd: _ => Assert.Fail("Wrong handler called: LicenseCreate"),
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler called: KeypairUpdate"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler called: LicenseUpdate"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler called: KeypairShow"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler called: LicenseShow"));

		// Assert
		Assert.AreEqual(0, exitCode);
		Assert.IsNotNull(capturedOption);
		Assert.AreEqual(targetKeypairPath, capturedOption.KeypairPath.FullName);
		Assert.IsNull(capturedOption.LicensePath);
		Assert.AreEqual("1.2.3", capturedOption.ProductVersion);
	}

	[TestMethod]
	public void TestCreateCommand_WithProductPublishDate_BindsDateCorrectly()
	{
		// Arrange
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;
		DateOnly expectedDate = new(2025, 6, 15);

		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--product-publish-date", "2025-06-15",
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: captureHandler,
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler"));

		// Assert
		Assert.AreEqual(0, exitCode);
		Assert.IsNotNull(capturedOption);
		Assert.AreEqual(expectedDate, capturedOption.ProductPublishDate, "Product publish date should be parsed correctly");
	}

	[TestMethod]
	public void TestCreateCommand_WithExpirationDate_BindsDateCorrectly()
	{
		// Arrange
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;
		DateOnly expectedDate = new(2026, 12, 31);

		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--expiration-date", "2026-12-31",
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: captureHandler,
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler"));

		// Assert
		Assert.AreEqual(0, exitCode);
		Assert.IsNotNull(capturedOption);
		Assert.AreEqual(expectedDate, capturedOption.ExpirationDate, "Expiration date should be parsed correctly");
	}

	[TestMethod]
	public void TestCreateCommand_WithProductFeatures_BindsDictionaryCorrectly()
	{
		// Arrange
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--product-features", "Feature1=Value1", "Feature2=Value2", "Feature3=Value3",
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: captureHandler,
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler"));

		// Assert
		Assert.AreEqual(0, exitCode);
		Assert.IsNotNull(capturedOption);
		Assert.IsNotNull(capturedOption.ProductFeatures, "Product features should be populated");
		Assert.HasCount(3, capturedOption.ProductFeatures, "Should have 3 features");
		Assert.AreEqual("Value1", capturedOption.ProductFeatures["Feature1"]);
		Assert.AreEqual("Value2", capturedOption.ProductFeatures["Feature2"]);
		Assert.AreEqual("Value3", capturedOption.ProductFeatures["Feature3"]);
	}

	[TestMethod]
	public void TestCreateCommand_WithLicenseAttributes_BindsDictionaryCorrectly()
	{
		// Arrange
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--license-attributes", "Attr1=Val1", "Attr2=Val2",
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: captureHandler,
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler"));

		// Assert
		Assert.AreEqual(0, exitCode);
		Assert.IsNotNull(capturedOption);
		Assert.IsNotNull(capturedOption.LicenseAttributes, "License attributes should be populated");
		Assert.HasCount(2, capturedOption.LicenseAttributes, "Should have 2 attributes");
		Assert.AreEqual("Val1", capturedOption.LicenseAttributes["Attr1"]);
		Assert.AreEqual("Val2", capturedOption.LicenseAttributes["Attr2"]);
	}

	[TestMethod]
	public void TestCreateCommand_WithLockOption_BindsPathCorrectly()
	{
		// Arrange
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		// Create a lock file for testing
		File.WriteAllText(PathLockFile, "test content");

		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--lock", PathLockFile,
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: captureHandler,
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler"));

		// Assert
		Assert.AreEqual(0, exitCode);
		Assert.IsNotNull(capturedOption);
		Assert.IsNotNull(capturedOption.LockPath, "Lock path should be populated");
		Assert.AreEqual(PathLockFile, capturedOption.LockPath.FullName, "Lock path should match");
	}

	[TestMethod]
	public void TestUpdateCommand_WithoutLicense_InvokesUpdateKeypairHandler()
	{
		// Arrange
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		string[] args = [
			"keypair",
			"update",
			PathKeypairFile,
			"--product-version", "3.0.0",
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: _ => Assert.Fail("Wrong handler called: LicenseCreate"),
			keypairUpdateCmd: captureHandler,
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler called: LicenseUpdate"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler called: KeypairShow"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler called: LicenseShow"));

		// Assert
		Assert.AreEqual(0, exitCode, "Exit code should be 0");
		Assert.IsNotNull(capturedOption, "KeypairUpdate handler should have been invoked");
		Assert.IsNull(capturedOption.LicensePath, "License path should be null (keypair update mode)");
		Assert.AreEqual("3.0.0", capturedOption.ProductVersion, "Product version should be 3.0.0");
	}

	[TestMethod]
	public void TestUpdateCommand_WithLicense_InvokesUpdateLicenseHandler()
	{
		// Arrange
		CreateTestLicenseFile(); // Must exist for update validation
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		string[] args = [
			"license",
			"update",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--quantity", "99",
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: _ => Assert.Fail("Wrong handler called: LicenseCreate"),
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler called: KeypairUpdate"),
			licenseUpdateCmd: captureHandler,
			keypairShowCmd: _ => Assert.Fail("Wrong handler called: KeypairShow"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler called: LicenseShow"));

		// Assert
		Assert.AreEqual(0, exitCode, "Exit code should be 0");
		Assert.IsNotNull(capturedOption, "LicenseUpdate handler should have been invoked");
		Assert.IsNotNull(capturedOption.LicensePath, "License path should be present (license update mode)");
		Assert.AreEqual(PathLicenseFile, capturedOption.LicensePath.FullName, "License path should match");
		Assert.AreEqual(99, capturedOption.Quantity, "Quantity should be 99");
	}

	[TestMethod]
	public void TestUpdateCommand_WithMultipleOptions_BindsAllCorrectly()
	{
		// Arrange
		CreateTestLicenseFile();
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		string[] args = [
			"license",
			"update",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"--type", "Standard",
			"--quantity", "25",
			"--product-version", "4.5.0",
			"--product-features", "UpdatedFeature=UpdatedValue",
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: _ => Assert.Fail("Wrong handler called: LicenseCreate"),
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler called: KeypairUpdate"),
			licenseUpdateCmd: captureHandler,
			keypairShowCmd: _ => Assert.Fail("Wrong handler called: KeypairShow"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler called: LicenseShow"));

		// Assert
		Assert.AreEqual(0, exitCode);
		Assert.IsNotNull(capturedOption);
		Assert.AreEqual(LicenseType.Standard, capturedOption.Type);
		Assert.AreEqual(25, capturedOption.Quantity);
		Assert.AreEqual("4.5.0", capturedOption.ProductVersion);
		Assert.HasCount(1, capturedOption.ProductFeatures);
		Assert.AreEqual("UpdatedValue", capturedOption.ProductFeatures["UpdatedFeature"]);
	}

	[TestMethod]
	public void TestShowCommand_WithoutLicense_InvokesShowKeypairHandler()
	{
		// Arrange
		FileInfo? capturedKeypair = null;
		Action<FileInfo> captureHandler = fi => capturedKeypair = fi;

		string[] args = ["keypair", "show", PathKeypairFile];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: _ => Assert.Fail("Wrong handler called: LicenseCreate"),
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler called: KeypairUpdate"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler called: LicenseUpdate"),
			keypairShowCmd: captureHandler,
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler called: LicenseShow"));

		// Assert
		Assert.AreEqual(0, exitCode, "Exit code should be 0");
		Assert.IsNotNull(capturedKeypair, "KeypairShow handler should have been invoked");
		Assert.AreEqual(PathKeypairFile, capturedKeypair.FullName, "Keypair path should match");
	}

	[TestMethod]
	public void TestShowCommand_WithLicense_InvokesShowLicenseHandler()
	{
		// Arrange
		CreateTestLicenseFile();
		FileInfo? capturedKeypair = null;
		FileInfo? capturedLicense = null;
		Action<FileInfo, FileInfo> captureHandler = (kp, lic) => {
			capturedKeypair = kp;
			capturedLicense = lic;
		};

		string[] args = ["license", "show", PathKeypairFile, "--license", PathLicenseFile];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: _ => Assert.Fail("Wrong handler called: LicenseCreate"),
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler called: KeypairUpdate"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler called: LicenseUpdate"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler called: KeypairShow"),
			licenseShowCmd: captureHandler);

		// Assert
		Assert.AreEqual(0, exitCode, "Exit code should be 0");
		Assert.IsNotNull(capturedKeypair, "Keypair should be captured");
		Assert.IsNotNull(capturedLicense, "License should be captured");
		Assert.AreEqual(PathKeypairFile, capturedKeypair.FullName, "Keypair path should match");
		Assert.AreEqual(PathLicenseFile, capturedLicense.FullName, "License path should match");
	}

	[TestMethod]
	public void TestVersionCommand_DoesNotInvokeAnyHandlers()
	{
		// Arrange
		string[] args = ["version"];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("KeypairCreate handler should not be invoked"),
			licenseCreateCmd: _ => Assert.Fail("LicenseCreate handler should not be invoked"),
			keypairUpdateCmd: _ => Assert.Fail("KeypairUpdate handler should not be invoked"),
			licenseUpdateCmd: _ => Assert.Fail("LicenseUpdate handler should not be invoked"),
			keypairShowCmd: _ => Assert.Fail("KeypairShow handler should not be invoked"),
			licenseShowCmd: (_, _) => Assert.Fail("LicenseShow handler should not be invoked"));

		// Assert
		Assert.AreEqual(0, exitCode, "Version command should return 0");
		// No handlers should have been invoked - the test passes if no Assert.Fail() is triggered
	}

	[TestMethod]
	public void TestCreateCommand_WithShortOptions_BindsCorrectly()
	{
		// Arrange
		LicenseOption? capturedOption = null;
		Action<LicenseOption> captureHandler = opt => capturedOption = opt;

		string[] args = [
			"license",
			"create",
			PathKeypairFile,
			"--license", PathLicenseFile,
			"-t", "Trial",        // Short form
			"-q", "15",           // Short form
			"-pv", "5.0.0",       // Short form
		];

		// Act
		int exitCode = CliParser.RunCommand(
			args: args,
			keypairCreateCmd: _ => Assert.Fail("Wrong handler called: KeypairCreate"),
			licenseCreateCmd: captureHandler,
			keypairUpdateCmd: _ => Assert.Fail("Wrong handler called: KeypairUpdate"),
			licenseUpdateCmd: _ => Assert.Fail("Wrong handler called: LicenseUpdate"),
			keypairShowCmd: _ => Assert.Fail("Wrong handler called: KeypairShow"),
			licenseShowCmd: (_, _) => Assert.Fail("Wrong handler called: LicenseShow"));

		// Assert
		Assert.AreEqual(0, exitCode);
		Assert.IsNotNull(capturedOption);
		Assert.AreEqual(LicenseType.Trial, capturedOption.Type, "Type should be Trial (using -t)");
		Assert.AreEqual(15, capturedOption.Quantity, "Quantity should be 15 (using -q)");
		Assert.AreEqual("5.0.0", capturedOption.ProductVersion, "Version should be 5.0.0 (using -pv)");
	}
}
