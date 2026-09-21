using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class AdmissionApplicationsIndexViewModel
{
    public IReadOnlyList<AdmissionApplication> Applications { get; set; } = [];
    public string? Query { get; set; }
    public AdmissionApplicationStatus? Status { get; set; }
}
