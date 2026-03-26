using Microsoft.AspNetCore.Mvc;
using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;

namespace FaceRecognitionApp.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserDatabaseService _db;
    private readonly IFaceRecognitionService _faceService;

    public UsersController(IUserDatabaseService db, IFaceRecognitionService faceService)
    {
        _db = db;
        _faceService = faceService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserRegistration>>> GetAll()
        => Ok(await _db.GetAllUsersAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserRegistration>> GetById(int id)
    {
        var user = await _db.GetUserAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("by-userid/{userId}")]
    public async Task<ActionResult<UserRegistration>> GetByUserId(string userId)
    {
        var user = await _db.GetUserByUserIdAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("exists/{userId}")]
    public async Task<ActionResult<bool>> Exists(string userId)
        => Ok(await _db.UserIdExistsAsync(userId));

    [HttpGet("count")]
    public async Task<ActionResult<int>> Count()
        => Ok(await _db.GetUserCountAsync());

    /// <summary>
    /// Registers a new user and automatically extracts face embeddings from their photos.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserRegistrationResponse>> Save(
        [FromBody] UserRegistration user, CancellationToken ct)
    {
        // Save user first
        var userId = await _db.SaveUserAsync(user);

        // Extract embeddings from photos (if available)
        var embeddingsExtracted = 0;
        float[]? emb1 = null, emb2 = null, emb3 = null;

        if (user.Photo1 is { Length: > 0 })
            emb1 = await _faceService.ExtractEmbeddingAsync(user.Photo1, ct);

        if (user.Photo2 is { Length: > 0 })
            emb2 = await _faceService.ExtractEmbeddingAsync(user.Photo2, ct);

        if (user.Photo3 is { Length: > 0 })
            emb3 = await _faceService.ExtractEmbeddingAsync(user.Photo3, ct);

        // Store all embeddings
        if (emb1 is not null || emb2 is not null || emb3 is not null)
        {
            embeddingsExtracted = await _db.SaveUserEmbeddingsAsync(user.UserId, emb1, emb2, emb3);
        }

        var response = new UserRegistrationResponse(
            userId,//user.Id,
            user.UserId,
            user.Name,
            embeddingsExtracted);

        return CreatedAtAction(nameof(GetById), new { id = userId }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<int>> Update(int id, [FromBody] UserRegistration user, CancellationToken ct)
    {
        if (user.Id != id) return BadRequest("ID mismatch.");
        var result = await _db.UpdateUserAsync(user);
        // Extract and update embeddings from photos (if available)
        var embeddingsExtracted = 0;
        float[]? emb1 = null, emb2 = null, emb3 = null;

        if (user.Photo1 is { Length: > 0 })
            emb1 = await _faceService.ExtractEmbeddingAsync(user.Photo1, ct);

        if (user.Photo2 is { Length: > 0 })
            emb2 = await _faceService.ExtractEmbeddingAsync(user.Photo2, ct);

        if (user.Photo3 is { Length: > 0 })
            emb3 = await _faceService.ExtractEmbeddingAsync(user.Photo3, ct);

        // Store all embeddings
        if (emb1 is not null || emb2 is not null || emb3 is not null)
        {
            embeddingsExtracted = await _db.SaveUserEmbeddingsAsync(user.UserId, emb1, emb2, emb3);
        }
        return Ok(result);
    }

    /// <summary>
    /// Updates the face embeddings for an existing user by re-extracting from their photos.
    /// </summary>
    [HttpPost("{userId}/refresh-embeddings")]
    public async Task<ActionResult<EmbeddingRefreshResponse>> RefreshEmbeddings(
        string userId, CancellationToken ct)
    {
        var user = await _db.GetUserByUserIdAsync(userId);
        if (user is null)
            return NotFound($"User {userId} not found");

        // Delete existing embeddings
        await _db.DeleteUserEmbeddingsAsync(userId);

        // Extract new embeddings
        float[]? emb1 = null, emb2 = null, emb3 = null;

        if (user.Photo1 is { Length: > 0 })
            emb1 = await _faceService.ExtractEmbeddingAsync(user.Photo1, ct);

        if (user.Photo2 is { Length: > 0 })
            emb2 = await _faceService.ExtractEmbeddingAsync(user.Photo2, ct);

        if (user.Photo3 is { Length: > 0 })
            emb3 = await _faceService.ExtractEmbeddingAsync(user.Photo3, ct);

        var saved = await _db.SaveUserEmbeddingsAsync(userId, emb1, emb2, emb3);

        return Ok(new EmbeddingRefreshResponse(userId, saved));
    }

    /// <summary>
    /// Gets the embedding status for a user.
    /// </summary>
    [HttpGet("{userId}/embeddings")]
    public async Task<ActionResult<UserEmbeddingStatus>> GetEmbeddingStatus(string userId)
    {
        var user = await _db.GetUserByUserIdAsync(userId);
        if (user is null)
            return NotFound($"User {userId} not found");

        var embeddings = await _db.GetUserEmbeddingsAsync(userId);
        var photoNumbers = embeddings.Select(e => e.PhotoNumber).ToList();

        return Ok(new UserEmbeddingStatus(
            userId,
            user.Name,
            embeddings.Count));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<int>> Delete(int id)
    {
        var user = await _db.GetUserAsync(id);
        if (user is null) return NotFound();

        // Delete embeddings first (handled in DeleteUserAsync but explicit here)
        await _db.DeleteUserEmbeddingsAsync(user.UserId);

        return Ok(await _db.DeleteUserAsync(user));
    }
}

