namespace FaceRecognitionApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		try
		{
			InitializeComponent();
		}
		catch (Microsoft.Maui.Controls.Xaml.XamlParseException xamlEx)
		{
			System.Diagnostics.Debug.WriteLine("❌ XAML PARSE EXCEPTION in AppShell ❌");
			System.Diagnostics.Debug.WriteLine($"Message: {xamlEx.Message}");
			System.Diagnostics.Debug.WriteLine($"XmlInfo: {xamlEx.XmlInfo}");

			if (xamlEx.InnerException != null)
			{
				System.Diagnostics.Debug.WriteLine($"Inner Exception: {xamlEx.InnerException.Message}");
			}

			throw;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ AppShell initialization failed: {ex.Message}");
			throw;
		}
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		LoadAdminInfo();
	}

	private void LoadAdminInfo()
	{
		try
		{
			var adminUsername = SecureStorage.Default.GetAsync("AdminUsername").Result;
			if (!string.IsNullOrEmpty(adminUsername))
			{
				AdminNameLabel.Text = $"👤 {adminUsername}";
			}
		}
		catch
		{
			// Silently fail if SecureStorage is not available
		}
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		try
		{
			// Clear admin session
			await SecureStorage.Default.SetAsync("AdminUserId", string.Empty);
			await SecureStorage.Default.SetAsync("AdminUsername", string.Empty);
			await SecureStorage.Default.SetAsync("AdminEmail", string.Empty);
			await SecureStorage.Default.SetAsync("AdminRole", string.Empty);

			// Navigate to Login Page
			await Shell.Current.GoToAsync("//login");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
		}
	}
}
