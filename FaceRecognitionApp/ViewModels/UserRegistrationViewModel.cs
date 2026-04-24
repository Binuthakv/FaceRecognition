using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FaceRecognitionApp.Models;
using FaceRecognitionApp.Services;
using FaceRecognitionApp.Helpers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace FaceRecognitionApp.ViewModels;

public partial class UserRegistrationViewModel : ObservableObject
{
    private readonly IUserDatabaseService _databaseService;

    [ObservableProperty]
    private string _userId = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DateTime _dateOfBirth = DateTime.Today.AddYears(-18);

    [ObservableProperty]
    private string _sex = string.Empty;

    public List<string> SexOptions { get; } = new() { "Male", "Female", "Other" };

    [ObservableProperty]
    private ImageSource? _photo1;

    [ObservableProperty]
    private ImageSource? _photo2;

    [ObservableProperty]
    private ImageSource? _photo3;

    [ObservableProperty]
    private string _statusMessage = "Enter user details to register";

    [ObservableProperty]
    private Color _statusColor = Colors.Gray;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _photoCount;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isUserIdReadOnly;

    [ObservableProperty]
    private int _editingUserId;

    [ObservableProperty]
    private bool _isCameraPreviewVisible;

    [ObservableProperty]
    private int _activePhotoSlot;

    private byte[]? _photo1Data;
    private byte[]? _photo2Data;
    private byte[]? _photo3Data;

    public UserRegistrationViewModel(IUserDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task LoadUserForEditAsync(UserRegistration user)
    {
        try
        {
            IsEditMode = true;
            IsUserIdReadOnly = true;
            EditingUserId = user.Id;

            UserId = user.UserId;
            Name = user.Name;
            DateOfBirth = user.DateOfBirth;
            Sex = user.Sex;

            await Task.Run(async () =>
            {
                if (user.Photo1 != null)
                {
                    _photo1Data = user.Photo1;
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Photo1 = ImageSource.FromStream(() => new MemoryStream(user.Photo1)));
                }

                if (user.Photo2 != null)
                {
                    _photo2Data = user.Photo2;
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Photo2 = ImageSource.FromStream(() => new MemoryStream(user.Photo2)));
                }

                if (user.Photo3 != null)
                {
                    _photo3Data = user.Photo3;
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Photo3 = ImageSource.FromStream(() => new MemoryStream(user.Photo3)));
                }
            });

            UpdatePhotoCount();
            StatusMessage = $"Editing user: {user.Name}";
            StatusColor = Colors.Blue;
            AppLogger.Info($"Loaded user for editing: {user.Name} ({user.UserId})");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading user: {ex.Message}";
            StatusColor = Colors.Red;
            AppLogger.Error("Error loading user for edit", ex);
        }
    }

    // ── Camera Capture ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task StartCapturePhoto1Async() => await StartCameraForSlotAsync(1);

    [RelayCommand]
    private async Task StartCapturePhoto2Async() => await StartCameraForSlotAsync(2);

    [RelayCommand]
    private async Task StartCapturePhoto3Async() => await StartCameraForSlotAsync(3);

    [RelayCommand]
    private void CancelCameraPreview()
    {
        IsCameraPreviewVisible = false;
        ActivePhotoSlot = 0;
        StatusMessage = "Camera cancelled";
        StatusColor = Colors.Gray;
    }

    private async Task StartCameraForSlotAsync(int slot)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Permission Required",
                    "Camera permission is required to capture photos. Please enable it in device settings.",
                    "OK");
                return;
            }

            ActivePhotoSlot = slot;
            IsCameraPreviewVisible = true;
            StatusMessage = $"Position face for Photo {slot} and tap capture";
            StatusColor = Colors.Blue;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to start camera: {ex.Message}";
            StatusColor = Colors.Red;
            AppLogger.Error("Failed to start camera", ex);
        }
    }

    /// <summary>Called by the code-behind when MediaCaptured fires after a capture request.</summary>
    public async Task ProcessCapturedPhotoAsync(byte[] imageData)
    {
        IsCameraPreviewVisible = false;

        if (imageData.Length == 0)
        {
            StatusMessage = "Captured photo is empty. Please try again.";
            StatusColor = Colors.Orange;
            return;
        }

        IsLoading = true;
        StatusMessage = $"Processing photo {ActivePhotoSlot}...";
        StatusColor = Colors.Blue;

        try
        {
            var compressed = await ResizeForStorageAsync(imageData);

            switch (ActivePhotoSlot)
            {
                case 1:
                    _photo1Data = compressed;
                    Photo1 = ImageSource.FromStream(() => new MemoryStream(compressed));
                    break;
                case 2:
                    _photo2Data = compressed;
                    Photo2 = ImageSource.FromStream(() => new MemoryStream(compressed));
                    break;
                case 3:
                    _photo3Data = compressed;
                    Photo3 = ImageSource.FromStream(() => new MemoryStream(compressed));
                    break;
            }

            UpdatePhotoCount();
            StatusMessage = $"✅ Photo {ActivePhotoSlot} captured successfully!";
            StatusColor = Colors.Green;
            AppLogger.Success($"Photo {ActivePhotoSlot} captured successfully");
            ActivePhotoSlot = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error processing photo: {ex.Message}";
            StatusColor = Colors.Red;
            AppLogger.Error("Error processing captured photo", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task<byte[]> ResizeForStorageAsync(byte[] photoData)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var inputStream = new MemoryStream(photoData);
                using var image = PlatformImage.FromStream(inputStream);
                if (image is null || (image.Width <= 800 && image.Height <= 800))
                    return photoData;
                using var resized = image.Downsize(800, true);
                return resized?.AsBytes(ImageFormat.Jpeg) ?? photoData;
            }
            catch
            {
                return photoData;
            }
        });
    }

    // ── Gallery Pick ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PickPhoto1Async() => await PickPhotoAsync(1);

    [RelayCommand]
    private async Task PickPhoto2Async() => await PickPhotoAsync(2);

    [RelayCommand]
    private async Task PickPhoto3Async() => await PickPhotoAsync(3);

    private async Task PickPhotoAsync(int photoNumber)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Selecting photo {photoNumber}...";
            StatusColor = Colors.Blue;

            var status = await Permissions.CheckStatusAsync<Permissions.Photos>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Photos>();

            if (status != PermissionStatus.Granted)
            {
                StatusMessage = "Photo library permission denied. Please enable access in settings.";
                StatusColor = Colors.Red;
                IsLoading = false;
                await Shell.Current.DisplayAlertAsync(
                    "Permission Required",
                    "Photo library access is required to select photos. Please grant permission in your device settings.",
                    "OK");
                return;
            }

            AppLogger.Info($"Picking photo {photoNumber}");

            var photos = await MediaPicker.Default.PickPhotosAsync();
            var photo = photos?.FirstOrDefault();

            if (photo is null)
            {
                StatusMessage = "Photo selection cancelled";
                StatusColor = Colors.Gray;
                return;
            }

            AppLogger.Info($"Photo selected: {photo.FullPath}");

            await Task.Run(async () =>
            {
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var photoData = memoryStream.ToArray();

                if (photoData.Length == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = "Selected photo is empty. Please try again.";
                        StatusColor = Colors.Orange;
                    });
                    return;
                }

                if (photoData.Length > 10_000_000)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        StatusMessage = "Photo is too large (max 10 MB). Please select a smaller photo.";
                        StatusColor = Colors.Orange;
                    });
                    return;
                }

                var compressed = await ResizeForStorageAsync(photoData);
                AppLogger.Info($"Compressed photo size: {compressed.Length} bytes");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    switch (photoNumber)
                    {
                        case 1:
                            _photo1Data = compressed;
                            Photo1 = ImageSource.FromStream(() => new MemoryStream(compressed));
                            break;
                        case 2:
                            _photo2Data = compressed;
                            Photo2 = ImageSource.FromStream(() => new MemoryStream(compressed));
                            break;
                        case 3:
                            _photo3Data = compressed;
                            Photo3 = ImageSource.FromStream(() => new MemoryStream(compressed));
                            break;
                    }

                    UpdatePhotoCount();
                    StatusMessage = $"✅ Photo {photoNumber} selected successfully!";
                    StatusColor = Colors.Green;
                    AppLogger.Success($"Photo {photoNumber} saved successfully");
                });
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error selecting photo: {ex.Message}";
            StatusColor = Colors.Red;
            AppLogger.Error("Photo selection error", ex);
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to select photo: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Registration ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RegisterUserAsync()
    {
        if (!ValidateInputs())
            return;

        IsLoading = true;
        StatusMessage = IsEditMode ? "Updating user..." : "Registering user...";
        StatusColor = Colors.Blue;

        try
        {
            if (IsEditMode)
            {
                var user = new UserRegistration
                {
                    Id = EditingUserId,
                    UserId = UserId.Trim(),
                    Name = Name.Trim(),
                    DateOfBirth = DateOfBirth,
                    Sex = Sex,
                    Photo1 = _photo1Data,
                    Photo2 = _photo2Data,
                    Photo3 = _photo3Data
                };

                await _databaseService.UpdateUserAsync(user);
                StatusMessage = $"✅ User '{Name}' updated successfully!";
                StatusColor = Colors.Green;
                AppLogger.Success($"User updated: {user.Name} ({user.UserId})");

                await Shell.Current.DisplayAlertAsync(
                    "Update Successful",
                    $"User '{Name}' has been updated!\n\nUser ID: {UserId}",
                    "OK");
                await Shell.Current.GoToAsync("///UsersPage");
            }
            else
            {
                if (await _databaseService.UserIdExistsAsync(UserId))
                {
                    StatusMessage = $"User ID '{UserId}' already exists. Please use a different ID.";
                    StatusColor = Colors.Red;
                    return;
                }

                var user = new UserRegistration
                {
                    UserId = UserId.Trim(),
                    Name = Name.Trim(),
                    DateOfBirth = DateOfBirth,
                    Sex = Sex,
                    Photo1 = _photo1Data,
                    Photo2 = _photo2Data,
                    Photo3 = _photo3Data
                };

                await _databaseService.SaveUserAsync(user);
                StatusMessage = $"✅ User '{Name}' registered! (DB ID: {user.Id})";
                StatusColor = Colors.Green;
                AppLogger.Success($"Employee registered: {user.Name} ({user.UserId}), ID: {user.Id}");

                await Shell.Current.DisplayAlertAsync(
                    "Registration Successful",
                    $"Employee '{Name}' saved!\n\nUser ID: {UserId}\nDatabase ID: {user.Id}",
                    "OK");

                bool registerAnother = await Shell.Current.DisplayAlertAsync(
                    "Register Another Employee?",
                    "Would you like to register another Employee?",
                    "Yes", "No");

                if (registerAnother)
                    ResetForm();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ {(IsEditMode ? "Update" : "Registration")} failed: {ex.Message}";
            StatusColor = Colors.Red;
            AppLogger.Error($"{(IsEditMode ? "Update" : "Registration")} error", ex);
            await Shell.Current.DisplayAlertAsync(
                $"{(IsEditMode ? "Update" : "Registration")} Failed",
                $"Unable to {(IsEditMode ? "update" : "save")} Employee:\n{ex.Message}",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ResetForm()
    {
        UserId = string.Empty;
        Name = string.Empty;
        DateOfBirth = DateTime.Today.AddYears(-18);
        Photo1 = null;
        Photo2 = null;
        Photo3 = null;
        _photo1Data = null;
        _photo2Data = null;
        _photo3Data = null;
        PhotoCount = 0;
        IsEditMode = false;
        IsUserIdReadOnly = false;
        EditingUserId = 0;
        IsCameraPreviewVisible = false;
        ActivePhotoSlot = 0;
        StatusMessage = "Enter Employee details to register";
        StatusColor = Colors.Gray;
        AppLogger.Info("Registration form reset");
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            StatusMessage = "Please enter an Employee ID";
            StatusColor = Colors.Orange;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Please enter a Name";
            StatusColor = Colors.Orange;
            return false;
        }

        if (DateOfBirth > DateTime.Today)
        {
            StatusMessage = "Date of Birth cannot be in the future";
            StatusColor = Colors.Orange;
            return false;
        }

        if (DateOfBirth.Year < 1900)
        {
            StatusMessage = "Please enter a valid Date of Birth";
            StatusColor = Colors.Orange;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Sex))
        {
            StatusMessage = "Please select a Sex";
            StatusColor = Colors.Orange;
            return false;
        }

        if (_photo1Data == null || _photo2Data == null || _photo3Data == null)
        {
            StatusMessage = "Please capture all 3 photos before registering";
            StatusColor = Colors.Orange;
            return false;
        }

        return true;
    }

    private void UpdatePhotoCount()
    {
        int count = 0;
        if (_photo1Data != null) count++;
        if (_photo2Data != null) count++;
        if (_photo3Data != null) count++;
        PhotoCount = count;
    }
}
