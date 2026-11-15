namespace FitCore.Application.Identity;

/// <summary>Password strength rules (PRS §11). Shared between wizard (Step 5) and auth (Step 6).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;

    public static IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required.");
            return errors;
        }
        if (password.Length < MinLength) errors.Add($"Password must be at least {MinLength} characters.");
        if (!password.Any(char.IsUpper)) errors.Add("Password must contain an uppercase letter.");
        if (!password.Any(char.IsLower)) errors.Add("Password must contain a lowercase letter.");
        if (!password.Any(char.IsDigit)) errors.Add("Password must contain a digit.");
        if (!password.Any(c => !char.IsLetterOrDigit(c))) errors.Add("Password must contain a symbol.");
        return errors;
    }

    public static bool IsValid(string? password) => Validate(password).Count == 0;
}
