namespace School_Management_System.Services;

public record CameraRecognitionResult(
    bool IsConfigured,
    string ProviderName,
    int FaceCount,
    int? StudentId,
    decimal? Confidence,
    string? TemplateReference,
    string Message);

public record CameraEnrollmentResult(
    bool Success,
    bool IsConfigured,
    string ProviderName,
    string? TemplateReference,
    string Message);

public interface ICameraRecognitionProvider
{
    Task<CameraRecognitionResult> RecognizeAsync(
        int schoolId,
        byte[] imageBytes,
        CancellationToken cancellationToken = default);

    Task<CameraEnrollmentResult> EnrollAsync(
        int schoolId,
        int studentId,
        IReadOnlyList<byte[]> samples,
        CancellationToken cancellationToken = default);
}
