using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Licensing;

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
		DateTime EXPIRATION_DATE = MyNow.UtcNow().Date.AddDays(EXPIRATION_DAYS);
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
		manager.ExpirationDateUTC = EXPIRATION_DATE;
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
		Assert.AreEqual(EXPIRATION_DATE, manager.ExpirationDateUTC);
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
		DateTime EXPIRATION_DATE = MyNow.UtcNow().Date.AddDays(EXPIRATION_DAYS);
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
		manager.ExpirationDateUTC = EXPIRATION_DATE;
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
		Assert.AreEqual(EXPIRATION_DATE, manager.ExpirationDateUTC);
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

		///
		/// 1 day => not expired
		///
		manager.ProductId = PRODUCT_ID;
		manager.ExpirationDays = 1;

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
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");
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
		/// Arrange
		// Create keypair
		LicenseManagerX.LicenseManager manager = CreateLicenseManager("Rebum vel ipsum magna labore amet elitr dolor ea.");

		const string PRODUCT_ID = "Badger Product ID";

		manager.ProductId = PRODUCT_ID;

		///
		/// Now is (next year) 20xx-04-10 15:30 UTC.
		///
		DateTime now = new(DateTime.UtcNow.Year + 1, 4, 10, 15, 30, 00, DateTimeKind.Utc);
		MyNow.UtcNow = () => now;
		///
		/// License expiry is in 2 days. It is valid on April 10 and 11 but not April 12 00:00:00.
		///
		manager.ExpirationDays = 2;

		/// Act
		/// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Assert

		/// April 10 VALID

		/// Validate license today Apr 10 one minute before midnight (NOT EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(1).AddMinutes(-1);
		Assert.AreEqual(new DateTime(now.Year, 4, 10, 23, 59, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string messages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// April 11 VALID

		/// Validate license today Apr 10 midnight (NOT EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(1);
		Assert.AreEqual(new DateTime(now.Year, 4, 11, 00, 00, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// Validate license tomorrow Apr 11 one minute after midnight (NOT EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(1).AddMinutes(1);
		Assert.AreEqual(new DateTime(now.Year, 4, 11, 00, 01, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// Validate license in tomorrow Apr 11 one minute before midnight (NOT EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(2).AddMinutes(-1);
		Assert.AreEqual(new DateTime(now.Year, 4, 11, 23, 59, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// April 12 INVALID

		/// Validate license tomorrow Apr 11 at midnight (EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(2);
		Assert.AreEqual(new DateTime(now.Year, 4, 12, 00, 00, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// Validate license day after tomorrow Apr 12 one minute after midnight (EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(2).AddMinutes(1);
		Assert.AreEqual(new DateTime(now.Year, 4, 12, 00, 01, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// Validate license tomorrow Apr 12 one minute before midnight (NOT EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(3).AddMinutes(-1);
		Assert.AreEqual(new DateTime(now.Year, 4, 12, 23, 59, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// April 13 INVALID

		/// Validate license tomorrow Apr 13 at midnight (NOT EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(3);
		Assert.AreEqual(new DateTime(now.Year, 4, 13, 00, 00, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");

		/// Validate license day after tomorrow Apr 13 one minute after midnight (NOT EXPIRED)
		MyNow.UtcNow = () => now.Date.AddDays(3).AddMinutes(1);
		Assert.AreEqual(new DateTime(now.Year, 4, 13, 00, 01, 00, DateTimeKind.Utc), MyNow.UtcNow());
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out messages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(messages), "Some properties have changed from the default.");
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
		DateTime EXPIRATION_DATE = MyNow.UtcNow().Date.AddDays(EXPIRATION_DAYS);
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
		manager.ExpirationDateUTC = EXPIRATION_DATE;
		manager.ExpirationDays = EXPIRATION_DAYS;
		manager.Quantity = QUANTITY;

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
		Assert.AreEqual(EXPIRATION_DATE, manager.ExpirationDateUTC);
		Assert.AreEqual(EXPIRATION_DAYS, manager.ExpirationDays);
		Assert.AreEqual(QUANTITY, manager.Quantity);

		Assert.AreEqual(PATH_ASSEMBLY, manager.PathAssembly);
	}
}
