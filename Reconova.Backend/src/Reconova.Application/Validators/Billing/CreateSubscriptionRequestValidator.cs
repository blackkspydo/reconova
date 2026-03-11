using FluentValidation;
using Reconova.Application.DTOs.Billing;

namespace Reconova.Application.Validators.Billing;

public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("Plan ID is required.");
    }
}
