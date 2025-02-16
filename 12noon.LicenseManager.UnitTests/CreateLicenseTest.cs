using LicenseManager_12noon.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Licensing;
using System.Diagnostics;

namespace LicenseKeys.UnitTests;

[TestClass]
public class CreateLicenseTest
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	private static TestContext _testContext;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	private static string PathTestFolder = string.Empty;

	private string PathLicenseFile = string.Empty;
	private string PathKeypairFile = string.Empty;

	[ClassInitialize]
	public static void ClassSetup(TestContext testContext)
	{
		_testContext = testContext;

		PathTestFolder = testContext.TestRunResultsDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
	}

	[ClassCleanup]
	public static void ClassTeardown()
	{
	}

	[TestInitialize]
	public void TestSetup()
	{
		PathLicenseFile = Path.Combine(PathTestFolder, _testContext.TestName + LicenseManager_12noon.LicenseManager.FileExtension_License);
		PathKeypairFile = Path.Combine(PathTestFolder, _testContext.TestName + LicenseManager_12noon.LicenseManager.FileExtension_PrivateKey);
	}

	[TestCleanup]
	public void TestTeardown()
	{
	}


	[TestMethod]
	public void TestCreateKeypair()
	{
		LicenseManager_12noon.LicenseManager manager = new();

		// Create keypair -- error no passphrase
		Assert.ThrowsException<ArgumentException>(() => manager.CreateKeypair());

		// Create keypair
		manager.Passphrase = "This is a new random passphrase.";
		manager.CreateKeypair();
	}

	[TestMethod]
	public void TestCreateAndLoadKeypairDefaults()
	{
		LicenseManager_12noon.LicenseManager manager = new();

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
		LicenseManager_12noon.LicenseManager manager = new();

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
	private LicenseManager_12noon.LicenseManager CreateLicenseManager(string passphrase)
	{
		LicenseManager_12noon.LicenseManager manager = new();

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
		Assert.ThrowsException<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Passphrase = s;
		s = manager.KeyPublic;
		manager.KeyPublic = string.Empty;
		Assert.ThrowsException<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.KeyPublic = s;

		Guid g = manager.Id;
		manager.Id = Guid.Empty;
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Id = g;
		s = manager.ProductId;
		manager.ProductId = string.Empty;
		Assert.ThrowsException<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.ProductId = s;
		s = manager.Product;
		manager.Product = string.Empty;
		Assert.ThrowsException<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Product = s;
		s = manager.Version;
		manager.Version = string.Empty;
		Assert.ThrowsException<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Version = s;

		// Quantity is not specified
		manager.Quantity = 0;
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Quantity = -1;
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Quantity = 1;

		// Expiration days is invalid
		manager.ExpirationDays = -1;
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.ExpirationDays = 0;

		s = manager.Name;
		manager.Name = string.Empty;
		Assert.ThrowsException<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Name = s;
		s = manager.Email;
		manager.Email = string.Empty;
		Assert.ThrowsException<ArgumentException>(() => manager.SaveLicenseFile(PathLicenseFile));
		manager.Email = s;

		return manager;
	}

	[TestMethod]
	public void TestCreateLicenseBasic()
	{
		// Create keypair
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("This is another random passphrase.");

		// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));
	}

	[TestMethod]
	public void TestCreateLicenseAndValidateDefaults()
	{
		// Create keypair
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("This is another random passphrase dolor lorem.");

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
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("This is another random passphrase dolor lorem ipsum.");

		// Default value
		Assert.AreEqual(LicenseType.Standard, manager.StandardOrTrial);

		const LicenseType LICENSE_TYPE = LicenseType.Trial;
		const string PRODUCT_ID = "Elephant Product ID";
		const string PRODUCT_NAME = "My Product";
		const string VERSION = "1.24.836";
		// Local time
		DateOnly DatePublished = new(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, DateTimeOffset.Now.Day);
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
		manager.ExpirationDays = 10;
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
		Assert.AreEqual(10, manager.ExpirationDays);
		Assert.AreEqual(PRODUCT_QUANTITY, manager.Quantity);
		Assert.AreEqual(LICENSEE_NAME, manager.Name);
		Assert.AreEqual(LICENSEE_EMAIL, manager.Email);
		Assert.AreEqual(LICENSEE_COMPANY, manager.Company);
	}

	[TestMethod]
	public void TestMismatchedProductId()
	{
		// Create keypair
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("Ut exerci ad nonummy at amet elitr facilisis ipsum dolor iusto et takimata ut iriure. Elit eos ut accusam amet justo.");

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
		string pathAssemblyFileGood = Path.Combine(PathTestFolder, _testContext.TestName + "Good.txt");
		File.WriteAllText(pathAssemblyFileGood, @"Tempor sanctus et. Accusam nonumy labore dolor takimata nibh stet sit qui duo vero.");

		string pathAssemblyFileBad = Path.Combine(PathTestFolder, _testContext.TestName + "Bad.txt");
		File.WriteAllText(pathAssemblyFileBad, @"Nonumy consectetuer et justo veniam. At stet est.");

		// Create keypair
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("Sit dolor facilisi dolore amet autem. Amet stet sadipscing autem diam hendrerit.");

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
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("Et sed esse et diam facilisi rebum ipsum adipiscing diam.");

		// Default value
		Assert.AreEqual(LicenseType.Standard, manager.StandardOrTrial);

		const LicenseType LICENSE_TYPE = LicenseType.Trial;
		const string PRODUCT_ID = "Elephant Product ID";
		const string PRODUCT_NAME = "My Product";
		const string VERSION = "1.24.836";
		// Local time
		DateOnly DatePublished = new(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, DateTimeOffset.Now.Day);
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
		manager.ExpirationDays = 10;
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
		Debug.WriteLineIf(!string.IsNullOrEmpty(errorMessages), errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);
		Assert.AreEqual(PRODUCT_ID, manager.ProductId);
		Assert.AreEqual(PRODUCT_NAME, manager.Product);
		Assert.AreEqual(VERSION, manager.Version);
		Assert.AreEqual(DatePublished, manager.PublishDate);
		Assert.AreEqual(10, manager.ExpirationDays);
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
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("Ut exerci ad nonummy at amet elitr facilisis ipsum dolor iusto et takimata ut iriure. Elit eos ut accusam amet justo.");

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
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("Dolor amet eirmod erat esse minim ut iriure sit aliquyam ipsum ad.");

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
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("Hendrerit nihil et aliquyam amet tempor lorem sed.");

		const string PRODUCT_ID = "Badger Product ID";

		///
		/// -1 day => invalid value
		///
		manager.ProductId = PRODUCT_ID;
		manager.ExpirationDays = -1;

		/// Act
		/// Assert
		/// Create license
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => manager.SaveLicenseFile(PathLicenseFile));
	}

	[TestMethod]
	public void TestExpirationPast()
	{
		/// Arrange
		// Create keypair
		LicenseManager_12noon.LicenseManager manager = CreateLicenseManager("Rebum vel ipsum magna labore amet elitr dolor ea.");

		const string PRODUCT_ID = "Badger Product ID";

		///
		/// 2 days and skip ahead two days.
		///
		manager.ProductId = PRODUCT_ID;
		manager.ExpirationDays = 2;

		/// Act
		/// Create license
		manager.SaveLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Assert
		/// Validate license in 23:59:59 (NOT EXPIRED)
		MyNow.UtcNow = () => DateTime.UtcNow.AddDays(1).AddMinutes(-1);
		manager = new();
		bool isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out string errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		/// Validate license in 1 day (NOT EXPIRED)
		MyNow.UtcNow = () => DateTime.UtcNow.AddDays(1);
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		/// Validate license in 23:59:59 (NOT EXPIRED)
		MyNow.UtcNow = () => DateTime.UtcNow.AddDays(1).AddMinutes(1);
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		/// Validate license in 47:59:59 (NOT EXPIRED)
		MyNow.UtcNow = () => DateTime.UtcNow.AddDays(2).AddMinutes(-1);
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out errorMessages);
		Assert.IsTrue(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages), "Some properties have changed from the default.");

		/// Validate license in 2 days (EXPIRED)
		MyNow.UtcNow = () => DateTime.UtcNow.AddDays(2);
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out errorMessages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages));

		/// Validate license in 3 days (EXPIRED)
		MyNow.UtcNow = () => DateTime.UtcNow.AddDays(3);
		manager = new();
		isValid = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty, out errorMessages);
		Assert.IsFalse(isValid);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages));
	}

	[TestMethod]
	public void TestCreateKeypairProperties()
	{
		/// Arrange
		// Create a keypair
		LicenseManager_12noon.LicenseManager manager = new();
		const string PASSPHRASE = "Test passphrase";
		manager.Passphrase = PASSPHRASE;
		manager.CreateKeypair();

		Guid ORIGINAL_ID = manager.Id;

		const string PRODUCT_ID = "Test Product ID";

		const string PRODUCT = "Test Product";
		const string VERSION = "1.0.0";
		DateOnly PUBLISH_DATE = DateOnly.FromDateTime(DateTime.UtcNow);

		const string NAME = "Test Name";
		const string EMAIL = "test@example.com";
		const string COMPANY = "Test Company";

		const LicenseType LICENSE_TYPE = LicenseType.Trial;
		const int EXPIRATION_DAYS = 30;
		const int QUANTITY = 5;

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
		Assert.AreEqual(EXPIRATION_DAYS, manager.ExpirationDays);
		Assert.AreEqual(QUANTITY, manager.Quantity);

		Assert.AreEqual(PATH_ASSEMBLY, manager.PathAssembly);
	}
}
