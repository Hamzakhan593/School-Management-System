using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class UserEditViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
