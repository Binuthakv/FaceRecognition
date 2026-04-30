namespace FaceRecognitionApp.Api.Models;

public record PhotoEmbeddingError(int PhotoId, string Message);

public record PhotoEmbeddingValidationResponse(
    string UserId,
    string Name,
    List<PhotoEmbeddingError> Errors,
    string OverallMessage);

