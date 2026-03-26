namespace FaceRecognitionApp.Api.Models;

/// <summary>
/// Represents an attendance record for a user scan/verification event.
/// </summary>
public class Attendance
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime ScanTime { get; set; }

    public bool Processed { get; set; }
}
