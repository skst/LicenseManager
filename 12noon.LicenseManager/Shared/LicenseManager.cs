using CommunityToolkit.Mvvm.ComponentModel;
using Org.BouncyCastle.Crypto;
using Standard.Licensing;
using Standard.Licensing.Security.Cryptography;
using Standard.Licensing.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Shared;

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
/// manager.CreateLicenseFile("C:\Path\To\TheLicense.lic");
/// string publicKey = TheLicenseManager.KeyPublic;
/// </example>
/// <example>
/// LicenseManager manager = new();
/// string errorMessages = manager.IsLicenseValid("My Product ID", "... public key ...");
/// if (string.IsNullOrEmpty(errorMessages))
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

	private const string MESSAGE_LICENSE_MISSING1 = "Unable to find license file {0}.";
	private const string MESSAGE_LICENSE_INVALID_PRODUCT_IDENTITY1 = "License file {0} is not associated with this product.";
	private const string MESSAGE_LICENSE_INVALID_PRODUCT_INSTANCE1 = "License file {0} is not associated with this instance of the product.";
	private const string MESSAGE_LICENSE_INVALID = "License validation failure.";
	private const string MESSAGE_LICENSE_RESOLVE = "Please contact your company's IT department or Support at 12noon.com.";

	private const string FileExtension_PrivateKey = ".private";
	private const string ELEMENT_NAME_ROOT = "private";
	private const string ELEMENT_NAME_PASSPHRASE = "passphrase";
	private const string ELEMENT_NAME_PRIVATEKEY = "private-key";
	private const string ELEMENT_NAME_PUBLICKEY = "public-key";
	private const string ELEMENT_NAME_ID = "id";
	private const string ELEMENT_NAME_PRODUCTID = "product-id";
	private const string ELEMENT_NAME_PATHASSEMBLY = "path-assembly";

	private const string ProductFeature_Name_Product = "Product";
	private const string ProductFeature_Name_Version = "Version";
	private const string ProductFeature_Name_PublishDate = "Publish Date";

	private const string Attribute_Name_ProductIdentity = "Product Identity";
	private const string Attribute_Name_AssemblyIdentity = "Assembly Identity";
	private const string Attribute_Name_ExpirationDays = "Expiration Days";

	/*
	 * Licensor: These properties are set for creating a new license.
	 * Licensee: These properties are set when a license has been validated.
	 */
	[ObservableProperty]
	private LicenseType _standardOrTrial = LicenseType.Standard;
	[ObservableProperty]
	private int _expirationDays;
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
	/// The .private file does not exist.
	/// </exception>
	/// <param name="pathLicense"></param>
	public void LoadKeypair(string pathLicense)
	{
		string pathPrivate = Path.ChangeExtension(pathLicense, FileExtension_PrivateKey);
		XDocument xmlDoc = XDocument.Load(pathPrivate);

		Passphrase = xmlDoc.Element(ELEMENT_NAME_ROOT)!.Element(ELEMENT_NAME_PASSPHRASE)!.Value;
		KeyPrivate = xmlDoc.Element(ELEMENT_NAME_ROOT)!.Element(ELEMENT_NAME_PRIVATEKEY)!.Value;
		KeyPublic = xmlDoc.Element(ELEMENT_NAME_ROOT)!.Element(ELEMENT_NAME_PUBLICKEY)!.Value;
		Id = (Guid)xmlDoc.Element(ELEMENT_NAME_ROOT)!.Element(ELEMENT_NAME_ID)!;
		ProductId = xmlDoc.Element(ELEMENT_NAME_ROOT)!.Element(ELEMENT_NAME_PRODUCTID)!.Value;
		PathAssembly = xmlDoc.Element(ELEMENT_NAME_ROOT)!.Element(ELEMENT_NAME_PATHASSEMBLY)!.Value;
		IsLockedToAssembly = !string.IsNullOrEmpty(PathAssembly);
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
	/// <param name="pathLicense"></param>
	public void SaveKeypair(string pathLicense)
	{
		new XDocument(
			new XElement(ELEMENT_NAME_ROOT,
				new XElement(ELEMENT_NAME_PASSPHRASE, Passphrase),
				new XElement(ELEMENT_NAME_PRIVATEKEY, KeyPrivate),
				new XElement(ELEMENT_NAME_PUBLICKEY, KeyPublic),
				new XElement(ELEMENT_NAME_ID, Id),
				new XElement(ELEMENT_NAME_PRODUCTID, ProductId),
				new XElement(ELEMENT_NAME_PATHASSEMBLY, PathAssembly)
			)
		)
		.Save(Path.ChangeExtension(pathLicense, FileExtension_PrivateKey));
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
	///
	/// Optional properties:
	///	Path to Assembly
	/// </summary>
	/// <exception cref="InvalidCipherTextException">
	/// You probably changed the passphrase and did not generate a new keypair.
	/// </exception>
	/// <param name="pathLicense">Full path to license file: MyApplication.lic</param>
	public void CreateLicenseFile(string pathLicense)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(Passphrase));
		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(KeyPrivate));
		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(KeyPublic));

		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(Id));
		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(ProductId));
		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(Product));
		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(Version));
		if (Quantity < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(Quantity));
		}
		// Expiration optional

		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(Name));
		ArgumentException.ThrowIfNullOrWhiteSpace(nameof(Email));
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
			licenseBuilder.ExpiresAt(DateTime.Now.AddDays(ExpirationDays));
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

		SaveKeypair(pathLicense);
	}

	/// <summary>
	/// This is the public-facing API used by the licensed app to validate its license.
	/// If the license is valid, it loads the license information into their corresponding properties.
	/// </summary>
	/// <example>
	/// string errorMessages = IsLicenseValid("My Product ID", publicKey);
	/// if (!string.IsNullOrEmpty(errorMessages))
	/// {
	///	MessageBox.Show("License is invalid." + Environment.NewLine + errorMessages, "12noon License Manager");
	///	return false;
	/// }
	/// MessageBox.Show("License validated successfully.", "12noon License Manager");
	/// </example>
	/// <param name="productID">String to verify that the license file is associated with this product.</param>
	/// <param name="publicKey">Public encryption key</param>
	/// <returns>Empty string if the license is valid. Otherwise, it returns the error message(s).</returns>
	public string IsLicenseValid(string productID, string publicKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(productID);
		ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);

		string pathLicense = GetLicensePath();
		return IsThisLicenseValid(productID, publicKey, pathLicense, GetAssemblyFilePath());
	}

	/// <summary>
	/// Validate the passed license file.
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
	/// <returns>Empty string if the license is valid. Otherwise, it returns the error message(s).</returns>
	public string IsThisLicenseValid(string productID, string publicKey, string pathLicense, string pathAssembly)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(productID);
		ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);
		ArgumentException.ThrowIfNullOrWhiteSpace(pathLicense);

		ProductId = productID;
		KeyPublic = publicKey;
		IsLockedToAssembly = false;

		Product = string.Empty;
		Version = string.Empty;
		PublishDate = null;

		ExpirationDays = 0;
		Quantity = 0;

		Name = string.Empty;
		Email = string.Empty;
		Company = string.Empty;

		try
		{
			if (!File.Exists(pathLicense))
			{
				return string.Format(MESSAGE_LICENSE_MISSING1, pathLicense) + Environment.NewLine + Environment.NewLine + MESSAGE_LICENSE_RESOLVE;
			}

			string xmlLicense = File.ReadAllText(pathLicense, Encoding.UTF8);
			License license = License.Load(xmlLicense);

			List<IValidationFailure> validationFailures = new();

			/// Required
			string identityProductLicense = license.AdditionalAttributes.Get(Attribute_Name_ProductIdentity);
			string identityProductCaller = SecureHash.ComputeSHA256Hash(CreateProductIdentity(productID, publicKey));
			if (identityProductCaller != identityProductLicense)
			{
				validationFailures.Add(
					new GeneralValidationFailure()
					{
						Message = string.Format(MESSAGE_LICENSE_INVALID_PRODUCT_IDENTITY1, pathLicense),
						HowToResolve = MESSAGE_LICENSE_RESOLVE,
					}
				);
			}

			/// Optional: If an assembly hash is in the license file, test it.
			string identityAssemblyLicense = license.AdditionalAttributes.Get(Attribute_Name_AssemblyIdentity);
			if (!string.IsNullOrWhiteSpace(identityAssemblyLicense))
			{
				IsLockedToAssembly = true;
				string identityAssemblyCaller = SecureHash.ComputeSHA256HashFile(pathAssembly);
				if (identityAssemblyCaller != identityAssemblyLicense)
				{
					validationFailures.Add(
						new GeneralValidationFailure()
						{
							Message = string.Format(MESSAGE_LICENSE_INVALID_PRODUCT_INSTANCE1, pathLicense),
							HowToResolve = MESSAGE_LICENSE_RESOLVE,
						}
					);
				}
			}

			string expirationDays = license.AdditionalAttributes.Get(Attribute_Name_ExpirationDays);
			IEnumerable<IValidationFailure> loadErrors =
				license
					.Validate()
					.ExpirationDate()
					.When(lic => !string.IsNullOrEmpty(expirationDays))
					// Only check the expiry WHEN the license is Trial.
					// https://github.com/junian/Standard.Licensing/issues/21
					//.When(lic => lic.Type == LicenseType.Trial)
					.And()
					.Signature(publicKey)
					.AssertValidLicense()
				??
				[
					new GeneralValidationFailure()
					{
						Message = MESSAGE_LICENSE_INVALID,
						HowToResolve = MESSAGE_LICENSE_RESOLVE,
					}
				];
			if (!loadErrors.Any())
			{
				Product = license.ProductFeatures.Get(ProductFeature_Name_Product);
				Version = license.ProductFeatures.Get(ProductFeature_Name_Version);
				string s = license.ProductFeatures.Get(ProductFeature_Name_PublishDate);
				if (!string.IsNullOrEmpty(s))
				{
					PublishDate = DateOnly.Parse(s);
				}

				StandardOrTrial = license.Type;
				/// Get the number of days REMAINING until expiration.
				if (license.Expiration.Date != DateTime.MaxValue.Date)
				{
					ExpirationDays = Convert.ToInt32(license.Expiration.Subtract(DateTime.UtcNow).TotalDays);
				}
				/// This is the number of days until expiration ORIGINALLY specified.
				//if (!string.IsNullOrEmpty(expirationDays))
				//{
				//	ExpirationDays = Convert.ToInt32(expirationDays);
				//}

				Quantity = license.Quantity;

				Name = license.Customer.Name;
				Email = license.Customer.Email;
				Company = license.Customer.Company;
			}
			validationFailures.AddRange(loadErrors);

			if (validationFailures.Count > 0)
			{
				List<string> errorMessages = [];
				foreach (var failure in validationFailures)
				{
					errorMessages.Add($"{failure.GetType().Name}: {failure.Message}{Environment.NewLine}{failure.HowToResolve}");
				}
				return string.Join(Environment.NewLine, errorMessages);
			}
		}
		catch (FileNotFoundException ex)
		{
			return ex.Message;
		}
		catch (Exception ex)
		{
			return ex.Message;
		}

		return string.Empty;
	}

	private static string GetLicensePath()
	{
		string pathExecutable = GetAssemblyFilePath();
		return Path.ChangeExtension(pathExecutable, FileExtension_License);
	}

	/// <summary>
	/// Return the path to the main (entry) assembly (.exe).
	/// </summary>
	/// <example>
	/// C:\Path\To\Executable.exe
	/// </example>
	/// <see cref="Assembly.GetEntryAssembly" />
	/// <seealso cref="Assembly.GetExecutingAssembly" />
	/// <returns>Path to the main (entry) assembly (.exe)</returns>
	public static string GetAssemblyFilePath()
	{
		// AppContext.BaseDirectory is just the folder path (e.g., "C:\Path\To\").
		Assembly? asm = Assembly.GetEntryAssembly();
		return asm?.Location ?? string.Empty;
	}
}
