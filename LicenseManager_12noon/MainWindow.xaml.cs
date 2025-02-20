using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;

namespace LicenseManager_12noon;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[INotifyPropertyChanged]
public partial class MainWindow : Window
{
	private const string FileDialogGuid = "96B96BE5-5249-44A8-9A33-293BF654D2D1";

	[ObservableProperty]
	private string _pathKeypair = string.Empty;
	[ObservableProperty]
	private string _pathLicense = string.Empty;

	public string PathDefaultFolder
	{
		get
		{
			if (!string.IsNullOrEmpty(PathKeypair))
			{
				return System.IO.Path.GetDirectoryName(PathKeypair) ?? @"C:\";
			}
			else if (!string.IsNullOrEmpty(PathLicense))
			{
				return System.IO.Path.GetDirectoryName(PathLicense) ?? @"C:\";
			}
			else
			{
				return @"C:\";
			}
		}
	}


	public MainWindow()
	{
		InitializeComponent();

		TheLicenseManager.PropertyChanged += (s, e) =>
		{
			if (TheLicenseManager.IsKeypairDirty || TheLicenseManager.IsLicenseDirty)
			{
				CtlLicenseValid.Visibility = Visibility.Collapsed;
				CtlLicenseInvalid.Visibility = Visibility.Collapsed;
				CtlErrors.Text = string.Empty;
			}
		};
	}

	/// <summary>
	/// Create new keypair for generating licenses.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void CreateKeypairButton_Click(object sender, RoutedEventArgs e)
	{
		TheLicenseManager.CreateKeypair();

		SetValidationDisplay(isValid: null);
	}

	private void CopyPublicKeyButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(TheLicenseManager.KeyPublic);
		}
		catch (System.Runtime.InteropServices.ExternalException)
		{
			// Another app has the clipboard open
		}
	}

	private void CopyProductIdButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(TheLicenseManager.ProductId);
		}
		catch (System.Runtime.InteropServices.ExternalException)
		{
			// Another app has the clipboard open
		}
	}

	private void NewIdButton_Click(object sender, RoutedEventArgs e)
	{
		TheLicenseManager.NewID();

		SetValidationDisplay(isValid: null);
	}

	private void SelectAssemblyButton_Click(object sender, RoutedEventArgs e)
	{
		string currentPath = TheLicenseManager.PathAssembly;

		OpenFileDialog ofd = new()
		{
			ClientGuid = new(FileDialogGuid),
			Title = "Select Assembly",
			DefaultDirectory = PathDefaultFolder,
			AddToRecent = false,
			Multiselect = false,
			Filter = "Assembly Files|*.exe;*.dll|All Files|*.*",
			DefaultExt = @".exe",
			FileName = currentPath,
		};

		if (!ofd.ShowDialog().GetValueOrDefault())
		{
			return;
		}

		TheLicenseManager.PathAssembly = ofd.FileName;

		SetValidationDisplay(isValid: null);
	}

	private void Window_DragOver(object sender, DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			e.Effects = DragDropEffects.None;
		}
		else
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length > 2)
			{
				e.Effects = DragDropEffects.None;
			}
			else
			{
				bool validFiles = true;
				foreach (var file in files)
				{
					if (file.EndsWith(LicenseManager.FileExtension_PrivateKey, StringComparison.OrdinalIgnoreCase))
					{
						if (TheLicenseManager.IsKeypairDirty)
						{
							validFiles = false;
							break;
						}
					}
					else if (file.EndsWith(LicenseManager.FileExtension_License, StringComparison.OrdinalIgnoreCase))
					{
						if (TheLicenseManager.IsLicenseDirty)
						{
							validFiles = false;
							break;
						}
					}
					else
					{
						validFiles = false;
						break;
					}
				}
				e.Effects = validFiles ? DragDropEffects.Copy : DragDropEffects.None;
			}
		}
		e.Handled = true;
	}

	private void WindowFile_Drop(object sender, DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			return;
		}

		string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
		if (files.Length > 2)
		{
			MessageBox.Show($"Please drop a keypair {LicenseManager.FileExtension_PrivateKey} file, a license file {LicenseManager.FileExtension_License}, or both.", Title);
			return;
		}

		LoadKeypairLicenseFiles(files);
	}

	private void LoadKeypairLicenseButton_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog ofd = new()
		{
			ClientGuid = new(FileDialogGuid),
			Title = "Select Keypair or License File or Both",
			DefaultDirectory = PathDefaultFolder,
			AddToRecent = false,
			Multiselect = true,
			Filter =
$$"""
Keypair/License|*{{LicenseManager.FileExtension_PrivateKey}};*{{LicenseManager.FileExtension_License}}|
Keypair Files|*{{LicenseManager.FileExtension_PrivateKey}}|
License Files|*{{LicenseManager.FileExtension_License}}|
All Files|*.*
""",
			DefaultExt = LicenseManager.FileExtension_PrivateKey,
		};

		if (!ofd.ShowDialog().GetValueOrDefault())
		{
			return;
		}

		LoadKeypairLicenseFiles(ofd.FileNames);
	}

	private void LoadKeypairLicenseFiles(string[] files)
	{
		SetValidationDisplay(isValid: null);

		string? keypairFile = null;
		string? licenseFile = null;
		foreach (var file in files)
		{
			if (file.EndsWith(LicenseManager.FileExtension_PrivateKey, StringComparison.OrdinalIgnoreCase))
			{
				if (keypairFile is not null)
				{
					MessageBox.Show("Please drop only one keypair file.", Title);
					return;
				}
				keypairFile = file;
			}
			else if (file.EndsWith(LicenseManager.FileExtension_License, StringComparison.OrdinalIgnoreCase))
			{
				if (licenseFile is not null)
				{
					MessageBox.Show("Please drop only one license file.", Title);
					return;
				}
				licenseFile = file;
			}
			else
			{
				MessageBox.Show($"Please select only {LicenseManager.FileExtension_PrivateKey} or {LicenseManager.FileExtension_License} files.", Title);
				return;
			}
		}

		if (keypairFile is not null)
		{
			LoadKeypair(keypairFile);
		}

		if (licenseFile is not null)
		{
			PathLicense = licenseFile;
			if (!string.IsNullOrEmpty(TheLicenseManager.ProductId) && !string.IsNullOrEmpty(TheLicenseManager.KeyPublic))
			{
				ValidateLicense(licenseFile);
			}
			else
			{
				MessageBox.Show("Please create or load a keypair file before loading a license.", Title);
			}
		}
	}

	private void SaveKeypairButton_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog sfd = new()
		{
			ClientGuid = new(FileDialogGuid),
			Title = "Save Keypair File",
			DefaultDirectory = PathDefaultFolder,
			AddToRecent = false,
			Filter = $"Keypair Files|*{LicenseManager.FileExtension_PrivateKey}|All Files|*.*",
			DefaultExt = LicenseManager.FileExtension_PrivateKey,
			FileName = PathKeypair,
		};

		if (!sfd.ShowDialog().GetValueOrDefault())
		{
			return;
		}

		PathKeypair = sfd.FileName;

		try
		{
			TheLicenseManager.SaveKeypair(PathKeypair);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Unable to save keypair: {ex}", Title);
		}
	}

	private void SaveLicenseButton_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog sfd = new()
		{
			ClientGuid = new(FileDialogGuid),
			Title = "Save License File",
			DefaultDirectory = PathDefaultFolder,
			AddToRecent = false,
			Filter = $"License Files|*{LicenseManager.FileExtension_License}|All Files|*.*",
			DefaultExt = LicenseManager.FileExtension_License,
			FileName = PathLicense,
		};

		if (!sfd.ShowDialog().GetValueOrDefault())
		{
			return;
		}

		PathLicense = sfd.FileName;

		try
		{
			TheLicenseManager.SaveLicenseFile(PathLicense);
			ValidateLicense(PathLicense);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Unable to save license: {ex}", Title);
		}
	}

	private void LoadKeypair(string pathKeypair)
	{
		Debug.Assert(!string.IsNullOrEmpty(pathKeypair));
		Debug.Assert(pathKeypair.EndsWith(LicenseManager.FileExtension_PrivateKey, StringComparison.OrdinalIgnoreCase));
		Debug.Assert(System.IO.File.Exists(pathKeypair));

		SetValidationDisplay(isValid: null);

		try
		{
			TheLicenseManager.LoadKeypair(pathKeypair);
			PathKeypair = pathKeypair;
			PathLicense = string.Empty;
		}
		catch (Exception)
		{
			// Example: .private file does not exist.
			PathKeypair = string.Empty;
			PathLicense = string.Empty;
			CtlErrors.Text = "Error loading keypair file. It might be in an old format and need to be recreated.";
		}
	}

	private void ValidateLicense(string pathLicense)
	{
		SetValidationDisplay(isValid: null);

		PathLicense = pathLicense;

		bool isValid = TheLicenseManager.IsThisLicenseValid(
																			TheLicenseManager.ProductId,
																			TheLicenseManager.KeyPublic,
																			pathLicense,
																			TheLicenseManager.PathAssembly,
																			out string messages);
		if (isValid)
		{
			SetValidationDisplay(isValid: true);
		}
		else
		{
			// Reload the properties that were cleared to validate the license.
			// (Reload first to avoid clearing the error message.)
			LoadKeypair(PathKeypair);

			SetValidationDisplay(isValid: false);
		}

		CtlErrors.Text = messages;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="isValid">True = valid; False = invalid; Null = stateless</param>
	private void SetValidationDisplay(bool? isValid)
	{
		CtlLicenseValid.Visibility = (isValid ?? false) ? Visibility.Visible : Visibility.Collapsed;
		CtlLicenseInvalid.Visibility = (isValid ?? true) ? Visibility.Collapsed : Visibility.Visible;

		if (isValid is null)
		{
			CtlErrors.Text = string.Empty;
		}
	}
}
