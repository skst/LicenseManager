# License Manager by [12noon LLC](https://12noon.com)

[![build](https://github.com/skst/LicenseManager/actions/workflows/dotnet.yml/badge.svg)](https://github.com/skst/LicenseManager/actions/workflows/dotnet.yml)

[![](https://img.shields.io/github/v/release/skst/LicenseManager.svg?label=latest%20release&color=007edf)](https://github.com/skst/LicenseManager/releases/latest)

[![GitHub last commit](https://img.shields.io/github/last-commit/skst/LicenseManager)](https://github.com/skst/LicenseManager)

This is a graphical front-end for the [Standard.Licensing](https://github.com/junian/Standard.Licensing) project.

![License Manager](https://raw.githubusercontent.com/skst/LicenseManager/20df8e069ceca6100f755736bcc51b8536e9c180/12noon.LicenseManager.png)

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
| Expiration | The date on which the license expires. |
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

Note that the public key and product ID are passed by the licensed application,
to validate the license, so you only want to create a new keypair or change the
product ID if you want to rebuild the licensed application and create new licenses
for anyone who will use the new build.

1. Create a keypair by entering a value for _Passphrase_ and pressing _Create Keypair_ button.
1. Enter a _Product ID_.
1. Optionally, lock the license to a specific build of the licensed application.
1. Fill in the product information, license information, and licensee information.
1. Press the _Create License..._ button. This will prompt you for where to save the `.lic` and `.private` files.

The `.private` file contains all of the secret information used to create the license.
Do keep the `.private` file somewhere safe.
Do NOT add the `.private` file to source control.
You will need it to create more licenses for your licensed application
(unless you want to update the application to use a new public key).

### Create a License Based on an Existing License

1. Press the *Load License & Keys* button to select a `.lic` file.
1. This will validate the license file.

If the license is invalid (_e.g._, the assembly has changed), you can create a new (valid) license.

You can also drag/drop an existing `.lic` file (with its associated `.private` file in same folder).

1. Now you can update the product, license, or licensee information as needed.
1. Press the _Create License..._ button to create a new license. This will
prompt you for where to save the `.lic` file. (The `.private` file will be saved in the same folder.)

### The Licensed Application

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
