using FaceRecognitionApp.Constants;
using FaceRecognitionApp.Models;
using System.Net.Http.Json;

namespace FaceRecognitionApp.Services;

/// <summary>
/// <see cref="IFaceRecognitionService"/> implementation that delegates all
/// ML inference to the remote REST API (<c>api/face</c>).
/// </summary>
public class ApiFaceRecognitionService : IFaceRecognitionService
{
    private readonly HttpClient _http;

    //public ApiFaceRecognitionService(HttpClient http)
    //{
    //    _http = http;
    //    if (_http.BaseAddress is null)
    //        _http.BaseAddress = new Uri(AppConstants.ApiBaseUrl);
    //}
    public ApiFaceRecognitionService(HttpClient http) => _http = http;

    public async Task<byte[]?> DetectFaceAsync(
        byte[] imageData, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/face/detect",
            new ImageRequest(Convert.ToBase64String(imageData)),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DetectFaceResponse>(cancellationToken);
        return dto?.FaceData is null ? null : Convert.FromBase64String(dto.FaceData);
    }

    public async Task<(bool detected, byte[]? faceData)> DetectFaceInFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/face/detect-in-frame",
            new ImageRequest(Convert.ToBase64String(frameData)),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DetectFaceInFrameResponse>(cancellationToken);
        if (dto is null) return (false, null);
        return (dto.Detected, dto.FaceData is null ? null : Convert.FromBase64String(dto.FaceData));
    }

    public async Task<FaceVerificationResult> VerifyFacesAsync(
        byte[] referenceFace, byte[] capturedFace,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/face/verify",
            new VerifyFacesRequest(
                Convert.ToBase64String(referenceFace),
                Convert.ToBase64String(capturedFace)),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FaceVerificationResult>(cancellationToken)
               ?? new FaceVerificationResult { IsMatch = false };
    }

    public Task<FaceVerificationResult> VerifyFaceInRealtimeAsync(
        byte[] referenceFace, byte[] frameData,
        CancellationToken cancellationToken = default)
        => VerifyFacesAsync(referenceFace, frameData, cancellationToken);

    public async Task<(bool leftOpen, bool rightOpen)> DetectEyeStateAsync(
        byte[] imageData, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/face/eye-state",
            new ImageRequest(Convert.ToBase64String(imageData)),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<EyeStateResponse>(cancellationToken);
        return dto is null ? (false, false) : (dto.LeftOpen, dto.RightOpen);
    }

    public async Task<FrameAnalysisResult> AnalyzeFrameAsync(
        byte[] frameData, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/face/analyze-frame",
            new ImageRequest(Convert.ToBase64String(frameData)),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FrameAnalysisResult>(cancellationToken)
               ?? new FrameAnalysisResult(false, false, false, null);
    }

    public async Task<float[]?> ExtractEmbeddingAsync(
        byte[] imageData, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/face/extract-embedding",
            new ImageRequest(Convert.ToBase64String(imageData)),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
        return dto?.Embedding;
    }

    public async Task<FaceVerificationResult> VerifyEmbeddingsAsync(
        float[] referenceEmbedding, float[] capturedEmbedding)
    {
        var response = await _http.PostAsJsonAsync(
            "api/face/verify-embeddings",
            new VerifyEmbeddingsRequest(referenceEmbedding, capturedEmbedding));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FaceVerificationResult>()
               ?? new FaceVerificationResult { IsMatch = false };
    }

    public async Task<List<EmbeddingSearchResult>> SearchEmbeddingsAsync(
        float[] queryEmbedding, int topK = 5, float threshold = 0.42f,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/face/embeddings/search",
            new SearchEmbeddingsRequest(queryEmbedding, topK, threshold),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EmbeddingSearchResult>>(cancellationToken)
               ?? [];
    }
}
