using FluentValidation;
using Reconova.Application.DTOs.Tenancy;

namespace Reconova.Application.Validators.Tenancy;

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MinimumLength(2).WithMessage("Tenant name must be at least 2 characters.")
            .MaximumLength(200);

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200)
            .When(x => x.CompanyName != null);
    }
}
