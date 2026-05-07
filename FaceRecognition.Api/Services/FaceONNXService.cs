using FaceONNX;
using FaceRecognitionApp.Api.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Drawing;
using UMapx.Core;
using Image = SixLabors.ImageSharp.Image;
using Point = System.Drawing.Point;

namespace FaceRecognitionApp.Api.Services;

public class FaceONNXService : IFaceONNXService
{
    private FaceDetector? _detector;
    private Face68LandmarksExtractor? _faceLandmarksExtractor;
    private FaceEmbedder _faceEmbedder;
    private FaceAgeEstimator _faceAgeEstimator;
    private FaceGenderClassifier _faceGenderClassifier;


    private readonly ILogger<FaceONNXService> _logger;

    private const int EmbeddingDimension = 512;  // ArcFace produces 512-dimensional embeddings

    public FaceONNXService(ILogger<FaceONNXService> logger)
    {
        _logger = logger;
        try
        {
            _detector = new FaceDetector();
            _faceLandmarksExtractor = new Face68LandmarksExtractor();
            _faceEmbedder = new FaceEmbedder();
            _faceAgeEstimator = new FaceAgeEstimator();
            _faceGenderClassifier = new FaceGenderClassifier();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to load face detection models. Please ensure ONNX Runtime is properly installed.: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<FrameAnalysisResult> AnalyzeFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default)
    {

        return await Task.Run(() =>
        {
            try
            {
                var imagedata = FixExifOrientation(frameData);
                using var image = GetImage(imagedata);
                var labels = FaceGenderClassifier.Labels;
                var faces = _detector.Forward(image);

                if (faces.Length == 0)
                {
                    _logger.LogTrace("AnalyzeFrame: No face detected");
                    return new FrameAnalysisResult(false, false, false, null);
                }

                var face = faces.MaxBy(f => f.Score);
                var box = face.Box;
                _logger.LogTrace("AnalyzeFrame: Face detected with confidence {Confidence}", face?.Score);
                var landmarks68 = _faceLandmarksExtractor.Forward(image, box);
                var angle = landmarks68.RotationAngle;
                var aligned = FaceProcessingExtensions.Align(image, box, angle, false);
                // ── Eye state detection (non-blocking) ───────────────────────────
                bool leftOpen = false, rightOpen = false;
                try
                {
                    double leftEAR = CalculateEAR(landmarks68.LeftEye);
                    double rightEAR = CalculateEAR(landmarks68.RightEye);
                    //double ear = (leftEAR + rightEAR) / 2.0;
                    leftOpen = leftEAR > 0.20;
                    rightOpen = rightEAR > 0.20;
                    _logger.LogTrace("AnalyzeFrame: Eye states - left={Left}, right={Right}", leftOpen, rightOpen);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AnalyzeFrame: Eye state detection failed: {Message}", ex.Message);
                    // Continue with embedding generation even if eye detection fails
                }
                // ── Age and gender detection ─────────────────────────────────────────
                try
                {
                    //var age = _faceAgeEstimator.Forward(aligned);
                    //var genderClassifier = _faceGenderClassifier.Forward(aligned);
                    //var max = Matrice.Max(genderClassifier, out int genderIndex);
                    //var gender = labels[genderIndex];
                    //_logger.LogInformation($"Status: Detected {gender} gender with probability {genderClassifier.Max()} and age {age.First()}");

                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AnalyzeFrame: Age and gender detection failed: {Message}", ex.Message);
                    // Continue with embedding generation even if Age and gender detection fails
                }
                // ── Embedding generation ─────────────────────────────────────────
                float[]? embedding = null;
                try
                {

                    embedding = _faceEmbedder.Forward(aligned);
                    // Validate 512-dimensional embedding
                    if (embedding is not null && embedding.Length != EmbeddingDimension)
                    {
                        _logger.LogWarning("AnalyzeFrame: Unexpected embedding dimension {Actual}, expected {Expected}",
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

    public async Task<float[]?> ExtractEmbeddingAsync(
       byte[] imageData, CancellationToken cancellationToken = default)
    {

        return await Task.Run(() => ExtractEmbedding(imageData), cancellationToken);
    }

    public async Task<(bool detected, byte[]? faceData)> DetectFaceInFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default)
    {

        return await Task.Run(() =>
        {
            var imagedata = FixExifOrientation(frameData);
            using var image = GetImage(imagedata);
            var faces = _detector.Forward(image);
            if (faces.Length == 0) return (false, (byte[]?)null);
            var face = faces.MaxBy(f => f.Score);
            var box = face.Box;
            var landmarks68 = _faceLandmarksExtractor.Forward(image, box);
            var angle = landmarks68.RotationAngle;
            var aligned = FaceProcessingExtensions.Align(image, box, angle, false);

            //using var cropped = BitmapTransform.Crop(image, face.Box);

            return (true, (byte[]?)EncodeImageToJpeg(aligned));
        }, cancellationToken);
    }


    // ── Private helpers ───────────────────────────────────────────────────────
    private float[]? ExtractEmbedding(byte[] imageData)
    {
        try
        {
            using var image = GetImage(imageData);
            var faces = _detector.Forward(image);
            if (faces.Length == 0)
            {
                _logger.LogTrace("ExtractEmbedding: No face detected");
                return null;
            }
            var face = faces.MaxBy(f => f.Score);
            var box = face.Box;
            var landmarks68 = _faceLandmarksExtractor.Forward(image, box);
            var angle = landmarks68.RotationAngle;
            var aligned = FaceProcessingExtensions.Align(image, box, angle, false);
            return _faceEmbedder.Forward(aligned);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExtractEmbedding: Failed to extract embedding");
            return null;
        }
    }
    private double Distance(Point a, Point b)
    {
        return Math.Sqrt((a.X - b.X) * (a.X - b.X) +
                         (a.Y - b.Y) * (a.Y - b.Y));
    }

    private double CalculateEAR(Point[] eye)
    {
        double A = Distance(eye[1], eye[5]);
        double B = Distance(eye[2], eye[4]);
        double C = Distance(eye[0], eye[3]);

        return (A + B) / (2.0 * C);
    }


    private static Bitmap GetImage(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        {
            return new Bitmap(ms);
        }
    }
    private static byte[] EncodeImageToJpeg(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        return ms.ToArray();
    }
    public static byte[] FixExifOrientation(byte[] data)
    {
        using var imageStream = new MemoryStream(data);
        using var image = Image.Load(imageStream);
        // Automatically rotates/flips based on EXIF
        image.Mutate(x => x.AutoOrient());
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }
}
