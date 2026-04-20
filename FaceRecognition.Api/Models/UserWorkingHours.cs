namespace FaceRecognitionApp.Api.Models;

/// <summary>
/// Represents user working hours tracked from attendance records.
/// </summary>
public class UserWorkingHours
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime LoginDate { get; set; }

    public decimal WorkingHours { get; set; }
}
