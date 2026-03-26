using FaceAiSharp;
using FaceAiSharp.Extensions;
using FaceRecognitionApp.Api.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ISImage = SixLabors.ImageSharp.Image;
using PointF = SixLabors.ImageSharp.PointF;

namespace FaceRecognitionApp.Api.Services;

public class FaceRecognitionService : IFaceRecognitionService
{
    private IFaceDetectorWithLandmarks? _detector;
    private IFaceEmbeddingsGenerator? _embedder;
    private IEyeStateDetector? _eyeDetector;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    private readonly ILogger<FaceRecognitionService> _logger;

    private const float MatchThreshold = 0.42f;
    private const float EyeBoxDivisor = 2.5f;
    private const int EmbeddingDimension = 512;  // ArcFace produces 512-dimensional embeddings

    public FaceRecognitionService(ILogger<FaceRecognitionService> logger)
    {
        _logger = logger;
    }

    // ── Initialization ────────────────────────────────────────────────────────

    public async Task InitializeFaceAiSharpAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            await Task.Run(() =>
            {
                _detector = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();
                _embedder = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();
                _eyeDetector = FaceAiSharpBundleFactory.CreateEyeStateDetector();
            });

            _initialized = true;
            _logger.LogInformation("FaceAiSharp models initialized");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
            await InitializeFaceAiSharpAsync();
    }

    // ── Face detection ────────────────────────────────────────────────────────

    public async Task<byte[]?> DetectFaceAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        return await Task.Run(() =>
        {
            using var image = LoadRgb24(imageData);
            var faces = _detector!.DetectFaces(image);
            if (faces.Count == 0) return null;

            using var crop = image.Clone(ctx => ctx.Crop(ClampRect(image, faces.First().Box)));
            return EncodeJpeg(crop);
        }, cancellationToken);
    }

    /// <summary>
    /// Single-pass analysis: one image decode, one face-detection inference,
    /// then eye-state and embedding on the already-loaded image.
    /// Eye state detection failures do not block embedding generation.
    /// </summary>
    public async Task<FrameAnalysisResult> AnalyzeFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        return await Task.Run(() =>
        {
            try
            {
                using var image = LoadRgb24(frameData);
                var faces = _detector!.DetectFaces(image);
                if (faces.Count == 0)
                {
                    _logger.LogTrace("AnalyzeFrame: No face detected");
                    return new FrameAnalysisResult(false, false, false, null);
                }

                var face = faces.MaxBy(f => f.Confidence ?? 0f);
                _logger.LogTrace("AnalyzeFrame: Face detected with confidence {Confidence}", face.Confidence);

                // ── Eye state detection (non-blocking) ───────────────────────────
                bool leftOpen = false, rightOpen = false;
                try
                {
                    (leftOpen, rightOpen) = DetectEyeStates(image, face.Box);
                    _logger.LogTrace("AnalyzeFrame: Eye states - left={Left}, right={Right}", leftOpen, rightOpen);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AnalyzeFrame: Eye state detection failed: {Message}", ex.Message);
                    // Continue with embedding generation even if eye detection fails
                }

                // ── Embedding generation ─────────────────────────────────────────
                float[]? embedding = null;
                try
                {
                    if (face.Landmarks is { Count: > 0 })
                    {
                        // Preferred: align face using landmarks for better accuracy
                        using var aligned = image.Clone();
                        _embedder!.AlignFaceUsingLandmarks(aligned, face.Landmarks);
                        embedding = _embedder.GenerateEmbedding(aligned);
                        _logger.LogTrace("AnalyzeFrame: Embedding generated with landmark alignment");
                    }
                    else
                    {
                        // Fallback: crop face region and generate embedding without alignment
                        _logger.LogTrace("AnalyzeFrame: No landmarks, using cropped face fallback");
                        var cropRect = ClampRect(image, face.Box);
                        if (cropRect.Width > 0 && cropRect.Height > 0)
                        {
                            using var cropped = image.Clone(ctx => ctx.Crop(cropRect));
                            embedding = _embedder!.GenerateEmbedding(cropped);
                            _logger.LogTrace("AnalyzeFrame: Embedding generated from cropped face");
                        }
                    }

                    // Validate 512-dimensional embedding
                    if (embedding is not null && embedding.Length != EmbeddingDimension)
                    {
                        _logger.LogWarning(
                            "AnalyzeFrame: Unexpected embedding dimension {Actual}, expected {Expected}",
                            embedding.Length, EmbeddingDimension);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AnalyzeFrame: Embedding generation failed: {Message}", ex.Message);
                }

                return new FrameAnalysisResult(true, leftOpen, rightOpen, embedding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyzeFrame: Unexpected error");
                return new FrameAnalysisResult(false, false, false, null);
            }
        }, cancellationToken);
    }

    public async Task<(bool detected, byte[]? faceData)> DetectFaceInFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        return await Task.Run(() =>
        {
            using var image = LoadRgb24(frameData);
            var faces = _detector!.DetectFaces(image);
            if (faces.Count == 0) return (false, (byte[]?)null);

            using var crop = image.Clone(ctx => ctx.Crop(ClampRect(image, faces.First().Box)));
            return (true, (byte[]?)EncodeJpeg(crop));
        }, cancellationToken);
    }

    // ── Face recognition ──────────────────────────────────────────────────────

    public async Task<FaceVerificationResult> VerifyFacesAsync(
        byte[] referenceFace, byte[] capturedFace, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var refEmb = await Task.Run(() => ExtractEmbedding(referenceFace), cancellationToken);
        if (refEmb is null) return Fail("No face detected in reference image.");

        var capEmb = await Task.Run(() => ExtractEmbedding(capturedFace), cancellationToken);
        if (capEmb is null) return Fail("No face detected in captured image.");

        return ScoreEmbeddings(refEmb, capEmb);
    }

    public async Task<float[]?> ExtractEmbeddingAsync(
        byte[] imageData, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        return await Task.Run(() => ExtractEmbedding(imageData), cancellationToken);
    }

    public Task<FaceVerificationResult> VerifyEmbeddingsAsync(
        float[] referenceEmbedding, float[] capturedEmbedding)
        => Task.FromResult(ScoreEmbeddings(referenceEmbedding, capturedEmbedding));

    // ── Eye state detection ───────────────────────────────────────────────────

    public async Task<(bool leftOpen, bool rightOpen)> DetectEyeStateAsync(
        byte[] imageData, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        return await Task.Run(() =>
        {
            using var image = LoadRgb24(imageData);
            var faces = _detector!.DetectFaces(image);
            if (faces.Count == 0) return (false, false);

            return DetectEyeStates(image, faces.First().Box);
        }, cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private float[]? ExtractEmbedding(byte[] imageData)
    {
        try
        {
            using var image = LoadRgb24(imageData);
            var faces = _detector!.DetectFaces(image);
            if (faces.Count == 0)
            {
                _logger.LogTrace("ExtractEmbedding: No face detected");
                return null;
            }

            var face = faces.MaxBy(f => f.Confidence ?? 0f);
            float[]? embedding = null;

            // Preferred: align using landmarks for better accuracy
            if (face.Landmarks is { Count: > 0 })
            {
                using var aligned = image.Clone();
                _embedder!.AlignFaceUsingLandmarks(aligned, face.Landmarks);
                embedding = _embedder.GenerateEmbedding(aligned);
                _logger.LogTrace("ExtractEmbedding: Generated with landmark alignment");
            }
            else
            {
                // Fallback: crop face and generate without alignment
                _logger.LogTrace("ExtractEmbedding: No landmarks, using cropped face fallback");
                var cropRect = ClampRect(image, face.Box);
                if (cropRect.Width <= 0 || cropRect.Height <= 0)
                {
                    _logger.LogWarning("ExtractEmbedding: Invalid crop rect");
                    return null;
                }

                using var cropped = image.Clone(ctx => ctx.Crop(cropRect));
                embedding = _embedder!.GenerateEmbedding(cropped);
            }

            // Validate embedding dimension (ArcFace produces 512-dimensional vectors)
            if (embedding is null)
            {
                _logger.LogWarning("ExtractEmbedding: Embedder returned null");
                return null;
            }

            if (embedding.Length != EmbeddingDimension)
            {
                _logger.LogWarning(
                    "ExtractEmbedding: Unexpected dimension {Actual}, expected {Expected}",
                    embedding.Length, EmbeddingDimension);
                // Still return the embedding but log the discrepancy
            }

            _logger.LogTrace(
                "ExtractEmbedding: Successfully generated {Dimension}-dimensional embedding",
                embedding.Length);

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExtractEmbedding exception: {Message}", ex.Message);
            return null;
        }
    }

    private FaceVerificationResult ScoreEmbeddings(float[] reference, float[] captured)
    {
        // Validate embedding dimensions
        if (reference.Length != EmbeddingDimension || captured.Length != EmbeddingDimension)
        {
            _logger.LogWarning(
                "ScoreEmbeddings: Dimension mismatch - reference={RefDim}, captured={CapDim}, expected={Expected}",
                reference.Length, captured.Length, EmbeddingDimension);

            // If dimensions don't match, we can't compare reliably
            if (reference.Length != captured.Length)
            {
                return new FaceVerificationResult
                {
                    IsMatch = false,
                    Confidence = 0,
                    Message = $"Embedding dimension mismatch: {reference.Length} vs {captured.Length}"
                };
            }
        }

        var similarity = GeometryExtensions.CosineSimilarity(reference, captured);
        var confidence = (double)Math.Max(0f, similarity) * 100.0;

        return new FaceVerificationResult
        {
            IsMatch = similarity >= MatchThreshold,
            Confidence = confidence,
            Message = similarity >= MatchThreshold
                ? $"Face verified ({confidence:F1}%)"
                : $"Face not recognized ({confidence:F1}%)"
        };
    }

    private (bool leftOpen, bool rightOpen) DetectEyeStates(Image<Rgb24> image, RectangleF faceBox)
    {
        var cropRect = ClampRect(image, faceBox);
        if (cropRect.Width <= 0 || cropRect.Height <= 0) return (false, false);

        using var faceCrop = image.Clone(ctx => ctx.Crop(cropRect));

        IReadOnlyList<PointF> landmarks;
        try
        {
            landmarks = _detector!.DetectLandmarks(faceCrop);
        }
        catch (InvalidOperationException)
        {
            return (false, false);
        }

        if (landmarks.Count == 0) return (false, false);

        var leftCenter = _detector.GetLeftEyeCenter(landmarks);
        var rightCenter = _detector.GetRightEyeCenter(landmarks);
        var boxes = ImageCalculations.GetEyeBoxesFromCenterPoints(leftCenter, rightCenter, EyeBoxDivisor);

        return (TryDetectEyeState(faceCrop, boxes.Left), TryDetectEyeState(faceCrop, boxes.Right));
    }

    private bool TryDetectEyeState(Image<Rgb24> parentImage, Rectangle eyeBox)
    {
        var rect = ClampRect(parentImage, eyeBox);
        if (rect.Width <= 0 || rect.Height <= 0) return false;

        using var eyeCrop = parentImage.Clone(ctx => ctx.Crop(rect));
        return _eyeDetector!.IsOpen(eyeCrop);
    }

    private static Image<Rgb24> LoadRgb24(byte[] data)
    {
        using var ms = new MemoryStream(data);
        var image = ISImage.Load<Rgb24>(ms);
        image.Mutate(x => x.AutoOrient());
        return image;
    }

    private static byte[] EncodeJpeg(Image<Rgb24> image, int quality = 85)
    {
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = quality });
        return ms.ToArray();
    }

    private static Rectangle ClampRect(Image<Rgb24> image, RectangleF box)
    {
        int x = Math.Max(0, (int)box.X);
        int y = Math.Max(0, (int)box.Y);
        int w = Math.Min((int)Math.Ceiling(box.Width), image.Width - x);
        int h = Math.Min((int)Math.Ceiling(box.Height), image.Height - y);
        return new Rectangle(x, y, Math.Max(0, w), Math.Max(0, h));
    }

    private static Rectangle ClampRect(Image<Rgb24> image, Rectangle rect)
    {
        int x = Math.Max(0, rect.X);
        int y = Math.Max(0, rect.Y);
        int w = Math.Min(rect.Width, image.Width - x);
        int h = Math.Min(rect.Height, image.Height - y);
        return new Rectangle(x, y, Math.Max(0, w), Math.Max(0, h));
    }

    private static FaceVerificationResult Fail(string message) => new()
    {
        IsMatch = false,
        Confidence = 0,
        Message = message
    };

}
