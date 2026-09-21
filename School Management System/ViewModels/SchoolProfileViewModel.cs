using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace School_Management_System.ViewModels;

public class SchoolProfileViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "School Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Registration Number")]
    public string? RegistrationNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(160)]
    public string? Email { get; set; }

    [StringLength(150)]
    [Display(Name = "Principal Name")]
    public string? PrincipalName { get; set; }

    public string? ExistingLogoPath { get; set; }

    [Display(Name = "School Logo")]
    public IFormFile? Logo { get; set; }

    [StringLength(500)]
    [Display(Name = "Fee Challan Footer")]
    public string? ChallanFooterText { get; set; }

    [StringLength(500)]
    [Display(Name = "Fee Receipt Footer")]
    public string? ReceiptFooterText { get; set; }
}
