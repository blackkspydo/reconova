using FluentValidation;
using Reconova.Application.DTOs.Admin;

namespace Reconova.Application.Validators.Admin;

public class UpdateConfigRequestValidator : AbstractValidator<UpdateConfigRequest>
{
    public UpdateConfigRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
    }
}
