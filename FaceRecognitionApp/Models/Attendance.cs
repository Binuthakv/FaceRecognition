namespace FaceRecognitionApp.Models;

public class Attendance
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime ScanTime { get; set; }

    public bool Processed { get; set; }
}
