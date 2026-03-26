namespace FaceRecognitionApp.Models;

public class FaceData
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? ImageUrl { get; set; }
    public byte[]? ImageData { get; set; }
    public DateTime? LastUpdated { get; set; }
    public bool IsCameraActive { get; set; }
}



public class FaceVerificationResult
{
    public bool IsMatch { get; set; }
    public double Confidence { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}


public class RealtimeFaceFrame
{
    public byte[]? FrameData { get; set; }
    public bool FaceDetected { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Result of a single-pass frame analysis: face detection, liveness, and embedding
/// extracted in one image-load / one ONNX face-detection call.
/// </summary>
public sealed record FrameAnalysisResult(
    bool   FaceDetected,
    bool   LeftEyeOpen,
    bool   RightEyeOpen,
    float[]? Embedding);
