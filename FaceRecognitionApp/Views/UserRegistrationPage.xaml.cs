using CommunityToolkit.Maui.Core;
using FaceRecognitionApp.Helpers;
using FaceRecognitionApp.Services;
using FaceRecognitionApp.ViewModels;
using System.ComponentModel;

namespace FaceRecognitionApp.Views;

public partial class UserRegistrationPage : ContentPage
{
    private readonly UserRegistrationViewModel _viewModel;
    private readonly ICameraProvider _cameraProvider;
    private readonly IFaceRecognitionService _faceRecognitionService;
    private volatile bool _captureRequested;

    public UserRegistrationPage(
        UserRegistrationViewModel viewModel,
        ICameraProvider cameraProvider,
        IFaceRecognitionService faceRecognitionService)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _cameraProvider = cameraProvider ?? throw new ArgumentNullException(nameof(cameraProvider));
        _faceRecognitionService = faceRecognitionService ?? throw new ArgumentNullException(nameof(faceRecognitionService));
        BindingContext = _viewModel;
    }
    

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UserRegistrationViewModel.IsCameraPreviewVisible))
            return;

        // IsCameraPreviewVisible may be set from a thread-pool continuation in the
        // ViewModel. Camera view operations must always run on the main thread on
        // Android; async void must never let exceptions escape (ExceptionDispatchInfo
        // rethrow on the UI thread crashes the app).
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (_viewModel.IsCameraPreviewVisible)
                {
                    shutterButton.IsEnabled = true;
                    await cameraView.StartCameraPreview(CancellationToken.None);
                }
                else
                {
                    _captureRequested = false;
                    cameraView.StopCameraPreview();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Camera preview operation failed", ex);
                // Reset state so the user can retry by tapping the photo button again.
                // IsCameraPreviewVisible will only fire PropertyChanged if it was true,
                // preventing an infinite callback loop.
                shutterButton.IsEnabled = false;
                _viewModel.CancelCameraPreviewCommand.Execute(null);
            }
        });
    }

    private async void OnCaptureClicked(object? sender, EventArgs e)
    {
        if (_captureRequested)
            return;

        _captureRequested = true;
        shutterButton.IsEnabled = false;

        try
        {
            await cameraView.CaptureImage(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _captureRequested = false;
            shutterButton.IsEnabled = true;
            AppLogger.Error("CaptureImage failed", ex);
        }
    }

    private async void OnFlipCameraClicked(object? sender, EventArgs e)
    {
        try
        {
            await _cameraProvider.RefreshAvailableCameras(CancellationToken.None);

            if (_cameraProvider.AvailableCameras is not { Count: > 1 } cameras)
                return;

            var targetPosition = cameraView.SelectedCamera?.Position == CameraPosition.Front
                ? CameraPosition.Rear
                : CameraPosition.Front;

            cameraView.SelectedCamera = cameras.FirstOrDefault(c => c.Position == targetPosition)
                                        ?? cameras.First();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Camera flip failed", ex);
        }
    }

    private async void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
    {
        if (!_captureRequested || e.Media is null)
            return;

        _captureRequested = false;

        // Run detection in a dedicated method so the raw full-resolution frame
        // byte[] lives only in that method's async state machine. By the time
        // we reach the awaits below (alert or VM call), the large allocation is
        // already out of scope and eligible for GC.
        var (faceDetected, faceData) = await DetectFaceInCaptureAsync(e.Media);

        if (!faceDetected || faceData is null)
        {
            AppLogger.Warning("Registration photo rejected — no face detected");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                shutterButton.IsEnabled = true;
                await DisplayAlert(
                    "No Face Detected",
                    "The photo must clearly show your face.\nPlease adjust your position and try again.",
                    "Retake");
            });
            return;
        }

        // Forward the face crop (not the full frame) to the view model.
        // The crop is already extracted, so the VM skips re-running face
        // detection and stores a significantly smaller image to the database.
        await MainThread.InvokeOnMainThreadAsync(
            () => _viewModel.ProcessCapturedPhotoAsync(faceData));
    }

    /// <summary>
    /// Reads the capture stream and runs face detection in an isolated async
    /// scope. Keeping the raw frame allocation here prevents it from being
    /// promoted into the <see cref="OnMediaCaptured"/> state machine, where
    /// it would survive through subsequent awaits (alert dialogs, VM calls).
    /// Pre-sizing <see cref="MemoryStream"/> from the stream length avoids
    /// the internal buffer doubling on each write.
    /// On Android the stream position may be non-zero on arrival; seeking to
    /// the beginning before reading prevents an empty read.
    /// </summary>
    private async Task<(bool detected, byte[]? faceData)> DetectFaceInCaptureAsync(Stream media)
    {
        if (media.CanSeek && media.Position != 0)
            media.Seek(0, SeekOrigin.Begin);

        var capacity = media.CanSeek && media.Length > 0
            ? (int)media.Length
            : 4 * 1024 * 1024;

        using var ms = new MemoryStream(capacity);
        await media.CopyToAsync(ms);

        var imageBytes = ms.ToArray();

        if (imageBytes.Length == 0)
        {
            AppLogger.Warning("DetectFaceInCaptureAsync — capture stream yielded 0 bytes (Android stream position issue?)");
            return (false, null);
        }

        return await _faceRecognitionService.DetectFaceInFrameAsync(imageBytes);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        cameraView.MediaCaptured += OnMediaCaptured;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        if (!_viewModel.IsEditMode)
            _viewModel.ResetFormCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        cameraView.MediaCaptured -= OnMediaCaptured;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        if (_viewModel.IsCameraPreviewVisible)
            cameraView.StopCameraPreview();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(LandingPage)}");
    }
}
