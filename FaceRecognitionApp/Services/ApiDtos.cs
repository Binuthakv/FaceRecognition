namespace FaceRecognitionApp.Services;

// ── Request DTOs ──────────────────────────────────────────────────────────────

internal sealed record ImageRequest(string ImageData);

internal sealed record VerifyFacesRequest(string ReferenceFace, string CapturedFace);

internal sealed record VerifyEmbeddingsRequest(float[] ReferenceEmbedding, float[] CapturedEmbedding);

// ── Response DTOs ─────────────────────────────────────────────────────────────

internal sealed record DetectFaceResponse(string? FaceData);

internal sealed record DetectFaceInFrameResponse(bool Detected, string? FaceData);

internal sealed record EyeStateResponse(bool LeftOpen, bool RightOpen);

internal sealed record EmbeddingResponse(float[]? Embedding);

// ── User API Response DTOs ────────────────────────────────────────────────────

internal sealed record UserRegistrationResponse(
    int Id,
    string UserId,
    string Name,
    int EmbeddingsExtracted);

internal sealed record EmbeddingRefreshResponse(
    string UserId,
    int EmbeddingsRefreshed);

internal sealed record UserEmbeddingStatus(
    string UserId,
    string UserName,
    int TotalEmbeddings);

// ── Embedding Search DTOs ─────────────────────────────────────────────────────

internal sealed record SearchEmbeddingsRequest(float[] QueryEmbedding, int TopK = 5, float Threshold = 0.42f);

public sealed record EmbeddingSearchResult(
    string UserId,
    string UserName,
    int PhotoNumber,
    float Similarity);

// ── Admin Login DTOs ──────────────────────────────────────────────────────────

internal sealed record AdminLoginRequest(string Username, string Password, bool RememberMe);

public sealed record AdminLoginResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    bool IsActive);
