using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using School_Management_System.Models;

namespace School_Management_System.ViewModels;

public class StudentDocumentUploadViewModel
{
    public int StudentId { get; set; }

    [Display(Name = "Document Type")]
    public AdmissionDocumentType DocumentType { get; set; } = AdmissionDocumentType.Other;

    [Required]
    public IFormFile File { get; set; } = null!;
}
