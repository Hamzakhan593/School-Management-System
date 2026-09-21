using System.ComponentModel.DataAnnotations;

namespace School_Management_System.ViewModels;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
