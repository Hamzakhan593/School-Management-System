using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class BiometricEnrollmentFormViewModel
{
    [Required]
    [Display(Name = "Device")]
    public int BiometricDeviceId { get; set; }

    [Required]
    [Display(Name = "Student")]
    public int StudentId { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Device User ID / Reference")]
    public string DeviceUserReference { get; set; } = string.Empty;

    public BiometricModality Modality { get; set; } = BiometricModality.Fingerprint;

    [StringLength(200)]
    [Display(Name = "Template Reference (optional)")]
    public string? TemplateReference { get; set; }

    public List<SelectListItem> Devices { get; set; } = [];
    public List<SelectListItem> Students { get; set; } = [];
}
