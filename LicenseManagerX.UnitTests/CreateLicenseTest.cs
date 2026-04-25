using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Licensing;
using System.Text;

namespace LicenseManagerX.UnitTests;

[TestClass]
public class CreateLicenseTest
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
		PathLicenseFile = Path.Combine(PathTestFolder, TestContext.TestName + LicenseManagerX.LicenseManager.FileExtension_License);
		PathKeypairFile = Path.Combine(PathTestFolder, TestContext.TestName + LicenseManagerX.LicenseManager.FileExtension_PrivateKey);
	}

	[TestCleanup]
	public void TestTeardown()
	{
		// Reset culture to English
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

		MyNow.UtcNow = () => DateTime.UtcNow;
		MyNow.Now = () => MyNow.UtcNow().ToLocalTime();
	}


	[TestMethod]
	public void TestCreateKeypair()
	{
		LicenseManagerX.LicenseManager manager = new();

		// Create keypair -- error no passphrase
		Assert.ThrowsExactly<ArgumentException>(() => manager.CreateKeypair());

		// Create keypair
		manager.Passphrase = "This is a new random passphrase.";
		manager.CreateKeypair();
	}

	[TestMethod]
	public void TestCreateAndLoadKeypairDefaults()
	{
		LicenseManagerX.LicenseManager manager = new();

		const string PASSPHRASE = "Sadipscing vero tincidunt no minim enim aliquyam duo. Consetetur facer nonumy ut eleifend duo sit.";

		/// Create keypair
		manager.Passphrase = PASSPHRASE;
		manager.CreateKeypair();

		string keyPublic = manager.KeyPublic;
		Guid id = manager.Id;

		manager.SaveKeypair(PathKeypairFile);

		manager = new();
		manager.LoadKeypair(PathKeypairFile);

		Assert.AreEqual(PASSPHRASE, manager.Passphrase);
		Assert.AreEqual(keyPublic, manager.KeyPublic);
		Assert.AreEqual(id, manager.Id);
		Assert.IsTrue(string.IsNullOrEmpty(manager.ProductId));
		Assert.IsTrue(string.IsNullOrEmpty(manager.PathAssembly));
	}

	[TestMethod]
	public void TestCreateAndLoadKeypair()
	{
		LicenseManagerX.LicenseManager manager = new();

		const string PASSPHRASE = "Sadipscing vero tincidunt no minim enim aliquyam duo. Consetetur facer nonumy ut eleifend duo sit.";
		const string PRODUCT_ID = "** My Product ID **";
		const string PATH_ASSEMBLY = @"C:\Path\To\Product.exe";

		/// Create keypair
		manager.Passphrase = PASSPHRASE;
		manager.ProductId = PRODUCT_ID;
		manager.IsLockedToAssembly = true;
		manager.PathAssembly = PATH_ASSEMBLY;
		manager.CreateKeypair();

		string keyPublic = manager.KeyPublic;
		Guid id = manager.Id;

		manager.SaveKeypair(PathKeypairFile);

		manager = new();
		manager.LoadKeypair(PathKeypairFile);

		Assert.AreEqual(PASSPHRASE, manager.Passphrase);
		Assert.AreEqual(keyPublic, manager.KeyPublic);
		Assert.AreEqual(id, manager.Id);
		Assert.AreEqual(PRODUCT_ID, manager.ProductId);
		Assert.AreEqual(PATH_ASSEMBLY, manager.PathAssembly);
	}

	/// <summary>
	/// The InitializeLicenseManager method is designed to initialize a
	/// LicenseManager instance with invalid values and verify that
	/// appropriate exceptions are thrown when attempting to create a
	/// license file with these invalid values.
	/// </summary>
	/// <param name="passphrase">Passphrase to use for the license manager</param>
	/// <returns>New instance of the license manager</returns>
	private LicenseManagerX.LicenseManager CreateLicenseManager(string passphrase)
	{
		LicenseManagerX.LicenseManager manager = new();

		// Initialize a valid LicenseManager instance
		// Assert that creating a license file does not throw an exception.
		// Set one property to an invalid value and assert that an exception is thrown.
		// Set property back to a valid value.
		// Repeat until all properties have been tested.

		/// Arrange
		manager.Passphrase = passphrase;
		manager.CreateKeypair();

		manager.ProductId = "My Product ID";

		manager.Product = "My Product";
		manager.Version = "1.2.3";

		manager.Quantity = 1;
		manager.ExpirationDays = 0; // Never expires

		manager.Name = "John Doe";
		manager.Email = "no@thankyou.com";

		/// Act and Assert
		manager.SaveLicenseFile(PathLicenseFile);	// No exception

		string s = manager.Passphrase;
		manager.Passphrase = string.Empty;
		Assert.ThrowsExactly<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Passphrase = s;
		s = manager.KeyPublic;
		manager.KeyPublic = string.Empty;
		Assert.ThrowsExactly<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.KeyPublic = s;

		Guid g = manager.Id;
		manager.Id = Guid.Empty;
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Id = g;
		s = manager.ProductId;
		manager.ProductId = string.Empty;
		Assert.ThrowsExactly<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.ProductId = s;
		s = manager.Product;
		manager.Product = string.Empty;
		Assert.ThrowsExactly<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Product = s;
		s = manager.Version;
		manager.Version = string.Empty;
		Assert.ThrowsExactly<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Version = s;

		// Quantity is not specified
		manager.Quantity = 0;
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Quantity = -1;
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Quantity = 1;

		// Expiration days is invalid
		manager.ExpirationDays = -1;
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.ExpirationDays = 0;

		s = manager.Name;
		manager.Name = string.Empty;
		Assert.ThrowsExactly<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Name = s;
		s = manager.Email;
		manager.Email = string.Empty;
		Assert.ThrowsExactly<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Email = s;

		return manager;
	}

	[TestMethod]
	public void TestCreateLicenseBasic()
	{
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("This is another random passphrase.");

		// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));
	}

	[TestMethod]
	public void TestCreateLicenseAndValidateDefaults()
	{
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("This is another random passphrase dolor lorem.");

		// Default value
		Assert.AreEqual(LicenseType.Standard, manager.StandardOrTrial);
		Assert.AreEqual(1, manager.Quantity);

		// Create license
		const LicenseType LICENSE_TYPE = LicenseType.Standard;

		const string PRODUCT_ID = "Giraffe Product ID";
		const string PRODUCT_NAME = "My Product";
		const string VERSION = "1.24.836";
		const string LICENSEE_NAME = "John Doe";
		const string LICENSEE_EMAIL = "john.doe@outlook.com";

		manager.ProductId = PRODUCT_ID;
		manager.Product = PRODUCT_NAME;
		manager.Version = VERSION;
		manager.Name = LICENSEE_NAME;
		manager.Email = LICENSEE_EMAIL;

		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);

		string publicKey = manager.KeyPublic;

		// Validate license
		manager = new();

		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsTrue(string.IsNullOrEmpty(errorMessages));

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);
		Assert.AreEqual(PRODUCT_ID, manager.ProductId);
		Assert.AreEqual(PRODUCT_NAME, manager.Product);
		Assert.AreEqual(VERSION, manager.Version);
		Assert.IsNull(manager.PublishDate);
		Assert.AreEqual(0, manager.ExpirationDays);
		Assert.AreEqual(1, manager.Quantity);
		Assert.IsTrue(string.IsNullOrEmpty(manager.Company));
	}

	[TestMethod]
	public void TestCreateLicenseAndValidate()
	{
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("This is another random passphrase dolor lorem ipsum.");

		// Default value
		Assert.AreEqual(LicenseType.Standard, manager.StandardOrTrial);

		const LicenseType LICENSE_TYPE = LicenseType.Trial;
		const string PRODUCT_ID = "Elephant Product ID";
		const string PRODUCT_NAME = "My Product";
		const string VERSION = "1.24.836";
		// Local time
		DateOnly DatePublished = new(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, DateTimeOffset.Now.Day);
		const int EXPIRATION_DAYS = 10;
		DateOnly EXPIRATION_DATE = DateOnly.FromDateTime(MyNow.Now()).AddDays(EXPIRATION_DAYS);
		const int PRODUCT_QUANTITY = 15;
		const string LICENSEE_NAME = "John Doe";
		const string LICENSEE_EMAIL = "john.doe@outlook.com";
		const string LICENSEE_COMPANY = "Acme Corp.";

		// Create license
		manager.StandardOrTrial = LICENSE_TYPE;
		manager.ProductId = PRODUCT_ID;
		manager.Product = PRODUCT_NAME;
		manager.Version = VERSION;
		manager.PublishDate = DatePublished;
		manager.ExpirationDate = EXPIRATION_DATE;
		manager.ExpirationDays = EXPIRATION_DAYS;
		manager.Quantity = PRODUCT_QUANTITY;
		manager.Name = LICENSEE_NAME;
		manager.Email = LICENSEE_EMAIL;
		manager.Company = LICENSEE_COMPANY;

		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);

		string publicKey = manager.KeyPublic;

		// Validate license
		manager = new();

		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);
		Assert.AreEqual(PRODUCT_ID, manager.ProductId);
		Assert.AreEqual(PRODUCT_NAME, manager.Product);
		Assert.AreEqual(VERSION, manager.Version);
		Assert.AreEqual(DatePublished, manager.PublishDate);
		Assert.AreEqual(EXPIRATION_DATE, manager.ExpirationDate);
		Assert.AreEqual(EXPIRATION_DAYS, manager.ExpirationDays);
		Assert.AreEqual(PRODUCT_QUANTITY, manager.Quantity);
		Assert.AreEqual(LICENSEE_NAME, manager.Name);
		Assert.AreEqual(LICENSEE_EMAIL, manager.Email);
		Assert.AreEqual(LICENSEE_COMPANY, manager.Company);
	}

	[TestMethod]
	public void TestMismatchedProductId()
	{
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Ut exerci ad nonummy at amet elitr facilisis ipsum dolor iusto et takimata ut iriure. Elit eos ut accusam amet justo.");

		const string PRODUCT_ID = "Badger Product ID";

		manager.ProductId = PRODUCT_ID;

		// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		// Validate license
		manager = new();

		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsTrue(string.IsNullOrEmpty(errorMessages));

		Assert.AreEqual(PRODUCT_ID, manager.ProductId);

		isValid = manager.IsThisLicenseValid("WRONG PRODUCT ID", publicKey, PathLicenseFile, pathAssembly: string.Empty, out errorMessages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages));
	}

	[TestMethod]
	public void TestMismatchedAssemblyIdentity()
	{
		// Create a file to act as the assembly file.
		string pathAssemblyFileGood = Path.Combine(PathTestFolder, TestContext.TestName + "Good.txt");
		File.WriteAllText(pathAssemblyFileGood, @"Tempor sanctus et. Accusam nonumy labore dolor takimata nibh stet sit qui duo vero.");

		string pathAssemblyFileBad = Path.Combine(PathTestFolder, TestContext.TestName + "Bad.txt");
		File.WriteAllText(pathAssemblyFileBad, @"Nonumy consectetuer et justo veniam. At stet est.");

		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Sit dolor facilisi dolore amet autem. Amet stet sadipscing autem diam hendrerit.");

		const string PRODUCT_ID = "Gazelle Product ID";

		manager.ProductId = PRODUCT_ID;
		manager.IsLockedToAssembly = true;
		manager.PathAssembly = pathAssemblyFileGood;

		// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Validate license
		manager = new();

		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssemblyFileGood, out string errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssemblyFileBad, out errorMessages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages));

	}

	[TestMethod]
	public void TestCreateLicenseAndValidateCulture()
	{
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-ES");
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("es-ES");

		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Et sed esse et diam facilisi rebum ipsum adipiscing diam.");

		// Default value
		Assert.AreEqual(LicenseType.Standard, manager.StandardOrTrial);

		const LicenseType LICENSE_TYPE = LicenseType.Trial;
		const string PRODUCT_ID = "Elephant Product ID";
		const string PRODUCT_NAME = "My Product";
		const string VERSION = "1.24.836";
		// Local time
		DateOnly DatePublished = new(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, DateTimeOffset.Now.Day);
		const int EXPIRATION_DAYS = 10;
		DateOnly EXPIRATION_DATE = DateOnly.FromDateTime(MyNow.Now()).AddDays(EXPIRATION_DAYS);
		const int PRODUCT_QUANTITY = 15;
		const string LICENSEE_NAME = "John Doe";
		const string LICENSEE_EMAIL = "john.doe@outlook.com";
		const string LICENSEE_COMPANY = "Acme Corp.";

		/// Create license
		manager.StandardOrTrial = LICENSE_TYPE;
		manager.ProductId = PRODUCT_ID;
		manager.Product = PRODUCT_NAME;
		manager.Version = VERSION;
		manager.PublishDate = DatePublished;
		manager.ExpirationDate = EXPIRATION_DATE;
		manager.ExpirationDays = EXPIRATION_DAYS;
		manager.Quantity = PRODUCT_QUANTITY;
		manager.Name = LICENSEE_NAME;
		manager.Email = LICENSEE_EMAIL;
		manager.Company = LICENSEE_COMPANY;

		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);

		string publicKey = manager.KeyPublic;

		/// Validate license
		manager = new();

		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string errorMessages);
		System.Diagnostics.Debug.WriteLineIf(!string.IsNullOrEmpty(errorMessages), errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);
		Assert.AreEqual(PRODUCT_ID, manager.ProductId);
		Assert.AreEqual(PRODUCT_NAME, manager.Product);
		Assert.AreEqual(VERSION, manager.Version);
		Assert.AreEqual(DatePublished, manager.PublishDate);
		Assert.AreEqual(EXPIRATION_DATE, manager.ExpirationDate);
		Assert.AreEqual(EXPIRATION_DAYS, manager.ExpirationDays);
		Assert.AreEqual(PRODUCT_QUANTITY, manager.Quantity);
		Assert.AreEqual(LICENSEE_NAME, manager.Name);
		Assert.AreEqual(LICENSEE_EMAIL, manager.Email);
		Assert.AreEqual(LICENSEE_COMPANY, manager.Company);
	}

	[TestMethod]
	public void TestExpirationNever()
	{
		/// Arrange
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Ut exerci ad nonummy at amet elitr facilisis ipsum dolor iusto et takimata ut iriure. Elit eos ut accusam amet justo.");

		const string PRODUCT_ID = "Badger Product ID";

		///
		/// 0 days => never expires
		///
		manager.ProductId = PRODUCT_ID;
		manager.ExpirationDays = 0;

		/// Act
		/// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Assert
		/// Validate license
		manager = new();
		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsTrue(string.IsNullOrEmpty(errorMessages));
	}

	[TestMethod]
	public void TestExpirationFuture()
	{
		/// Arrange
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Dolor amet eirmod erat esse minim ut iriure sit aliquyam ipsum ad.");

		const string PRODUCT_ID = "Badger Product ID";

		manager.ProductId = PRODUCT_ID;

		DateTime baseLocalNow = new(DateTime.UtcNow.Year + 1, 4, 10, 12, 0, 0, DateTimeKind.Unspecified);
		MyNow.UtcNow = () => baseLocalNow.ToUniversalTime();
		MyNow.Now = () => baseLocalNow;

		manager.ExpirationDays = 1;
		DateOnly expectedExpirationDate = DateOnly.FromDateTime(baseLocalNow).AddDays(manager.ExpirationDays);

		/// Act
		/// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Assert - day before expiration should be valid
		DateTime testTime = expectedExpirationDate.ToDateTime(new()).AddDays(-1).AddHours(12);
		MyNow.UtcNow = () => testTime.ToUniversalTime();
		MyNow.Now = () => testTime;
		manager = new();
		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		/// Assert - expiration date at local midnight should be invalid
		testTime = expectedExpirationDate.ToDateTime(new());
		MyNow.UtcNow = () => testTime.ToUniversalTime();
		MyNow.Now = () => testTime;
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out errorMessages);
		Assert.IsFalse(isValid, "License should be invalid starting at local midnight on the expiration date.");
	}

	[TestMethod]
	public void TestExpirationNegative()
	{
		/// Arrange
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Hendrerit nihil et aliquyam amet tempor lorem sed.");

		const string PRODUCT_ID = "Badger Product ID";

		///
		/// -1 day => invalid value
		///
		manager.ProductId = PRODUCT_ID;
		manager.ExpirationDays = -1;

		/// Act
		/// Assert
		/// Create license
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
	}

	[TestMethod]
	public void TestExpirationPast()
	{
		/// Test expiration with UTC+0 timezone
		TestExpirationPastWithOffset(TimeSpan.Zero, "UTC+0");
	}

	[TestMethod]
	public void TestExpirationPastWithPositiveOffset()
	{
		/// Test expiration with UTC+1 timezone (local time ahead of UTC)
		TestExpirationPastWithOffset(TimeSpan.FromHours(1), "UTC+1");
	}

	[TestMethod]
	public void TestExpirationPastWithNegativeOffset()
	{
		/// Test expiration with UTC-5 timezone (local time behind UTC)
		TestExpirationPastWithOffset(TimeSpan.FromHours(-5), "UTC-5");
	}

	/// <summary>
	/// Test expiration boundary conditions with a specific timezone offset.
	/// Verifies that expiration dates work correctly across different timezones.
	/// </summary>
	/// <param name="offset">Local time offset from UTC</param>
	/// <param name="timezoneName">Name of timezone for test identification</param>
	private void TestExpirationPastWithOffset(TimeSpan offset, string timezoneName)
	{
		/// Arrange
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Rebum vel ipsum magna labore amet elitr dolor ea.");

		const string PRODUCT_ID = "Badger Product ID";

		manager.ProductId = PRODUCT_ID;

		/// Set current time: April 10, 15:30 UTC
		DateTime baseUtcNow = new(DateTime.UtcNow.Year + 1, 4, 10, 15, 30, 00, DateTimeKind.Utc);
		DateTime baseLocalNow = baseUtcNow.Add(offset);
		MyNow.UtcNow = () => baseUtcNow;
		MyNow.Now = () => baseLocalNow;

		///
		/// License expiry is in 2 days. It is valid on April 10 and 11 but not April 12 00:00:00.
		///
		/// Set expiration 2 days from now (local date)
		/// When we create the license, it should expire at midnight local on the expiration date
		manager.ExpirationDays = 2;
		DateOnly expectedExpirationDate = DateOnly.FromDateTime(baseLocalNow).AddDays(manager.ExpirationDays);

		/// Act
		/// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Assert - One hour before expiration date should be valid
		DateTime testTime = expectedExpirationDate.ToDateTime(new()).AddDays(-1).AddHours(23);
		MyNow.UtcNow = () => testTime.Subtract(offset);
		MyNow.Now = () => testTime;
		manager = new();
		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string messages);
		Assert.IsTrue(isValid, $"{timezoneName}: One hour before expiration date should be valid");
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// Assert - One minute before expiration date should be valid
		testTime = expectedExpirationDate.ToDateTime(new()).AddDays(-1).AddHours(23).AddMinutes(59);
		MyNow.UtcNow = () => testTime.Subtract(offset);
		MyNow.Now = () => testTime;
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsTrue(isValid, $"{timezoneName}: One minute before expiration date should be valid");
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// Assert - At midnight on expiration date should be expired
		testTime = expectedExpirationDate.ToDateTime(new());
		MyNow.UtcNow = () => testTime.Subtract(offset);
		MyNow.Now = () => testTime;
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid, $"{timezoneName}: At midnight on expiration date should be expired");

		/// Assert - One minute after midnight on expiration date should remain expired
		testTime = expectedExpirationDate.ToDateTime(new()).AddMinutes(1);
		MyNow.UtcNow = () => testTime.Subtract(offset);
		MyNow.Now = () => testTime;
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid, $"{timezoneName}: One minute after expiration should be expired");

		/// Assert - One hour after midnight on expiration date should remain expired
		testTime = expectedExpirationDate.ToDateTime(new()).AddHours(1);
		MyNow.UtcNow = () => testTime.Subtract(offset);
		MyNow.Now = () => testTime;
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid, $"{timezoneName}: One hour after expiration should be expired");
	}

	[TestMethod]
	public void TestCreateKeypairProperties()
	{
		/// Arrange
		// Create a keypair
		LicenseManagerX.LicenseManager manager = new();
		const string PASSPHRASE = "Test passphrase";
		manager.Passphrase = PASSPHRASE;
		manager.CreateKeypair();

		Guid ORIGINAL_ID = manager.Id;

		const string PRODUCT_ID = "Test Product ID";

		const string PRODUCT = "Test Product";
		const string VERSION = "1.0.0";
		DateOnly PUBLISH_DATE = DateOnly.FromDateTime(MyNow.UtcNow().Date);

		const string NAME = "Test Name";
		const string EMAIL = "test@example.com";
		const string COMPANY = "Test Company";

		const LicenseType LICENSE_TYPE = LicenseType.Trial;
		const int EXPIRATION_DAYS = 5;
		DateOnly EXPIRATION_DATE = DateOnly.FromDateTime(MyNow.Now()).AddDays(EXPIRATION_DAYS);
		const int QUANTITY = 10;

		const string PATH_ASSEMBLY = @"C:\Path\To\Product.exe";

		string ORIGINAL_PUBLICKEY = manager.KeyPublic;
		manager.ProductId = PRODUCT_ID;

		manager.Name = NAME;
		manager.Email = EMAIL;
		manager.Company = COMPANY;

		manager.Product = PRODUCT;
		manager.Version = VERSION;
		manager.PublishDate = PUBLISH_DATE;

		manager.StandardOrTrial = LICENSE_TYPE;
		manager.ExpirationDate = EXPIRATION_DATE;
		manager.ExpirationDays = EXPIRATION_DAYS;
		manager.Quantity = QUANTITY;

		manager.IsLockedToAssembly = true;
		manager.PathAssembly = PATH_ASSEMBLY;

		/// Act
		// Save it in a file
		manager.SaveKeypair(PathKeypairFile);

		// Reload keypair file
		manager = new();
		manager.LoadKeypair(PathKeypairFile);

		/// Assert
		Assert.AreEqual(ORIGINAL_ID, manager.Id);
		Assert.AreEqual(PASSPHRASE, manager.Passphrase);

		Assert.AreEqual(ORIGINAL_PUBLICKEY, manager.KeyPublic);

		Assert.AreEqual(NAME, manager.Name);
		Assert.AreEqual(EMAIL, manager.Email);
		Assert.AreEqual(COMPANY, manager.Company);

		Assert.AreEqual(PRODUCT, manager.Product);
		Assert.AreEqual(VERSION, manager.Version);
		Assert.AreEqual(PUBLISH_DATE, manager.PublishDate);

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);
		Assert.AreEqual(EXPIRATION_DATE, manager.ExpirationDate);
		Assert.AreEqual(EXPIRATION_DAYS, manager.ExpirationDays);
		Assert.AreEqual(QUANTITY, manager.Quantity);

		Assert.AreEqual(PATH_ASSEMBLY, manager.PathAssembly);
	}

	/// <summary>
	/// Verifies that unticking "Lock to assembly" and saving removes the assembly path
	/// from the keypair file so it is not re-loaded on the next open.
	/// </summary>
	[TestMethod]
	public void TestSaveKeypairClearsPathAssemblyWhenNotLocked()
	{
		LicenseManagerX.LicenseManager manager = new();
		const string PASSPHRASE = "Sadipscing vero tincidunt no minim enim aliquyam duo.";
		const string PATH_ASSEMBLY = @"C:\Path\To\Product.exe";

		manager.Passphrase = PASSPHRASE;
		manager.CreateKeypair();

		// Enable lock-to-assembly and save.
		manager.IsLockedToAssembly = true;
		manager.PathAssembly = PATH_ASSEMBLY;
		manager.SaveKeypair(PathKeypairFile);

		// Reload and confirm the path was persisted.
		manager = new();
		manager.LoadKeypair(PathKeypairFile);
		Assert.IsTrue(manager.IsLockedToAssembly);
		Assert.AreEqual(PATH_ASSEMBLY, manager.PathAssembly);

		// Disable lock-to-assembly and save again.
		manager.IsLockedToAssembly = false;
		manager.SaveKeypair(PathKeypairFile);

		// Reload and confirm that the assembly path is no longer persisted.
		manager = new();
		manager.LoadKeypair(PathKeypairFile);
		Assert.IsFalse(manager.IsLockedToAssembly);
		Assert.IsTrue(string.IsNullOrEmpty(manager.PathAssembly));
	}

	/// <summary>
	/// Verify that ILicenseBuilder.ExpiresAt correctly handles UTC DateTime and generates
	/// expected expiration date in the resulting license file.
	/// </summary>
	[TestMethod]
	public void TestExpirationDateSerializationToLicenseFile()
	{
		/// Arrange
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Tempor labore sea dolor amet vero tempor sed et vero.");

		const string PRODUCT_ID = "Test Product ID";
		manager.ProductId = PRODUCT_ID;

		/// Set expiration to a specific date (e.g., April 15, 2025)
		/// This date should be stored in the .lic file as-is
		DateOnly expirationDate = new(DateTime.UtcNow.Year + 1, 4, 15);
		manager.ExpirationDate = expirationDate;
		manager.ExpirationDays = (int)(expirationDate.ToDateTime(new()) - DateOnly.FromDateTime(MyNow.Now().Date).ToDateTime(new())).TotalDays;

		/// Act
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Reload the license file and verify the expiration date is preserved
		LicenseFile license = new();
		bool isValid = license.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, string.Empty, out string messages);

		/// Assert
		Assert.IsTrue(isValid, $"License should be valid. Messages: {messages}");
		Assert.AreEqual(expirationDate, license.ExpirationDate, "Expiration date should be preserved when loading from license file");
	}

	/// <summary>
	/// Verify that expiration dates are properly handled when creating and validating licenses.
	/// </summary>
	[TestMethod]
	public void TestExpirationDateBoundaryValidation()
	{
		/// Arrange
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Sed kasd justo nonumy gubergren labore tempor tincidunt.");

		const string PRODUCT_ID = "Test Product ID";
		manager.ProductId = PRODUCT_ID;

		/// Set a known base time for the license creation
		DateTime baseTime = new(2025, 4, 15, 10, 0, 0, DateTimeKind.Unspecified);
		MyNow.UtcNow = () => baseTime.ToUniversalTime();
		MyNow.Now = () => baseTime;

		/// Set expiration to 3 days from the base time => April 18
		DateOnly expirationDate = DateOnly.FromDateTime(baseTime.AddDays(3));
		manager.ExpirationDate = expirationDate;
		manager.ExpirationDays = 3;

		/// Act
		manager.SaveLicenseFile(PathLicenseFile);
		string publicKey = manager.KeyPublic;

		/// Assert - On base date (April 15), should be valid
		MyNow.UtcNow = () => baseTime.ToUniversalTime();
		MyNow.Now = () => baseTime;

		manager = new();
		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string messages);
		Assert.IsTrue(isValid, $"License should be valid on April 15. Messages: {messages}");

		/// Assert - One minute before expiration date midnight (April 17 23:59), should be valid
		DateTime testTime = expirationDate.ToDateTime(new()).AddDays(-1).AddHours(23).AddMinutes(59);
		MyNow.UtcNow = () => testTime.ToUniversalTime();
		MyNow.Now = () => testTime;

		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsTrue(isValid, $"License should be valid on April 17 at 23:59. Messages: {messages}");

		/// Assert - At expiration date midnight (April 18 00:00), should be invalid
		testTime = expirationDate.ToDateTime(new());
		MyNow.UtcNow = () => testTime.ToUniversalTime();
		MyNow.Now = () => testTime;

		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid, $"License should be expired at April 18 midnight. Messages: {messages}");

		/// Assert - During expiration date (April 18 10:00), should remain invalid
		testTime = expirationDate.ToDateTime(new()).AddHours(10);
		MyNow.UtcNow = () => testTime.ToUniversalTime();
		MyNow.Now = () => testTime;

		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid, $"License should remain expired on April 18. Messages: {messages}");
	}

	[TestMethod]
	public void TestExpirationDateShouldNotShiftEarlierForNegativeOffset()
	{
		/// Arrange
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Labore accusam dolore diam invidunt lorem ipsum dolor sit amet.");
		const string PRODUCT_ID = "Negative Offset Product";
		manager.ProductId = PRODUCT_ID;

		TimeSpan offset = TimeSpan.FromHours(-5);
		DateTime baseUtcNow = new(DateTime.UtcNow.Year + 1, 4, 10, 15, 30, 00, DateTimeKind.Utc);
		DateTime baseLocalNow = baseUtcNow.Add(offset);
		MyNow.UtcNow = () => baseUtcNow;
		MyNow.Now = () => baseLocalNow;

		manager.ExpirationDays = 2;
		DateOnly expectedExpirationDate = DateOnly.FromDateTime(baseLocalNow).AddDays(manager.ExpirationDays);
		manager.SaveLicenseFile(PathLicenseFile);
		string publicKey = manager.KeyPublic;

		/// Assert - One minute before local midnight on expiration date should still be valid.
		DateTime oneMinuteBeforeExpirationDate = expectedExpirationDate.ToDateTime(new()).AddDays(-1).AddHours(23).AddMinutes(59);
		MyNow.UtcNow = () => oneMinuteBeforeExpirationDate.Subtract(offset);
		MyNow.Now = () => oneMinuteBeforeExpirationDate;
		manager = new();
		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string messages);
		Assert.IsTrue(isValid, $"Expected valid one minute before expiration date local midnight; failure indicates expiration date shifted earlier. Messages: {messages}");
	}

	[TestMethod]
	public void TestExpirationDayBeforeShouldRemainValid()
	{
		/// Arrange
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Lorem ipsum dolor sit amet consectetuer adipiscing elit.");
		const string PRODUCT_ID = "Badger Product ID";
		manager.ProductId = PRODUCT_ID;

		DateTime baseLocalNow = new(DateTime.UtcNow.Year + 1, 4, 10, 9, 0, 0, DateTimeKind.Unspecified);
		MyNow.UtcNow = () => baseLocalNow.ToUniversalTime();
		MyNow.Now = () => baseLocalNow;

		manager.ExpirationDays = 2;
		DateOnly expectedExpirationDate = DateOnly.FromDateTime(baseLocalNow).AddDays(manager.ExpirationDays);
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Act
		/// Set time to day BEFORE expiration date.
		DateTime dayBeforeExpiration = expectedExpirationDate.ToDateTime(new()).AddDays(-1).AddHours(12);
		MyNow.UtcNow = () => dayBeforeExpiration.ToUniversalTime();
		MyNow.Now = () => dayBeforeExpiration;

		manager = new();
		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string messages);

		/// Assert
		Assert.IsTrue(isValid, $"License should still be valid on the day before expiration. Messages: {messages}");

		/// Act
		/// Set time to day OF expiration date.
		DateTime dayOfExpiration = expectedExpirationDate.ToDateTime(new()).AddHours(12);
		MyNow.UtcNow = () => dayOfExpiration.ToUniversalTime();
		MyNow.Now = () => dayOfExpiration;

		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);

		/// Assert
		Assert.IsFalse(isValid, $"License should be invalid on the day of expiration. Messages: {messages}");
	}
}
