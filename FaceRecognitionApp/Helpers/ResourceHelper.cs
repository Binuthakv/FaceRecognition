using FaceRecognitionApp.Constants;

namespace FaceRecognitionApp.Helpers;

public static class ResourceHelper
{
    private static readonly HttpClient _httpClient = new();

    public static async Task ExtractHaarCascadeAsync()
    {
        var cascadePath = Path.Combine(FileSystem.AppDataDirectory, AppConstants.HaarCascadeFileName);

        if (File.Exists(cascadePath))
        {
            AppLogger.Info("Haar cascade file already exists");
            return;
        }

        try
        {
            AppLogger.Info("Downloading Haar cascade file...");
            var cascadeData = await _httpClient.GetByteArrayAsync(AppConstants.HaarCascadeUrl);
            await File.WriteAllBytesAsync(cascadePath, cascadeData);
            AppLogger.Success("Haar cascade file downloaded successfully");
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to download Haar cascade file", ex);
            throw;
        }
    }
}
