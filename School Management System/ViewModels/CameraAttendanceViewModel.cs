using Microsoft.AspNetCore.Mvc.Rendering;

namespace School_Management_System.ViewModels;

public class CameraAttendanceViewModel
{
    public int? ManualStudentId { get; set; }
    public string? CapturedImageDataUrl { get; set; }
    public int? ClientDetectedFaceCount { get; set; }
    public string? ResultMessage { get; set; }
    public bool ResultSuccess { get; set; }
    public List<SelectListItem> Students { get; set; } = [];
}
