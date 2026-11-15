using FluentValidation;
using FitCore.Application.Identity;
using FitCore.Application.Setup.Dtos;

namespace FitCore.Application.Setup.Validators;

public sealed class RolesAndOwnerValidator : AbstractValidator<OwnerCreationDto>
{
    public RolesAndOwnerValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).MaximumLength(50)
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("Username may contain letters, digits, '.', '_', '-' only.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .Must(p => PasswordPolicy.IsValid(p))
            .WithMessage("Password does not meet strength requirements.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}
