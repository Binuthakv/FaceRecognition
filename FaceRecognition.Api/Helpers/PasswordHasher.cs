namespace FaceRecognitionApp.Api.Helpers;

/// <summary>
/// Utility class for password hashing and verification using BCrypt.
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// Hashes a password using BCrypt.
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        // BCrypt with cost factor of 12 (default)
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verifies a password against a BCrypt hash.
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
