using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Standard.Licensing;

namespace LicenseManagerX.UnitTests;

[TestClass]
public class CliArgumentParserTest
{
	public TestContext TestContext { get; private set; }

	private static string PathTestFolder = string.Empty;

	private string PathLicenseFile = string.Empty;
	private string PathKeypairFile = string.Empty;

	[ClassInitialize]
	public static void ClassSetup(TestContext testContext)
	{
		PathTestFolder = testContext.TestRunResultsDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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
	}

	[TestCleanup]
	public void TestTeardown()
	{
		File.Delete(PathLicenseFile);
		File.Delete(PathKeypairFile);

		// Reset culture to English
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
	}

	[TestMethod]
	public void TestParseBasicArguments()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test.private", result.PrivateFilePath);
		Assert.AreEqual("test.lic", result.LicenseFilePath);
		Assert.IsFalse(result.HelpRequested);
	}

	[TestMethod]
	public void TestParseShortArguments()
	{
		// Arrange
		string[] args = ["-p", "test.private", "-l", "test.lic"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test.private", result.PrivateFilePath);
		Assert.AreEqual("test.lic", result.LicenseFilePath);
	}

	[TestMethod]
	public void TestParsePrivateFilePath_RemovesQuotationMarks()
	{
		// Arrange
		string[] args = ["--private", "\"test path\\quoted.private\"", "--license", "test.lic"];

		// Act
		CliArgumentParser result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test path\\quoted.private", result.PrivateFilePath, "PrivateFilePath should not contain surrounding quotes.");
	}

	[TestMethod]
	public void TestParseLicenseFilePath_RemovesQuotationMarks()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "\"test path\\quoted.lic\""];

		// Act
		CliArgumentParser result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test path\\quoted.lic", result.LicenseFilePath, "LicenseFilePath should not contain surrounding quotes.");
	}

	[TestMethod]
	public void TestParseBothPaths_RemovesQuotationMarks()
	{
		// Arrange
		string[] args = ["--private", "\"C:\\My Folder\\keypair.private\"", "--license", "\"C:\\My Folder\\license.lic\""];

		// Act
		CliArgumentParser result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("C:\\My Folder\\keypair.private", result.PrivateFilePath, "PrivateFilePath should not contain surrounding quotes.");
		Assert.AreEqual("C:\\My Folder\\license.lic", result.LicenseFilePath, "LicenseFilePath should not contain surrounding quotes.");
	}

	[TestMethod]
	public void TestParseAllArguments()
	{
		// Arrange
		string[] args = [
			"--private", "test.private",
			"--license", "test.lic",
			"--type", "Trial",
			"--quantity", "5",
			"--expiration-days", "30",
			"--product-version", "2.1.0",
			"--product-publish-date", "2023-12-01",
		];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test.private", result.PrivateFilePath);
		Assert.AreEqual("test.lic", result.LicenseFilePath);
		Assert.AreEqual(LicenseType.Trial, result.LicenseType);
		Assert.AreEqual(5, result.Quantity);
		Assert.AreEqual(30, result.ExpirationDays);
		Assert.AreEqual("2.1.0", result.ProductVersion);
		Assert.AreEqual(new DateOnly(2023, 12, 1), result.ProductPublishDate);
	}

	[TestMethod]
	public void TestParseHelp()
	{
		// Arrange
		string[] args = ["--help"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.IsTrue(result.HelpRequested);
	}

	[TestMethod]
	public void TestParseInvalidLicenseType()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--type", "Invalid"];

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => CliArgumentParser.Parse(args));
	}

	[TestMethod]
	public void TestParseInvalidQuantity()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--quantity", "0"];

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => CliArgumentParser.Parse(args));
	}

	[TestMethod]
	public void TestParseNegativeExpirationDays()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--expiration-days", "-1"];

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => CliArgumentParser.Parse(args));
	}

	[TestMethod]
	public void TestParseMissingPrivateValue()
	{
		// Arrange
		string[] args = ["--private"];

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => CliArgumentParser.Parse(args));
	}

	[TestMethod]
	public void TestParseUnknownArgument()
	{
		// Arrange
		string[] args = ["--unknown", "value"];

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => CliArgumentParser.Parse(args));
	}

	[TestMethod]
	public void TestValidateRequiredPrivateFile()
	{
		// Arrange
		var parser = new CliArgumentParser
		{
			LicenseFilePath = "test.lic",
		};

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => parser.Validate());
	}

	[TestMethod]
	public void TestValidateRequiredLicenseFile()
	{
		// Arrange
		var parser = new CliArgumentParser
		{
			PrivateFilePath = "test.private",
		};

		// Act & Assert
		Assert.ThrowsExactly<FileNotFoundException>(parser.Validate);
	}

	[TestMethod]
	public void TestValidateMutuallyExclusiveExpirationOptions()
	{
		// Arrange
		var parser = new CliArgumentParser
		{
			PrivateFilePath = "test.private",
			LicenseFilePath = "test.lic",
			ExpirationDays = 30,
			ExpirationDate = DateTime.Now.AddDays(30),
		};

		// Act & Assert
		Assert.ThrowsExactly<FileNotFoundException>(parser.Validate);
	}

	[TestMethod]
	public void TestApplyOverrides()
	{
		// Arrange
		var manager = new LicenseManager()
		{
			StandardOrTrial = LicenseType.Standard,
			Quantity = 1,
			ExpirationDays = 0,
			Version = "1.0.0",
		};

		var parser = new CliArgumentParser()
		{
			LicenseType = LicenseType.Trial,
			Quantity = 5,
			ExpirationDays = 30,
			ProductVersion = "2.1.0",
			ProductPublishDate = new DateOnly(2023, 12, 1),
		};

		// Act
		parser.ApplyOverrides(manager);

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
		var manager = new LicenseManager();
		var expirationDate = DateTime.UtcNow.Date.AddDays(45);

		var parser = new CliArgumentParser
		{
			ExpirationDate = expirationDate
		};

		// Act
		parser.ApplyOverrides(manager);

		// Assert
		Assert.AreEqual(expirationDate, manager.ExpirationDateUTC);
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

		CliArgumentParser parser = new()
		{
			LicenseType = LicenseType.Trial, // Same value
			Quantity = 5,                    // Same value
			ExpirationDays = 30,             // Same value
			ProductVersion = "2.1.0",        // Same value
		};

		// Act
		parser.ApplyOverrides(manager);

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

		CliArgumentParser parser = new()
		{
			LicenseType = LicenseType.Standard,	// Same - should not trigger change
			Quantity = 5,                       // Different - should trigger change
			ExpirationDays = 14,                // Same - should not trigger change
			ProductVersion = "2.1.0",           // Different - should trigger change
		};

		// Act
		parser.ApplyOverrides(manager);

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
		var existingDate = DateTime.UtcNow.Date.AddDays(30);
		LicenseManager manager = new()
		{
			ExpirationDateUTC = existingDate,
		};

		var parser1 = new CliArgumentParser()
		{
			ExpirationDate = existingDate,	// Same date
		};

		var parser2 = new CliArgumentParser()
		{
			ExpirationDate = existingDate.AddDays(15),	// Different date
		};

		// Act & Assert - Same date should not change anything
		parser1.ApplyOverrides(manager);
		Assert.AreEqual(existingDate, manager.ExpirationDateUTC);

		// Act & Assert - Different date should change the value
		parser2.ApplyOverrides(manager);
		Assert.AreEqual(existingDate.AddDays(15), manager.ExpirationDateUTC);
	}

	[TestMethod]
	public void TestApplyOverrides_PublishDateComparison()
	{
		// Arrange
		var existingDate = new DateOnly(2023, 6, 1);
		LicenseManager manager = new()
		{
			PublishDate = existingDate,
		};

		CliArgumentParser parser1 = new()
		{
			ProductPublishDate = existingDate,	// Same date
		};

		CliArgumentParser parser2 = new()
		{
			ProductPublishDate = new DateOnly(2023, 12, 1),	// Different date
		};

		// Act & Assert - Same date should not change anything
		parser1.ApplyOverrides(manager);
		Assert.AreEqual(existingDate, manager.PublishDate);

		// Act & Assert - Different date should change the value
		parser2.ApplyOverrides(manager);
		Assert.AreEqual(new DateOnly(2023, 12, 1), manager.PublishDate);
	}

	[TestMethod]
	public void TestParseLockArgument()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--lock", "C:\\MyApp\\MyApp.exe"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("C:\\MyApp\\MyApp.exe", result.LockPath);
	}

	[TestMethod]
	public void TestParseLockArgumentSpaces()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--lock", "\"C:\\My App\\My App.exe\""];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("C:\\My App\\My App.exe", result.LockPath);
	}

	[TestMethod]
	public void TestParseProductFeatures()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--product-features", "Color=Blue Bird=Heron Edition=Pro"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.HasCount(3, result.ProductFeatures);
		Assert.AreEqual("Blue", result.ProductFeatures["Color"]);
		Assert.AreEqual("Heron", result.ProductFeatures["Bird"]);
		Assert.AreEqual("Pro", result.ProductFeatures["Edition"]);
	}

	[TestMethod]
	public void TestParseLicenseAttributes()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--license-attributes", "Size=Large Department=Engineering Location=Seattle"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.HasCount(3, result.LicenseAttributes);
		Assert.AreEqual("Large", result.LicenseAttributes["Size"]);
		Assert.AreEqual("Engineering", result.LicenseAttributes["Department"]);
		Assert.AreEqual("Seattle", result.LicenseAttributes["Location"]);
	}

	[TestMethod]
	public void TestParseKeyValuePairs_EmptyValue1()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--product-features", "Key1= Key2=Value EmptyValue"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.HasCount(3, result.ProductFeatures);
		Assert.AreEqual(string.Empty, result.ProductFeatures["Key1"]);
		Assert.AreEqual("Value", result.ProductFeatures["Key2"]);
		Assert.AreEqual(string.Empty, result.ProductFeatures["EmptyValue"]);
	}

	[TestMethod]
	public void TestParseKeyValuePairs_EmptyValue2()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--product-features", "Key1= EmptyValue Key2=Value"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.HasCount(3, result.ProductFeatures);
		Assert.AreEqual(string.Empty, result.ProductFeatures["Key1"]);
		Assert.AreEqual("Value", result.ProductFeatures["Key2"]);
		Assert.AreEqual(string.Empty, result.ProductFeatures["EmptyValue"]);
	}

	[TestMethod]
	public void TestParseKeyValuePairs_EmptyKey()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--product-features", "=Value"];

		// Act & Assert
		var exception = Assert.ThrowsExactly<ArgumentException>(() => CliArgumentParser.Parse(args));
		Assert.Contains("Expected key=value format", exception.Message);
	}

	[TestMethod]
	public void TestValidateReservedProductFeatureNames()
	{
		// Arrange
		CliArgumentParser parser = new()
		{
			PrivateFilePath = PathKeypairFile,
			LicenseFilePath = PathLicenseFile,
		};
		parser.ProductFeatures["Product"] = "SomeValue"; // Reserved name

		// Create the private file to avoid that error.
		File.WriteAllText(parser.PrivateFilePath, "Private file");

		// Act & Assert
		var exception = Assert.ThrowsExactly<ArgumentException>(() => parser.Validate());
		Assert.Contains("reserved product feature name", exception.Message);
	}

	[TestMethod]
	public void TestValidateReservedLicenseAttributeNames()
	{
		// Arrange
		CliArgumentParser parser = new()
		{
			PrivateFilePath = PathKeypairFile,
			LicenseFilePath = PathLicenseFile,
		};
		parser.LicenseAttributes["Product Identity"] = "SomeValue"; // Reserved name

		// Create the private file to avoid that error.
		File.WriteAllText(parser.PrivateFilePath, "Private file");

		// Act & Assert
		var exception = Assert.ThrowsExactly<ArgumentException>(() => parser.Validate());
		Assert.Contains("reserved license attribute name", exception.Message);
	}

	[TestMethod]
	public void TestApplyLockOverride()
	{
		// Arrange
		var manager = new LicenseManager();
		CliArgumentParser parser = new()
		{
			LockPath = "C:\\MyApp\\MyApp.exe",
		};

		// Act
		parser.ApplyOverrides(manager);

		// Assert
		Assert.AreEqual("C:\\MyApp\\MyApp.exe", manager.PathAssembly);
		Assert.IsTrue(manager.IsLockedToAssembly);
	}

	[TestMethod]
	public void TestApplyProductFeaturesOverride()
	{
		// Arrange
		var manager = new LicenseManager();
		manager.ProductFeatures["ExistingFeature"] = "ExistingValue";

		CliArgumentParser parser = new();
		parser.ProductFeatures["Color"] = "Blue";
		parser.ProductFeatures["Edition"] = "Pro";

		// Act
		parser.ApplyOverrides(manager);

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
		var manager = new LicenseManager();
		manager.LicenseAttributes["ExistingAttr"] = "ExistingValue";

		CliArgumentParser parser = new();
		parser.LicenseAttributes["Size"] = "Large";
		parser.LicenseAttributes["Department"] = "Engineering";

		// Act
		parser.ApplyOverrides(manager);

		// Assert
		Assert.HasCount(3, manager.LicenseAttributes);
		Assert.AreEqual("ExistingValue", manager.LicenseAttributes["ExistingAttr"]);
		Assert.AreEqual("Large", manager.LicenseAttributes["Size"]);
		Assert.AreEqual("Engineering", manager.LicenseAttributes["Department"]);
	}

	[TestMethod]
	public void TestParseAllNewArguments()
	{
		// Arrange
		string[] args =
		[
			"--private", "test.private",
			"--license", "test.lic",
			"--lock", "C:\\MyApp\\MyApp.exe",
			"--product-features", "Color=Blue Edition=Pro",
			"--license-attributes", "Size=Large Department=Engineering",
		];

		// Act
		CliArgumentParser result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("C:\\MyApp\\MyApp.exe", result.LockPath);
		Assert.HasCount(2, result.ProductFeatures);
		Assert.AreEqual("Blue", result.ProductFeatures["Color"]);
		Assert.AreEqual("Pro", result.ProductFeatures["Edition"]);
		Assert.HasCount(2, result.LicenseAttributes);
		Assert.AreEqual("Large", result.LicenseAttributes["Size"]);
		Assert.AreEqual("Engineering", result.LicenseAttributes["Department"]);
	}

	[TestMethod]
	public void TestParseSaveKeypairSwitch()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--save"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test.private", result.PrivateFilePath);
		Assert.IsTrue(result.SaveKeypair);
		Assert.IsTrue(string.IsNullOrEmpty(result.LicenseFilePath));
	}

	[TestMethod]
	public void TestValidateSaveKeypairWithoutLicense()
	{
		// Arrange
		string privateFile = PathKeypairFile;
		File.WriteAllText(privateFile, "Private file");
		var parser = new CliArgumentParser
		{
			PrivateFilePath = privateFile,
			SaveKeypair = true,
			LicenseFilePath = string.Empty,
		};

		// Act & Assert
		parser.Validate(); // Should not throw
	}

	[TestMethod]
	public void TestValidateNoLicenseOrSaveThrows()
	{
		// Arrange
		string privateFile = PathKeypairFile;
		File.WriteAllText(privateFile, "Private file");
		var parser = new CliArgumentParser
		{
			PrivateFilePath = privateFile,
			SaveKeypair = false,
			LicenseFilePath = string.Empty,
		};

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(parser.Validate);
	}

	[TestMethod]
	public void TestParseSaveKeypairWithLicense()
	{
		// Arrange
		string[] args = ["--private", "test.private", "--license", "test.lic", "--save"];

		// Act
		var result = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual("test.private", result.PrivateFilePath);
		Assert.AreEqual("test.lic", result.LicenseFilePath);
		Assert.IsTrue(result.SaveKeypair);
	}

	[TestMethod]
	public void TestParseForceSwitch_AllowsOverwriteLicenseFile()
	{
		// Arrange
		File.WriteAllText(PathLicenseFile, "Existing license file");
		File.WriteAllText(PathKeypairFile, "Private file");
		string[] args = ["--private", PathKeypairFile, "--license", PathLicenseFile, "--force"];

		// Act
		CliArgumentParser parser = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual(PathKeypairFile, parser.PrivateFilePath);
		Assert.AreEqual(PathLicenseFile, parser.LicenseFilePath);
		Assert.IsTrue(parser.ForceOverwrite);

		// Should not throw, even though license file exists
		parser.Validate();
	}

	[TestMethod]
	public void TestParseForceShortSwitch_AllowsOverwriteLicenseFile()
	{
		// Arrange
		File.WriteAllText(PathLicenseFile, "Existing license file");
		File.WriteAllText(PathKeypairFile, "Private file");
		string[] args = ["-p", PathKeypairFile, "-l", PathLicenseFile, "-f"];

		// Act
		CliArgumentParser parser = CliArgumentParser.Parse(args);

		// Assert
		Assert.AreEqual(PathKeypairFile, parser.PrivateFilePath);
		Assert.AreEqual(PathLicenseFile, parser.LicenseFilePath);
		Assert.IsTrue(parser.ForceOverwrite);

		// Should not throw, even though license file exists
		parser.Validate();
	}
}
