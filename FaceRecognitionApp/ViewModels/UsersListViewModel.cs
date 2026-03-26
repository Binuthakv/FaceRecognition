using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaceRecognitionApp.Models;
using FaceRecognitionApp.Services;
using FaceRecognitionApp.Helpers;
using System.Collections.ObjectModel;

namespace FaceRecognitionApp.ViewModels;

public partial class UsersListViewModel : ObservableObject
{
    private readonly IUserDatabaseService _databaseService;

    [ObservableProperty]
    private ObservableCollection<UserRegistration> _users = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Color _statusColor = Colors.Gray;

    [ObservableProperty]
    private int _totalUsers;

    public UsersListViewModel(IUserDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }


    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading users...";
        StatusColor = Colors.Blue;

        try
        {
            var allUsers = await _databaseService.GetAllUsersAsync();

            Users.Clear();
            foreach (var user in allUsers)
            {
                Users.Add(user);
            }

            TotalUsers = Users.Count;
            StatusMessage = $"Loaded {TotalUsers} users";
            StatusColor = Colors.Green;

            AppLogger.Info($"Loaded {TotalUsers} users");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading users: {ex.Message}";
            StatusColor = Colors.Red;
            AppLogger.Error("Error loading users", ex);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshUsersAsync()
    {
        IsRefreshing = true;
        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("///RegistrationPage");
        }
        catch (Exception ex)
        {
            AppLogger.Error("Navigation error", ex);
        }
    }

    [RelayCommand]
    private async Task EditUserAsync(UserRegistration user)
    {
        if (user == null) return;

        try
        {
            AppLogger.Info($"Editing user: {user.Name} ({user.UserId})");
            
            await Shell.Current.GoToAsync("///RegistrationPage");
            
            if (Shell.Current.CurrentPage?.BindingContext is UserRegistrationViewModel viewModel)
            {
                await viewModel.LoadUserForEditAsync(user);
            }
            else
            {
                await Task.Delay(100);
                
                var registrationPage = Shell.Current.CurrentPage;
                if (registrationPage?.BindingContext is UserRegistrationViewModel vm)
                {
                    await vm.LoadUserForEditAsync(user);
                }
                else
                {
                    AppLogger.Warning("Could not find UserRegistrationViewModel");
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Edit navigation error", ex);
            StatusMessage = $"Error opening edit: {ex.Message}";
            StatusColor = Colors.Red;
        }
    }

    [RelayCommand]
    private async Task DeleteUserAsync(UserRegistration user)
    {
        if (user == null) return;

        try
        {
            bool confirm = false;
            if (Application.Current?.MainPage != null)
            {
                confirm = await Application.Current.MainPage.DisplayAlert(
                    "Delete User",
                    $"Are you sure you want to delete '{user.Name}' (ID: {user.UserId})?",
                    "Delete",
                    "Cancel");
            }

            if (!confirm)
                return;

            IsLoading = true;
            StatusMessage = $"Deleting {user.Name}...";
            StatusColor = Colors.Blue;

            await _databaseService.DeleteUserAsync(user);

            Users.Remove(user);
            TotalUsers = Users.Count;

            StatusMessage = $"User '{user.Name}' deleted successfully";
            StatusColor = Colors.Green;

            AppLogger.Success($"Deleted user: {user.Name} ({user.UserId})");

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "User Deleted",
                    $"'{user.Name}' has been removed from the database.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting user: {ex.Message}";
            StatusColor = Colors.Red;
            AppLogger.Error("Error deleting user", ex);

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Delete Failed",
                    $"Unable to delete user: {ex.Message}",
                    "OK");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewUserDetailsAsync(UserRegistration user)
    {
        if (user == null) return;

        try
        {
            var details = $"User ID: {user.UserId}\n" +
                         $"Name: {user.Name}\n" +
                         $"Age: {user.Age} years\n" +
                         $"Date of Birth: {user.DateOfBirth:MMM dd, yyyy}\n" +
                         $"Registered: {user.RegisteredDate:MMM dd, yyyy HH:mm}\n" +
                         $"Photos: {user.PhotoCount}/3";

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "User Details",
                    details,
                    "OK");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Error showing details", ex);
        }
    }
}
