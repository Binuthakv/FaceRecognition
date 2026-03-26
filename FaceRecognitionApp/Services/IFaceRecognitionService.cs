using FaceRecognitionApp.Models;

namespace FaceRecognitionApp.Services;

public interface IFaceRecognitionService
{
    Task<byte[]?> DetectFaceAsync(
        byte[] imageData, CancellationToken cancellationToken = default);

    Task<(bool detected, byte[]? faceData)> DetectFaceInFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default);

    Task<FaceVerificationResult> VerifyFacesAsync(
        byte[] referenceFace, byte[] capturedFace, CancellationToken cancellationToken = default);

    Task<FaceVerificationResult> VerifyFaceInRealtimeAsync(
        byte[] referenceFace, byte[] frameData, CancellationToken cancellationToken = default);

    Task<(bool leftOpen, bool rightOpen)> DetectEyeStateAsync(
        byte[] imageData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs face detection, liveness check, and embedding extraction in a single
    /// image-load / ONNX-inference pass. Use this for real-time processing instead of
    /// calling <see cref="DetectFaceInFrameAsync"/>, <see cref="DetectEyeStateAsync"/>,
    /// and <see cref="ExtractEmbeddingAsync"/> separately — those three calls each
    /// reload and re-decode the image and re-run ONNX face detection.
    /// Default implementation falls back to the three individual methods.
    /// </summary>
    async Task<FrameAnalysisResult> AnalyzeFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default)
    {
        var (faceDetected, _) = await DetectFaceInFrameAsync(frameData, cancellationToken);
        if (!faceDetected) return new FrameAnalysisResult(false, false, false, null);

        var (left, right) = await DetectEyeStateAsync(frameData, cancellationToken);
        if (!left && !right) return new FrameAnalysisResult(true, false, false, null);

        var embedding = await ExtractEmbeddingAsync(frameData, cancellationToken);
        return new FrameAnalysisResult(true, left, right, embedding);
    }

    /// <summary>
    /// Extracts the ArcFace embedding for <paramref name="imageData"/>.
    /// Cache the result and pass it to <see cref="VerifyEmbeddingsAsync"/> to avoid
    /// re-running model inference for every stored reference photo per live frame.
    /// Default implementation returns <see langword="null"/> (platform not supported).
    /// </summary>
    Task<float[]?> ExtractEmbeddingAsync(
        byte[] imageData, CancellationToken cancellationToken = default)
        => Task.FromResult<float[]?>(null);

    /// <summary>
    /// Scores two pre-computed embeddings via cosine similarity — no model inference.
    /// Default implementation returns a "not supported" fail result.
    /// </summary>
    Task<FaceVerificationResult> VerifyEmbeddingsAsync(
        float[] referenceEmbedding, float[] capturedEmbedding)
        => Task.FromResult(new FaceVerificationResult
        {
            IsMatch    = false,
            Confidence = 0,
            Message    = "Face recognition is not supported on this platform yet."
        });

    /// <summary>
    /// Searches for similar face embeddings via the API.
    /// Returns matching users ordered by similarity score descending.
    /// </summary>
    Task<List<EmbeddingSearchResult>> SearchEmbeddingsAsync(
        float[] queryEmbedding, int topK = 5, float threshold = 0.42f,
        CancellationToken cancellationToken = default)
        => Task.FromResult<List<EmbeddingSearchResult>>([]);
}
