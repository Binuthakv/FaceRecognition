namespace FaceRecognitionApp.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    public const string AppName = "FaceRecognitionApp";
    
    // Camera settings
    public const int FrameCaptureIntervalMs = 500;
    public const double JpegQuality = 0.85;
    
    // Face recognition settings
    public const double DefaultConfidenceThreshold = 70.0;
    public const double HighConfidenceThreshold = 85.0;
    
    // File names
    public const string HaarCascadeFileName = "haarcascade_frontalface_default.xml";
    
    // URLs
    public const string HaarCascadeUrl = "https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_frontalface_default.xml";

    // API
    /// <summary>Base address of the FaceRecognition REST API. Set via API_BASE_URL environment variable or falls back to the dev tunnel URL.</summary>
    //public static string ApiBaseUrl =>
    //    Environment.GetEnvironmentVariable("API_BASE_URL")
    //    ?? "https://localhost:7053/";
    public static string ApiBaseUrl =
        "https://bc3m95tg-7053.inc1.devtunnels.ms";
    //public static string ApiBaseUrl =
    //    "https://localhost:7053/";
    public const string ApiHttpClientName = "FaceRecognitionApi";
    
    // Database
    public const string DatabaseFileName = "facerecognition.db3";
    
    // Messages
    public static class Messages
    {
        public const string CameraStartPrompt = "Click 'Start Camera' to begin face verification";
        public const string CameraActive = "Camera active";
        public const string CameraInactive = "Camera inactive";
        public const string NoUsersRegistered = "No users registered in database. Please register users first.";
        public const string LoadingUsers = "Loading registered users from database...";
        public const string ScanningFaces = "Scanning for registered faces...";
    }
}
