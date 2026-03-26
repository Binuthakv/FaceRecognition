using FaceRecognitionApp.Api.Models;

namespace FaceRecognitionApp.Api.Services;

public interface IFaceRecognitionService
{
    Task InitializeFaceAiSharpAsync();
    Task<byte[]?> DetectFaceAsync(
        byte[] imageData, CancellationToken cancellationToken = default);

    Task<(bool detected, byte[]? faceData)> DetectFaceInFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default);

    Task<FaceVerificationResult> VerifyFacesAsync(
        byte[] referenceFace, byte[] capturedFace, CancellationToken cancellationToken = default);

    Task<(bool leftOpen, bool rightOpen)> DetectEyeStateAsync(
        byte[] imageData, CancellationToken cancellationToken = default);

    Task<float[]?> ExtractEmbeddingAsync(
        byte[] imageData, CancellationToken cancellationToken = default);

    Task<FaceVerificationResult> VerifyEmbeddingsAsync(
        float[] referenceEmbedding, float[] capturedEmbedding);

    /// <summary>
    /// Single-pass frame analysis: one image decode and one ONNX face-detection
    /// inference covering face detection, liveness, and embedding extraction.
    /// </summary>
    Task<FrameAnalysisResult> AnalyzeFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default);
}
