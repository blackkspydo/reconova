using FluentValidation;
using Reconova.Application.DTOs.Billing;

namespace Reconova.Application.Validators.Billing;

public class AdminCreditAdjustmentRequestValidator : AbstractValidator<AdminCreditAdjustmentRequest>
{
    public AdminCreditAdjustmentRequestValidator()
    {
        RuleFor(x => x.Amount)
            .NotEqual(0).WithMessage("Adjustment amount must not be zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
    }
}
