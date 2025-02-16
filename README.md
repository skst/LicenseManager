# License Manager by [12noon LLC](https://12noon.com)

[![](https://img.shields.io/github/v/release/skst/LicenseManager.svg?label=latest%20release&color=007edf)](https://github.com/skst/LicenseManager/releases/latest)
[![build](https://github.com/skst/LicenseManager/actions/workflows/dotnet.yml/badge.svg)](https://github.com/skst/LicenseManager/actions/workflows/dotnet.yml)
[![GitHub last commit](https://img.shields.io/github/last-commit/skst/LicenseManager)](https://github.com/skst/LicenseManager)

This is a graphical front-end for the [Standard.Licensing](https://github.com/junian/Standard.Licensing) project.

![License Manager](https://raw.githubusercontent.com/skst/LicenseManager/master/12noon.LicenseManager.png)

## Features

### Keys

| Property | Usage |
|----------|-------|
| Passphrase | Secret used to generate public/private keypair and to create a license |
| Public key | Used by the licensed application to validate the license |
| ID | License ID (You can use it any way you want or not at all) |
| Product ID | Used by the licensed application to verify the executable and public key. |
| Lock to assembly | This ensures the license is associated _ONLY_ with _THIS_ build of the licensed application. |

The application maintains the private key in the `.private` file but does not display it.

### Product

| Property | Usage |
|----------|-------|
| Name | The product name |
| Version | The product version |
| Date published | The date of publishing |

These values can be displayed by the licensed application.

The publish date can represent any date you want.

### License

| Property | Usage |
|----------|-------|
| Type | Standard or trial license |
| Expiration | The number of days in which the license expires. Zero means no expiry. |
| Quantity | Minimum value is one (1) |

The licensed application can check the type to permit only certain features.

If the expiration is set to zero, there is no expiry.

The quantity is not enforced.

### Licensee

This information can be displayed by the licensed application.

| Property | Usage |
|----------|-------|
| Name | Name of the licensee |
| Email | Email of the licensee |
| Company | Company of the licensee (optional) |

## Usage

### Create a New License

Note that the public key and product ID are passed by the licensed application
to validate the license, so you only want to create a new keypair or change the
product ID if you want to change them in the licensed application, rebuild it,
and create new licenses for anyone who will use the new build.

1. Create a keypair by entering a value for _Passphrase_ and pressing _Create Keypair_ button.
1. Enter a _Product ID_.
1. Optionally, lock the license to a specific build of the licensed application.
1. Fill in the product information, license information, and licensee information.
1. Press the _Save Keypair..._ button. This will prompt you for where to save the `.private` file.
1. Press the _Save License..._ button. This will prompt you for where to save the `.lic` file.

The `.private` file contains all of the information used to create the license, including the secrets.
Do keep the `.private` file somewhere safe.
Do NOT add the `.private` file to source control.
You will need it to create more licenses for your licensed application
(unless you want to update the application to use a new public key).

### Create a License Based on an Existing License

1. Press the *Load Keypair or License or Both...* button to select a `.private` or
`.lic` file (or both of them). Alternatively, you can drag/drop a `.private` and/or `.lic` file.
1. After loading both files, License Manager will validate the license file.

If the license is invalid (_e.g._, it expired or the assembly has changed), you can create a new (valid) license.

1. Now you can update the product, license, or licensee information as needed.
1. Press the _Save Keypair..._ button to save the keypair file. This will
prompt you for where to save the `.private` file.
1. Press the _Save License..._ button to create a new license. This will
prompt you for where to save the `.lic` file.

### The Licensed Application

The licensed application must use the `LicenseManager` class to validate the license.
This means that it will also need to include the following NuGet packages:
* Standard.Licensing
* CommunityToolkit.Mvvm

The licensed application must pass the `Product ID` and the `Public Key` to the license validation API.

```
LicenseManager manager = new();
string errorMessages = manager.IsLicenseValid(productID: "... My Product ID ...", publicKey: "... public key ...");
if (!string.IsNullOrEmpty(errorMessages))
{
	// INVALID
	MessageBox.Show("The license is invalid. " + errorMessages);
	return;
}

// VALID
if (manager.StandardOrTrial == LicenseType.Trial)
{
	// Example: LIMIT FEATURES FOR TRIAL
}
```

If the license is valid, you can use any of the properties (_e.g._, for display or to limit features).

Note: Of course, the hash of _Product ID_ and _Public Key_ will not prevent a determined
hacker from working around the license. However, it will prevent a simple text substitution
of the public key.

You could also do something more involved, such as prompting the licensee the first
time they run the application to enter some secret text (_e.g._, a password or GUID)
and storing a hash of it and the public key in protected storage.
Then the application could use the hash as the _Product ID_.
Of course, the licensee would have to keep that text as secret as they
should keep the license file.
