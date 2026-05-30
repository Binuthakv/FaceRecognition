namespace FaceRecognitionApp.Models;

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
    /// Photo2 and Photo3 are reserved for future use or multi-reference scenarios.
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

    /// <summary>
    /// Count of available photos (currently only Photo1 is used).
    /// </summary>
    public int PhotoCount =>
        (Photo1 != null ? 1 : 0) +
        (Photo2 != null ? 1 : 0) +
        (Photo3 != null ? 1 : 0);
}
