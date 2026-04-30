using Microsoft.Extensions.Logging;
using FaceRecognitionApp.Constants;
using FaceRecognitionApp.Services;
using FaceRecognitionApp.ViewModels;
using FaceRecognitionApp.Views;
using FaceRecognitionApp.Helpers;
using CommunityToolkit.Maui;

namespace FaceRecognitionApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkitCamera()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		RegisterServices(builder.Services);

#if DEBUG
		builder.Logging.AddDebug();
#endif

		AppLogger.Success("MAUI app created successfully");
		
		return builder.Build();
	}

	private static void RegisterServices(IServiceCollection services)
	{
		// HTTP client shared by the API-backed service implementations.
		// Update AppConstants.ApiBaseUrl to point to your running API instance.
		services.AddHttpClient<IUserDatabaseService, ApiUserDatabaseService>(client =>
			client.BaseAddress = new Uri(AppConstants.ApiBaseUrl));

		services.AddHttpClient<ApiUserDatabaseService>(client =>
			client.BaseAddress = new Uri(AppConstants.ApiBaseUrl));

		services.AddHttpClient<IFaceRecognitionService, ApiFaceRecognitionService>(client =>
			  client.BaseAddress = new Uri(AppConstants.ApiBaseUrl));

		services.AddHttpClient<IAttendanceService, ApiAttendanceService>(client =>
			  client.BaseAddress = new Uri(AppConstants.ApiBaseUrl));

		// Services — API-backed implementations (swap for local ones when offline)
		//services.AddTransient<IUserDatabaseService, ApiUserDatabaseService>();

		// ViewModels
		services.AddTransient<AdminLoginViewModel>();
		services.AddTransient<FaceVerificationViewModel>();
		services.AddTransient<UserRegistrationViewModel>();
		services.AddTransient<UsersListViewModel>();

		// Pages
		services.AddTransient<AdminLoginPage>();
		services.AddTransient<FaceVerificationPage>();
		services.AddTransient<UserRegistrationPage>();
		services.AddTransient<UsersListPage>();
	}
    //Apk genration command:
    //dotnet publish -f net10.0-android -c Debug -p:AndroidPackageFormat=apk -p:AndroidKeyStore=false
}
