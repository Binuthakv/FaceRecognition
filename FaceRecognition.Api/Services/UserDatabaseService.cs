using FaceRecognitionApp.Api.Models;
using Microsoft.Data.Sqlite;
using SQLite;

namespace FaceRecognitionApp.Api.Services;

public class UserDatabaseService : IUserDatabaseService, IDisposable
{
    private SqliteConnection? _connection;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    private readonly ILogger<UserDatabaseService> _logger;

    // ArcFace produces 512-dimensional embeddings
    private const int EmbeddingDimension = 512;

    public UserDatabaseService(ILogger<UserDatabaseService> logger)
    {
        _logger = logger;
        _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FaceRecognitionDB.db");
        _logger.LogInformation("UserDatabaseService created. Database path: {Path}", _dbPath);
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _logger.LogDebug("Initializing database connection...");

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            _connection = new SqliteConnection(connectionString);
            await _connection.OpenAsync();

            _connection.EnableExtensions(true);  //Enable vector search extension       
            _connection.LoadExtension("vec0.dll");//Enable vector search extension       

            _logger.LogDebug("Creating Users table if not exists...");
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    DateOfBirth TEXT NOT NULL,
                    Sex TEXT NOT NUll,
                    RegisteredDate TEXT NOT NULL,
                    Photo1 BLOB,
                    Photo2 BLOB,
                    Photo3 BLOB
                );
                CREATE INDEX IF NOT EXISTS IX_Users_UserId ON Users(UserId);
                """;
            await cmd.ExecuteNonQueryAsync();

            _logger.LogDebug("Creating UserEmbeddings table if not exists...");
            //cmd.CommandText = """
            //    CREATE TABLE IF NOT EXISTS UserEmbeddings (
            //        Id INTEGER PRIMARY KEY AUTOINCREMENT,
            //        UserId TEXT NOT NULL,
            //        PhotoNumber INTEGER NOT NULL,
            //        Embedding BLOB NOT NULL,
            //        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
            //        UNIQUE(UserId, PhotoNumber),
            //        FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
            //    );
            //    CREATE INDEX IF NOT EXISTS IX_UserEmbeddings_UserId ON UserEmbeddings(UserId);
            //    """;
            //Enable vector search extension       
            cmd.CommandText = """
                CREATE VIRTUAL  TABLE IF NOT EXISTS UserEmbeddings USING vec0 (
                    UserId TEXT,
                    PhotoNumber INTEGER,
                    Embedding float[512],
                );
                """;       
            await cmd.ExecuteNonQueryAsync();

            // Create AdminUsers table
            _logger.LogDebug("Creating AdminUsers table if not exists...");
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS AdminUsers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    Email TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL DEFAULT 'Admin',
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedDate TEXT NOT NULL,
                    LastLoginDate TEXT
                );
                CREATE INDEX IF NOT EXISTS IX_AdminUsers_Username ON AdminUsers(Username);
                CREATE INDEX IF NOT EXISTS IX_AdminUsers_Email ON AdminUsers(Email);
                """;
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("AdminUsers table created/verified successfully");

            _initialized = true;
            _logger.LogInformation("Database initialized successfully with embedding support at {Path}", _dbPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed at {Path}", _dbPath);
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            _logger.LogDebug("Database not initialized, initializing now...");
            await InitializeAsync();
        }
    }

    // ── User CRUD operations ──────────────────────────────────────────────────

    public async Task<int> SaveUserAsync(UserRegistration user)
    {
        _logger.LogDebug("Saving user {UserId} with name '{Name}'...", user.UserId, user.Name);
        await EnsureInitializedAsync();
        user.RegisteredDate = DateTime.UtcNow;

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Users (UserId, Name, DateOfBirth,Sex, RegisteredDate, Photo1, Photo2, Photo3)
                VALUES (@userId, @name, @dob, @sex, @regDate, @photo1, @photo2, @photo3);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("@userId", user.UserId);
            cmd.Parameters.AddWithValue("@name", user.Name);
            cmd.Parameters.AddWithValue("@dob", user.DateOfBirth.ToString("O"));
            cmd.Parameters.AddWithValue("@sex", user.Sex);
            cmd.Parameters.AddWithValue("@regDate", user.RegisteredDate.ToString("O"));
            cmd.Parameters.AddWithValue("@photo1", user.Photo1 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@photo2", user.Photo2 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@photo3", user.Photo3 ?? (object)DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            user.Id = Convert.ToInt32(result);

            _logger.LogInformation("User '{Name}' saved successfully (UserId={UserId}, Id={Id})", user.Name, user.UserId, user.Id);
            return user.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user {UserId} with name '{Name}'", user.UserId, user.Name);
            throw;
        }
    }

    public async Task<int> UpdateUserAsync(UserRegistration user)
    {
        _logger.LogDebug("Updating user {UserId}...", user.UserId);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                UPDATE Users SET 
                    Name = @name,
                    DateOfBirth = @dob,
                    Sex = @sex,
                    Photo1 = @photo1,
                    Photo2 = @photo2,
                    Photo3 = @photo3
                WHERE UserId = @userId;
                """;
            cmd.Parameters.AddWithValue("@userId", user.UserId);
            cmd.Parameters.AddWithValue("@name", user.Name);
            cmd.Parameters.AddWithValue("@dob", user.DateOfBirth.ToString("O"));
            cmd.Parameters.AddWithValue("@sex", user.Sex);
            cmd.Parameters.AddWithValue("@photo1", user.Photo1 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@photo2", user.Photo2 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@photo3", user.Photo3 ?? (object)DBNull.Value);

            var result = await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("User '{Name}' (UserId={UserId}) updated. Rows affected: {RowsAffected}", user.Name, user.UserId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", user.UserId);
            throw;
        }
    }

    public async Task<int> DeleteUserAsync(UserRegistration user)
    {
        _logger.LogDebug("Deleting user {UserId}...", user.UserId);
        await EnsureInitializedAsync();

        try
        {
            // Delete embeddings first (foreign key cascade should handle this, but be explicit)
            await using (var embCmd = _connection!.CreateCommand())
            {
                embCmd.CommandText = "DELETE FROM UserEmbeddings WHERE UserId = @userId;";
                embCmd.Parameters.AddWithValue("@userId", user.UserId);
                var embeddingsDeleted = await embCmd.ExecuteNonQueryAsync();
                _logger.LogDebug("Deleted {Count} embeddings for user {UserId}", embeddingsDeleted, user.UserId);
            }

            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE UserId = @userId;";
            cmd.Parameters.AddWithValue("@userId", user.UserId);

            var result = await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("User '{Name}' (UserId={UserId}) deleted. Rows affected: {RowsAffected}", user.Name, user.UserId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", user.UserId);
            throw;
        }
    }

    public async Task<UserRegistration?> GetUserAsync(int id)
    {
        _logger.LogDebug("Retrieving user by Id={Id}...", id);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = MapUserFromReader(reader);
                _logger.LogDebug("User found: Id={Id}, UserId={UserId}, Name='{Name}'", user.Id, user.UserId, user.Name);
                return user;
            }

            _logger.LogDebug("No user found with Id={Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user by Id={Id}", id);
            throw;
        }
    }

    public async Task<UserRegistration?> GetUserByUserIdAsync(string userId)
    {
        _logger.LogDebug("Retrieving user by UserId={UserId}...", userId);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE UserId = @userId;";
            cmd.Parameters.AddWithValue("@userId", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = MapUserFromReader(reader);
                _logger.LogDebug("User found: UserId={UserId}, Name='{Name}'", user.UserId, user.Name);
                return user;
            }

            _logger.LogDebug("No user found with UserId={UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user by UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserRegistration>> GetAllUsersAsync()
    {
        _logger.LogDebug("Retrieving all users...");
        await EnsureInitializedAsync();

        try
        {
            var users = new List<UserRegistration>();

            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users ORDER BY Id;";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                users.Add(MapUserFromReader(reader));

            _logger.LogInformation("Retrieved {Count} users from database", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all users");
            throw;
        }
    }

    public async Task<bool> UserIdExistsAsync(string userId)
    {
        _logger.LogDebug("Checking if UserId={UserId} exists...", userId);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE UserId = @userId;";
            cmd.Parameters.AddWithValue("@userId", userId);

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            var exists = count > 0;
            _logger.LogDebug("UserId={UserId} exists: {Exists}", userId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if UserId={UserId} exists", userId);
            throw;
        }
    }

    public async Task<int> GetUserCountAsync()
    {
        _logger.LogDebug("Getting user count...");
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users;";

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            _logger.LogDebug("Total user count: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user count");
            throw;
        }
    }

    // ── Embedding operations ──────────────────────────────────────────────────

    public async Task SaveUserEmbeddingAsync(string userId, int photoNumber, float[] embedding)
    {
        _logger.LogDebug("Saving embedding for user {UserId}, photo {PhotoNumber}...", userId, photoNumber);
        await EnsureInitializedAsync();

        if (embedding.Length != EmbeddingDimension)
        {
            _logger.LogError("Invalid embedding dimension for user {UserId}: expected {Expected}, got {Actual}",
                userId, EmbeddingDimension, embedding.Length);
            throw new ArgumentException($"Embedding must have {EmbeddingDimension} dimensions, got {embedding.Length}");
        }

        try
        {
            var normalized = NormalizeEmbedding(embedding);
            var embeddingBlob = FloatArrayToBlob(normalized);

            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                INSERT OR REPLACE INTO UserEmbeddings (UserId, PhotoNumber, Embedding)
                VALUES (@userId, @photoNumber, @embedding);
                """;
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@photoNumber", photoNumber);
            cmd.Parameters.AddWithValue("@embedding", embeddingBlob);

            await cmd.ExecuteNonQueryAsync();
            _logger.LogDebug("Saved embedding for user {UserId} photo {PhotoNumber} ({Dimensions} dimensions)", 
                userId, photoNumber, embedding.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save embedding for user {UserId}, photo {PhotoNumber}", userId, photoNumber);
            throw;
        }
    }

    public async Task<List<EmbeddingSearchResult>> SearchByEmbeddingAsync(
        float[] queryEmbedding, int topK = 5, float threshold = 0.42f)
    {
        _logger.LogDebug("Searching embeddings with topK={TopK}, threshold={Threshold}...", topK, threshold);
        await EnsureInitializedAsync();

        if (queryEmbedding.Length != EmbeddingDimension)
        {
            _logger.LogError("Invalid query embedding dimension: expected {Expected}, got {Actual}",
                EmbeddingDimension, queryEmbedding.Length);
            throw new ArgumentException($"Query embedding must have {EmbeddingDimension} dimensions");
        }

        try
        {
            var normalized = NormalizeEmbedding(queryEmbedding);
            var results = new List<EmbeddingSearchResult>();

            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                SELECT e.UserId, e.PhotoNumber, e.Embedding, u.Name
                FROM UserEmbeddings e
                INNER JOIN Users u ON e.UserId = u.UserId;
                """;

            var totalEmbeddings = 0;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                totalEmbeddings++;
                var userId = reader.GetString(0);
                var photoNumber = reader.GetInt32(1);
                var embeddingBlob = (byte[])reader.GetValue(2);
                var userName = reader.GetString(3);
                var storedEmbedding = BlobToFloatArray(embeddingBlob);

                var similarity = CosineSimilarity(normalized, storedEmbedding);

                if (similarity >= threshold)
                {
                    results.Add(new EmbeddingSearchResult(userId, userName, photoNumber, similarity));
                }
            }

            var topResults = results
                .OrderByDescending(r => r.Similarity)
                .Take(topK)
                .ToList();

            _logger.LogInformation("Embedding search completed. Scanned {TotalEmbeddings} embeddings, found {MatchCount} matches above threshold {Threshold}, returning top {ReturnCount}",
                totalEmbeddings, results.Count, threshold, topResults.Count);

            return topResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search embeddings");
            throw;
        }
    }

    public async Task<List<EmbeddingSearchResult>> SearchByEmbeddingSQLAsync(float[] queryEmbedding, int topK = 5, float threshold = 0.42f)
    {
        _logger.LogDebug("Searching embeddings using query with topK={TopK}, threshold={Threshold}...", topK, threshold);
        await EnsureInitializedAsync();

        if (queryEmbedding.Length != EmbeddingDimension)
        {
            _logger.LogError("Invalid query embedding dimension: expected {Expected}, got {Actual}",
                EmbeddingDimension, queryEmbedding.Length);
            throw new ArgumentException($"Query embedding must have {EmbeddingDimension} dimensions");
        }

        try
        {
            var normalized = NormalizeEmbedding(queryEmbedding);
            var results = new List<EmbeddingSearchResult>();

            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                SELECT e.UserId, e.PhotoNumber, e.Embedding, u.Name,
                vec_distance_cosine(e.embedding, @target_emb) AS distance
                FROM UserEmbeddings e
                INNER JOIN Users u ON e.UserId = u.UserId
                WHERE e.embedding MATCH @target_emb AND k = @topK
                ORDER BY distance ASC;
                
                """;
            //cmd.Parameters.AddWithValue("@target_emb", normalized);
            cmd.Parameters.Add("@target_emb", SqliteType.Blob)
            .Value = FloatArrayToBytes(queryEmbedding);

            cmd.Parameters.AddWithValue("@topK", topK);
           

            var totalEmbeddings = 0;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                totalEmbeddings++;
                var userId = reader.GetString(0);
                var photoNumber = reader.GetInt32(1);
                var embeddingBlob = (byte[])reader.GetValue(2);
                var userName = reader.GetString(3);
                var distance = reader.GetDouble(4);
                if (distance <= threshold) //distance distance = 0   → identical faces, distance = 1   → completely different
                {
                    results.Add(new EmbeddingSearchResult(userId, userName, photoNumber, 1-(float)distance));
                }
            }

            var topResults = results
                .OrderBy(r => r.Similarity)
                .Take(topK)
                .ToList();

            _logger.LogInformation("Embedding search completed. Scanned {TotalEmbeddings} embeddings, found {MatchCount} matches above threshold {Threshold}, returning top {ReturnCount}",
                totalEmbeddings, results.Count, threshold, topResults.Count);

            return topResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search embeddings");
            throw;
        }
    }
    private static byte[] FloatArrayToBytes(float[] vector)
    {
        byte[] bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }
    public async Task<List<(int PhotoNumber, float[] Embedding)>> GetUserEmbeddingsAsync(string userId)
    {
        _logger.LogDebug("Retrieving embeddings for user {UserId}...", userId);
        await EnsureInitializedAsync();

        try
        {
            var results = new List<(int, float[])>();

            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                SELECT PhotoNumber, Embedding
                FROM UserEmbeddings
                WHERE UserId = @userId
                ORDER BY PhotoNumber;
                """;
            cmd.Parameters.AddWithValue("@userId", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var photoNumber = reader.GetInt32(0);
                var embeddingBlob = (byte[])reader.GetValue(1);
                var embedding = BlobToFloatArray(embeddingBlob);
                results.Add((photoNumber, embedding));
            }

            _logger.LogDebug("Retrieved {Count} embeddings for user {UserId}", results.Count, userId);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve embeddings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> SaveUserEmbeddingsAsync(string userId, float[]? embedding1, float[]? embedding2, float[]? embedding3)
    {
        _logger.LogDebug("Saving multiple embeddings for user {UserId}...", userId);
        await EnsureInitializedAsync();

        var savedCount = 0;
        var embeddings = new (int PhotoNumber, float[]? Embedding)[]
        {
            (1, embedding1),
            (2, embedding2),
            (3, embedding3)
        };

        foreach (var (photoNumber, embedding) in embeddings)
        {
            if (embedding is null)
            {
                _logger.LogDebug("Skipping embedding {PhotoNumber} for user {UserId}: null embedding", photoNumber, userId);
                continue;
            }

            if (embedding.Length != EmbeddingDimension)
            {
                _logger.LogWarning("Skipping embedding {PhotoNumber} for user {UserId}: invalid dimension {Length} (expected {Expected})",
                    photoNumber, userId, embedding.Length, EmbeddingDimension);
                continue;
            }

            try
            {
                var normalized = NormalizeEmbedding(embedding);
                var embeddingBlob = FloatArrayToBlob(normalized);

                await using var cmd = _connection!.CreateCommand();
                cmd.CommandText = """
                    INSERT OR REPLACE INTO UserEmbeddings (UserId, PhotoNumber, Embedding)
                    VALUES (@userId, @photoNumber, @embedding);
                    """;
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@photoNumber", photoNumber);
                cmd.Parameters.AddWithValue("@embedding", embeddingBlob);

                await cmd.ExecuteNonQueryAsync();
                savedCount++;
                _logger.LogDebug("Saved embedding {PhotoNumber} for user {UserId}", photoNumber, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save embedding {PhotoNumber} for user {UserId}", photoNumber, userId);
                throw;
            }
        }

        _logger.LogInformation("Saved {Count} embeddings for user {UserId}", savedCount, userId);
        return savedCount;
    }

    public async Task DeleteUserEmbeddingsAsync(string userId)
    {
        _logger.LogDebug("Deleting all embeddings for user {UserId}...", userId);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "DELETE FROM UserEmbeddings WHERE UserId = @userId;";
            cmd.Parameters.AddWithValue("@userId", userId);

            var deleted = await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Deleted {Count} embeddings for user {UserId}", deleted, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete embeddings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetUserEmbeddingCountAsync(string userId)
    {
        _logger.LogDebug("Getting embedding count for user {UserId}...", userId);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM UserEmbeddings WHERE UserId = @userId;";
            cmd.Parameters.AddWithValue("@userId", userId);

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            _logger.LogDebug("User {UserId} has {Count} embeddings", userId, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get embedding count for user {UserId}", userId);
            throw;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static UserRegistration MapUserFromReader(SqliteDataReader reader)
    {
        return new UserRegistration
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            UserId = reader.GetString(reader.GetOrdinal("UserId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            DateOfBirth = DateTime.Parse(reader.GetString(reader.GetOrdinal("DateOfBirth"))),
            Sex = reader.GetString(reader.GetOrdinal("Sex")),
            RegisteredDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("RegisteredDate"))),
            Photo1 = reader.IsDBNull(reader.GetOrdinal("Photo1")) ? null : (byte[])reader.GetValue(reader.GetOrdinal("Photo1")),
            Photo2 = reader.IsDBNull(reader.GetOrdinal("Photo2")) ? null : (byte[])reader.GetValue(reader.GetOrdinal("Photo2")),
            Photo3 = reader.IsDBNull(reader.GetOrdinal("Photo3")) ? null : (byte[])reader.GetValue(reader.GetOrdinal("Photo3"))
        };
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same length");

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = MathF.Sqrt(magnitudeA);
        magnitudeB = MathF.Sqrt(magnitudeB);

        if (magnitudeA < 1e-10f || magnitudeB < 1e-10f)
            return 0f;

        return dotProduct / (magnitudeA * magnitudeB);
    }

    private static float[] NormalizeEmbedding(float[] embedding)
    {
        var magnitude = MathF.Sqrt(embedding.Sum(x => x * x));
        if (magnitude < 1e-10f) return embedding;

        var normalized = new float[embedding.Length];
        for (var i = 0; i < embedding.Length; i++)
            normalized[i] = embedding[i] / magnitude;

        return normalized;
    }

    private static byte[] FloatArrayToBlob(float[] array)
    {
        var bytes = new byte[array.Length * sizeof(float)];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BlobToFloatArray(byte[] blob)
    {
        var array = new float[blob.Length / sizeof(float)];
        Buffer.BlockCopy(blob, 0, array, 0, blob.Length);
        return array;
    }

    // ── Admin User operations ─────────────────────────────────────────────────

    public async Task<AdminUser?> GetAdminUserByUsernameAsync(string username)
    {
        _logger.LogDebug("Getting admin user by username: {Username}", username);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                SELECT Id, Username, Email, PasswordHash, Role, IsActive, CreatedDate, LastLoginDate
                FROM AdminUsers
                WHERE Username = @username AND IsActive = 1;
                """;
            cmd.Parameters.AddWithValue("@username", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new AdminUser
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    Role = reader.GetString(4),
                    IsActive = reader.GetInt32(5) == 1,
                    CreatedDate = DateTime.Parse(reader.GetString(6)),
                    LastLoginDate = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get admin user by username: {Username}", username);
            throw;
        }
    }

    public async Task<AdminUser?> GetAdminUserByEmailAsync(string email)
    {
        _logger.LogDebug("Getting admin user by email: {Email}", email);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                SELECT Id, Username, Email, PasswordHash, Role, IsActive, CreatedDate, LastLoginDate
                FROM AdminUsers
                WHERE Email = @email AND IsActive = 1;
                """;
            cmd.Parameters.AddWithValue("@email", email);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new AdminUser
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    Role = reader.GetString(4),
                    IsActive = reader.GetInt32(5) == 1,
                    CreatedDate = DateTime.Parse(reader.GetString(6)),
                    LastLoginDate = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get admin user by email: {Email}", email);
            throw;
        }
    }

    public async Task<int> SaveAdminUserAsync(AdminUser adminUser)
    {
        _logger.LogDebug("Saving admin user {Username}", adminUser.Username);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                INSERT INTO AdminUsers (Username, Email, PasswordHash, Role, IsActive, CreatedDate)
                VALUES (@username, @email, @passwordHash, @role, @isActive, @createdDate);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("@username", adminUser.Username);
            cmd.Parameters.AddWithValue("@email", adminUser.Email);
            cmd.Parameters.AddWithValue("@passwordHash", adminUser.PasswordHash);
            cmd.Parameters.AddWithValue("@role", adminUser.Role);
            cmd.Parameters.AddWithValue("@isActive", adminUser.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@createdDate", adminUser.CreatedDate.ToString("O"));

            var result = await cmd.ExecuteScalarAsync();
            adminUser.Id = Convert.ToInt32(result);

            _logger.LogInformation("Admin user '{Username}' saved successfully (Id={Id})", adminUser.Username, adminUser.Id);
            return adminUser.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save admin user {Username}", adminUser.Username);
            throw;
        }
    }

    public async Task<int> UpdateAdminUserLastLoginAsync(int adminUserId)
    {
        _logger.LogDebug("Updating last login for admin user {AdminUserId}", adminUserId);
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                UPDATE AdminUsers
                SET LastLoginDate = @lastLoginDate
                WHERE Id = @id;
                """;
            cmd.Parameters.AddWithValue("@lastLoginDate", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("@id", adminUserId);

            var result = await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Updated last login for admin user {AdminUserId}", adminUserId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update last login for admin user {AdminUserId}", adminUserId);
            throw;
        }
    }

    public async Task<List<AdminUser>> GetAllAdminUsersAsync()
    {
        _logger.LogDebug("Getting all admin users");
        await EnsureInitializedAsync();

        try
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                SELECT Id, Username, Email, PasswordHash, Role, IsActive, CreatedDate, LastLoginDate
                FROM AdminUsers
                ORDER BY Username;
                """;

            var adminUsers = new List<AdminUser>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                adminUsers.Add(new AdminUser
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    Role = reader.GetString(4),
                    IsActive = reader.GetInt32(5) == 1,
                    CreatedDate = DateTime.Parse(reader.GetString(6)),
                    LastLoginDate = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))
                });
            }

            _logger.LogInformation("Retrieved {Count} admin users", adminUsers.Count);
            return adminUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all admin users");
            throw;
        }
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing UserDatabaseService...");
        _connection?.Dispose();
        _initLock.Dispose();
        _logger.LogInformation("UserDatabaseService disposed");
    }
}
