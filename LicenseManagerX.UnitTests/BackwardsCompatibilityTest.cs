using LicenseManagerX;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Xml.Linq;
using Standard.Licensing;

namespace LicenseManagerX.UnitTests;

/// <summary>
/// Tests backwards compatibility for expiration date formats in license files.
/// </summary>
[TestClass]
public class BackwardsCompatibilityTest
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

   [TestCleanup]
   public void TestTeardown()
   {
      if (File.Exists(PathLicenseFile)) File.Delete(PathLicenseFile);
      if (File.Exists(PathKeypairFile)) File.Delete(PathKeypairFile);
   }

   /// <summary>
   /// Test that old DateTime format (MM/dd/yyyy HH:mm:ss) can be parsed.
   /// </summary>
   [TestMethod]
   public void TestBackwardsCompatibility_OldDateTimeFormat()
   {
      // Arrange
      var manager = new LicenseManager();
      const string PASSPHRASE = "Test passphrase for backwards compat";
      manager.Passphrase = PASSPHRASE;
      manager.CreateKeypair();
      manager.Id = Guid.NewGuid();
      manager.ProductId = "Test Product";
      manager.Name = "Test User";
      manager.Email = "test@example.com";
      manager.Product = "Test Product";
      manager.Version = "1.0.0";

      // Create a keypair file
      manager.SaveKeypair(PathKeypairFile);

      // Load and modify the XML to use old DateTime format
      XDocument doc = XDocument.Load(PathKeypairFile);
      XElement license = doc.Root!.Element("license")!;
      var expirationDateElement = license.Element("expiration-date");

		// Replace with old format: MM/dd/yyyy HH:mm:ss
		expirationDateElement?.Value = "12/01/2027 00:00:00";

		doc.Save(PathKeypairFile);

      // Act - Load the file with old format
      var manager2 = new LicenseManager();
      manager2.LoadKeypair(PathKeypairFile);

      // Assert
      Assert.AreEqual(new DateOnly(2027, 12, 1), manager2.ExpirationDate, 
         "Old DateTime format (MM/dd/yyyy HH:mm:ss) should be parsed correctly");
   }

   /// <summary>
   /// Test that new ISO 8601 format (yyyy-MM-dd) is used when saving.
   /// </summary>
   [TestMethod]
   public void TestForwardCompatibility_NewFormat()
   {
      // Arrange
      var manager = new LicenseManager();
      const string PASSPHRASE = "Test passphrase for new format";
      manager.Passphrase = PASSPHRASE;
      manager.CreateKeypair();
      manager.Id = Guid.NewGuid();
      manager.ProductId = "Test Product";
      manager.Name = "Test User";
      manager.Email = "test@example.com";
      manager.Product = "Test Product";
      manager.Version = "1.0.0";
      manager.ExpirationDays = 30;

      // Act - Save the keypair
      manager.SaveKeypair(PathKeypairFile);

      // Assert - Check the format in the XML
      XDocument doc = XDocument.Load(PathKeypairFile);
      XElement license = doc.Root!.Element("license")!;
      string? expirationDateValue = license.Element("expiration-date")?.Value;

      Assert.IsNotNull(expirationDateValue, "Expiration date element should exist");

      // Verify it's in yyyy-MM-dd format (ISO 8601)
      Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(expirationDateValue!, @"^\d{4}-\d{2}-\d{2}$"),
         $"Expiration date should be in yyyy-MM-dd format, but got: {expirationDateValue}");

      // Verify it can be parsed as DateOnly
      Assert.IsTrue(DateOnly.TryParseExact(expirationDateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate),
         $"Expiration date should be parseable as DateOnly with yyyy-MM-dd format, but got: {expirationDateValue}");
   }

   /// <summary>
   /// Test that RFC1123 format (from old .lic files) can be handled if encountered.
   /// </summary>
   [TestMethod]
   public void TestBackwardsCompatibility_RFC1123Format()
   {
      // Arrange
      var manager = new LicenseManager();
      const string PASSPHRASE = "Test passphrase for RFC1123";
      manager.Passphrase = PASSPHRASE;
      manager.CreateKeypair();
      manager.Id = Guid.NewGuid();
      manager.ProductId = "Test Product";
      manager.Name = "Test User";
      manager.Email = "test@example.com";
      manager.Product = "Test Product";
      manager.Version = "1.0.0";

      // Create a keypair file
      manager.SaveKeypair(PathKeypairFile);

      // Load and modify the XML to use RFC1123 format (as would be in old .lic files)
      XDocument doc = XDocument.Load(PathKeypairFile);
      XElement license = doc.Root!.Element("license")!;
      var expirationDateElement = license.Element("expiration-date");

      if (expirationDateElement != null)
      {
         // Replace with RFC1123 format: Sun, 02 Jan 2028 00:00:00 GMT
         expirationDateElement.Value = "Sun, 02 Jan 2028 00:00:00 GMT";
      }

      doc.Save(PathKeypairFile);

      // Act - Load the file with RFC1123 format
      var manager2 = new LicenseManager();
      manager2.LoadKeypair(PathKeypairFile);

      // Assert
      Assert.AreEqual(new DateOnly(2028, 1, 2), manager2.ExpirationDate,
         "RFC1123 format (Sun, 02 Jan 2028 00:00:00 GMT) should be parsed correctly");
   }
}
