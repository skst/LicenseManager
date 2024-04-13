using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standard.Licensing;

namespace LicenseKeys.UnitTests;

[TestClass]
public class CreateLicenseTest
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	private static TestContext _testContext;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	private static string PathLicenseFolder = string.Empty;

	private string PathLicenseFile = string.Empty;

	[ClassInitialize]
	public static void ClassSetup(TestContext testContext)
	{
		_testContext = testContext;

		PathLicenseFolder = testContext.TestRunResultsDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
	}

	[ClassCleanup]
	public static void ClassTeardown()
	{
	}

	[TestInitialize]
	public void TestSetup()
	{
		PathLicenseFile = Path.Combine(PathLicenseFolder, _testContext.TestName + Shared.LicenseManager.FileExtension_License);
	}

	[TestCleanup]
	public void TestTeardown()
	{
	}


	[TestMethod]
	public void TestCreateKeypair()
	{
		Shared.LicenseManager manager = new();

		// Create keypair -- error no passphrase
		Assert.ThrowsException<ArgumentException>(() => manager.CreateKeypair());

		// Create keypair
		manager.Passphrase = "This is a new random passphrase.";
		manager.CreateKeypair();
	}

	[TestMethod]
	public void TestCreateAndLoadKeypairDefaults()
	{
		Shared.LicenseManager manager = new();

		const string PASSPHRASE = "Sadipscing vero tincidunt no minim enim aliquyam duo. Consetetur facer nonumy ut eleifend duo sit.";

		/// Create keypair
		manager.Passphrase = PASSPHRASE;
		manager.CreateKeypair();

		string keyPublic = manager.KeyPublic;
		Guid id = manager.Id;

		manager.SaveKeypair(PathLicenseFile);

		manager = new();
		manager.LoadKeypair(PathLicenseFile);

		Assert.AreEqual(PASSPHRASE, manager.Passphrase);
		Assert.AreEqual(keyPublic, manager.KeyPublic);
		Assert.AreEqual(id, manager.Id);
		Assert.IsTrue(string.IsNullOrEmpty(manager.ProductId));
		Assert.IsTrue(string.IsNullOrEmpty(manager.PathAssembly));
	}

	[TestMethod]
	public void TestCreateAndLoadKeypair()
	{
		Shared.LicenseManager manager = new();

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

		manager.SaveKeypair(PathLicenseFile);

		manager = new();
		manager.LoadKeypair(PathLicenseFile);

		Assert.AreEqual(PASSPHRASE, manager.Passphrase);
		Assert.AreEqual(keyPublic, manager.KeyPublic);
		Assert.AreEqual(id, manager.Id);
		Assert.AreEqual(PRODUCT_ID, manager.ProductId);
		Assert.AreEqual(PATH_ASSEMBLY, manager.PathAssembly);
	}

	[TestMethod]
	public void TestCreateLicenseBasic()
	{
		Shared.LicenseManager manager = new();

		// Create keypair
		manager.Passphrase = "This is another random passphrase.";
		manager.CreateKeypair();

		manager.Quantity = 0;

		// Create license -- error quantity is not specified
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => manager.CreateLicenseFile(PathLicenseFile));

		manager.Quantity = 1;

		// Create license
		manager.CreateLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));
	}

	[TestMethod]
	public void TestCreateLicenseAndValidateDefaults()
	{
		Shared.LicenseManager manager = new();

		/// Create keypair
		manager.Passphrase = "This is another random passphrase dolor lorem.";
		manager.CreateKeypair();

		// Default value
		Assert.AreEqual(LicenseType.Standard, manager.StandardOrTrial);
		Assert.AreEqual(1, manager.Quantity);

		/// Create license
		const LicenseType LICENSE_TYPE = LicenseType.Standard;
		const string PRODUCT_ID = "Giraffe Product ID";
		manager.ProductId = PRODUCT_ID;

		manager.CreateLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);

		string publicKey = manager.KeyPublic;

		/// Validate license
		manager = new();

		string errorMessages = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty);
		Assert.IsTrue(string.IsNullOrEmpty(errorMessages));

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);
		Assert.AreEqual(PRODUCT_ID, manager.ProductId);
		Assert.IsTrue(string.IsNullOrEmpty(manager.Product));
		Assert.IsTrue(string.IsNullOrEmpty(manager.Version));
		Assert.IsNull(manager.PublishDate);
		Assert.AreEqual(0, manager.ExpirationDays);
		Assert.AreEqual(1, manager.Quantity);
		Assert.IsTrue(string.IsNullOrEmpty(manager.Name));
		Assert.IsTrue(string.IsNullOrEmpty(manager.Email));
		Assert.IsTrue(string.IsNullOrEmpty(manager.Company));
	}

	[TestMethod]
	public void TestCreateLicenseAndValidate()
	{
		Shared.LicenseManager manager = new();

		/// Create keypair
		manager.Passphrase = "This is another random passphrase dolor lorem.";
		manager.CreateKeypair();

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

		manager.CreateLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		Assert.AreEqual(LICENSE_TYPE, manager.StandardOrTrial);

		string publicKey = manager.KeyPublic;

		/// Validate license
		manager = new();

		string errorMessages = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty);
		Assert.IsTrue(string.IsNullOrEmpty(errorMessages));

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
		Shared.LicenseManager manager = new();

		// Create keypair
		manager.Passphrase = @"Ut exerci ad nonummy at amet elitr facilisis ipsum dolor iusto et takimata ut iriure. Elit eos ut accusam amet justo.";
		manager.CreateKeypair();

		const string PRODUCT_ID = "Badger Product ID";

		manager.ProductId = PRODUCT_ID;

		// Create license
		manager.CreateLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Validate license
		manager = new();

		string errorMessages = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssembly: string.Empty);
		Assert.IsTrue(string.IsNullOrEmpty(errorMessages));

		Assert.AreEqual(PRODUCT_ID, manager.ProductId);

		errorMessages = manager.IsThisLicenseValid("WRONG PRODUCT ID", publicKey, PathLicenseFile, pathAssembly: string.Empty);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages));
	}

	[TestMethod]
	public void TestMismatchedAssemblyIdentity()
	{
		// Create a file to act as the assembly file.
		string pathAssemblyFileGood = Path.Combine(PathLicenseFolder, _testContext.TestName + "Good.txt");
		File.WriteAllText(pathAssemblyFileGood, @"Tempor sanctus et. Accusam nonumy labore dolor takimata nibh stet sit qui duo vero.");

		string pathAssemblyFileBad = Path.Combine(PathLicenseFolder, _testContext.TestName + "Bad.txt");
		File.WriteAllText(pathAssemblyFileBad, @"Nonumy consectetuer et justo veniam. At stet est.");

		Shared.LicenseManager manager = new();

		// Create keypair
		manager.Passphrase = @"Sit dolor facilisi dolore amet autem. Amet stet sadipscing autem diam hendrerit.";
		manager.CreateKeypair();

		const string PRODUCT_ID = "Gazelle Product ID";

		manager.ProductId = PRODUCT_ID;
		manager.IsLockedToAssembly = true;
		manager.PathAssembly = pathAssemblyFileGood;

		// Create license
		manager.CreateLicenseFile(PathLicenseFile);
		Assert.IsTrue(File.Exists(PathLicenseFile));

		string publicKey = manager.KeyPublic;

		/// Validate license
		manager = new();

		string errorMessages = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssemblyFileGood);
		Assert.IsTrue(string.IsNullOrEmpty(errorMessages));

		errorMessages = manager.IsThisLicenseValid(PRODUCT_ID, publicKey, PathLicenseFile, pathAssemblyFileBad);
		Assert.IsFalse(string.IsNullOrEmpty(errorMessages));

	}
}
