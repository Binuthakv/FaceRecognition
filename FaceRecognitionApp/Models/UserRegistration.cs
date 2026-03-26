namespace FaceRecognitionApp.Models;

public class UserRegistration
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public DateTime RegisteredDate { get; set; } = DateTime.Now;

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

    public bool HasAllPhotos => Photo1 != null && Photo2 != null && Photo3 != null;

    public int PhotoCount =>
        (Photo1 != null ? 1 : 0) +
        (Photo2 != null ? 1 : 0) +
        (Photo3 != null ? 1 : 0);
}
