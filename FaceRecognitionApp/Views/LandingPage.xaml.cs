namespace FaceRecognitionApp.Views;

public partial class LandingPage : ContentPage
{
    private IDispatcherTimer? _clockTimer;

    public LandingPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateClock();
        UpdateMenuVisibilityByRole();
        _clockTimer = Dispatcher.CreateTimer();
        _clockTimer.Interval = TimeSpan.FromSeconds(1);
        _clockTimer.Tick += (s, e) => UpdateClock();
        _clockTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _clockTimer?.Stop();
        _clockTimer = null;
    }


    private void UpdateClock()
    {
        var now = DateTime.Now;

        // Time (12-hour format)
        int hours = now.Hour % 12;
        if (hours == 0) hours = 12;
        string minutes = now.Minute.ToString("D2");
        string ampm = now.Hour >= 12 ? "PM" : "AM";

        TimeLabel.Text = $"{hours}:{minutes}";
        AmPmLabel.Text = ampm;

        // Date (e.g. MONDAY, APRIL 21, 2026)
        DateLabel.Text = now.ToString("dddd, MMMM dd, yyyy").ToUpper();
    }


    private async void OnRegisterTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("///RegistrationPage");
    }

    private async void OnVerifyTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("///FaceVerificationPage");
    }

    private async void OnUsersTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("///UsersListPage");
    }

    private async void OnLogoutTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("///AdminLoginPage");
    }

    private async Task UpdateMenuVisibilityByRole()
    {
        try
        {
            var roleJson = await SecureStorage.Default.GetAsync("AdminRole");
            var role = string.IsNullOrEmpty(roleJson) ? "Viewer" : roleJson.Trim('"');

            // Reset all cards to invisible
            RegisterCard.IsVisible = false;
            VerifyCard.IsVisible = false;
            UsersCard.IsVisible = false;

            // Show cards based on role
            // Viewer: Sees only Verify
            // Manager: Sees Users and Verify
            // Admin: Sees Register, Verify and Users
            switch (role.ToLowerInvariant())
            {
                case "admin":
                    RegisterCard.IsVisible = true;
                    VerifyCard.IsVisible = true;
                    UsersCard.IsVisible = true;
                    break;

                case "manager":
                    VerifyCard.IsVisible = true;
                    UsersCard.IsVisible = true;
                    break;

                case "viewer":
                default:
                    VerifyCard.IsVisible = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            // If an error occurs, default to Viewer role (only Verify visible)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RegisterCard.IsVisible = false;
                VerifyCard.IsVisible = true;
                UsersCard.IsVisible = false;
            });
        }
    }
}