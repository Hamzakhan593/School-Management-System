using Microsoft.AspNetCore.Identity;
using School_Management_System.Models;

namespace School_Management_System.Services;

public class SchoolPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private readonly ISystemSettingsService _settings;

    public SchoolPasswordValidator(ISystemSettingsService settings)
    {
        _settings = settings;
    }

    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
            return IdentityResult.Failed(new IdentityError { Code = "PasswordRequired", Description = "Password is required." });

        var policy = user.SchoolId.HasValue
            ? await _settings.GetAsync(user.SchoolId.Value)
            : new SystemSetting();

        var errors = new List<IdentityError>();
        if (password.Length < policy.PasswordRequiredLength)
            errors.Add(Error("PasswordTooShort", $"Password must be at least {policy.PasswordRequiredLength} characters."));
        if (policy.PasswordRequireDigit && !password.Any(char.IsDigit))
            errors.Add(Error("PasswordRequiresDigit", "Password must contain at least one digit."));
        if (policy.PasswordRequireUppercase && !password.Any(char.IsUpper))
            errors.Add(Error("PasswordRequiresUpper", "Password must contain at least one uppercase letter."));
        if (policy.PasswordRequireLowercase && !password.Any(char.IsLower))
            errors.Add(Error("PasswordRequiresLower", "Password must contain at least one lowercase letter."));
        if (policy.PasswordRequireSpecialCharacter && password.All(char.IsLetterOrDigit))
            errors.Add(Error("PasswordRequiresSpecial", "Password must contain at least one special character."));

        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
    }

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
