using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FaceRecognitionApp.Services;

namespace FaceRecognitionApp.ViewModels;

public partial class AdminLoginViewModel : ObservableObject
{
    private readonly ApiUserDatabaseService _databaseService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    public AdminLoginViewModel(ApiUserDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var result = await _databaseService.LoginAdminAsync(Username, Password, RememberMe);

            if (result == null)
            {
                ErrorMessage = "Invalid username/email or password.";
                return;
            }

            SuccessMessage = $"Welcome back, {result.Username}!";

            // Store admin session (could use SecureStorage or Preferences)
            await SecureStorage.Default.SetAsync("AdminUserId", result.Id.ToString());
            await SecureStorage.Default.SetAsync("AdminUsername", result.Username);
            await SecureStorage.Default.SetAsync("AdminEmail", result.Email);
            await SecureStorage.Default.SetAsync("AdminRole", result.Role);

            // Navigate to LandingPage
            await Shell.Current.GoToAsync($"//LandingPage");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
