using Microsoft.AspNetCore.Mvc;
using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;

namespace FaceRecognitionApp.Api.Controllers;

[ApiController]
[Route("api/face")]
public class FaceRecognitionController : ControllerBase
{
    private readonly IFaceRecognitionService _service;
    private readonly IUserDatabaseService _userDb;

    private readonly IFaceONNXService _faceOnnxServ;

    public FaceRecognitionController(
        IFaceRecognitionService service,
        IUserDatabaseService userDb,
        IFaceONNXService faceOnnxServ)
    {
        _service = service;
        _userDb = userDb;
        _faceOnnxServ = faceOnnxServ;
    }

    /// <summary>
    /// Single-pass frame analysis (face detection + liveness + embedding).
    /// Use this for real-time verification instead of calling the three
    /// individual endpoints separately.
    /// </summary>
    [HttpPost("analyze-frame")]
    public async Task<ActionResult<FrameAnalysisResult>> AnalyzeFrame(
        [FromBody] ImageRequest request, CancellationToken ct)
    {
        var imageBytes = Convert.FromBase64String(request.ImageData);
        var result = await _faceOnnxServ.AnalyzeFrameAsync(imageBytes, ct);
        return Ok(result);
    }

    /// <summary>Detects a face and returns the cropped face image as Base64.</summary>
    [HttpPost("detect")]
    public async Task<ActionResult<DetectFaceResponse>> DetectFace(
        [FromBody] ImageRequest request, CancellationToken ct)
    {
        var imageBytes = Convert.FromBase64String(request.ImageData);
        var faceBytes  = await _service.DetectFaceAsync(imageBytes, ct);
        return Ok(new DetectFaceResponse(faceBytes is null ? null : Convert.ToBase64String(faceBytes)));
    }

    /// <summary>Detects a face in a frame; returns detected flag and cropped face as Base64.</summary>
    [HttpPost("detect-in-frame")]
    public async Task<ActionResult<DetectFaceInFrameResponse>> DetectFaceInFrame(
        [FromBody] ImageRequest request, CancellationToken ct)
    {
        var imageBytes            = Convert.FromBase64String(request.ImageData);
        var (detected, faceBytes) = await _faceOnnxServ.DetectFaceInFrameAsync(imageBytes, ct);
        return Ok(new DetectFaceInFrameResponse(detected, faceBytes is null ? null : Convert.ToBase64String(faceBytes)));
    }

    /// <summary>Verifies two face images against each other.</summary>
    [HttpPost("verify")]
    public async Task<ActionResult<FaceVerificationResult>> VerifyFaces(
        [FromBody] VerifyFacesRequest request, CancellationToken ct)
    {
        var refBytes = Convert.FromBase64String(request.ReferenceFace);
        var capBytes = Convert.FromBase64String(request.CapturedFace);
        var result   = await _service.VerifyFacesAsync(refBytes, capBytes, ct);
        return Ok(result);
    }

    /// <summary>Extracts the ArcFace embedding for the supplied image.</summary>
    [HttpPost("extract-embedding")]
    public async Task<ActionResult<EmbeddingResponse>> ExtractEmbedding(
        [FromBody] ImageRequest request, CancellationToken ct)
    {
        var imageBytes = Convert.FromBase64String(request.ImageData);
        var embedding  = await _service.ExtractEmbeddingAsync(imageBytes, ct);
        return Ok(new EmbeddingResponse(embedding));
    }

    /// <summary>Scores two pre-computed embeddings via cosine similarity (no ML inference).</summary>
    [HttpPost("verify-embeddings")]
    public async Task<ActionResult<FaceVerificationResult>> VerifyEmbeddings(
        [FromBody] VerifyEmbeddingsRequest request)
    {
        var result = await _service.VerifyEmbeddingsAsync(request.ReferenceEmbedding, request.CapturedEmbedding);
        return Ok(result);
    }

    /// <summary>Detects whether eyes are open in the supplied image.</summary>
    [HttpPost("eye-state")]
    public async Task<ActionResult<EyeStateResponse>> DetectEyeState(
        [FromBody] ImageRequest request, CancellationToken ct)
    {
        var imageBytes          = Convert.FromBase64String(request.ImageData);
        var (leftOpen, rightOpen) = await _service.DetectEyeStateAsync(imageBytes, ct);
        return Ok(new EyeStateResponse(leftOpen, rightOpen));
    }

    // ── Embedding operations ──────────────────────────────────────────────────

    /// <summary>Stores a face embedding for a user.</summary>
    [HttpPost("embeddings/store")]
    public async Task<ActionResult> StoreEmbedding([FromBody] StoreEmbeddingRequest request)
    {
        await _userDb.SaveUserEmbeddingAsync(request.UserId, request.PhotoNumber, request.Embedding);
        return Ok(new { message = "Embedding stored successfully" });
    }

    /// <summary>Searches the database for similar face embeddings.</summary>
    [HttpPost("embeddings/search")]
    public async Task<ActionResult<List<EmbeddingSearchResult>>> SearchEmbeddings(
        [FromBody] SearchEmbeddingsRequest request)
    {
        //var matches = await _userDb.SearchByEmbeddingAsync(
        //    request.QueryEmbedding,
        //    request.TopK,
        //    request.Threshold);
        // Using SQL-based search for better performance with large datasets using vector search extension 
        var matches = await _userDb.SearchByEmbeddingSQLAsync(
            request.QueryEmbedding,
            request.TopK,
            request.Threshold);
        return Ok(matches);
    }

    /// <summary>Extracts embedding from image and searches for matching users.</summary>
    [HttpPost("search-face")]
    public async Task<ActionResult<FaceSearchResult>> SearchFace(
        [FromBody] FaceSearchRequest request, CancellationToken ct)
    {
        var imageBytes = Convert.FromBase64String(request.ImageData);
        var embedding = await _service.ExtractEmbeddingAsync(imageBytes, ct);

        if (embedding is null)
            return Ok(new FaceSearchResult(false, null, []));

        var matches = await _userDb.SearchByEmbeddingAsync(
            embedding,
            request.TopK,
            request.Threshold);

        var bestMatch = matches.MaxBy(m => m.Similarity);

        return Ok(new FaceSearchResult(
            matches.Count > 0,
            bestMatch,
            matches));
    }

    /// <summary>Gets all stored embeddings for a user.</summary>
    [HttpGet("embeddings/{userId}")]
    public async Task<ActionResult<UserEmbeddingsResponse>> GetUserEmbeddings(string userId)
    {
        var embeddings = await _userDb.GetUserEmbeddingsAsync(userId);
        return Ok(new UserEmbeddingsResponse(
            userId,
            embeddings.Select(e => new StoredEmbedding(e.PhotoNumber, e.Embedding)).ToList()));
    }
}
