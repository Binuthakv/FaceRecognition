namespace FaceRecognitionApp.Api.Models;

// ── Shared domain models ──────────────────────────────────────────────────────

public class FaceVerificationResult
{
    public bool IsMatch { get; set; }
    public double Confidence { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed record FrameAnalysisResult(
    bool     FaceDetected,
    bool     LeftEyeOpen,
    bool     RightEyeOpen,
    float[]? Embedding);

// ── Request DTOs ──────────────────────────────────────────────────────────────

/// <summary>Single image payload — image bytes encoded as Base64.</summary>
public sealed record ImageRequest(string ImageData);

/// <summary>Two images for face verification.</summary>
public sealed record VerifyFacesRequest(string ReferenceFace, string CapturedFace);

/// <summary>Two pre-computed embeddings for cosine-similarity scoring.</summary>
public sealed record VerifyEmbeddingsRequest(float[] ReferenceEmbedding, float[] CapturedEmbedding);

/// <summary>Store an embedding for a user in the vector database.</summary>
public sealed record StoreEmbeddingRequest(string UserId, int PhotoNumber, float[] Embedding);

/// <summary>Search for similar embeddings in the vector database.</summary>
public sealed record SearchEmbeddingsRequest(float[] QueryEmbedding, int TopK = 5, float Threshold = 0.42f);

/// <summary>Search for a face in the vector database using an image.</summary>
public sealed record FaceSearchRequest(string ImageData, int TopK = 5, float Threshold = 0.42f);

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>Result of DetectFaceAsync — null FaceData means no face was found.</summary>
public sealed record DetectFaceResponse(string? FaceData);

/// <summary>Result of DetectFaceInFrameAsync.</summary>
public sealed record DetectFaceInFrameResponse(bool Detected, string? FaceData);

/// <summary>Result of DetectEyeStateAsync.</summary>
public sealed record EyeStateResponse(bool LeftOpen, bool RightOpen);

/// <summary>Result of ExtractEmbeddingAsync — null means no face/alignment failure.</summary>
public sealed record EmbeddingResponse(float[]? Embedding);

/// <summary>Result of face search in database.</summary>
public sealed record FaceSearchResult(
    bool Found,
    Services.EmbeddingSearchResult? BestMatch,
    List<Services.EmbeddingSearchResult> AllMatches);

/// <summary>A stored embedding with its photo number.</summary>
public sealed record StoredEmbedding(int PhotoNumber, float[] Embedding);

/// <summary>All embeddings stored for a user.</summary>
public sealed record UserEmbeddingsResponse(string UserId, List<StoredEmbedding> Embeddings);
// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record UserRegistrationResponse(
    int Id,
    string UserId,
    string Name,
    int EmbeddingsExtracted);

public sealed record EmbeddingRefreshResponse(
    string UserId,
    int EmbeddingsRefreshed);

public sealed record UserEmbeddingStatus(
    string UserId,
    string UserName,
    int TotalEmbeddings);


