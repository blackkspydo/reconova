using FluentValidation;
using Reconova.Application.DTOs.Scanning;

namespace Reconova.Application.Validators.Scanning;

public class CreateScanRequestValidator : AbstractValidator<CreateScanRequest>
{
    public CreateScanRequestValidator()
    {
        RuleFor(x => x.DomainId)
            .NotEmpty().WithMessage("Domain ID is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid scan type.");
    }
}
