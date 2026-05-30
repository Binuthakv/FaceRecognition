namespace FaceRecognitionApp.Api.Models;

/// <summary>
/// Represents a registered user with their photos and face embeddings.
/// Currently only Photo1 is required and used for face recognition.
/// Photo2 and Photo3 are reserved for future functionality.
/// </summary>
public class UserRegistration
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string Sex { get; set; } = string.Empty; // Male, Female, Other

    public DateTime RegisteredDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Primary face photo for recognition (required).
    /// </summary>
    public byte[]? Photo1 { get; set; }
    public byte[]? Photo2 { get; set; }
    public byte[]? Photo3 { get; set; }

    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    /// <summary>
    /// Checks if user has at least one photo (required for registration).
    /// </summary>
    public bool HasPhoto => Photo1 != null;
}

