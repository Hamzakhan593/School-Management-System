using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class AdmissionsIndexViewModel
{
    public IReadOnlyList<AdmissionEnquiry> Enquiries { get; set; } = [];
    public IReadOnlyList<AdmissionClassStatViewModel> ClassStats { get; set; } = [];
    public string? Query { get; set; }
    public AdmissionEnquiryStage? Stage { get; set; }
    public string? DesiredClass { get; set; }
    public int TotalEnquiries { get; set; }
    public int Approved { get; set; }
    public int Admitted { get; set; }
    public int FollowUpsDue { get; set; }
}

public class AdmissionClassStatViewModel
{
    public string DesiredClass { get; set; } = string.Empty;
    public int EnquiryCount { get; set; }
    public int AdmittedCount { get; set; }
}
