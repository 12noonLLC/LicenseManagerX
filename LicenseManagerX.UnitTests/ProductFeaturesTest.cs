using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Licensing;

namespace LicenseManagerX.UnitTests;

[TestClass]
public class ProductFeaturesTest
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
		PathLicenseFile = Path.Combine(PathTestFolder, TestContext.TestName + LicenseManagerX.LicenseManager.FileExtension_License);
		PathKeypairFile = Path.Combine(PathTestFolder, TestContext.TestName + LicenseManagerX.LicenseManager.FileExtension_PrivateKey);
	}

	/// <summary>
	/// Creates a license manager with valid settings for testing.
	/// </summary>
	private LicenseManagerX.LicenseManager CreateLicenseManagerWithValidSettings()
	{
		LicenseManagerX.LicenseManager manager = new();

		manager.Passphrase = "Test Product Features Passphrase";
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
	public void TestIsReservedFeatureName()
	{
		// Arrange
		string[] reservedNames = { "Product", "Version", "Publish Date" };
		string[] validNames = { "MaxUsers", "AllowBackups", "EnablePremium", "product", "VERSION" };

		// Act & Assert
		foreach (var name in reservedNames)
		{
			Assert.IsTrue(LicenseManagerX.LicenseManager.IsReservedFeatureName(name), $"'{name}' should be recognized as reserved");
		}

		foreach (var name in validNames)
		{
			Assert.IsFalse(LicenseManagerX.LicenseManager.IsReservedFeatureName(name), $"'{name}' should not be recognized as reserved");
		}
	}

	[TestMethod]
	public void TestSaveLoadProductFeatures()
	{
		// Arrange
		var manager = CreateLicenseManagerWithValidSettings();

		Dictionary<string, string> features = new()
		{
			["MaxUsers"] = "100",
			["AllowBackups"] = "true",
			["EnablePremium"] = "",
			["CustomSetting"] = "Value with spaces"
		};

		foreach (var feature in features)
		{
			manager.ProductFeatures[feature.Key] = feature.Value;
		}

		// Act
		manager.SaveKeypair(PathKeypairFile);

		// Create a new manager and load the keypair
		var newManager = new LicenseManagerX.LicenseManager();
		newManager.LoadKeypair(PathKeypairFile);

		// Assert
		Assert.HasCount(features.Count, newManager.ProductFeatures, "Product features count should match");
		foreach (var feature in features)
		{
			Assert.IsTrue(newManager.ProductFeatures.ContainsKey(feature.Key), $"Feature '{feature.Key}' should exist");
			Assert.AreEqual(feature.Value, newManager.ProductFeatures[feature.Key], $"Feature '{feature.Key}' value should match");
		}
	}

	[TestMethod]
	public void TestSaveLicenseWithProductFeatures()
	{
		// Arrange
		var manager = CreateLicenseManagerWithValidSettings();

		Dictionary<string, string> features = new()
		{
			["MaxUsers"] = "50",
			["IsEnterprise"] = "true"
		};

		foreach (var feature in features)
		{
			manager.ProductFeatures[feature.Key] = feature.Value;
		}

		// Act
		manager.SaveLicenseFile(PathLicenseFile);
		string publicKey = manager.KeyPublic;
		string productId = manager.ProductId;

		// Create a new manager and validate the license
		var newManager = new LicenseManagerX.LicenseManager();
		bool isValid = newManager.IsThisLicenseValid(productId, publicKey, PathLicenseFile, string.Empty, out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		Assert.HasCount(features.Count, newManager.ProductFeatures, "Product features count should match");
		foreach (var feature in features)
		{
			Assert.IsTrue(newManager.ProductFeatures.ContainsKey(feature.Key), $"Feature '{feature.Key}' should exist");
			Assert.AreEqual(feature.Value, newManager.ProductFeatures[feature.Key], $"Feature '{feature.Key}' value should match");
		}
	}

	[TestMethod]
	public void TestLicenseFileGetProductFeature()
	{
		// Arrange
		var manager = CreateLicenseManagerWithValidSettings();

		string featureName = "EnableAdvancedReporting";
		string featureValue = "true";
		manager.ProductFeatures[featureName] = featureValue;

		// Act
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new LicenseFile and validate
		var licenseFile = new LicenseFile();
		bool isValid = licenseFile.IsThisLicenseValid(
			 manager.ProductId,
			 manager.KeyPublic,
			 PathLicenseFile,
			 string.Empty,
			 out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		Assert.IsTrue(licenseFile.HasProductFeature(featureName), $"Feature '{featureName}' should exist");
		Assert.AreEqual(featureValue, licenseFile.GetProductFeature(featureName), $"Feature value should match");
	}

	[TestMethod]
	public void TestLicenseFileGetProductFeature_NonExistentFeature()
	{
		// Arrange
		var manager = CreateLicenseManagerWithValidSettings();
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new LicenseFile and validate
		var licenseFile = new LicenseFile();
		bool isValid = licenseFile.IsThisLicenseValid(
			 manager.ProductId,
			 manager.KeyPublic,
			 PathLicenseFile,
			 string.Empty,
			 out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		Assert.IsFalse(licenseFile.HasProductFeature("NonExistentFeature"), "Should return false for non-existent feature");
		Assert.ThrowsExactly<ArgumentException>(() => licenseFile.GetProductFeature("NonExistentFeature"),
			 "Should throw ArgumentException for non-existent feature");
	}

	[TestMethod]
	public void TestEmptyAndNullFeatureValues()
	{
		// Arrange
		var manager = CreateLicenseManagerWithValidSettings();

		manager.ProductFeatures["EmptyValue"] = string.Empty;
		manager.ProductFeatures["NormalValue"] = "some value";

		// Act
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new manager and validate the license
		var newManager = new LicenseManagerX.LicenseManager();
		bool isValid = newManager.IsThisLicenseValid(
			 manager.ProductId,
			 manager.KeyPublic,
			 PathLicenseFile,
			 string.Empty,
			 out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		Assert.IsTrue(newManager.ProductFeatures.ContainsKey("EmptyValue"), "Feature with empty value should exist");
		Assert.AreEqual(string.Empty, newManager.ProductFeatures["EmptyValue"], "Empty value should be preserved");
		Assert.AreEqual("some value", newManager.ProductFeatures["NormalValue"], "Normal value should be preserved");
	}

	[TestMethod]
	public void TestSpecialCharactersInFeatureValues()
	{
		// Arrange
		var manager = CreateLicenseManagerWithValidSettings();

		Dictionary<string, string> features = new()
		{
			["SpecialChars"] = "!@#$%^&*()_+",
			["WithSpaces"] = "This is a test value",
			["WithEquals"] = "key=value",
			["WithXml"] = "<element>content</element>"
		};

		foreach (var feature in features)
		{
			manager.ProductFeatures[feature.Key] = feature.Value;
		}

		// Act
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new manager and validate the license
		var newManager = new LicenseManagerX.LicenseManager();
		bool isValid = newManager.IsThisLicenseValid(
			 manager.ProductId,
			 manager.KeyPublic,
			 PathLicenseFile,
			 string.Empty,
			 out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");
		foreach (var feature in features)
		{
			Assert.IsTrue(newManager.ProductFeatures.ContainsKey(feature.Key), $"Feature '{feature.Key}' should exist");
			Assert.AreEqual(feature.Value, newManager.ProductFeatures[feature.Key], $"Feature '{feature.Key}' value should match");
		}
	}

	[TestMethod]
	public void TestReservedFeatureNamesNotAddedToProductFeatures()
	{
		// Arrange
		var manager = CreateLicenseManagerWithValidSettings();

		// Act - Save license with product, version, etc.
		manager.SaveLicenseFile(PathLicenseFile);

		// Create a new LicenseFile and validate
		var licenseFile = new LicenseFile();
		bool isValid = licenseFile.IsThisLicenseValid(
			 manager.ProductId,
			 manager.KeyPublic,
			 PathLicenseFile,
			 string.Empty,
			 out string messages);

		// Assert
		Assert.IsTrue(isValid, $"License should be valid. Errors: {messages}");

		// These should be accessed through the dedicated properties, not through ProductFeatures
		Assert.IsFalse(licenseFile.HasProductFeature("Product"), "Reserved feature 'Product' should not be in ProductFeatures");
		Assert.IsFalse(licenseFile.HasProductFeature("Version"), "Reserved feature 'Version' should not be in ProductFeatures");
		Assert.IsFalse(licenseFile.HasProductFeature("Publish Date"), "Reserved feature 'Publish Date' should not be in ProductFeatures");

		// But their values should be available through their dedicated properties
		Assert.AreEqual(manager.Product, licenseFile.Product, "Product property should be set");
		Assert.AreEqual(manager.Version, licenseFile.Version, "Version property should be set");
	}

	[TestMethod]
	public void TestUpdateProductFeatures()
	{
		// Arrange
		LicenseManagerX.LicenseManager manager = new();
		Dictionary<string, string> newFeatures = new()
		{
			["MaxUsers"] = "100",
			["AllowBackups"] = "true"
		};

		// Act
		manager.UpdateProductFeatures(newFeatures);

		// Assert
		Assert.HasCount(2, manager.ProductFeatures, "Product features count should match");
		Assert.AreEqual("100", manager.ProductFeatures["MaxUsers"]);
		Assert.AreEqual("true", manager.ProductFeatures["AllowBackups"]);
	}

	[TestMethod]
	public void TestUpdateProductFeatures_NoChanges()
	{
		// Arrange
		LicenseManagerX.LicenseManager manager = new();
		Dictionary<string, string> initialFeatures = new()
		{
			["MaxUsers"] = "100",
			["AllowBackups"] = "true"
		};

		// Add initial features
		manager.ProductFeatures["MaxUsers"] = "100";
		manager.ProductFeatures["AllowBackups"] = "true";

		// Clear dirty flag
		typeof(LicenseManagerX.LicenseManager)
			 .GetMethod("ClearKeypairDirtyFlag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			 ?.Invoke(manager, null);
		typeof(LicenseManagerX.LicenseManager)
			 .GetMethod("ClearLicenseDirtyFlag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			 ?.Invoke(manager, null);

		// Verify flags are initially clean
		Assert.IsFalse(manager.IsKeypairDirty);
		Assert.IsFalse(manager.IsLicenseDirty);

		// Act
		manager.UpdateProductFeatures(initialFeatures);

		// Assert
		Assert.HasCount(2, manager.ProductFeatures, "Product features count should match");

		// If no changes were detected, the dirty flags should remain false
		Assert.IsFalse(manager.IsKeypairDirty, "IsKeypairDirty flag should not change when features haven't changed");
		Assert.IsFalse(manager.IsLicenseDirty, "IsLicenseDirty flag should not change when features haven't changed");
	}

	[TestMethod]
	public void TestUpdateProductFeatures_WithChanges()
	{
		// Arrange
		LicenseManagerX.LicenseManager manager = new();
		Dictionary<string, string> initialFeatures = new()
		{
			["MaxUsers"] = "100",
			["AllowBackups"] = "true"
		};

		// Add initial features
		manager.ProductFeatures["MaxUsers"] = "100";
		manager.ProductFeatures["AllowBackups"] = "true";

		// Clear dirty flag
		typeof(LicenseManagerX.LicenseManager)
			 .GetMethod("ClearKeypairDirtyFlag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			 ?.Invoke(manager, null);
		typeof(LicenseManagerX.LicenseManager)
			 .GetMethod("ClearLicenseDirtyFlag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			 ?.Invoke(manager, null);

		// Modify features
		Dictionary<string, string> modifiedFeatures = new()
		{
			["MaxUsers"] = "200",  // Changed value
			["AllowBackups"] = "true",
			["NewFeature"] = "value" // New feature
		};

		// Act
		manager.UpdateProductFeatures(modifiedFeatures);

		// Assert
		Assert.HasCount(3, manager.ProductFeatures, "Product features count should match");
		Assert.AreEqual("200", manager.ProductFeatures["MaxUsers"]);
		Assert.AreEqual("true", manager.ProductFeatures["AllowBackups"]);
		Assert.AreEqual("value", manager.ProductFeatures["NewFeature"]);

		// Dirty flags should be set when features change
		Assert.IsTrue(manager.IsKeypairDirty, "IsKeypairDirty flag should be set when features change");
		Assert.IsTrue(manager.IsLicenseDirty, "IsLicenseDirty flag should be set when features change");
	}

	[TestMethod]
	public void TestUpdateProductFeatures_ReservedName()
	{
		// Arrange
		LicenseManagerX.LicenseManager manager = new();
		Dictionary<string, string> featuresWithReservedName = new()
		{
			["MaxUsers"] = "100",
			["Product"] = "Reserved Name" // "Product" is a reserved name
		};

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => manager.UpdateProductFeatures(featuresWithReservedName),
			 "Should throw ArgumentException for reserved feature name");

		// Verify the dictionary wasn't modified
		Assert.IsEmpty(manager.ProductFeatures, "ProductFeatures should remain empty");
	}

	[TestMethod]
	public void TestUpdateProductFeatures_EmptyValueAndKey()
	{
		// Arrange
		LicenseManagerX.LicenseManager manager = new();
		Dictionary<string, string> features = new()
		{
			["EmptyValue"] = string.Empty,
			["NormalValue"] = "some value"
		};

		// Act
		manager.UpdateProductFeatures(features);

		// Assert
		Assert.HasCount(2, manager.ProductFeatures, "Product features count should match");
		Assert.AreEqual(string.Empty, manager.ProductFeatures["EmptyValue"], "Empty value should be preserved");
		Assert.AreEqual("some value", manager.ProductFeatures["NormalValue"], "Normal value should be preserved");
	}

	[TestMethod]
	public void TestUpdateProductFeatures_RemoveFeature()
	{
		// Arrange
		LicenseManagerX.LicenseManager manager = new();

		// Add initial features
		manager.ProductFeatures["FeatureToKeep"] = "value1";
		manager.ProductFeatures["FeatureToRemove"] = "value2";

		// New features without one of the original features
		Dictionary<string, string> newFeatures = new()
		{
			["FeatureToKeep"] = "value1"
		};

		// Act
		manager.UpdateProductFeatures(newFeatures);

		// Assert
		Assert.HasCount(1, manager.ProductFeatures, "Product features count should match");
		Assert.IsTrue(manager.ProductFeatures.ContainsKey("FeatureToKeep"), "Feature to keep should exist");
		Assert.IsFalse(manager.ProductFeatures.ContainsKey("FeatureToRemove"), "Removed feature should not exist");
	}
}
