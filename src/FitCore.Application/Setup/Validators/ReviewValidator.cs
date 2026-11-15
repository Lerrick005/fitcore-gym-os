using FluentValidation;

namespace FitCore.Application.Setup.Validators;

/// <summary>Review step has no per-field validation (PRS §10 step 10). Kept for uniform wizard pattern.</summary>
public sealed class ReviewValidator : AbstractValidator<object>
{
    public ReviewValidator() { }
}
