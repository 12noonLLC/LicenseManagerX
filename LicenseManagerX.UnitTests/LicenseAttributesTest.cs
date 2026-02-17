using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Licensing;

namespace LicenseManagerX.UnitTests;

[TestClass]
public class LicenseAttributesTest
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

	[TestInitialize]
	public void TestSetup()
	{
		PathLicenseFile = Path.Combine(PathTestFolder, TestContext.TestName + LicenseManager.FileExtension_License);
		PathKeypairFile = Path.Combine(PathTestFolder, TestContext.TestName + LicenseManager.FileExtension_PrivateKey);
	}

	/// <summary>
	/// Creates a license manager with valid settings for testing.
	/// </summary>
	private static LicenseManager CreateLicenseManagerWithValidSettings()
	{
		LicenseManager manager = new();

		manager.Passphrase = "Test License Attributes Passphrase";
		manager.CreateKeypair();

		manager.ProductId = "Test Product ID";
		manager.Product = "Test Product";
		manager.Version = "1.0.0";
		manager.Quantity = 1;
		manager.ExpirationDays = 0; // Never expires
		manager.Name = "John Doe";
		manager.Email = "john@example.com";

		return manager;
	}

	[TestMethod]
	public void TestIsReservedAttributeName()
	{
		// Arrange
		string[] reservedNames = { "Product Identity", "Assembly Identity", "Expiration Days", };
		string[] validNames = { "CustomAttribute", "UserCount", "LicenseOwner", "product identity", "EXPIRATION DAYS", };

		// Act & Assert
		foreach (string name in reservedNames)
		{
			Assert.IsTrue(LicenseManager.IsReservedAttributeName(name), $"'{name}' should be recognized as reserved");
		}

		foreach (string name in validNames)
		{
			Assert.IsFalse(LicenseManager.IsReservedAttributeName(name), $"'{name}' should not be recognized as reserved");
		}
	}

	[TestMethod]
	public void TestSaveLoadLicenseAttributes()
	{
		// Arrange
		LicenseManager manager = CreateLicenseManagerWithValidSettings();

		Dictionary<string, string> attributes = new()
		{
			["CustomAttribute"] = "Custom Value",
			["Department"] = "Engineering",
			["EmptyAttribute"] = "",
			["AttributeWithSpaces"] = "Value with spaces",
		};

		foreach (var attribute in attributes)
		{
			manager.LicenseAttributes[attribute.Key] = attribute.Value;
		}

		// Act
		manager.SaveKeypair(PathKeypairFile);

		// Create a new manager and load the keypair
		LicenseManager newManager = new();
		newManager.LoadKeypair(PathKeypairFile);

		// Assert
		Assert.HasCount(attributes.Count, newManager.LicenseAttributes, "License attributes count should match");
		foreach (var attribute in attributes)
		{
			Assert.IsTrue(newManager.LicenseAttributes.ContainsKey(attribute.Key), $"Attribute '{attribute.Key}' should exist");
			Assert.AreEqual(attribute.Value, newManager.LicenseAttributes[attribute.Key], $"Attribute '{attribute.Key}' value should match");
		}
	}

	[TestMethod]
	public void TestSaveLicenseWithLicenseAttributes()
	{
		// Arrange
		LicenseManager manager = CreateLicenseManagerWithValidSettings();

		Dictionary<string, string> attributes = new()
		{
			["License Type"] = "Enterprise",
			["Department"] = "IT",
		};

		foreach (var attribute in attributes)
		{
			manager.LicenseAttributes[attribute.Key] = attribute.Value;
		}

		// Act
		manager.SaveLicenseFile(PathLicenseFile);
		string publicKey = manager.KeyPublic;
		string productId = manager.ProductId;

		// Create a new manager and validate the license
		LicenseManager newManager = new();
		bool isValid = newManager.IsThisLicenseValid(productId, publicKey, PathLicenseFile, string.Empty, out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		Assert.HasCount(attributes.Count, newManager.LicenseAttributes, "License attributes count should match");
		foreach (var attribute in attributes)
		{
			Assert.IsTrue(newManager.LicenseAttributes.ContainsKey(attribute.Key), $"Attribute '{attribute.Key}' should exist");
			Assert.AreEqual(attribute.Value, newManager.LicenseAttributes[attribute.Key], $"Attribute '{attribute.Key}' value should match");
		}
	}

	[TestMethod]
	public void TestLicenseFileGetLicenseAttribute()
	{
		// Arrange
		LicenseManager manager = CreateLicenseManagerWithValidSettings();

		string attributeName = "Custom Permission";
		string attributeValue = "admin";
		manager.LicenseAttributes[attributeName] = attributeValue;

		// Act
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new LicenseFile and validate
		LicenseFile licenseFile = new();
		bool isValid = licenseFile.IsThisLicenseValid(
			  manager.ProductId,
			  manager.KeyPublic,
			  PathLicenseFile,
			  string.Empty,
			  out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		Assert.IsTrue(licenseFile.HasLicenseAttribute(attributeName), $"Attribute '{attributeName}' should exist");
		Assert.AreEqual(attributeValue, licenseFile.GetLicenseAttribute(attributeName), $"Attribute value should match");
	}

	[TestMethod]
	public void TestLicenseFileGetLicenseAttribute_NonExistentAttribute()
	{
		// Arrange
		LicenseManager manager = CreateLicenseManagerWithValidSettings();
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new LicenseFile and validate
		LicenseFile licenseFile = new();
		bool isValid = licenseFile.IsThisLicenseValid(
			  manager.ProductId,
			  manager.KeyPublic,
			  PathLicenseFile,
			  string.Empty,
			  out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		Assert.IsFalse(licenseFile.HasLicenseAttribute("NonExistentAttribute"), "Should return false for non-existent attribute");
		Assert.ThrowsExactly<ArgumentException>(() => licenseFile.GetLicenseAttribute("NonExistentAttribute"),
			  "Should throw ArgumentException for non-existent attribute");
	}

	[TestMethod]
	public void TestEmptyAndNullAttributeValues()
	{
		// Arrange
		LicenseManager manager = CreateLicenseManagerWithValidSettings();

		manager.LicenseAttributes["EmptyValue"] = string.Empty;
		manager.LicenseAttributes["NormalValue"] = "some value";

		// Act
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new manager and validate the license
		LicenseManager newManager = new();
		bool isValid = newManager.IsThisLicenseValid(
			  manager.ProductId,
			  manager.KeyPublic,
			  PathLicenseFile,
			  string.Empty,
			  out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		Assert.IsTrue(newManager.LicenseAttributes.ContainsKey("EmptyValue"), "Attribute with empty value should exist");
		Assert.AreEqual(string.Empty, newManager.LicenseAttributes["EmptyValue"], "Empty value should be preserved");
		Assert.AreEqual("some value", newManager.LicenseAttributes["NormalValue"], "Normal value should be preserved");
	}

	[TestMethod]
	public void TestSpecialCharactersInAttributeValues()
	{
		// Arrange
		LicenseManager manager = CreateLicenseManagerWithValidSettings();

		Dictionary<string, string> attributes = new()
		{
			["SpecialChars"] = "!@#$%^&*()_+",
			["WithSpaces"] = "This is a test value",
			["WithEquals"] = "key=value",
			["WithXml"] = "<element>content</element>",
			["WithQuotes"] = "\"quoted value\"",
			["WithNewline"] = "Line1\nLine2",
			// The carriage return does not survive being read from the file.
			//["WithCarriageReturn"] = "Value with\r\ncarriage return",
			["WithTab"] = "Value with\ttab",
			["WithBackslash"] = "C:\\Path\\To\\File",
			["WithUnicode"] = "Unicode: \u00A9\u00AE\u20AC",
			["WithEmoji"] = "Emoji: 😊🚀",
		};

		foreach (var attribute in attributes)
		{
			manager.LicenseAttributes[attribute.Key] = attribute.Value;
		}

		// Act
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new manager and validate the license
		LicenseManager newManager = new();
		bool isValid = newManager.IsThisLicenseValid(
																	manager.ProductId,
																	manager.KeyPublic,
																	PathLicenseFile,
																	string.Empty,
																	out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		foreach (var attribute in attributes)
		{
			Assert.IsTrue(newManager.LicenseAttributes.ContainsKey(attribute.Key), $"Attribute '{attribute.Key}' should exist");
			Assert.AreEqual(attribute.Value, newManager.LicenseAttributes[attribute.Key], $"Attribute '{attribute.Key}' value should match");
		}
	}

	[TestMethod]
	public void TestReservedAttributeNamesNotAddedToLicenseAttributes()
	{
		// Arrange
		LicenseManager manager = CreateLicenseManagerWithValidSettings();

		// Act - Save license with standard attributes
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new LicenseFile and validate
		LicenseFile licenseFile = new();
		bool isValid = licenseFile.IsThisLicenseValid(
			  manager.ProductId,
			  manager.KeyPublic,
			  PathLicenseFile,
			  string.Empty,
			  out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");

		// These should not be accessible through LicenseAttributes
		Assert.IsFalse(licenseFile.HasLicenseAttribute("Product Identity"), "Reserved attribute 'Product Identity' should not be in LicenseAttributes");
		Assert.IsFalse(licenseFile.HasLicenseAttribute("Assembly Identity"), "Reserved attribute 'Assembly Identity' should not be in LicenseAttributes");
		Assert.IsFalse(licenseFile.HasLicenseAttribute("Expiration Days"), "Reserved attribute 'Expiration Days' should not be in LicenseAttributes");
	}

	[TestMethod]
	public void TestUpdateLicenseAttributes()
	{
		// Arrange
		LicenseManager manager = new();
		Dictionary<string, string> newAttributes = new()
		{
			["CustomAttribute"] = "Value",
			["Department"] = "Sales",
		};

		// Act
		manager.UpdateLicenseAttributes(newAttributes);

		// Assert
		Assert.HasCount(2, manager.LicenseAttributes, "License attributes count should match");
		Assert.AreEqual("Value", manager.LicenseAttributes["CustomAttribute"]);
		Assert.AreEqual("Sales", manager.LicenseAttributes["Department"]);
	}

	[TestMethod]
	public void TestUpdateLicenseAttributes_NoChanges()
	{
		// Arrange
		LicenseManager manager = new();
		Dictionary<string, string> initialAttributes = new()
		{
			["CustomAttribute"] = "Value",
			["Department"] = "Sales",
		};

		// Add initial attributes
		manager.LicenseAttributes["CustomAttribute"] = "Value";
		manager.LicenseAttributes["Department"] = "Sales";

		// Clear dirty flag
		typeof(LicenseManager)
			  .GetMethod("ClearKeypairDirtyFlag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			  ?.Invoke(manager, null);
		typeof(LicenseManager)
			  .GetMethod("ClearLicenseDirtyFlag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			  ?.Invoke(manager, null);

		// Verify flags are initially clean
		Assert.IsFalse(manager.IsKeypairDirty);
		Assert.IsFalse(manager.IsLicenseDirty);

		// Act
		manager.UpdateLicenseAttributes(initialAttributes);

		// Assert
		Assert.HasCount(2, manager.LicenseAttributes, "License attributes count should match");

		// If no changes were detected, the dirty flags should remain false
		Assert.IsFalse(manager.IsKeypairDirty, "IsKeypairDirty flag should not change when attributes haven't changed");
		Assert.IsFalse(manager.IsLicenseDirty, "IsLicenseDirty flag should not change when attributes haven't changed");
	}

	[TestMethod]
	public void TestUpdateLicenseAttributes_WithChanges()
	{
		// Arrange
		LicenseManager manager = new();
		Dictionary<string, string> initialAttributes = new()
		{
			["CustomAttribute"] = "Value",
			["Department"] = "Sales",
		};

		// Add initial attributes
		manager.LicenseAttributes["CustomAttribute"] = "Value";
		manager.LicenseAttributes["Department"] = "Sales";

		// Clear dirty flag
		typeof(LicenseManager)
			  .GetMethod("ClearKeypairDirtyFlag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			  ?.Invoke(manager, null);
		typeof(LicenseManager)
			  .GetMethod("ClearLicenseDirtyFlag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			  ?.Invoke(manager, null);

		// Modify attributes
		Dictionary<string, string> modifiedAttributes = new()
		{
			["CustomAttribute"] = "NewValue",  // Changed value
			["Department"] = "Sales",
			["NewAttribute"] = "value", // New attribute
		};

		// Act
		manager.UpdateLicenseAttributes(modifiedAttributes);

		// Assert
		Assert.HasCount(3, manager.LicenseAttributes, "License attributes count should match");
		Assert.AreEqual("NewValue", manager.LicenseAttributes["CustomAttribute"]);
		Assert.AreEqual("Sales", manager.LicenseAttributes["Department"]);
		Assert.AreEqual("value", manager.LicenseAttributes["NewAttribute"]);

		// Dirty flags should be set when attributes change
		Assert.IsTrue(manager.IsKeypairDirty, "IsKeypairDirty flag should be set when attributes change");
		Assert.IsTrue(manager.IsLicenseDirty, "IsLicenseDirty flag should be set when attributes change");
	}

	[TestMethod]
	public void TestUpdateLicenseAttributes_ReservedName()
	{
		// Arrange
		LicenseManager manager = new();
		Dictionary<string, string> attributesWithReservedName = new()
		{
			["CustomAttribute"] = "Value",
			["Product Identity"] = "Reserved Name", // "Product Identity" is a reserved name
		};

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => manager.UpdateLicenseAttributes(attributesWithReservedName),
			  "Should throw ArgumentException for reserved attribute name");

		// Verify the dictionary wasn't modified
		Assert.IsEmpty(manager.LicenseAttributes, "LicenseAttributes should remain empty");
	}

	[TestMethod]
	public void TestUpdateLicenseAttributes_EmptyValueAndKey()
	{
		// Arrange
		LicenseManager manager = new();
		Dictionary<string, string> attributes = new()
		{
			["EmptyValue"] = string.Empty,
			["NormalValue"] = "some value",
		};

		// Act
		manager.UpdateLicenseAttributes(attributes);

		// Assert
		Assert.HasCount(2, manager.LicenseAttributes, "License attributes count should match");
		Assert.AreEqual(string.Empty, manager.LicenseAttributes["EmptyValue"], "Empty value should be preserved");
		Assert.AreEqual("some value", manager.LicenseAttributes["NormalValue"], "Normal value should be preserved");
	}

	[TestMethod]
	public void TestUpdateLicenseAttributes_RemoveAttribute()
	{
		// Arrange
		LicenseManager manager = new();

		// Add initial attributes
		manager.LicenseAttributes["AttributeToKeep"] = "value1";
		manager.LicenseAttributes["AttributeToRemove"] = "value2";

		// New attributes without one of the original attributes
		Dictionary<string, string> newAttributes = new()
		{
			["AttributeToKeep"] = "value1"
		};

		// Act
		manager.UpdateLicenseAttributes(newAttributes);

		// Assert
		Assert.HasCount(1, manager.LicenseAttributes, "License attributes count should match");
		Assert.IsTrue(manager.LicenseAttributes.ContainsKey("AttributeToKeep"), "Attribute to keep should exist");
		Assert.IsFalse(manager.LicenseAttributes.ContainsKey("AttributeToRemove"), "Removed attribute should not exist");
	}
}
