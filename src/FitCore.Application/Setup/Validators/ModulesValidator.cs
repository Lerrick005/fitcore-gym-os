using FluentValidation;
using FitCore.Application.Abstractions;

namespace FitCore.Application.Setup.Validators;

/// <summary>Validates a set of module keys against the registry (PRS §9).</summary>
public sealed class ModulesValidator : AbstractValidator<IReadOnlyList<string>>
{
    public ModulesValidator(IModuleRegistry registry)
    {
        RuleFor(keys => keys)
            .NotNull()
            .Must(keys => keys.All(k => registry.Find(k) is not null))
            .WithMessage("One or more module keys are not recognised.");
    }
}
