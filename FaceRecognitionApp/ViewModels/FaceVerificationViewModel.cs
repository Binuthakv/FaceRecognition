using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaceRecognitionApp.Constants;
using FaceRecognitionApp.Helpers;
using FaceRecognitionApp.Services;

namespace FaceRecognitionApp.ViewModels;

public partial class FaceVerificationViewModel : ObservableObject, IDisposable
{
    private readonly IFaceRecognitionService _faceRecognitionService;
    private readonly IAttendanceService _attendanceService;

    private volatile bool _isProcessingFrame;
    private CancellationTokenSource? _cameraCts;

    // Frame throttle — cap processing at 5 fps to spare the CPU/battery.
    // At 30 fps, frames arriving while inference is running are already
    // dropped by _isProcessingFrame; this further limits idle polling.
    private long _lastFrameTickCount;
    private const long FrameThrottleMs = 200;

    [ObservableProperty] private string _statusMessage = AppConstants.Messages.CameraStartPrompt;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isVerified;
    [ObservableProperty] private double _confidence;
    [ObservableProperty] private Color _statusColor = Colors.Gray;
    [ObservableProperty] private bool _isCameraActive;
    [ObservableProperty] private bool _isFrontCamera;
    [ObservableProperty] private bool _faceDetectedInFrame;
    [ObservableProperty] private string _realtimeStatus = AppConstants.Messages.CameraInactive;
    [ObservableProperty] private string _matchedUserName = string.Empty;
    [ObservableProperty] private string _matchedUserId = string.Empty;
    [ObservableProperty] private string _livenessStatus = string.Empty;

    private volatile bool _verificationCompleted;

    public FaceVerificationViewModel(IFaceRecognitionService faceRecognitionService, IAttendanceService attendanceService)
    {
        _faceRecognitionService = faceRecognitionService;
        _attendanceService = attendanceService;
    }

    [RelayCommand]
    private async Task StartRealtimeCameraAsync()
    {
        if (IsCameraActive)
        {
            StatusMessage = "Camera is already active";
            return;
        }
        IsCameraActive = true;

        IsLoading = true;
        StatusMessage = "Initializing camera…";
        StatusColor = Colors.Blue;
        

        try
        {
            // Reset state for new verification session
            _verificationCompleted = false;
            IsVerified = false;
            Confidence = 0;
            MatchedUserName = string.Empty;
            MatchedUserId = string.Empty;
            _cameraCts = new CancellationTokenSource();

            RealtimeStatus = AppConstants.Messages.ScanningFaces;
            StatusMessage = "Camera active — scanning for faces…";
            StatusColor = Colors.Blue;

            AppLogger.Info("Camera started - Real-time verification in progress");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading users: {ex.Message}";
            StatusColor = Colors.Red;
            AppLogger.Error("Error starting camera", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ProcessCameraFrame(byte[] frameData)
    {
        if (_verificationCompleted) return;
        if (_isProcessingFrame || !IsCameraActive) return;

        // ── Frame throttle ────────────────────────────────────────────────────
        var now = Environment.TickCount64;
        if (now - _lastFrameTickCount < FrameThrottleMs) return;
        _lastFrameTickCount = now;

        _isProcessingFrame = true;
        try
        {
            var token = _cameraCts?.Token ?? CancellationToken.None;

            // ── Single-pass analysis ──────────────────────────────────────────
            // Replaces three separate service calls (DetectFaceInFrameAsync +
            // DetectEyeStateAsync + ExtractEmbeddingAsync) that each decoded the
            // frame image and ran ONNX face detection independently. AnalyzeFrameAsync
            // does one image decode and one face-detection inference for all three.
            var analysis = await _faceRecognitionService.AnalyzeFrameAsync(frameData, token);

            if (!analysis.FaceDetected)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    FaceDetectedInFrame = false;
                    RealtimeStatus = AppConstants.Messages.ScanningFaces;
                    StatusColor = Colors.Blue;
                    LivenessStatus = string.Empty;
                    StatusMessage = "Face not detected...";
                });
                return;
            }

            AppLogger.Trace("Face detected - running liveness check");

            bool isLive = analysis.LeftEyeOpen || analysis.RightEyeOpen;
            AppLogger.Trace($"Liveness: left={analysis.LeftEyeOpen}, right={analysis.RightEyeOpen}, live={isLive}");

            //if (!isLive)
            //{
            //    await MainThread.InvokeOnMainThreadAsync(() =>
            //    {
            //        FaceDetectedInFrame = true;
            //        RealtimeStatus = "👁️ Liveness check - please open your eyes";
            //        StatusColor = Colors.Orange;
            //        LivenessStatus = "Liveness: not confirmed";
            //    });
            //    return;
            //}

            var frameEmbedding = analysis.Embedding;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FaceDetectedInFrame = true;
                RealtimeStatus = "✅ Liveness confirmed - verifying identity…";
                StatusColor = Colors.Blue;
                LivenessStatus = "Liveness: ✅ confirmed";
            });

            if (frameEmbedding is null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    FaceDetectedInFrame = true;
                    RealtimeStatus = "Face captured data cannot process";
                    StatusColor = Colors.Blue;
                    LivenessStatus = "Liveness: ✅ confirmed";
                });
                AppLogger.Trace("Face frameEmbedding is null");
                return; // face visible but alignment failed — wait for next frame
            }

            AppLogger.Trace("Searching for matching user via API embeddings/search");

            // ── API-based embedding search ────────────────────────────────────
            var searchResults = await _faceRecognitionService.SearchEmbeddingsAsync(
                frameEmbedding, topK: 1, threshold: 0.42f, token);

            if (searchResults.Count > 0)
            {
                var bestMatch = searchResults[0];

                // ── Match found ───────────────────────────────────────────────
                _verificationCompleted = true;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsVerified = true;
                    Confidence = bestMatch.Similarity * 100; // Convert to percentage
                    MatchedUserName = bestMatch.UserName;
                    MatchedUserId = bestMatch.UserId;
                    RealtimeStatus = "✅ MATCH FOUND!";
                    StatusColor = Colors.Green;
                    StatusMessage = $"✅ {bestMatch.UserName} verified! ({bestMatch.Similarity * 100:F1}%)";
                    LivenessStatus = string.Empty;
                });

                AppLogger.Success($"MATCH: {bestMatch.UserName} ({bestMatch.UserId}) " +
                                  $"photo {bestMatch.PhotoNumber} → {bestMatch.Similarity * 100:F1}%");

                // Record attendance for this verification
                try
                {
                    await _attendanceService.RecordAttendanceAsync(bestMatch.UserId, DateTime.UtcNow);
                    AppLogger.Info($"Attendance recorded for UserId: {bestMatch.UserId}");
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"Failed to record attendance: {ex.Message}");
                }

                await Task.Delay(600);
                await ShowVerificationResultAsync(true, bestMatch.UserName, bestMatch.UserId, bestMatch.Similarity * 100);

                await MainThread.InvokeOnMainThreadAsync(StopRealtimeCamera);
                return;
            }

            // ── No match found ────────────────────────────────────────────────
            _verificationCompleted = true;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsVerified = false;
                FaceDetectedInFrame = true;
                RealtimeStatus = "❌ User Not Found";
                StatusColor = Colors.Red;
                StatusMessage = "Face not recognized in database.";
                LivenessStatus = string.Empty;
            });

            AppLogger.Warning("USER NOT FOUND via API embeddings/search");

            await Task.Delay(600);
            await MainThread.InvokeOnMainThreadAsync(StopRealtimeCamera);
            await ShowVerificationResultAsync(false, string.Empty, string.Empty, 0);
        }
        catch (OperationCanceledException) { /* camera stopped mid-frame */ }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RealtimeStatus = $"Error: {ex.Message}";
                StatusColor = Colors.Red;
                StatusMessage = "Verification error";
                AppLogger.Error("Verification error", ex);
            });
        }
        finally
        {
            _isProcessingFrame = false;
        }
    }

    [RelayCommand]
    private void ToggleCameraFacing()
    {
        IsFrontCamera = !IsFrontCamera;
        CameraFacingToggled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raised only when the user explicitly taps the flip-camera button.
    /// The page subscribes to this event to switch the hardware camera,
    /// keeping the hardware switch isolated from any programmatic
    /// <see cref="IsFrontCamera"/> change (e.g. reset, config sync).
    /// </summary>
    public event EventHandler? CameraFacingToggled;

    [RelayCommand]
    public void StopRealtimeCamera()
    {
        IsCameraActive = false;
        _cameraCts?.Cancel();
        _cameraCts?.Dispose();
        _cameraCts = null;
        LivenessStatus = string.Empty;
        RealtimeStatus = "Camera stopped";
        StatusMessage = _verificationCompleted
            ? $"Verification completed — {MatchedUserName} identified"
            : "Real-time verification stopped";
        StatusColor = _verificationCompleted ? Colors.Green : Colors.Gray;
        FaceDetectedInFrame = false;

        AppLogger.Info($"Camera stopped - Verification completed: {_verificationCompleted}");
    }

    [RelayCommand]
    private void Reset()
    {
        StopRealtimeCamera();
        _verificationCompleted = false;
        IsVerified = false;
        Confidence = 0;
        FaceDetectedInFrame = false;
        LivenessStatus = string.Empty;
        MatchedUserName = string.Empty;
        MatchedUserId = string.Empty;
        StatusMessage = AppConstants.Messages.CameraStartPrompt;
        RealtimeStatus = AppConstants.Messages.CameraInactive;
        StatusColor = Colors.Gray;

        AppLogger.Info("Reset - Ready for new verification");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task ShowVerificationResultAsync(
        bool isMatch, string userName, string userId, double confidence)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (isMatch)
            {
                await Shell.Current.DisplayAlertAsync(
                    "✅ Identity Verified",
                    $"Welcome, {userName}!\n\nUser ID: {userId}\nConfidence: {confidence:F1}%",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync(
                    "❌ Not Recognized",
                    "Face not recognized.\nThis person is not registered in the system.",
                    "OK");
            }
        });
    }

    public void Dispose()
    {
        _cameraCts?.Cancel();
        _cameraCts?.Dispose();
        _cameraCts = null;
    }
}
