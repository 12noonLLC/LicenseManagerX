using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace LicenseManagerX.UnitTests;

[TestClass]
public class ClientLibraryDirectTests
{
   public TestContext TestContext { get; private set; }

   private static string PathTestFolder = string.Empty;

   private string PathLicenseFile = string.Empty;
   private string PathKeypairFile = string.Empty;
   private string PathLockFile = string.Empty;

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
      PathLockFile = Path.Combine(PathTestFolder, TestContext.TestName + ".txt");
   }

   [TestCleanup]
   public void TestTeardown()
   {
      File.Delete(PathLicenseFile);
      File.Delete(PathKeypairFile);
      File.Delete(PathLockFile);
   }

   [TestMethod]
   public void SecureHash_ComputeSHA256Hash_MatchesFrameworkImplementation()
   {
      const string TEXT = "Client hash direct test";

      string actual = SecureHash.ComputeSHA256Hash(TEXT);
      byte[] expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(TEXT));
      string expected = Convert.ToHexString(expectedBytes).ToLowerInvariant();

      Assert.AreEqual(expected, actual);
   }

   [TestMethod]
   public void SecureHash_ComputeSHA256HashFile_MatchesFrameworkImplementation()
   {
      const string TEXT = "File hash direct test";
      File.WriteAllText(PathLockFile, TEXT, Encoding.UTF8);

      string actual = SecureHash.ComputeSHA256HashFile(PathLockFile);
      using FileStream stream = File.OpenRead(PathLockFile);
      byte[] expectedBytes = SHA256.HashData(stream);
      string expected = Convert.ToHexString(expectedBytes).ToLowerInvariant();

      Assert.AreEqual(expected, actual);
   }

   [TestMethod]
   public void LicenseFile_IsThisLicenseValid_DetectsTamperedPayload()
   {
      LicenseManager manager = CreateValidManager();
      manager.SaveLicenseFile(PathLicenseFile);

      XDocument xmlDoc = XDocument.Load(PathLicenseFile);
      XElement? nameElement = xmlDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Name");
      Assert.IsNotNull(nameElement, "Expected Name element in license XML.");
      nameElement.Value = nameElement.Value + " tampered";
      xmlDoc.Save(PathLicenseFile);

      LicenseFile licenseFile = new();
      bool isValid = licenseFile.IsThisLicenseValid(manager.ProductId, manager.KeyPublic, PathLicenseFile, string.Empty, out string messages);

      Assert.IsFalse(isValid);
      Assert.IsFalse(string.IsNullOrWhiteSpace(messages));
   }

   [TestMethod]
   public void LicenseFile_IsThisLicenseValid_AllowsLockToNonAssemblyFile_WhenHashMatches()
   {
      File.WriteAllText(PathLockFile, "lock identity", Encoding.UTF8);

      LicenseManager manager = CreateValidManager();
      manager.IsLockedToAssembly = true;
      manager.PathAssembly = PathLockFile;
      manager.SaveLicenseFile(PathLicenseFile);

      LicenseFile licenseFile = new();
      bool isValid = licenseFile.IsThisLicenseValid(manager.ProductId, manager.KeyPublic, PathLicenseFile, PathLockFile, out string messages);

      Assert.IsTrue(isValid, messages);
      Assert.IsTrue(licenseFile.IsLockedToAssembly);
   }

   private static LicenseManager CreateValidManager()
   {
      LicenseManager manager = new();
      manager.Passphrase = "Client direct tests passphrase";
      manager.CreateKeypair();
      manager.ProductId = "Direct Test Product";
      manager.Product = "Direct Test App";
      manager.Version = "1.0.0";
      manager.Quantity = 1;
      manager.ExpirationDays = 0;
      manager.Name = "Direct User";
      manager.Email = "direct@example.com";
      return manager;
   }
}
