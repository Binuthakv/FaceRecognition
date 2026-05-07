using FaceRecognitionApp.Api.Models;

namespace FaceRecognitionApp.Api.Services;

public interface IFaceONNXService
{

    /// <summary>
    /// Single-pass frame analysis: one image decode and one ONNX face-detection
    /// inference covering face detection, liveness, and embedding extraction.
    /// </summary>
    Task<FrameAnalysisResult> AnalyzeFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default);

    Task<float[]?> ExtractEmbeddingAsync(
      byte[] imageData, CancellationToken cancellationToken = default);

    Task<(bool detected, byte[]? faceData)> DetectFaceInFrameAsync(
       byte[] frameData, CancellationToken cancellationToken = default);
}
