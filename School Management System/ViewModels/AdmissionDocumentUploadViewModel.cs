using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class AdmissionDocumentUploadViewModel
{
    [Required]
    public int AdmissionApplicationId { get; set; }

    [Required, Display(Name = "Document type")]
    public AdmissionDocumentType DocumentType { get; set; }

    [Required, Display(Name = "File")]
    public IFormFile? File { get; set; }
}
