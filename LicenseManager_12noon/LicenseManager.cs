using CommunityToolkit.Mvvm.ComponentModel;
using LicenseManager_12noon.Client;
using Org.BouncyCastle.Crypto;
using Standard.Licensing;
using Standard.Licensing.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LicenseManager_12noon;

/// <summary>
/// Create license file using a secret passphrase.
/// Validate license file with public key. (Does not require passphrase.)
/// </summary>
/// <example>
/// LicenseManager manager = new();
/// manager.Passphrase = "My secret passphrase."
/// manager.CreateKeypair();
/// manager.Product = "My Product";
/// manager.Version = "5.8.02 Beta";
/// manager.SaveLicenseFile("C:\Path\To\TheLicense.lic");
/// string publicKey = TheLicenseManager.KeyPublic;
/// </example>
/// <example>
/// LicenseManager manager = new();
/// bool isValid = manager.IsLicenseValid("My Product ID", "The Public Key", out string messages);
/// if (isValid)
/// {
///	// VALID
///	if (manager.StandardOrTrial == LicenseType.Trial)
///	{
///		// Example: LIMIT FEATURES FOR TRIAL
///	}
/// }
/// </example>
public partial class LicenseManager : ObservableObject
{
	public const string FileExtension_License = ".lic";
	public const string FileExtension_PrivateKey = ".private";

	private const string ELEMENT_NAME_ROOT = "private";
	private const string ATTRIBUTE_NAME_VERSION = "version";

	private const string ELEMENT_NAME_ID = "id";

	private const string ELEMENT_NAME_SECRET = "secret";
	private const string ELEMENT_NAME_PASSPHRASE = "passphrase";
	private const string ELEMENT_NAME_PRIVATEKEY = "private-key";

	private const string ELEMENT_NAME_APP = "application";
	private const string ELEMENT_NAME_PUBLICKEY = "public-key";
	private const string ELEMENT_NAME_PRODUCT_ID = "product-id";

	private const string ELEMENT_NAME_CUSTOMER = "customer";
	private const string ELEMENT_NAME_NAME = "name";
	private const string ELEMENT_NAME_EMAIL = "email";
	private const string ELEMENT_NAME_COMPANY = "company";

	private const string ELEMENT_NAME_PRODUCT = "product";
	private const string ELEMENT_NAME_PRODUCT_NAME = "product-name";
	private const string ELEMENT_NAME_VERSION = "version";
	private const string ELEMENT_NAME_PUBLISH_DATE = "publish-date-utc";

	private const string ELEMENT_NAME_LICENSE = "license";
	private const string ELEMENT_NAME_STANDARD_OR_TRIAL = "standard-or-trial";
	private const string ELEMENT_NAME_EXPIRATION_DATE = "expiration-date";
	private const string ELEMENT_NAME_EXPIRATION_DAYS = "expiration-days";
	private const string ELEMENT_NAME_QUANTITY = "quantity";

	private const string ELEMENT_NAME_PATHASSEMBLY = "path-assembly";

	private const string ProductFeature_Name_Product = "Product";
	private const string ProductFeature_Name_Version = "Version";
	private const string ProductFeature_Name_PublishDate = "Publish Date";

	private const string Attribute_Name_ProductIdentity = "Product Identity";
	private const string Attribute_Name_AssemblyIdentity = "Assembly Identity";
	private const string Attribute_Name_ExpirationDays = "Expiration Days";

	private LicenseFile _licenseFile = new();

	[ObservableProperty]
	private LicenseType _standardOrTrial = LicenseType.Standard;
	[ObservableProperty]
	private DateTime _expirationDateUTC = DateTime.MaxValue.Date;
	[ObservableProperty]
	private int _expirationDays;
	partial void OnExpirationDaysChanged(int value)
	{
		ExpirationDateUTC = MyNow.UtcNow().Date.AddDays(value);
	}
	[ObservableProperty]
	private int _quantity = 1;

	[ObservableProperty]
	private Guid _id = Guid.NewGuid();
	[ObservableProperty]
	private string _product = string.Empty;
	[ObservableProperty]
	private string _version = string.Empty;
	[ObservableProperty]
	private DateOnly? _publishDate;

	[ObservableProperty]
	private string _name = string.Empty;
	[ObservableProperty]
	private string _email = string.Empty;
	[ObservableProperty]
	private string _company = string.Empty;

	[ObservableProperty]
	private string _passphrase = string.Empty;


	[ObservableProperty]
	private string _keyPublic = string.Empty;

	private string KeyPrivate = string.Empty;

	/// <summary>
	/// These properties are NOT stored in the license file because it is used to
	/// validate that the license file is associated with the product calling it.
	/// </summary>
	[ObservableProperty]
	private bool _isLockedToAssembly = false;
	[ObservableProperty]
	private string _pathAssembly = string.Empty;
	[ObservableProperty]
	private string _productId = string.Empty;
	private static string CreateProductIdentity(string productId, string keyPublic) => productId + " " + keyPublic;


	/// <summary>
	/// Indicates if any of the properties have changed.
	/// (If so, the keypair file must be saved.)
	/// </summary>
	private void ClearKeypairDirtyFlag() => IsKeypairDirty = false;
	public void ClearLicenseDirtyFlag() => IsLicenseDirty = false;
	[ObservableProperty]
	private bool _isKeypairDirty = false;
	[ObservableProperty]
	private bool _isLicenseDirty = false;
	/// <summary>
	/// We need to know if changes have been made to any of the properties.
	/// If so, we require the user to save the keypair file
	/// (before saving or validating the license).
	/// </summary>
	/// <param name="e"></param>
	protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		// Changing the IsKeypairDirty or IsLicenseDirty property does not make the object dirty.
		if ((e.PropertyName != nameof(IsKeypairDirty)) && (e.PropertyName != nameof(IsLicenseDirty)))
		{
			IsKeypairDirty = true;
			IsLicenseDirty = true;
		}
	}


	/*
	 * We handle two use cases:
	 *
	 * 1. Licensor -- private passphrase and key to create keypair and license file(s) for OTHER executables.
	 * 2. Licensee -- public key to load and validate license file for THIS executable.
	 */
	public LicenseManager()
	{
	}

	public void CreateKeypair()
	{
		ArgumentException.ThrowIfNullOrEmpty(Passphrase);

		KeyGenerator keyGenerator = KeyGenerator.Create();
		KeyPair keyPair = keyGenerator.GenerateKeyPair();
		KeyPrivate = keyPair.ToEncryptedPrivateKeyString(Passphrase);
		KeyPublic = keyPair.ToPublicKeyString();
	}

	public void NewID()
	{
		Id = Guid.NewGuid();
	}

	/// <summary>
	///
	/// </summary>
	/// <remarks></remarks>
	/// <exception cref="FileNotFoundException">
	/// The keypair file does not exist.
	/// </exception>
	/// <param name="pathKeypair"></param>
	public void LoadKeypair(string pathKeypair)
	{
		ConvertOldPrivateFile(pathKeypair);

		XDocument xmlDoc = XDocument.Load(pathKeypair);

		XElement root = xmlDoc.Element(ELEMENT_NAME_ROOT)!;

		XElement secret = root.Element(ELEMENT_NAME_SECRET)!;
		Passphrase = secret.Element(ELEMENT_NAME_PASSPHRASE)!.Value;
		KeyPrivate = secret.Element(ELEMENT_NAME_PRIVATEKEY)!.Value;

		Id = (Guid)root.Element(ELEMENT_NAME_ID)!;

		XElement app = root.Element(ELEMENT_NAME_APP)!;
		KeyPublic = app.Element(ELEMENT_NAME_PUBLICKEY)!.Value;
		ProductId = app.Element(ELEMENT_NAME_PRODUCT_ID)!.Value;

		XElement customer = root.Element(ELEMENT_NAME_CUSTOMER)!;
		Name = customer.Element(ELEMENT_NAME_NAME)!.Value;
		Email = customer.Element(ELEMENT_NAME_EMAIL)!.Value;
		Company = customer.Element(ELEMENT_NAME_COMPANY)!.Value;

		XElement product = root.Element(ELEMENT_NAME_PRODUCT)!;
		Product = product.Element(ELEMENT_NAME_PRODUCT_NAME)!.Value;
		Version = product.Element(ELEMENT_NAME_VERSION)!.Value;
		string publishDate = product.Element(ELEMENT_NAME_PUBLISH_DATE)!.Value;
		PublishDate = string.IsNullOrEmpty(publishDate) ? null : DateOnly.Parse(publishDate, CultureInfo.InvariantCulture);

		XElement license = root.Element(ELEMENT_NAME_LICENSE)!;
		StandardOrTrial = Enum.Parse<LicenseType>(license.Element(ELEMENT_NAME_STANDARD_OR_TRIAL)!.Value);

		ExpirationDateUTC = DateTime.Parse(license.Element(ELEMENT_NAME_EXPIRATION_DATE)!.Value,
														CultureInfo.InvariantCulture,
														DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
		//ExpirationDays = Convert.ToInt32(license.Element(ELEMENT_NAME_EXPIRATION_DAYS)!.Value);
		ExpirationDays = (ExpirationDateUTC == DateTime.MaxValue.Date) ? 0 : (ExpirationDateUTC - MyNow.UtcNow().Date).Days;
		Quantity = Convert.ToInt32(license.Element(ELEMENT_NAME_QUANTITY)!.Value);
		PathAssembly = root.Element(ELEMENT_NAME_PATHASSEMBLY)!.Value;
		IsLockedToAssembly = !string.IsNullOrEmpty(PathAssembly);

		ClearKeypairDirtyFlag();
		ClearLicenseDirtyFlag();
	}

	/// <summary>
	/// Convert old private and license files to the new format.
	/// </summary>
	/// <remarks>
	/// Feb 2025: Delete when no longer needed.
	/// </remarks>
	/// <param name="pathKeypair"></param>
	private void ConvertOldPrivateFile(string pathKeypair)
	{
		XDocument xmlDocKeypair = XDocument.Load(pathKeypair);
		XElement root = xmlDocKeypair.Element(ELEMENT_NAME_ROOT)!;
		XAttribute? versionAttribute = root.Attribute(ATTRIBUTE_NAME_VERSION);
		if (versionAttribute is not null)
		{
			return;
		}

		try
		{
			// Load properties from old private key file.
			Passphrase = root.Element("passphrase")!.Value;
			KeyPrivate = root.Element("private-key")!.Value;
			KeyPublic = root.Element("public-key")!.Value;
			Id = (Guid)root.Element("id")!;
			ProductId = root.Element("product-id")!.Value;
			PathAssembly = root.Element("path-assembly")!.Value;
			IsLockedToAssembly = !string.IsNullOrEmpty(PathAssembly);

			// Load other properties from license file.
			string pathLicense = Path.ChangeExtension(pathKeypair, ".lic");
			XDocument xmlDocLicense = XDocument.Load(pathLicense);
			root = xmlDocLicense.Element("License")!;

			StandardOrTrial = Enum.Parse<LicenseType>(root.Element("Type")!.Value);

			Product = GetNestedValue(root, "Feature", "Product");
			Version = GetNestedValue(root, "Feature", "Version");
			string publishDate = GetNestedValue(root, "Feature", "Publish Date");
			PublishDate = string.IsNullOrEmpty(publishDate) ? null : DateOnly.Parse(publishDate, CultureInfo.InvariantCulture);

			XElement customer = root.Element("Customer")!;
			Name = customer.Element("Name")!.Value;
			Email = customer.Element("Email")!.Value;
			Company = customer.Element("Company")?.Value ?? string.Empty;

			ExpirationDays = Convert.ToInt32(GetNestedValue(root, "Attribute", "Expiration Days"));
			//ExpirationDateUTC is a dependent property and automatically updated.

			SaveKeypair(pathKeypair);
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show("Error converting keypair file: " + ex.Message, "12noon License Manager");
		}

		///
		string GetNestedValue(XElement root, string tag, string name)
		{
			return root.Descendants(tag)
						.FirstOrDefault(e => e.Attribute("name")?.Value == name)
						?.Value ?? string.Empty;
		}
	}

	/// <summary>
	/// Save public/private keys and passphrase as XML.
	/// Change the extension of the passed file to ".private."
	/// Also save ID, product ID, and path to assembly (optional)
	/// so we do not forget them.
	/// </summary>
	/// <remarks>
	/// THIS FILE MUST BE KEPT SECRET.
	/// </remarks>
	/// <param name="pathKeypair"></param>
	public void SaveKeypair(string pathKeypair)
	{
		new XDocument(
			new XElement(ELEMENT_NAME_ROOT
				, new XAttribute(ATTRIBUTE_NAME_VERSION, 2)
				, new XElement(ELEMENT_NAME_ID, Id)
				, new XElement(ELEMENT_NAME_SECRET
					, new XElement(ELEMENT_NAME_PASSPHRASE, Passphrase)
					, new XElement(ELEMENT_NAME_PRIVATEKEY, KeyPrivate)
				)
				, new XElement(ELEMENT_NAME_APP
					, new XElement(ELEMENT_NAME_PUBLICKEY, KeyPublic)
					, new XElement(ELEMENT_NAME_PRODUCT_ID, ProductId)
				)
				, new XElement(ELEMENT_NAME_CUSTOMER
					, new XElement(ELEMENT_NAME_NAME, Name)
					, new XElement(ELEMENT_NAME_EMAIL, Email)
					, new XElement(ELEMENT_NAME_COMPANY, Company)
				)
				, new XElement(ELEMENT_NAME_PRODUCT
					, new XElement(ELEMENT_NAME_PRODUCT_NAME, Product)
					, new XElement(ELEMENT_NAME_VERSION, Version)
					, new XElement(ELEMENT_NAME_PUBLISH_DATE, PublishDate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
				)
				, new XElement(ELEMENT_NAME_LICENSE
					, new XElement(ELEMENT_NAME_STANDARD_OR_TRIAL, StandardOrTrial)
					, new XElement(ELEMENT_NAME_EXPIRATION_DATE, ExpirationDateUTC.ToString(CultureInfo.InvariantCulture))
					, new XElement(ELEMENT_NAME_EXPIRATION_DAYS, ExpirationDays)
					, new XElement(ELEMENT_NAME_QUANTITY, Quantity)
				)
				, new XElement(ELEMENT_NAME_PATHASSEMBLY, PathAssembly)
			)
		)
		.Save(pathKeypair);

		ClearKeypairDirtyFlag();
	}

	/// <summary>
	/// Creates a new license file with the passed path.
	/// Required properties:
	///	Passphrase
	///	Private key
	///	Public key
	///	Id
	///	ProductId
	///	Product
	///	Version
	///	Quantity
	///	Name
	///	Email
	///
	/// Optional properties:
	///	ExpirationDays
	///	Company
	///	Path to Assembly
	/// </summary>
	/// <exception cref="InvalidCipherTextException">
	/// You probably changed the passphrase and did not generate a new keypair.
	/// </exception>
	/// <param name="pathLicense">Full path to license file: MyApplication.lic</param>
	public void SaveLicenseFile(string pathLicense)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(Passphrase, nameof(Passphrase));
		ArgumentException.ThrowIfNullOrWhiteSpace(KeyPrivate, nameof(KeyPrivate));
		ArgumentException.ThrowIfNullOrWhiteSpace(KeyPublic, nameof(KeyPublic));

		if (Id == Guid.Empty)
		{
			throw new ArgumentOutOfRangeException(nameof(Id), "Id must be a valid GUID.");
		}
		ArgumentException.ThrowIfNullOrWhiteSpace(ProductId, nameof(ProductId));
		ArgumentException.ThrowIfNullOrWhiteSpace(Product, nameof(Product));
		ArgumentException.ThrowIfNullOrWhiteSpace(Version, nameof(Version));
		if (Quantity < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(Quantity), "License quantity must be one or more.");
		}
		// Expiration optional but cannot be negative
		if (ExpirationDays < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(ExpirationDays), "Expiration days must be zero (no expiry) or positive.");
		}

		ArgumentException.ThrowIfNullOrWhiteSpace(Name, nameof(Name));
		ArgumentException.ThrowIfNullOrWhiteSpace(Email, nameof(Email));
		// Company optional

		/// Create a hash to verify that this license is associated with the caller.
		/// Without this, an attacker could use the license file with ANY product
		/// (because we save the public key in the license file--which we do to
		/// prevent an attacker from substituting their own public key in the
		/// assembly and creating their own licenses).
		string identityProduct = SecureHash.ComputeSHA256Hash(CreateProductIdentity(ProductId, KeyPublic));

		// Optionally, tie the license file to only THIS instance of the calling assembly.
		string identityAssembly = (IsLockedToAssembly && !string.IsNullOrWhiteSpace(PathAssembly)) ? SecureHash.ComputeSHA256HashFile(PathAssembly) : string.Empty;

		ILicenseBuilder licenseBuilder =
			License.New()
			.WithUniqueIdentifier(Id)
			.As(StandardOrTrial);
		if (ExpirationDays > 0)
		{
			// ExpiresAt() converts passed date/time to UTC and assigns to Expiration property.
			licenseBuilder.ExpiresAt(MyNow.Now().Date.AddDays(ExpirationDays));
		}
		licenseBuilder
			.WithMaximumUtilization(Quantity)
			.WithProductFeatures(
				new Dictionary<string, string>
				{
					[ProductFeature_Name_Product] = Product,
					[ProductFeature_Name_Version] = Version,
					[ProductFeature_Name_PublishDate] = PublishDate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
				}
			)
			.LicensedTo(Name, Email, (Customer c) =>
			{
				if (!string.IsNullOrWhiteSpace(Company))
				{
					c.Company = Company;
				}
			})
			.WithAdditionalAttributes(
				new Dictionary<string, string>()
				{
					[Attribute_Name_ProductIdentity] = identityProduct,
					[Attribute_Name_AssemblyIdentity] = identityAssembly,
					[Attribute_Name_ExpirationDays] = (ExpirationDays == 0) ? string.Empty : ExpirationDays.ToString(),
				}
			);
		// InvalidCipherTextException probably means you changed the passphrase and did not generate a new keypair.
		License license = licenseBuilder.CreateAndSignWithPrivateKey(KeyPrivate, Passphrase);

		// Note: This emits a license file in clear text with an encrypted signature.
		File.WriteAllText(pathLicense, license.ToString(), Encoding.UTF8);

		// OR: using (var xmlWriter = System.Xml.XmlWriter.Create(filePath)) { license.Save(xmlWriter); }

		ClearLicenseDirtyFlag();
	}

	/// <summary>
	/// Validate the passed license file.
	/// If the license is valid, it loads the license information into their corresponding properties.
	/// All exceptions are caught.
	/// </summary>
	/// <remarks>
	/// Although the "assembly" is not required to be an assembly (it can be a
	/// text file, etc.) it makes more sense for it to be the calling assembly
	/// so that it can be verified to match the license file.
	/// </remarks>
	/// <param name="productID">String to verify that the license file is associated with this product.</param>
	/// <param name="publicKey">Public encryption key</param>
	/// <param name="pathLicense">Path to license file (.lic).</param>
	/// <param name="pathAssembly">Path to the calling assembly associated with the license file.</param>
	/// <param name="messages">Output parameter to hold messages, especially if the license is invalid.</param>
	/// <returns>True if the license is valid, otherwise false.</returns>
	public bool IsThisLicenseValid(string productID, string publicKey, string pathLicense, string pathAssembly, out string messages)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(productID);
		ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);
		ArgumentException.ThrowIfNullOrWhiteSpace(pathLicense);

		messages = string.Empty;

		try
		{
			KeyPublic = publicKey;
			bool isValid = _licenseFile.IsThisLicenseValid(productID, publicKey, pathLicense, pathAssembly, out string theErrors);

			if (isValid)
			{
				List<string> differences = [];

				// Check and list differences
				if (!string.IsNullOrEmpty(ProductId) && (ProductId != _licenseFile.ProductId))
				{
					differences.Add($"Product ID: Current = {ProductId}, New = {_licenseFile.ProductId}");
				}
				if (!string.IsNullOrEmpty(PathAssembly) && (PathAssembly != pathAssembly))
				{
					differences.Add($"Assembly path: Current = {PathAssembly}, New = {pathAssembly}");
				}
				if (IsLockedToAssembly != _licenseFile.IsLockedToAssembly)
				{
					differences.Add($"IsLockedToAssembly: Current = {IsLockedToAssembly}, New = {_licenseFile.IsLockedToAssembly}");
				}
				if (!string.IsNullOrEmpty(Product) && (Product != _licenseFile.Product))
				{
					differences.Add($"Product: Current = {Product}, New = {_licenseFile.Product}");
				}
				if (!string.IsNullOrEmpty(Version) && (Version != _licenseFile.Version))
				{
					differences.Add($"Version: Current = {Version}, New = {_licenseFile.Version}");
				}
				if (PublishDate.HasValue && (PublishDate != _licenseFile.PublishDate))
				{
					differences.Add($"Publish date: Current = {PublishDate:D}, New = {_licenseFile.PublishDate:D}");
				}
				if (StandardOrTrial != _licenseFile.StandardOrTrial)
				{
					differences.Add($"Type: Current = {StandardOrTrial}, New = {_licenseFile.StandardOrTrial}");
				}
				if (ExpirationDateUTC != _licenseFile.ExpirationDateUTC)
				{
					differences.Add($"Expiration date: " +
						$"Current = {((ExpirationDateUTC == DateTime.MaxValue.Date) ? "None" : ExpirationDateUTC):D}, " +
						$"New = {((_licenseFile.ExpirationDateUTC == DateTime.MaxValue.Date) ? "None" : _licenseFile.ExpirationDateUTC):D}"
					);
				}
				if (ExpirationDays != _licenseFile.ExpirationDays)
				{
					differences.Add($"Expiration days: Current = {ExpirationDays}, New = {_licenseFile.ExpirationDays}");
				}
				if (Quantity != _licenseFile.Quantity)
				{
					differences.Add($"Quantity: Current = {Quantity}, New = {_licenseFile.Quantity}");
				}
				if (!string.IsNullOrEmpty(Name) && (Name != _licenseFile.Name))
				{
					differences.Add($"Name: Current = {Name}, New = {_licenseFile.Name}");
				}
				if (!string.IsNullOrEmpty(Email) && (Email != _licenseFile.Email))
				{
					differences.Add($"Email: Current = {Email}, New = {_licenseFile.Email}");
				}
				if (!string.IsNullOrEmpty(Company) && (Company != _licenseFile.Company))
				{
					differences.Add($"Company: Current = {Company}, New = {_licenseFile.Company}");
				}

				if (differences.Any())
				{
					messages = "The license is valid but the following properties differ from the keypair file:" + Environment.NewLine
									+ string.Join(Environment.NewLine, differences);
				}

				// Copy the license properties to this class.
				ProductId = _licenseFile.ProductId;
				PathAssembly = pathAssembly;
				IsLockedToAssembly = _licenseFile.IsLockedToAssembly;

				Product = _licenseFile.Product;
				Version = _licenseFile.Version;
				PublishDate = _licenseFile.PublishDate;

				StandardOrTrial = _licenseFile.StandardOrTrial;
				ExpirationDateUTC = _licenseFile.ExpirationDateUTC;
				ExpirationDays = _licenseFile.ExpirationDays;
				Quantity = _licenseFile.Quantity;

				Name = _licenseFile.Name;
				Email = _licenseFile.Email;
				Company = _licenseFile.Company;
			}
			else
			{
				messages = theErrors;
			}

			return isValid;
		}
		finally
		{
			ClearLicenseDirtyFlag();
		}
	}
}
