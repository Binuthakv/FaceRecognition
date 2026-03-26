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
}
