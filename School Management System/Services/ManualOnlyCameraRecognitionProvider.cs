namespace School_Management_System.Services;

// Safe default for M07: camera capture/manual verification works immediately, while automated
// face matching stays behind this provider interface until a school-approved recognition service is connected.
public class ManualOnlyCameraRecognitionProvider : ICameraRecognitionProvider
{
    public Task<CameraRecognitionResult> RecognizeAsync(
        int schoolId,
        byte[] imageBytes,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new CameraRecognitionResult(
            false,
            "ManualOnly",
            0,
            null,
            null,
            null,
            "Automated face recognition is not configured. Use the manual confirmation fallback."));

    public Task<CameraEnrollmentResult> EnrollAsync(
        int schoolId,
        int studentId,
        IReadOnlyList<byte[]> samples,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new CameraEnrollmentResult(
            false,
            false,
            "ManualOnly",
            null,
            "Automated face enrollment requires a school-approved recognition provider. No raw samples were stored."));
}
