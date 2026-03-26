using FaceRecognitionApp.Helpers;
using FaceRecognitionApp.Services;


namespace FaceRecognitionApp;

public partial class App : Application
{
	private readonly IUserDatabaseService _databaseService;

	public App(IUserDatabaseService databaseService)
	{
		_databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

		InitializeComponent();

		// Initialize resources and request camera permission asynchronously
		_ = InitializeResourcesAsync();
	}

	private async Task InitializeResourcesAsync()
	{
		try
		{
			await ResourceHelper.ExtractHaarCascadeAsync();
			await _databaseService.InitializeAsync();

			// Request camera permission on app startup for all camera-dependent features
			await RequestCameraPermissionAsync();

			AppLogger.Success("Application initialized successfully");
		}
		catch (Exception ex)
		{
			AppLogger.Error("Application initialization failed", ex);
		}
	}

	private static async Task RequestCameraPermissionAsync()
	{
		try
		{
			var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
			if (status != PermissionStatus.Granted)
			{
				status = await Permissions.RequestAsync<Permissions.Camera>();
				if (status == PermissionStatus.Granted)
				{
					AppLogger.Info("Camera permission granted at startup");
				}
				else
				{
					AppLogger.Warning("Camera permission denied at startup");
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("Error requesting camera permission at startup", ex);
		}
	}
   

	private bool _permissionRequested;

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Request camera permission as early as possible, before tabs load
		if (!_permissionRequested)
		{
			_permissionRequested = true;
			_ = RequestCameraPermissionAsync();
		}

		return new Window(new AppShell());
	}
}