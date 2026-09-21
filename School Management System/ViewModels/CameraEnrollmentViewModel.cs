using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace School_Management_System.ViewModels;

public class CameraEnrollmentViewModel
{
    [Required]
    [Display(Name = "Student")]
    public int StudentId { get; set; }

    public string? Sample1DataUrl { get; set; }
    public string? Sample2DataUrl { get; set; }
    public string? Sample3DataUrl { get; set; }

    public List<SelectListItem> Students { get; set; } = [];
}
