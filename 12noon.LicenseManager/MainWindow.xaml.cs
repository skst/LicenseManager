using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using System;
using System.Windows;

namespace LicenseManager_12noon;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[INotifyPropertyChanged]
public partial class MainWindow : Window
{
	private const string FileDialogGuid = "96B96BE5-5249-44A8-9A33-293BF654D2D1";

	public string PathLicense { get; set; } = string.Empty;


	public MainWindow()
	{
		InitializeComponent();
	}

	private void Window_DragOver(object sender, DragEventArgs e)
	{
		e.Effects = DragDropEffects.Copy;
	}

	private void WindowFile_Drop(object sender, DragEventArgs e)
	{
		if (!e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			return;
		}

		// Load the first file. Ignore other files.
		string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
		LoadLicenseAndKeys(files[0]);
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

	private void NewIdButton_Click(object sender, RoutedEventArgs e)
	{
		TheLicenseManager.NewID();

		SetValidationDisplay(isValid: null);
	}

	private void SelectAssemblyButton_Click(object sender, RoutedEventArgs e)
	{
		TheLicenseManager.PathAssembly = string.Empty;

		OpenFileDialog ofd = new()
		{
			ClientGuid = new(FileDialogGuid),
			Title = "Select Assembly",
			DefaultDirectory = @"C:\OneDrive\Development\12noon",
			AddToRecent = false,
			Multiselect = false,
			Filter = "Assembly Files|*.exe;*.dll",
			DefaultExt = @".exe",
			FileName = TheLicenseManager.PathAssembly,
		};

		if (!ofd.ShowDialog().GetValueOrDefault())
		{
			return;
		}

		TheLicenseManager.PathAssembly = ofd.FileName;
	}

	private void LoadLicenseKeysButton_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog ofd = new()
		{
			ClientGuid = new(FileDialogGuid),
			Title = "Select License File",
			DefaultDirectory = @"C:\OneDrive\Development\12noon",
			AddToRecent = false,
			Multiselect = false,
			Filter = $"License Files|*{Shared.LicenseManager.FileExtension_License}",
			DefaultExt = Shared.LicenseManager.FileExtension_License,
		};

		if (!ofd.ShowDialog().GetValueOrDefault())
		{
			return;
		}

		LoadLicenseAndKeys(ofd.FileName);
	}

	private void CreateLicenseButton_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog sfd = new()
		{
			ClientGuid = new(FileDialogGuid),
			Title = "Save License File",
			DefaultDirectory = @"C:\OneDrive\Development\12noon",
			AddToRecent = false,
			Filter = $"License Files|*{Shared.LicenseManager.FileExtension_License}",
			DefaultExt = Shared.LicenseManager.FileExtension_License,
			FileName = PathLicense,
		};

		if (!sfd.ShowDialog().GetValueOrDefault())
		{
			return;
		}

		PathLicense = sfd.FileName;

		try
		{
			TheLicenseManager.CreateLicenseFile(PathLicense);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Unable to create license: {ex}", Title);
		}

		SetValidationDisplay(isValid: null);
	}

	private void ValidateLicenseButton_Click(object sender, RoutedEventArgs e)
	{
		ValidateLicense(PathLicense);
	}

	private void LoadLicenseAndKeys(string pathLicense)
	{
		PathLicense = pathLicense;

		try
		{
			TheLicenseManager.LoadKeypair(PathLicense);
		}
		catch (Exception)
		{
			// Example: .private file does not exist.
			SetValidationDisplay(isValid: false);
			CtlErrors.Text = "Error loading .private file.";
			return;
		}

		ValidateLicense(PathLicense);
	}

	private void ValidateLicense(string pathLicense)
	{
		string errorMessages = TheLicenseManager.IsThisLicenseValid(
																	TheLicenseManager.ProductId,
																	TheLicenseManager.KeyPublic,
																	pathLicense,
																	TheLicenseManager.PathAssembly);
		if (string.IsNullOrEmpty(errorMessages))
		{
			SetValidationDisplay(isValid: true);
		}
		else
		{
			SetValidationDisplay(isValid: false);
			CtlErrors.Text = errorMessages;
		}
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="isValid">True = valid; False = invalid; Null = stateless</param>
	private void SetValidationDisplay(bool? isValid)
	{
		CtlLicenseValid.Visibility = (isValid ?? false) ? Visibility.Visible : Visibility.Collapsed;
		CtlLicenseInvalid.Visibility = (isValid ?? true) ? Visibility.Collapsed : Visibility.Visible;

		if (isValid ?? true)
		{
			CtlErrors.Text = string.Empty;
		}
	}
}
