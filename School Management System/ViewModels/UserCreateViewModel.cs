using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class UserCreateViewModel
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required, DataType(DataType.Password)]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool ForcePasswordChange { get; set; } = true;
}
