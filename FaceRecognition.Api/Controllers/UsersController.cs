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
    private readonly IFaceONNXService _faceOnnxServ;

    public UsersController(IUserDatabaseService db,
        IFaceRecognitionService faceService,
        IFaceONNXService faceOnnxServ)
    {
        _db = db;
        _faceService = faceService;
        _faceOnnxServ = faceOnnxServ;
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
    /// Registers a new user and automatically extracts face embeddings from their photo.
    /// Requires exactly one photo (Photo1).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserRegistrationResponse>> Save(
        [FromBody] UserRegistration user, CancellationToken ct)
    {
        // Validate that exactly one photo is provided
        if (user.Photo1 is not { Length: > 0 })
        {
            var errors = new List<PhotoEmbeddingError>
            {
                new PhotoEmbeddingError(1, "Photo1 is required for registration")
            };
            var validationResponse = new PhotoEmbeddingValidationResponse(
                user.UserId,
                user.Name,
                errors,
                "No valid photo provided");
            return BadRequest(validationResponse);
        }

        int userId = 0;
        var embeddingsExtracted = 0;
        float[]? emb1 = null;
        var errors_extraction = new List<PhotoEmbeddingError>();

        // Extract embedding from Photo1
        emb1 = await _faceOnnxServ.ExtractEmbeddingAsync(user.Photo1, ct);
        if (emb1 is null)
            errors_extraction.Add(new PhotoEmbeddingError(1, "Failed to extract face embedding from Photo1. Ensure face is clearly visible."));

        // Store embedding if extraction was successful
        if (emb1 is not null)
        {
            // Save user first
            userId = await _db.SaveUserAsync(user);

            // Save the embedding
            embeddingsExtracted = await _db.SaveUserEmbeddingsAsync(user.UserId, emb1);

            var response = new UserRegistrationResponse(
                userId,
                user.UserId,
                user.Name,
                embeddingsExtracted);

            return CreatedAtAction(nameof(GetById), new { id = userId }, response);
        }
        else
        {
            var validationResponse = new PhotoEmbeddingValidationResponse(
                user.UserId,
                user.Name,
                errors_extraction,
                "Failed to extract face embedding from the provided photo");

            return BadRequest(validationResponse);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<int>> Update(int id, [FromBody] UserRegistration user, CancellationToken ct)
    {
        if (user.Id != id) return BadRequest("ID mismatch.");

        // Validate that at least Photo1 is provided
        if (user.Photo1 is not { Length: > 0 })
        {
            var errors = new List<PhotoEmbeddingError>
            {
                new PhotoEmbeddingError(1, "Photo1 is required for update")
            };
            var validationResponse = new PhotoEmbeddingValidationResponse(
                user.UserId,
                user.Name,
                errors,
                "No valid photo provided");
            return BadRequest(validationResponse);
        }

        // Extract embedding from Photo1
        var embeddingsExtracted = 0;
        float[]? emb1 = null;
        var errors_extraction = new List<PhotoEmbeddingError>();

        emb1 = await _faceOnnxServ.ExtractEmbeddingAsync(user.Photo1, ct);
        if (emb1 is null || emb1.Length != 512)
            errors_extraction.Add(new PhotoEmbeddingError(1, "Failed to extract face embedding from Photo1"));

        if (emb1 is not null)
        {
            var result = await _db.UpdateUserAsync(user);
            embeddingsExtracted = await _db.SaveUserEmbeddingsAsync(user.UserId, emb1);
            return Ok(result);
        }
        else
        {
            var validationResponse = new PhotoEmbeddingValidationResponse(
                user.UserId,
                user.Name,
                errors_extraction,
                "Failed to extract face embedding from the provided photo");

            return BadRequest(validationResponse);
        }
    }


    /// <summary>
    /// Updates the face embeddings for an existing user by re-extracting from their photo (Photo1 only).
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

        // Extract new embedding from Photo1 only
        float[]? emb1 = null;

        if (user.Photo1 is { Length: > 0 })
            emb1 = await _faceService.ExtractEmbeddingAsync(user.Photo1, ct);

        var saved = await _db.SaveUserEmbeddingsAsync(userId, emb1);

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

