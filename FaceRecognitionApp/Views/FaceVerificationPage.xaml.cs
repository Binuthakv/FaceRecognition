using CommunityToolkit.Maui.Core;
using FaceRecognitionApp.Helpers;
using FaceRecognitionApp.ViewModels;
using System.ComponentModel;

namespace FaceRecognitionApp.Views;

public partial class FaceVerificationPage : ContentPage
{
    private readonly FaceVerificationViewModel _viewModel;
    private readonly ICameraProvider _cameraProvider;

    private int _isProcessing;

    public FaceVerificationPage(FaceVerificationViewModel viewModel, ICameraProvider cameraProvider)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _cameraProvider = cameraProvider ?? throw new ArgumentNullException(nameof(cameraProvider));
        BindingContext = _viewModel;
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FaceVerificationViewModel.IsCameraActive))
            await HandleCameraActiveChangedAsync();
    }

    // Called only when the user taps the flip-camera button via ToggleCameraFacingCommand.
    private async void OnCameraFacingToggled(object? sender, EventArgs e)
        => await HandleCameraFacingChangedAsync();

    private async Task HandleCameraActiveChangedAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (_viewModel.IsCameraActive)
                {
                    //await Task.Delay(300);
                    await cameraView.StartCameraPreview(CancellationToken.None);
                    await cameraView.CaptureImage(CancellationToken.None);
                }
                else
                {
                    cameraView.StopCameraPreview();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Camera preview operation failed", ex);
            }
        });
    }

    private async Task HandleCameraFacingChangedAsync()
    {
        if (!_viewModel.IsCameraActive)
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (!_cameraProvider.AvailableCameras.Any())
                    await _cameraProvider.RefreshAvailableCameras(CancellationToken.None);

                var targetPosition = _viewModel.IsFrontCamera ? CameraPosition.Front : CameraPosition.Rear;
                var camera = _cameraProvider.AvailableCameras.FirstOrDefault(c => c.Position == targetPosition);

                if (camera is null)
                    return;

                cameraView.StopCameraPreview();
                cameraView.SelectedCamera = camera;
                await cameraView.StartCameraPreview(CancellationToken.None);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(250);
                        await MainThread.InvokeOnMainThreadAsync(() => cameraView.CaptureImage(CancellationToken.None));
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("Initial capture after switch failed", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error("Camera facing toggle failed", ex);
            }
        });
    }

    private async void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
    {
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
            return;

        try
        {
            if (e.Media is null)
                return;

            // On Android the stream position may be non-zero on arrival.
            if (e.Media.CanSeek && e.Media.Position != 0)
                e.Media.Seek(0, SeekOrigin.Begin);

            using var ms = new MemoryStream();
            await e.Media.CopyToAsync(ms);

            var imageBytes = ms.ToArray();
            if (imageBytes.Length == 0)
                return;

            //await Task.Delay(50); // Simulate processing time.

            await _viewModel.ProcessCameraFrame(imageBytes);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Camera OnMediaCaptured failed", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0);

            if (_viewModel.IsCameraActive)
            {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() => cameraView.CaptureImage(CancellationToken.None));
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Queue next capture failed", ex);
                }
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        cameraView.MediaCaptured += OnMediaCaptured;

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            AppLogger.Error("Camera permission not granted", null);
            return;
        }
        await _cameraProvider.RefreshAvailableCameras(CancellationToken.None);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.CameraFacingToggled += OnCameraFacingToggled;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        cameraView.MediaCaptured -= OnMediaCaptured;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.CameraFacingToggled -= OnCameraFacingToggled;

        if (_viewModel.IsCameraActive)
            cameraView.StopCameraPreview();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(LandingPage)}");
    }
}
