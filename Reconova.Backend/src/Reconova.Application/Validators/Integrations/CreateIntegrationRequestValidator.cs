using FluentValidation;
using Reconova.Application.DTOs.Integrations;
using Reconova.Domain.Common.Enums;

namespace Reconova.Application.Validators.Integrations;

public class CreateIntegrationRequestValidator : AbstractValidator<CreateIntegrationRequest>
{
    public CreateIntegrationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Integration name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid integration type.");

        RuleFor(x => x.WebhookUrl)
            .NotEmpty().WithMessage("Webhook URL is required for webhook integrations.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Webhook URL must be a valid HTTP or HTTPS URL.")
            .When(x => x.Type == IntegrationType.Webhook);
    }
}
