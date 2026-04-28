using FaceRecognitionApp.Api.Models;

namespace FaceRecognitionApp.Api.Services;

public interface IUserDatabaseService
{
    Task InitializeAsync();
    Task<int> SaveUserAsync(UserRegistration user);
    Task<int> UpdateUserAsync(UserRegistration user);
    Task<int> DeleteUserAsync(UserRegistration user);
    Task<UserRegistration?> GetUserAsync(int id);
    Task<UserRegistration?> GetUserByUserIdAsync(string userId);
    Task<List<UserRegistration>> GetAllUsersAsync();
    Task<bool> UserIdExistsAsync(string userId);
    Task<int> GetUserCountAsync();

    // Embedding operations

    /// <summary>
    /// Saves a single embedding for a user's photo.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="photoNumber">Which photo (1, 2, or 3).</param>
    /// <param name="embedding">The 512-dimensional face embedding.</param>
    Task SaveUserEmbeddingAsync(string userId, int photoNumber, float[] embedding);

    /// <summary>
    /// Saves all 3 embeddings for a user at once.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="embedding1">Embedding from photo 1 (can be null if no face detected).</param>
    /// <param name="embedding2">Embedding from photo 2 (can be null if no face detected).</param>
    /// <param name="embedding3">Embedding from photo 3 (can be null if no face detected).</param>
    /// <returns>Number of embeddings saved (0-3).</returns>
    Task<int> SaveUserEmbeddingsAsync(string userId, float[]? embedding1, float[]? embedding2, float[]? embedding3);

    /// <summary>
    /// Deletes all embeddings for a user.
    /// </summary>
    Task DeleteUserEmbeddingsAsync(string userId);

    /// <summary>
    /// Searches for similar face embeddings across all users.
    /// </summary>
    /// <param name="queryEmbedding">The embedding to search for.</param>
    /// <param name="topK">Maximum number of results.</param>
    /// <param name="threshold">Minimum similarity score (0-1).</param>
    Task<List<EmbeddingSearchResult>> SearchByEmbeddingAsync(float[] queryEmbedding, int topK = 5, float threshold = 0.42f);
    Task<List<EmbeddingSearchResult>> SearchByEmbeddingSQLAsync(float[] queryEmbedding, int topK = 5, float threshold = 0.42f);

    /// <summary>
    /// Gets all stored embeddings for a specific user.
    /// </summary>
    Task<List<(int PhotoNumber, float[] Embedding)>> GetUserEmbeddingsAsync(string userId);

    /// <summary>
    /// Gets the count of embeddings stored for a user.
    /// </summary>
    Task<int> GetUserEmbeddingCountAsync(string userId);

    // Admin User operations
    Task<AdminUser?> GetAdminUserByUsernameAsync(string username);
    Task<AdminUser?> GetAdminUserByEmailAsync(string email);
    Task<List<AdminUser>> GetAllAdminUsersAsync();
    Task<int> SaveAdminUserAsync(AdminUser adminUser);
    Task<int> UpdateAdminUserLastLoginAsync(int adminUserId);
}

/// <summary>
/// Result of an embedding similarity search.
/// </summary>
public record EmbeddingSearchResult(
    string UserId,
    string UserName,
    int PhotoNumber,
    float Similarity);
