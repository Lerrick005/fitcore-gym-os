namespace FitCore.Application.Setup.Dtos;

/// <summary>Wizard step 8 form (PRS §10 step 8). Not a settings group — creates a User row.</summary>
public sealed class OwnerCreationDto
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
