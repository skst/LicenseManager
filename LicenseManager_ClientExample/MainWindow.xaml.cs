using System.Windows;

namespace LicenseManager_ClientExample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public string LicenseType { get; private set; } = string.Empty;
	public string ExpirationDate { get; private set; } = string.Empty;
	public int ExpirationDays { get; private set; } = 0;
	public int Quantity { get; private set; } = 0;

	public string Product { get; private set; } = string.Empty;
	public string Version { get; private set; } = string.Empty;
	public string PublishDate { get; private set; } = string.Empty;

	public string Licensee { get; private set; } = string.Empty;
	public string Email { get; private set; } = string.Empty;
	public string Company { get; private set; } = string.Empty;

	public bool IsLockedToAssembly { get; private set; } = false;
	public string ProductId { get; private set; } = string.Empty;

	public MainWindow()
	{
		InitializeComponent();

		DataContext = this;

		CtlValid.Text = App.IsLicensed ? "Licensed" : "NOT Licensed";

		if (App.IsLicensed)
		{
			LicenseType = App.License.StandardOrTrial.ToString();
			ExpirationDate = App.License.ExpirationDateUTC.ToString("D") ?? "None";
			ExpirationDays = App.License.ExpirationDays;
			Quantity = App.License.Quantity;
			Product = App.License.Product;
			Version = App.License.Version;
			PublishDate = App.License.PublishDate?.ToString("D") ?? "None";
			Licensee = App.License.Name;
			Email = App.License.Email;
			Company = App.License.Company;
			IsLockedToAssembly = App.License.IsLockedToAssembly;
			ProductId = App.License.ProductId;
		}
	}
}
