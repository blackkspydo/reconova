using FluentValidation;
using Reconova.Application.DTOs.Scanning;

namespace Reconova.Application.Validators.Scanning;

public class CreateDomainRequestValidator : AbstractValidator<CreateDomainRequest>
{
    private static readonly string[] ValidVerificationMethods = { "dns", "http", "meta" };

    public CreateDomainRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Domain name is required.")
            .MaximumLength(253).WithMessage("Domain name must be 253 characters or fewer.")
            .Matches(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?)*$")
            .WithMessage("Domain name must contain only letters, digits, hyphens, and dots.");

        RuleFor(x => x.VerificationMethod)
            .NotEmpty().WithMessage("Verification method is required.")
            .Must(v => ValidVerificationMethods.Contains(v.ToLowerInvariant()))
            .WithMessage("Verification method must be one of: dns, http, meta.");
    }
}
